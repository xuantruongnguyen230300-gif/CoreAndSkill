import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ApiFailureError } from '../../../../core/http/api-result.model';
import { BangMaGocVaoO, applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { TraCuuVaiTroStore } from '../../vai-tro/state/tra-cuu-vai-tro.store';
import { VaiTroService } from '../../vai-tro/services/vai-tro.service';
import {
  LoiTruongTaoNguoiDung,
  TaoNguoiDungForm,
  TenTruongTaoNguoiDung,
  VaiTroChon,
} from '../models/nguoi-dung-hop.model';
import { NguoiDungService } from '../services/nguoi-dung.service';

/**
 * Mã gốc hiện dưới ô — Design/Screens/10-nguoi-dung.md, bảng Mã lỗi → chỗ hiện của hộp tạo: card
 * khai `messageParams`, không khai `fieldErrors`; FE tự gắn mã vào ô.
 */
const MA_GOC_VAO_O: BangMaGocVaoO<TaoNguoiDungForm> = {
  'CORE.USER.USERNAME_DUPLICATED': 'userName',
  'CORE.USER.EMAIL_DUPLICATED': 'email',
};

/**
 * Trạng thái + quy trình hộp "Thêm người dùng" (danh-sach-nguoi-dung.page) — tách khỏi page để giữ
 * *.page.ts dưới ngưỡng dòng (fe-architecture.md §5, luật F22). Store cấp ở `providers` của page
 * (ADR-0049) nên chết cùng màn; `moHop()` reset lại trạng thái mỗi lần mở. `HopTaoNguoiDungComponent`
 * KHÔNG biết store này: page đọc nó rồi truyền GIÁ TRỊ xuống bằng `input()` và nghe `output()` để
 * gọi hàm ở đây.
 */
@Injectable()
export class TaoNguoiDungStore {
  private readonly service = inject(NguoiDungService);
  private readonly vaiTroService = inject(VaiTroService);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly form: TaoNguoiDungForm = this.fb.group({
    userName: this.fb.control('', [Validators.required]),
    email: this.fb.control('', [Validators.required, Validators.email]),
    fullName: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
    tempPassword: this.fb.control('', [Validators.required]),
    roleIds: this.fb.control<readonly string[]>([]),
  });
  /** Một thể hiện riêng của hộp này — `TraCuuVaiTroStore` không cấp bằng DI (lý do ở đầu tệp đó). */
  readonly timVaiTro = new TraCuuVaiTroStore(this.vaiTroService, this.translate);

  readonly hienThi = signal(false);
  readonly dangLuu = signal(false);
  readonly daGui = signal(false);
  readonly loiChung = signal<string | null>(null);
  readonly hienThiXacNhanHuy = signal(false);
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp lắng nghe để đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = signal(0);

  /** Nhịp đổi trạng thái của form (giá trị, chạm, lỗi): control không phải signal, cần nó để `computed` tính lại. */
  private readonly nhipForm = signal(0);

  /** Câu lỗi từng ô, đã tính sẵn (`fieldErrorText`: hiện khi ô đã chạm hoặc đã bấm gửi). */
  readonly loiTruong = computed<LoiTruongTaoNguoiDung>(() => {
    this.nhipForm();
    const daGui = this.daGui();
    const c = this.form.controls;
    const loi = (ten: TenTruongTaoNguoiDung): string | null =>
      fieldErrorText(c[ten], daGui, this.translate);
    return {
      userName: loi('userName'),
      email: loi('email'),
      fullName: loi('fullName'),
      tempPassword: loi('tempPassword'),
      // Ô "Vai trò" chỉ nhận lỗi từ server (fieldErrors["RoleIds"], trần 50 — users.md §5).
      roleIds: fieldErrorText(c.roleIds, daGui, this.translate),
    };
  });

  /** Trạng thái ô chọn vai trò gói thành một giá trị — component hộp nhận qua `input()`. */
  readonly vaiTroChon = computed<VaiTroChon>(() => {
    this.nhipForm();
    return {
      options: this.timVaiTro.options(),
      loading: this.timVaiTro.loading(),
      error: this.timVaiTro.error(),
      selected: this.form.controls.roleIds.value,
    };
  });

  constructor() {
    this.form.events.pipe(takeUntilDestroyed()).subscribe(() => this.nhipForm.update((n) => n + 1));
  }

  moHop(): void {
    this.form.reset({ userName: '', email: '', fullName: '', tempPassword: '', roleIds: [] });
    this.timVaiTro.reset();
    this.daGui.set(false);
    this.loiChung.set(null);
    this.hienThi.set(true);
  }

  dongHop(): void {
    if (this.dangLuu()) return;
    this.hienThi.set(false);
  }

  chonVaiTro(gt: string | readonly string[]): void {
    this.form.controls.roleIds.setValue(Array.isArray(gt) ? gt : []);
  }

  // Dialog chỉ phát `dismissAttempted` khi `dirty` đang bật — nó KHÔNG tự đóng. Nối thẳng `dongHop()`
  // vào đó xoá dữ liệu người dùng vừa nhập mà không hỏi gì cả (Dialog.md §API).
  onDismissAttempted(): void {
    this.hienThiXacNhanHuy.set(true);
  }

  huyXacNhanHuy(): void {
    this.hienThiXacNhanHuy.set(false);
  }

  xacNhanHuy(): void {
    this.hienThiXacNhanHuy.set(false);
    this.dongHop();
  }

  /** `thanhCong` chạy khi tạo xong — page lo Toast + `list.reload()`; store này không biết `ListStateStore`. */
  gui(thanhCong: (tenDangNhap: string) => void): void {
    // Chặn gửi hai lần (Enter ngầm, bấm đúp): lần hai sẽ nhận USERNAME_DUPLICATED giả.
    if (this.dangLuu()) return;
    this.daGui.set(true);
    this.loiChung.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.lanGuiSai.update((n) => n + 1);
      return;
    }
    const gt = this.form.getRawValue();
    this.dangLuu.set(true);
    this.service
      .tao(gt)
      .pipe(finalize(() => this.dangLuu.set(false)))
      .subscribe({
        next: () => {
          this.hienThi.set(false);
          thanhCong(gt.userName);
        },
        error: (err: unknown) => this.xuLyLoi(err),
      });
  }

  private xuLyLoi(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiChung.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }
    // Mọi mã, KHÔNG lọc theo danh sách mã (fe-ui-conventions.md §6.2): hai mã trùng xuống ô của
    // chúng; `fieldErrors` xuống ô, mã chưa hiện ở ô nào lên banner; không mã nào → câu của mã gốc.
    this.loiChung.set(applyFormFailure(this.form, err, this.translate, MA_GOC_VAO_O));
  }
}
