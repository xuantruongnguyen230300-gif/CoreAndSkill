import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { finalize, merge } from 'rxjs';

import { ApiFailureError } from '../../../../core/http/api-result.model';
import { dichLoiChoMan } from '../../../../core/http/dich-loi';
import { ToastService } from '../../../../core/toast/toast.service';
import { BangMaGocVaoO, applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorsText } from '../../../../shared/forms/field-errors-text';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { LoaiHopChiTiet, LoiTruongSuaNguoiDung } from '../models/nguoi-dung-hop.model';
import { NguoiDung } from '../models/nguoi-dung.model';
import { NguoiDungService } from '../services/nguoi-dung.service';

/**
 * Mã gốc hiện dưới ô của hộp Sửa — Design/Screens/10-nguoi-dung.md, bảng Mã lỗi → chỗ hiện của màn
 * chi tiết ("FE tự gắn mã vào ô"). Câu mang `messageParams` (`{{Email}}`) của card.
 */
const MA_GOC_VAO_O_SUA: BangMaGocVaoO<HopChiTietNguoiDungStore['formSua']> = {
  'CORE.USER.EMAIL_DUPLICATED': 'email',
};

/**
 * Trạng thái CHUNG và MỌI luồng ghi của màn chi tiết người dùng (chi-tiet-nguoi-dung.page): Sửa,
 * Đặt lại mật khẩu, Gán vai trò, Khoá, Mở khoá — tách khỏi page để giữ *.page.ts dưới ngưỡng dòng
 * (fe-architecture.md §5, luật F22). Store cấp ở `providers` của page (ADR-0049) nên chết cùng màn.
 * Ba tín hiệu `dangGui`/`loiHop`/`xungDot` chỉ MỘT chủ ghi là store này — page không `.set()` vào
 * chúng. Component hộp KHÔNG biết store này: page truyền GIÁ TRỊ xuống bằng `input()` và nghe
 * `output()` để gọi hàm ở đây; `xong` là việc page làm sau khi ghi thành công (tải lại chi tiết).
 */
@Injectable()
export class HopChiTietNguoiDungStore {
  private readonly service = inject(NguoiDungService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly hopDangMo = signal<LoaiHopChiTiet>(null);
  readonly dangGui = signal(false);
  readonly daGui = signal(false);
  readonly loiHop = signal<string | null>(null);
  readonly xungDot = signal(false);
  /** Hỏi xác nhận khi bị đóng lúc còn thay đổi chưa lưu — dùng chung cho cả ba hộp form. */
  readonly hienThiXacNhanHuy = signal(false);
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp lắng nghe để đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = signal(0);
  /** Số vai trò sắp bị gỡ đang chờ xác nhận; `null` = không hỏi. */
  readonly hopXacNhanGo = signal<number | null>(null);
  readonly hienThiXacNhanKhoa = signal(false);
  /** Yêu cầu gán đang chờ người dùng xác nhận gỡ — chốt lúc bấm, gửi đúng bản đó khi xác nhận. */
  private yeuCauCho: { nd: NguoiDung; tapDich: readonly string[]; xong: () => void } | null = null;

  readonly formSua = this.fb.group({
    email: this.fb.control('', [Validators.required, Validators.email]),
    fullName: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
  });
  readonly formDatLai = this.fb.group({
    tempPassword: this.fb.control('', [Validators.required]),
  });

  /** Nhịp đổi trạng thái của hai form: control không phải signal, cần nó để `computed` tính lại. */
  private readonly nhipForm = signal(0);

  readonly loiSua = computed<LoiTruongSuaNguoiDung>(() => {
    this.nhipForm();
    const daGui = this.daGui();
    const c = this.formSua.controls;
    return {
      email: fieldErrorText(c.email, daGui, this.translate),
      fullName: fieldErrorText(c.fullName, daGui, this.translate),
    };
  });

  readonly loiMatKhau = computed<string | null>(() => {
    this.nhipForm();
    return fieldErrorText(this.formDatLai.controls.tempPassword, this.daGui(), this.translate);
  });

  constructor() {
    merge(this.formSua.events, this.formDatLai.events)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.nhipForm.update((n) => n + 1));
  }

  // ---- Mở / đóng ----
  moHopSua(nd: NguoiDung): void {
    this.xoaDauVetHop();
    this.formSua.reset({ email: nd.email ?? '', fullName: nd.fullName });
    this.hopDangMo.set('sua');
  }

  moHopDatLai(): void {
    this.xoaDauVetHop();
    this.formDatLai.reset({ tempPassword: '' });
    this.hopDangMo.set('dat-lai');
  }

  moHopGanVaiTro(): void {
    this.xoaDauVetHop();
    this.hopDangMo.set('gan-vai-tro');
  }

