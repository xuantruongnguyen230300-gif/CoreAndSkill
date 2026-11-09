import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ApiFailureError } from '../../../../core/http/api-result.model';
import { BangMaGocVaoO, applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { DonViService } from '../services/don-vi.service';
import { LoiTruongTaoDonVi, TaoDonViForm, TenTruongTaoDonVi } from '../models/don-vi-hop.model';
import { DonVi } from '../models/don-vi.model';
import { cauLoiKhongPhaiApi } from '../../../../core/http/dich-loi';

/** Khuôn mã đơn vị — cùng biểu thức với BE, kiểm SAU khi chuẩn hoá về chữ hoa (tenants.md §2). */
const MA_DON_VI_PATTERN = /^[A-Z0-9][A-Z0-9_-]{0,49}$/;

/** Mã gốc hiện dưới ô — Design/Screens/20-don-vi.md, bảng Mã lỗi → chỗ hiện ("FE tự gắn mã vào ô"). */
const MA_GOC_VAO_O: BangMaGocVaoO<TaoDonViForm> = { 'CORE.TENANT.CODE_DUPLICATE': 'code' };

/**
 * Trạng thái + quy trình hộp "Tạo đơn vị" (danh-sach-don-vi.page) — tách khỏi page để giữ
 * *.page.ts dưới ngưỡng dòng (fe-architecture.md §5, luật F22). Store cấp ở `providers` của page
 * (ADR-0049) nên chết cùng màn; `moHop()` reset lại trạng thái mỗi lần mở. `HopTaoDonViComponent`
 * KHÔNG biết store này: page đọc nó rồi truyền GIÁ TRỊ xuống bằng `input()` và nghe `output()` để
 * gọi hàm ở đây.
 */
@Injectable()
export class TaoDonViStore {
  private readonly service = inject(DonViService);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly form: TaoDonViForm = this.fb.group({
    code: this.fb.control('', [
      Validators.required,
      Validators.maxLength(50),
      Validators.pattern(MA_DON_VI_PATTERN),
    ]),
    // Trần 200 theo contracts/tenants.md §2 — chặn sớm; BE vẫn là nguồn phán quyết cuối.
    name: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
    adminUserName: this.fb.control('', [Validators.required]),
    adminEmail: this.fb.control('', [Validators.required, Validators.email]),
    adminFullName: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
    adminTempPassword: this.fb.control('', [Validators.required]),
  });
  private readonly _hienThi = signal(false);
  readonly hienThi = this._hienThi.asReadonly();
  private readonly _dangLuu = signal(false);
  readonly dangLuu = this._dangLuu.asReadonly();
  private readonly _daGui = signal(false);
  readonly daGui = this._daGui.asReadonly();
  private readonly _loiChung = signal<string | null>(null);
  readonly loiChung = this._loiChung.asReadonly();
  private readonly _hienThiXacNhanHuy = signal(false);
  readonly hienThiXacNhanHuy = this._hienThiXacNhanHuy.asReadonly();
  private readonly _lanGuiSai = signal(0);
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp lắng nghe để đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = this._lanGuiSai.asReadonly();

  /** Nhịp đổi trạng thái của form (giá trị, chạm, lỗi): control không phải signal, cần nó để `loiTruong` tính lại. */
  private readonly nhipForm = signal(0);

  /**
   * Câu lỗi của từng ô, đã tính sẵn — component hộp nhận GIÁ TRỊ này qua `input()`, không tự dịch và
   * không phải biết control nào mang lỗi gì (`fieldErrorText`: hiện khi ô đã chạm hoặc đã bấm gửi).
   */
  readonly loiTruong = computed<LoiTruongTaoDonVi>(() => {
    this.nhipForm();
    const daGui = this.daGui();
    const c = this.form.controls;
    const loi = (ten: TenTruongTaoDonVi): string | null =>
      fieldErrorText(c[ten], daGui, this.translate);
    return {
      code: loi('code'),
      name: loi('name'),
      adminUserName: loi('adminUserName'),
      adminEmail: loi('adminEmail'),
      adminFullName: loi('adminFullName'),
      adminTempPassword: loi('adminTempPassword'),
    };
  });

  constructor() {
    this.form.events.pipe(takeUntilDestroyed()).subscribe(() => this.nhipForm.update((n) => n + 1));
  }

  /** Ô mã đổi sang chữ hoa khi rời ô (Design/Screens/20-don-vi.md bảng Quyết định). */
  chuanHoaMa(): void {
    const control = this.form.controls.code;
    const hoa = control.value.toLocaleUpperCase('en-US');
    if (hoa !== control.value) control.setValue(hoa);
  }

  moHop(): void {
    this.form.reset({
      code: '',
      name: '',
      adminUserName: '',
      adminEmail: '',
      adminFullName: '',
      adminTempPassword: '',
    });
    this._daGui.set(false);
    this._loiChung.set(null);
    this._hienThi.set(true);
  }

  dongHop(): void {
    if (this.dangLuu()) return;
    this._hienThi.set(false);
  }

  onDismissAttempted(): void {
    this._hienThiXacNhanHuy.set(true);
  }

  huyXacNhanHuy(): void {
    this._hienThiXacNhanHuy.set(false);
  }

  xacNhanHuy(): void {
    this._hienThiXacNhanHuy.set(false);
    this.dongHop();
  }

  /** `thanhCong` chạy khi tạo xong — page lo Toast + `list.reload()`; store này không biết `ListStateStore`. */
  gui(thanhCong: (dv: DonVi) => void): void {
    // Chặn gửi hai lần (Enter ngầm, bấm đúp): lần hai sẽ nhận CODE_DUPLICATE giả.
    if (this.dangLuu()) return;
    this.chuanHoaMa();
    this._daGui.set(true);
    this._loiChung.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this._lanGuiSai.update((n) => n + 1);
      return;
    }
    const gt = this.form.getRawValue();
    this._dangLuu.set(true);
    this.service
      .tao(gt)
      .pipe(finalize(() => this._dangLuu.set(false)))
      .subscribe({
        next: (dv) => {
          this._hienThi.set(false);
          thanhCong(dv);
        },
        error: (err: unknown) => this.xuLyLoi(err),
      });
  }

  private xuLyLoi(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this._loiChung.set(cauLoiKhongPhaiApi(this.translate));
      return;
    }
    // Mọi mã, KHÔNG lọc theo danh sách mã (fe-ui-conventions.md §6.2): CODE_DUPLICATE xuống ô mã;
    // `fieldErrors` xuống ô, mã chưa hiện ở ô nào lên banner; không mã nào → câu của mã gốc — gồm
    // SEED_FAILED, hộp và nội dung giữ nguyên để gửi lại (Design/Screens/20-don-vi.md).
    this._loiChung.set(applyFormFailure(this.form, err, this.translate, MA_GOC_VAO_O));
  }
}
