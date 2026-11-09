import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, NonNullableFormBuilder, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { finalize, merge } from 'rxjs';

import { ApiFailureError } from '../../../../core/http/api-result.model';
import { applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { DonViService } from '../services/don-vi.service';
import {
  KhoiPhucForm,
  LoaiHopQuanTri,
  LoiTruongQuanTri,
  TaoQuanTriForm,
} from '../models/don-vi-hop.model';
import { DonVi } from '../models/don-vi.model';
import { cauLoiKhongPhaiApi } from '../../../../core/http/dich-loi';

/**
 * Trạng thái + quy trình HAI hộp "Khôi phục quản trị" (tenants.md §4) và "Tạo quản trị mới" (§6) —
 * gộp một store vì cùng chia sẻ `donViDangThaoTac` (hàng đang thao tác) và cùng luật "gõ lại mã đơn
 * vị" (Design/Screens/20-don-vi.md bảng Quyết định); chỉ một hộp mở tại một thời điểm (`dangMo()`).
 * Tách khỏi page để giữ *.page.ts dưới ngưỡng dòng (fe-architecture.md §5, luật F22). Store cấp ở
 * `providers` của page (ADR-0049) nên chết cùng màn. `HopQuanTriDonViComponent` KHÔNG biết store
 * này: page đọc nó rồi truyền GIÁ TRỊ xuống bằng `input()` và nghe `output()` để gọi hàm ở đây.
 */
@Injectable()
export class KhoiPhucTaoQuanTriStore {
  private readonly service = inject(DonViService);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly formKhoiPhuc: KhoiPhucForm = this.fb.group({
    userName: this.fb.control('', [Validators.required]),
    tempPassword: this.fb.control('', [Validators.required]),
    goLaiMa: this.fb.control('', [Validators.required]),
  });
  readonly formTaoQuanTri: TaoQuanTriForm = this.fb.group({
    userName: this.fb.control('', [Validators.required]),
    email: this.fb.control('', [Validators.required, Validators.email]),
    fullName: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
    tempPassword: this.fb.control('', [Validators.required]),
    goLaiMa: this.fb.control('', [Validators.required]),
  });
  private readonly _dangMo = signal<LoaiHopQuanTri>(null);
  readonly dangMo = this._dangMo.asReadonly();
  private readonly _donViDangThaoTac = signal<DonVi | null>(null);
  readonly donViDangThaoTac = this._donViDangThaoTac.asReadonly();
  private readonly _dangGui = signal(false);
  readonly dangGui = this._dangGui.asReadonly();
  private readonly _daGui = signal(false);
  readonly daGui = this._daGui.asReadonly();
  private readonly _loiChung = signal<string | null>(null);
  readonly loiChung = this._loiChung.asReadonly();
  private readonly _hienThiXacNhanHuy = signal(false);
  readonly hienThiXacNhanHuy = this._hienThiXacNhanHuy.asReadonly();
  private readonly _lanGuiSai = signal(0);
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp lắng nghe để đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = this._lanGuiSai.asReadonly();

  /** Nhịp đổi trạng thái của hai form (giá trị, chạm, lỗi): control không phải signal, cần nó để `loiTruong` tính lại. */
  private readonly nhipForm = signal(0);

  readonly loiTruong = computed<LoiTruongQuanTri>(() => {
    this.nhipForm();
    const daGui = this.daGui();
    const kp = this.formKhoiPhuc.controls;
    const tq = this.formTaoQuanTri.controls;
    const loi = (control: FormControl<string>): string | null =>
      fieldErrorText(control, daGui, this.translate);
    return {
      khoiPhuc: {
        userName: loi(kp.userName),
        tempPassword: loi(kp.tempPassword),
        goLaiMa: loi(kp.goLaiMa),
      },
      taoQuanTri: {
        userName: loi(tq.userName),
        email: loi(tq.email),
        fullName: loi(tq.fullName),
        tempPassword: loi(tq.tempPassword),
        goLaiMa: loi(tq.goLaiMa),
      },
    };
  });

  constructor() {
    // Ô "Gõ lại mã đơn vị" kiểm KHỚP ở FE, không gửi lên (tenants.md §2, §4 chỉ nhận đúng các
    // trường đã khai) — so không phân biệt hoa thường.
    this.formKhoiPhuc.controls.goLaiMa.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.kiemKhopMa(this.formKhoiPhuc.controls.goLaiMa));
    this.formTaoQuanTri.controls.goLaiMa.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.kiemKhopMa(this.formTaoQuanTri.controls.goLaiMa));
    merge(this.formKhoiPhuc.events, this.formTaoQuanTri.events)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.nhipForm.update((n) => n + 1));
  }

  private kiemKhopMa(control: FormControl<string>): void {
    const ma = this.donViDangThaoTac()?.code ?? '';
    const conLai: Record<string, unknown> = { ...control.errors };
    delete conLai['mismatch'];
    if (
      control.value !== '' &&
      control.value.toLocaleUpperCase('en-US') !== ma.toLocaleUpperCase('en-US')
    ) {
      control.setErrors({ ...conLai, mismatch: true });
    } else {
      control.setErrors(Object.keys(conLai).length > 0 ? conLai : null);
    }
  }

  moKhoiPhuc(dv: DonVi): void {
    this._donViDangThaoTac.set(dv);
    this.formKhoiPhuc.reset({ userName: '', tempPassword: '', goLaiMa: '' });
    this._daGui.set(false);
    this._loiChung.set(null);
    this._dangMo.set('khoi-phuc');
  }

  moTaoQuanTri(dv: DonVi): void {
    this._donViDangThaoTac.set(dv);
    this.formTaoQuanTri.reset({
      userName: '',
      email: '',
      fullName: '',
      tempPassword: '',
      goLaiMa: '',
    });
    this._daGui.set(false);
    this._loiChung.set(null);
    this._dangMo.set('tao-quan-tri');
  }

  /** Đóng do NGƯỜI DÙNG (nút Hủy, Esc, xác nhận hủy) — khoá khi đang gửi. */
  dongHop(): void {
    if (this.dangGui()) return;
    this.datLaiHop();
  }

  /**
   * Đóng vô điều kiện. Nhánh THÀNH CÔNG phải gọi hàm này, không gọi `dongHop()`: `finalize` chỉ hạ
   * `dangGui` SAU `complete`, tức sau `next` — `dongHop()` gọi từ `next` sẽ thấy cờ còn bật và
   * thoát im lặng, hộp không đóng.
   */
  private datLaiHop(): void {
    this._dangMo.set(null);
    this._donViDangThaoTac.set(null);
    this._daGui.set(false);
    this._loiChung.set(null);
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

  /**
   * `thanhCong` chạy khi khôi phục xong — page lo Toast (không nêu gì về tài khoản đích, ADR-0029).
   * `khiDonViKhongTonTai` chạy khi BE trả `CORE.TENANT.NOT_FOUND` — page tải lại danh sách (Screen 20);
   * store không biết `ListStateStore`.
   */
  guiKhoiPhuc(thanhCong: () => void, khiDonViKhongTonTai: () => void): void {
    // Chặn gửi hai lần (Enter ngầm, bấm đúp).
    if (this.dangGui()) return;
    const dv = this.donViDangThaoTac();
    if (!dv) return;
    this.kiemKhopMa(this.formKhoiPhuc.controls.goLaiMa);
    this._daGui.set(true);
    this._loiChung.set(null);
    if (this.formKhoiPhuc.invalid) {
      this.formKhoiPhuc.markAllAsTouched();
      this._lanGuiSai.update((n) => n + 1);
      return;
    }
    const gt = this.formKhoiPhuc.getRawValue();
    this._dangGui.set(true);
    this.service
      .khoiPhucQuanTri(dv.id, { userName: gt.userName, tempPassword: gt.tempPassword })
      .pipe(finalize(() => this._dangGui.set(false)))
      .subscribe({
        next: () => {
          this.datLaiHop();
          thanhCong();
        },
        error: (err: unknown) => this.xuLyLoi(err, this.formKhoiPhuc, khiDonViKhongTonTai),
      });
  }

  /** `thanhCong` nhận `tenDangNhap` vừa tạo — page lo Toast + không hiện lại mật khẩu. */
  guiTaoQuanTri(thanhCong: (tenDangNhap: string) => void, khiDonViKhongTonTai: () => void): void {
    // Chặn gửi hai lần (Enter ngầm, bấm đúp).
    if (this.dangGui()) return;
    const dv = this.donViDangThaoTac();
    if (!dv) return;
    this.kiemKhopMa(this.formTaoQuanTri.controls.goLaiMa);
    this._daGui.set(true);
    this._loiChung.set(null);
    if (this.formTaoQuanTri.invalid) {
      this.formTaoQuanTri.markAllAsTouched();
      this._lanGuiSai.update((n) => n + 1);
      return;
    }
    const gt = this.formTaoQuanTri.getRawValue();
    this._dangGui.set(true);
    this.service
      .taoQuanTriMoi(dv.id, {
        userName: gt.userName,
        email: gt.email,
        fullName: gt.fullName,
        tempPassword: gt.tempPassword,
      })
      .pipe(finalize(() => this._dangGui.set(false)))
      .subscribe({
        next: () => {
          this.datLaiHop();
          thanhCong(gt.userName);
        },
        error: (err: unknown) => this.xuLyLoi(err, this.formTaoQuanTri, khiDonViKhongTonTai),
      });
  }

  /** §4.3 09-forms-validation.md — lỗi không được biến mất: không gắn được vào ô thì vào banner chung. */
  private xuLyLoi(err: unknown, form: FormGroup, khiDonViKhongTonTai: () => void): void {
    if (!(err instanceof ApiFailureError)) {
      this._loiChung.set(cauLoiKhongPhaiApi(this.translate));
      return;
    }
    // Mọi mã đi cùng một đường (fe-ui-conventions.md §6.2): mã khớp ô thì gắn vào ô, mã chưa hiện ở ô
    // nào thì banner mang câu của từng mã đó; `fieldErrors` không mang mã nào thì câu của mã gốc. Mã
    // không khai `fieldErrors` —
    // NOT_FOUND, RECOVERY_TARGET_NOT_ELIGIBLE (một câu cho cả hai ca, KHÔNG gắn vào ô "Tên đăng
    // nhập", cố ý che việc tài khoản có tồn tại hay không) — cũng rơi vào banner ở đây.
    this._loiChung.set(applyFormFailure(form, err, this.translate));
    // NOT_FOUND (id không tồn tại hoặc là đơn vị hệ thống): hàng đó không được nằm lại trong lưới (Screen 20).
    if (err.body?.error.code === 'CORE.TENANT.NOT_FOUND') khiDonViKhongTonTai();
  }
}
