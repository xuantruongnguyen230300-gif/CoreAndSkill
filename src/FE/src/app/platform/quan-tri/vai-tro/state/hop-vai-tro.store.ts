import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ApiFailureError } from '../../../../core/http/api-result.model';
import { ToastService } from '../../../../core/toast/toast.service';
import { BangMaGocVaoO, applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { VaiTro } from '../models/vai-tro.model';
import { VaiTroService } from '../services/vai-tro.service';

/** Mã gốc hiện dưới ô — Design/Screens/11-vai-tro.md, bảng Mã lỗi → chỗ hiện ("FE tự gắn mã vào ô"). */
const MA_GOC_VAO_O: BangMaGocVaoO<HopVaiTroStore['form']> = { 'CORE.ROLE.NAME_DUPLICATE': 'name' };

/**
 * Hộp "Thêm vai trò" / "Đổi tên vai trò" của danh-sach-vai-tro.page (Design/Screens/11-vai-tro.md) —
 * tách khỏi page để giữ *.page.ts dưới ngưỡng dòng (fe-architecture.md §5, luật F22). Cấp ở
 * `providers` của page (ADR-0049) nên chết cùng màn. Store là chủ ghi DUY NHẤT của các tín hiệu dưới;
 * `xong` là việc page làm sau khi ghi thành công — nhận vai trò đã đổi (đổi tên) hoặc `null` (tạo).
 */
@Injectable()
export class HopVaiTroStore {
  private readonly service = inject(VaiTroService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly hienThi = signal(false);
  readonly dangSua = signal<VaiTro | null>(null);
  readonly dangLuu = signal(false);
  readonly daGui = signal(false);
  readonly loi = signal<string | null>(null);
  /** 409 `CORE.CONCURRENCY.CONFLICT` — khuôn của màn người dùng và hồ sơ (Screens/10, 03). */
  readonly xungDot = signal(false);
  /** Tăng mỗi lần Tải lại nạp xong bản mới — page lắng nghe để đưa focus về ô "Tên vai trò". */
  readonly lanNapLai = signal(0);
  /** Dialog chỉ phát `dismissAttempted` khi `dirty` đang bật — nó KHÔNG tự đóng; tự đóng là mất dữ liệu. */
  readonly hienThiXacNhanHuy = signal(false);

  readonly form = this.fb.group({
    name: this.fb.control('', [Validators.required]),
  });

  tieuDe(): string {
    return this.translate.instant(
      this.dangSua() ? 'vaiTro.form.tieuDeDoiTen' : 'vaiTro.form.tieuDeTao',
    );
  }

  loiTen(): string | null {
    return fieldErrorText(this.form.controls.name, this.daGui(), this.translate);
  }

  moHopTao(): void {
    this.mo(null, '');
  }

  moHopSua(vt: VaiTro): void {
    this.mo(vt, vt.name);
  }

  private mo(vt: VaiTro | null, ten: string): void {
    this.dangSua.set(vt);
    this.form.reset({ name: ten });
    this.daGui.set(false);
    this.loi.set(null);
    this.xungDot.set(false);
    this.hienThi.set(true);
  }

  dong(): void {
    if (this.dangLuu()) return;
    this.hienThi.set(false);
  }

  onDismissAttempted(): void {
    this.hienThiXacNhanHuy.set(true);
  }

  huyXacNhanHuy(): void {
    this.hienThiXacNhanHuy.set(false);
  }

  xacNhanHuy(): void {
    this.hienThiXacNhanHuy.set(false);
    this.dong();
  }

  gui(xong: (daDoi: VaiTro | null) => void): void {
    // Hộp chỉ có MỘT ô nhập → Enter gửi ngầm; chặn gửi hai lần (lần hai nhận NAME_DUPLICATE giả).
    if (this.dangLuu()) return;
    this.daGui.set(true);
    this.loi.set(null);
    this.xungDot.set(false);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const ten = this.form.getRawValue().name;
    const dangSua = this.dangSua();
    this.dangLuu.set(true);
    const thanhCong = (daDoi: VaiTro | null): void => {
      this.hienThi.set(false);
      this.toast.thanhCong(
        this.translate.instant(
          dangSua ? 'vaiTro.thongBao.doiTenThanhCong' : 'vaiTro.thongBao.taoThanhCong',
          { ten },
        ),
      );
      xong(daDoi);
    };
    // Hai lời gọi trả kiểu Observable khác nhau (VaiTro vs {id}) — tách nhánh thay vì gộp vào một
    // biến chung, tránh TS suy luận union Observable rồi hỏng phép giải quá tải của `subscribe`.
    if (dangSua) {
      this.service
        .doiTen(dangSua.id, ten, dangSua.version)
        .pipe(finalize(() => this.dangLuu.set(false)))
        .subscribe({ next: (moi) => thanhCong(moi), error: (err: unknown) => this.xuLyLoi(err) });
    } else {
      this.service
        .tao(ten)
        .pipe(finalize(() => this.dangLuu.set(false)))
        .subscribe({ next: () => thanhCong(null), error: (err: unknown) => this.xuLyLoi(err) });
    }
  }

  /**
   * Nút Tải lại của banner xung đột (Screens/11 §Trạng thái): GET bản mới kèm `version` mới
   * (roles.md §5), NẠP tên từ máy chủ vào ô — bỏ tên đang nhập — và đưa form về chưa sửa; page đưa
   * focus về ô theo `lanNapLai`. Người dùng xem bản mới rồi mới Lưu lại. GET hỏng thì ô giữ nguyên.
   * `taiLaiDanhSach` để bảng phía sau hiện bản mới của người khác.
   */
  taiLaiSauXungDot(taiLaiDanhSach: () => void): void {
    const vt = this.dangSua();
    if (!vt || this.dangLuu()) return;
    this.xungDot.set(false);
    this.dangLuu.set(true);
    taiLaiDanhSach();
    this.service
      .chiTiet(vt.id)
      // Lượt ĐỌC chết cùng màn (fe-api-client.md §6.1) — store cấp ở `providers` của page.
      .pipe(
        finalize(() => this.dangLuu.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (moi) => {
          this.dangSua.set(moi);
          this.form.reset({ name: moi.name });
          this.daGui.set(false);
          this.lanNapLai.update((n) => n + 1);
        },
        error: (err: unknown) => this.xuLyLoi(err),
      });
  }

  private xuLyLoi(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loi.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }
    // Hai mã cùng HTTP 409 — rẽ theo `code`, không theo status. Xung đột: KHÔNG gửi lại; ô giữ tên
    // đang nhập cho tới khi người dùng bấm Tải lại (`taiLaiSauXungDot`).
    if (err.body?.error.code === 'CORE.CONCURRENCY.CONFLICT') {
      this.xungDot.set(true);
      return;
    }
    // Mọi mã còn lại, KHÔNG lọc theo danh sách mã (fe-ui-conventions.md §6.2): NAME_DUPLICATE xuống
    // ô tên; `fieldErrors` xuống ô, mã chưa hiện ở ô nào lên banner; không mã nào → câu của mã gốc.
    this.loi.set(applyFormFailure(this.form, err, this.translate, MA_GOC_VAO_O));
  }
}