  /**
   * Ba tín hiệu dùng chung (`daGui`, `loiHop`, `xungDot`) không thuộc hộp nào — lần khoá lỗi hay hộp
   * trước để lại gì thì hộp mở sau mang theo cái đó. Mọi đường MỞ một hộp phải gọi hàm này trước.
   */
  private xoaDauVetHop(): void {
    this.daGui.set(false);
    this.loiHop.set(null);
    this.xungDot.set(false);
  }

  /** Đóng KHÔNG điều kiện và xoá mọi dấu vết của lần mở trước. */
  dongTatCa(): void {
    this.hopDangMo.set(null);
    this.xoaDauVetHop();
  }

  /** Người dùng đóng hộp — bị khoá khi đang gửi. */
  dong(): void {
    if (this.dangGui()) return;
    this.dongTatCa();
  }

  // Dialog chỉ phát `dismissAttempted` khi `dirty` đang bật — nó KHÔNG tự đóng; tự đóng thì mất dữ liệu.
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

  // ---- Sửa ----
  /** `xong` chạy sau khi lưu thành công — page lo tải lại chi tiết. */
  guiSua(nd: NguoiDung, xong: () => void): void {
    // Chặn gửi hai lần (Enter ngầm ở form một ô, bấm đúp): hai POST cùng version → 409 giả.
    if (this.dangGui()) return;
    this.daGui.set(true);
    this.loiHop.set(null);
    if (this.formSua.invalid) {
      this.formSua.markAllAsTouched();
      this.lanGuiSai.update((n) => n + 1);
      return;
    }
    const gt = this.formSua.getRawValue();
    this.dangGui.set(true);
    this.service
      .suaThongTin(nd.id, { ...gt, version: nd.version })
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => {
          this.dongTatCa();
          this.toast.thanhCong(this.translate.instant('nguoiDung.thongBao.suaThanhCong'));
          xong();
        },
        error: (err: unknown) => this.xuLyLoi(err, 'sua'),
      });
  }

  // ---- Đặt lại mật khẩu ----
  guiDatLai(nd: NguoiDung, xong: () => void): void {
    // Chặn gửi hai lần (Enter ngầm ở form một ô, bấm đúp): hai POST cùng version → 409 giả.
    if (this.dangGui()) return;
    this.daGui.set(true);
    this.loiHop.set(null);
    if (this.formDatLai.invalid) {
      this.formDatLai.markAllAsTouched();
      this.lanGuiSai.update((n) => n + 1);
      return;
    }
    this.dangGui.set(true);
    this.service
      .datLaiMatKhau(nd.id, {
        tempPassword: this.formDatLai.getRawValue().tempPassword,
        version: nd.version,
      })
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => {
          this.dongTatCa();
          this.toast.thanhCong(
            this.translate.instant('nguoiDung.thongBao.datLaiThanhCong', {
              tenDangNhap: nd.userName,
            }),
          );
          xong();
        },
        error: (err: unknown) => this.xuLyLoi(err, 'dat-lai'),
      });
  }

  // ---- Gán vai trò ----
  /**
   * `tapDich` là tập vai trò đích (gồm cả vai trò hệ thống đứng ngoài ô chọn — users.md §7). Gỡ bớt
   * vai trò nào thì hỏi xác nhận trước; không gỡ thì gửi ngay.
   */
  yeuCauGanVaiTro(nd: NguoiDung, tapDich: readonly string[], xong: () => void): void {
    // Chặn gửi hai lần (Enter ngầm ở form một ô, bấm đúp): hai POST cùng version → 409 giả.
    if (this.dangGui()) return;
    // Bấm Lưu là bắt đầu một lần gán MỚI: câu lỗi/xung đột của lần trước không được sống tiếp (giống
    // guiSua/guiDatLai xoá `loiHop` ở đầu) — nhất là khi hộp hỏi gỡ mở ra trước khi gửi gì.
    this.loiHop.set(null);
    this.xungDot.set(false);
    const dich = new Set(tapDich);
    const soGo = nd.roles.filter((r) => !dich.has(r.id)).length;
    if (soGo > 0) {
      this.yeuCauCho = { nd, tapDich, xong };
      this.hopXacNhanGo.set(soGo);
      return;
    }
    this.guiGanVaiTro(nd, tapDich, xong);
  }

  huyXacNhanGo(): void {
    this.yeuCauCho = null;
    this.hopXacNhanGo.set(null);
  }

  xacNhanGoVaiTro(): void {
    const cho = this.yeuCauCho;
    this.huyXacNhanGo();
    if (cho) this.guiGanVaiTro(cho.nd, cho.tapDich, cho.xong);
  }

  private guiGanVaiTro(nd: NguoiDung, tapDich: readonly string[], xong: () => void): void {
    // Chặn gửi hai lần (Enter ngầm ở form một ô, bấm đúp): hai POST cùng version → 409 giả.
    if (this.dangGui()) return;
    this.dangGui.set(true);
    this.loiHop.set(null);
    this.service
      .ganVaiTro(nd.id, { roleIds: [...tapDich], version: nd.version })
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => {
          this.dongTatCa();
          this.toast.thanhCong(
            this.translate.instant('nguoiDung.thongBao.ganVaiTroThanhCong', {
              tenDangNhap: nd.userName,
            }),
          );
          xong();
        },
        error: (err: unknown) => this.xuLyLoi(err, 'gan-vai-tro'),
      });
  }

  // ---- Khoá / mở khoá ----
  moXacNhanKhoa(): void {
    this.xoaDauVetHop();
    this.hienThiXacNhanKhoa.set(true);
  }

  huyXacNhanKhoa(): void {
    if (this.dangGui()) return;
    this.hienThiXacNhanKhoa.set(false);
    this.xoaDauVetHop();
  }

  xacNhanKhoa(nd: NguoiDung, xong: () => void): void {
    // Chặn gửi hai lần (Enter ngầm ở form một ô, bấm đúp): hai POST cùng version → 409 giả.
    if (this.dangGui()) return;
    this.dangGui.set(true);
    this.loiHop.set(null);
    this.service
      .khoa(nd.id, { version: nd.version })
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => {
          this.hienThiXacNhanKhoa.set(false);
          this.toast.thanhCong(
            this.translate.instant('nguoiDung.thongBao.khoaThanhCong', {
              tenDangNhap: nd.userName,
            }),
          );
          xong();
        },
        error: (err: unknown) => {
          // Hộp xác nhận khoá không có chỗ hiện `xungDot`, và gửi lại cùng `version` cũ sẽ xung đột
          // mãi: cảnh báo, đóng hộp, tải lại để lấy version mới — giống mở khoá.
          if (this.laXungDot(err)) {
            this.hienThiXacNhanKhoa.set(false);
            this.canhBaoXungDot(xong);
            return;
          }
          this.xuLyLoi(err, null);
        },
      });
  }

  moKhoa(nd: NguoiDung, xong: () => void): void {
    // Bấm đúp phát hai POST cùng `version`: bản đầu làm `version` đổi, bản hai nhận 409 GIẢ.
    if (this.dangGui()) return;
    this.dangGui.set(true);
    this.service
      .moKhoa(nd.id, { version: nd.version })
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => {
          this.toast.thanhCong(
            this.translate.instant('nguoiDung.thongBao.moKhoaThanhCong', {
              tenDangNhap: nd.userName,
            }),
          );
          xong();
        },
        error: (err: unknown) => {
          if (this.laXungDot(err)) {
            this.canhBaoXungDot(xong);
            return;
          }
          // Request đã tắt toast chung — toast này là thông báo duy nhất, phải mang traceId (fe-api-client.md §1.1).
          // Lớp lỗi xuyên suốt thì interceptor đã toast rồi: câu `null`, không toast lần hai.
          const cau = this.cauLoi(err);
          if (cau !== null) {
            this.toast.loi(
              cau,
              err instanceof ApiFailureError ? (err.body?.traceId ?? null) : null,
            );
          }
        },
      });
  }

  private laXungDot(err: unknown): boolean {
    return err instanceof ApiFailureError && err.body?.error.code === 'CORE.CONCURRENCY.CONFLICT';
  }

  private canhBaoXungDot(xong: () => void): void {
    this.toast.canhBao(this.translate.instant('loi.CORE.CONCURRENCY.CONFLICT'));
    xong();
  }

  private cauLoi(err: unknown): string | null {
    return err instanceof ApiFailureError
      ? dichLoiChoMan(this.translate, err)
      : this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
  }

  // ---- Lỗi dùng chung cho các hộp thoại ghi ----
  xuLyLoi(err: unknown, hop: LoaiHopChiTiet): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiHop.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }
    if (err.body?.error.code === 'CORE.CONCURRENCY.CONFLICT') {
      this.xungDot.set(true);
      return;
    }
    // Mọi mã còn lại, KHÔNG lọc theo danh sách mã (fe-ui-conventions.md §6.2).
    this.loiHop.set(this.cauKhuLoiChung(err, hop));
  }

  /**
   * fe-ui-conventions.md §6.2 — hộp có form: `fieldErrors` xuống ô, mã chưa hiện ở ô nào lên banner;
   * hộp Sửa thêm mã trùng email xuống ô Email (`MA_GOC_VAO_O_SUA`). Hộp không có form (Gán vai trò —
   * 10-nguoi-dung.md "không gắn vào ô"; xác nhận khoá, `hop` = null): câu của MỌI mã trong
   * `fieldErrors`. Cả hai: mỗi mã một dòng; không mã nào → câu của mã gốc.
   */
  private cauKhuLoiChung(err: ApiFailureError, hop: LoaiHopChiTiet): string | null {
    switch (hop) {
      case 'sua':
        return applyFormFailure(this.formSua, err, this.translate, MA_GOC_VAO_O_SUA);
      case 'dat-lai':
        return applyFormFailure(this.formDatLai, err, this.translate);
      default:
        return fieldErrorsText(this.translate, err);
    }
  }
}
