import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { DoiMatKhauService } from '../../../../core/auth/doi-mat-khau.service';
import { CORE_I18N } from '../../../../core/config/core-i18n';
import { ApiFailureError } from '../../../../core/http/api-result.model';
import { dichLoiChoMan } from '../../../../core/http/dich-loi';
import { ToastService } from '../../../../core/toast/toast.service';
import { UnsavedChangesService } from '../../../../core/unsaved-changes/unsaved-changes.service';
import { applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { fieldErrorsText } from '../../../../shared/forms/field-errors-text';
import { focusOSaiKhiGuiSai } from '../../../../shared/forms/focus-o-sai-dau-tien';
import { nhapLaiPhaiKhop } from '../../../../shared/forms/nhap-lai-phai-khop';
import { CardComponent } from '../../../../shared/components/card/card.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { FormRowComponent } from '../../../../shared/components/form-row/form-row.component';
import { NoticeBannerComponent } from '../../../../shared/components/notice-banner/notice-banner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { InputComponent, SegmentOption } from '../../../../shared/components/input/input.component';
import { HoSoService } from '../../services/ho-so.service';
import { HoSo } from '../../models/ho-so.model';

type TruongThongTin = 'fullName' | 'phoneNumber' | 'preferredLanguage';
type TruongMatKhau = 'currentPassword' | 'newPassword' | 'confirmNewPassword';

/** Giá trị form thông tin ĐÚNG như control giữ (ngôn ngữ "mặc định hệ thống" là `MA_NGON_NGU_MAC_DINH`). */
interface GiaTriThongTin {
  readonly fullName: string;
  readonly phoneNumber: string;
  readonly preferredLanguage: string;
}

const MA_NGON_NGU_MAC_DINH = '__mac-dinh__';

function khacGiaTri(a: GiaTriThongTin, b: GiaTriThongTin): boolean {
  return (
    a.fullName !== b.fullName ||
    a.phoneNumber !== b.phoneNumber ||
    a.preferredLanguage !== b.preferredLanguage
  );
}

/**
 * Số điện thoại — contracts/profile.md §2: tối đa 20 ký tự, chỉ gồm chữ số, khoảng trắng và
 * `+` `-` `(` `)`; cùng tập ký tự với biểu thức của BE. Ô rỗng hợp lệ: nó gửi đi là `null`.
 */
const TRAN_SO_DIEN_THOAI = 20;
const SO_DIEN_THOAI_PATTERN = /^[0-9\s+()-]+$/;

/** Design/Screens/03-ho-so-ca-nhan.md. Guard: authGuard + mustChangePasswordGuard, KHÔNG permissionGuard. */
@Component({
  selector: 'app-ho-so',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    CardComponent,
    ConfirmDialogComponent,
    FormRowComponent,
    NoticeBannerComponent,
    PageHeaderComponent,
    SkeletonLoaderComponent,
    ButtonComponent,
    InputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ho-so.page.html',
  styleUrl: './ho-so.page.scss',
})
export class HoSoPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly service = inject(HoSoService);
  private readonly doiMatKhauService = inject(DoiMatKhauService);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  private readonly i18n = inject(CORE_I18N);
  private readonly destroyRef = inject(DestroyRef);
  private readonly unsavedChanges = inject(UnsavedChangesService);

  protected readonly dangTaiHoSo = signal(true);
  /**
   * `GET` hồ sơ hỏng. Khung lỗi (tiêu đề + Thử lại) LUÔN vẽ khi khác `null`; `than` là câu `dichLoiChoMan`
   * — `null` với lớp lỗi xuyên suốt, vì interceptor đã toast nó kèm traceId (Design/Screens/03 §Trạng thái).
   */
  protected readonly loiTaiHoSo = signal<{ readonly than: string | null } | null>(null);
  protected readonly hoSo = signal<HoSo | null>(null);

  /** Số hàng skeleton của Card thông tin = số FormRow thật (03-ho-so-ca-nhan.md §Trạng thái). */
  protected readonly nhieuHangSkeleton = [0, 1, 2, 3, 4] as const;

  protected readonly tuyChonNgonNgu: readonly SegmentOption[] = [
    { value: MA_NGON_NGU_MAC_DINH, label: this.translate.instant('hoSo.ngonNgu.macDinhHeThong') },
    ...this.i18n.languages.map((l) => ({ value: l.code, label: l.nativeName })),
  ];

  /**
   * Tên control = tên field của request ở contracts/profile.md §2 (fe-ui-conventions.md §6.1), để
   * `fieldErrors["FullName"]` / `["PhoneNumber"]` / `["PreferredLanguage"]` xuống đúng ô. Giá trị
   * ô ngôn ngữ vẫn mang `MA_NGON_NGU_MAC_DINH` cho "mặc định hệ thống" — đổi sang `null` lúc gửi.
   */
  protected readonly formThongTin = this.fb.group({
    fullName: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
    phoneNumber: this.fb.control('', [
      Validators.maxLength(TRAN_SO_DIEN_THOAI),
      Validators.pattern(SO_DIEN_THOAI_PATTERN),
    ]),
    preferredLanguage: this.fb.control(MA_NGON_NGU_MAC_DINH),
  });
  /**
   * Mốc của "đã đổi thật": giá trị form thông tin ở lần nạp hoặc lưu thành công gần nhất. Gõ rồi sửa về
   * đúng giá trị này không còn là thay đổi — hỏi "Rời trang?" lúc đó là hỏi thừa (fe-routing-guard.md
   * §4.1). `null` khi chưa nạp được hồ sơ: form không hiện nên không có gì để mất.
   */
  private giaTriThongTinGoc: GiaTriThongTin | null = null;
  /**
   * PUT vừa nhận 409: lần GET thành công kế tiếp nạp ĐÈ form, kể cả khi đang gõ dở — "Tải lại" phải
   * cho người dùng thấy bản mới rồi mới lưu lại (Design/Screens/03 §Trạng thái, "PUT 409").
   */
  private napDeFormLanToi = false;
  protected readonly dangLuuThongTin = signal(false);
  protected readonly daGuiThongTin = signal(false);
  protected readonly loiLuuThongTin = signal<string | null>(null);
  protected readonly xungDotDongThoi = signal(false);
  /** Tăng mỗi lần bấm Lưu mà form thông tin không hợp lệ — `focusOSaiKhiGuiSai` đọc nó (§6.2). */
  private readonly lanGuiSaiThongTin = signal(0);
  private readonly formThongTinEl = viewChild<ElementRef<HTMLFormElement>>('formThongTinEl');

  /** Ô nhập lại khớp ô mật khẩu mới: chỉ FE kiểm (fe-ui-conventions.md §6.3), lỗi ở ô nhập lại. */
  protected readonly formMatKhau = this.fb.group(
    {
      currentPassword: this.fb.control('', [Validators.required]),
      newPassword: this.fb.control('', [Validators.required]),
      confirmNewPassword: this.fb.control('', [Validators.required]),
    },
    { validators: nhapLaiPhaiKhop('newPassword', 'confirmNewPassword') },
  );
  protected readonly dangDoiMatKhau = signal(false);
  protected readonly daGuiMatKhau = signal(false);
  protected readonly loiDoiMatKhau = signal<string | null>(null);
  /** Như `lanGuiSaiThongTin`, cho form đổi mật khẩu — mỗi form một bộ đếm, một gốc tìm ô sai. */
  private readonly lanGuiSaiMatKhau = signal(0);
  private readonly formMatKhauEl = viewChild<ElementRef<HTMLFormElement>>('formMatKhauEl');

  protected readonly hienHopXacNhanTuBo = signal(false);
  protected readonly dangTuBo = signal(false);
  protected readonly loiTuBo = signal<string | null>(null);
  /**
   * Lần từ bỏ GẦN NHẤT hỏng với `PERMISSION_BYPASS_NOT_HELD` → đóng hộp xong gọi lại `GET` hồ sơ, để
   * banner nhắc và `Card` cờ theo đúng dữ liệu (Design/Screens/03 §Trạng thái, "Từ bỏ cờ hỏng").
   */
  private taiLaiHoSoKhiDongHopTuBo = false;

  constructor() {
    this.taiHoSo();

    // Design/Screens/03 "kiểm tra dữ liệu": focus về ô lỗi đầu tiên CỦA FORM VỪA GỬI — gốc tìm ô sai
    // là chính form đó, nên ô sai của form kia không giành focus.
    focusOSaiKhiGuiSai(this.lanGuiSaiThongTin, () => this.formThongTinEl()?.nativeElement);
    focusOSaiKhiGuiSai(this.lanGuiSaiMatKhau, () => this.formMatKhauEl()?.nativeElement);

    // A17 (ADR-0040) — hai form của trang này khai "còn thay đổi chưa lưu" với guard dùng chung;
    // KHÔNG tự vẽ hộp hỏi ở đây (F30). Rời trang qua router: unsavedChangesGuard (ho-so.routes.ts);
    // đăng xuất/đổi ngôn ngữ ở khung: cùng registry, ShellComponent gọi thẳng UnsavedChangesService.
    // So GIÁ TRỊ, không so cờ `dirty` — `dirty` bật từ lần gõ đầu và không tắt khi giá trị quay về như cũ.
    const coThayDoiChuaLuu = (): boolean => this.thongTinDaDoi() || this.matKhauDaDoi();
    this.unsavedChanges.dangKy(coThayDoiChuaLuu);
    this.destroyRef.onDestroy(() => this.unsavedChanges.huyDangKy(coThayDoiChuaLuu));
  }

  private thongTinDaDoi(): boolean {
    const goc = this.giaTriThongTinGoc;
    return goc !== null && khacGiaTri(this.formThongTin.getRawValue(), goc);
  }

  /** Form mật khẩu không có giá trị nạp sẵn: "đổi" khi còn ô không rỗng. */
  private matKhauDaDoi(): boolean {
    return Object.values(this.formMatKhau.getRawValue()).some((giaTri) => giaTri !== '');
  }

  private taiHoSo(): void {
    this.dangTaiHoSo.set(true);
    this.loiTaiHoSo.set(null);
    this.service
      .layHoSo()
      // Lượt ĐỌC chết cùng màn (fe-api-client.md §6.1); các lượt GHI của màn cố ý chạy tới cùng.
      .pipe(
        finalize(() => this.dangTaiHoSo.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (hs) => this.apHoSo(hs),
        error: (err: unknown) => {
          this.loiTaiHoSo.set({
            than:
              err instanceof ApiFailureError
                ? dichLoiChoMan(this.translate, err)
                : this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'),
          });
        },
      });
  }

  /**
   * `hoSo` — tên đăng nhập, email, cờ, `version` — luôn theo máy chủ. Giá trị form thông tin và mốc so
   * sánh của nó thì KHÔNG khi form đang gõ dở: GET sau từ bỏ cờ chỉ để lấy `version` mới (POST trả
   * `data: null` — wiki-core/be/06-concurrency-control.md §6.3 luật 4), dòng "Từ bỏ cờ" của
   * Design/Screens/03 không nạp lại form.
   */
  private apHoSo(hs: HoSo): void {
    const gt: GiaTriThongTin = {
      fullName: hs.hoTen,
      phoneNumber: hs.soDienThoai ?? '',
      preferredLanguage: hs.ngonNguUaThich ?? MA_NGON_NGU_MAC_DINH,
    };
    const goc = this.giaTriThongTinGoc;
    if (this.napDeFormLanToi || goc === null || !khacGiaTri(this.formThongTin.getRawValue(), goc)) {
      this.napDeFormLanToi = false;
      this.hoSo.set(hs);
      this.formThongTin.patchValue(gt);
      this.giaTriThongTinGoc = gt;
      return;
    }
    // Giữ giá trị gõ dở. `version` mới chỉ nhận khi các trường sửa được trên máy chủ vẫn đúng như mốc
    // mà form đang dở dựa vào. Khác nghĩa là một thao tác khác đã sửa chúng: giữ `version` cũ để lần
    // Lưu nhận 409 và đi nhánh "Tải lại", không ghi đè im lặng (§6.3 luật 3).
    const versionCu = this.hoSo()?.version ?? hs.version;
    this.hoSo.set(khacGiaTri(gt, goc) ? { ...hs, version: versionCu } : hs);
  }

  protected thuLaiTaiHoSo(): void {
    this.taiHoSo();
  }

  // ---- Form thông tin cá nhân ----

  protected loiThongTin(ten: TruongThongTin): string | null {
    return fieldErrorText(this.formThongTin.controls[ten], this.daGuiThongTin(), this.translate);
  }

  protected luuThongTin(): void {
    this.daGuiThongTin.set(true);
    this.loiLuuThongTin.set(null);
    this.xungDotDongThoi.set(false);
    if (this.formThongTin.invalid) {
      this.formThongTin.markAllAsTouched();
      this.lanGuiSaiThongTin.update((n) => n + 1);
      return;
    }

    const hsHienTai = this.hoSo();
    if (!hsHienTai) return;

    const gt = this.formThongTin.getRawValue();
    this.dangLuuThongTin.set(true);
    this.service
      .capNhatHoSo({
        fullName: gt.fullName,
        phoneNumber: gt.phoneNumber === '' ? null : gt.phoneNumber,
        preferredLanguage:
          gt.preferredLanguage === MA_NGON_NGU_MAC_DINH ? null : gt.preferredLanguage,
        version: hsHienTai.version,
      })
      .pipe(finalize(() => this.dangLuuThongTin.set(false)))
      .subscribe({
        next: (hs) => {
          this.hoSo.set(hs);
          // Lưu xong xoá cờ thay đổi — rời trang ngay sau đó không bị hỏi lại (fe-routing-guard.md §4.1).
          // Mốc mới = đúng giá trị vừa gửi và được nhận — không lấy từ response, để máy chủ chuẩn hoá
          // (vd. cắt khoảng trắng) không biến một lần lưu thành công thành "còn thay đổi".
          this.giaTriThongTinGoc = gt;
          this.daGuiThongTin.set(false);
          this.formThongTin.markAsPristine();
          this.toast.thanhCong(this.translate.instant('hoSo.thongBao.luuThanhCong'));
          // Topbar đọc tên từ phiên, không từ response PUT — gọi lại me (fe-routing-guard.md §3.3).
          void this.auth.lamMoiPhien();
        },
        error: (err: unknown) => this.xuLyLoiThongTin(err),
      });
  }

  private xuLyLoiThongTin(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiLuuThongTin.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }
    if (err.body?.error.code === 'CORE.CONCURRENCY.CONFLICT') {
      this.xungDotDongThoi.set(true);
      this.napDeFormLanToi = true;
      return;
    }
    // Mọi mã còn lại, KHÔNG lọc theo danh sách mã (fe-ui-conventions.md §6.2).
    this.loiLuuThongTin.set(applyFormFailure(this.formThongTin, err, this.translate));
  }

  // ---- Form đổi mật khẩu ----

  protected loiMatKhau(ten: TruongMatKhau): string | null {
    return fieldErrorText(this.formMatKhau.controls[ten], this.daGuiMatKhau(), this.translate);
  }

  protected doiMatKhau(): void {
    this.daGuiMatKhau.set(true);
    this.loiDoiMatKhau.set(null);
    if (this.formMatKhau.invalid) {
      this.formMatKhau.markAllAsTouched();
      this.lanGuiSaiMatKhau.update((n) => n + 1);
      return;
    }

    const gt = this.formMatKhau.getRawValue();
    this.dangDoiMatKhau.set(true);
    this.doiMatKhauService
      .doiTuNguyen({ currentPassword: gt.currentPassword, newPassword: gt.newPassword })
      .pipe(finalize(() => this.dangDoiMatKhau.set(false)))
      .subscribe({
        next: () => {
          this.formMatKhau.reset();
          this.daGuiMatKhau.set(false);
          this.toast.thanhCong(this.translate.instant('hoSo.doiMatKhau.thongBao.thanhCong'));
        },
        error: (err: unknown) => this.xuLyLoiMatKhau(err),
      });
  }

  private xuLyLoiMatKhau(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiDoiMatKhau.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }
    // Mọi mã, KHÔNG lọc theo danh sách mã (fe-ui-conventions.md §6.2).
    this.loiDoiMatKhau.set(applyFormFailure(this.formMatKhau, err, this.translate));
  }

  // ---- Cờ đặc quyền ----

  protected moHopXacNhanTuBo(): void {
    this.loiTuBo.set(null);
    this.hienHopXacNhanTuBo.set(true);
  }

  protected dongHopXacNhanTuBo(): void {
    this.hienHopXacNhanTuBo.set(false);
    if (this.taiLaiHoSoKhiDongHopTuBo) {
      this.taiLaiHoSoKhiDongHopTuBo = false;
      this.taiHoSo();
    }
  }

  protected xacNhanTuBoCoDacQuyen(): void {
    this.dangTuBo.set(true);
    this.loiTuBo.set(null);
    this.taiLaiHoSoKhiDongHopTuBo = false;
    this.service
      .tuBoCoDacQuyen()
      .pipe(finalize(() => this.dangTuBo.set(false)))
      .subscribe({
        next: () => {
          this.hienHopXacNhanTuBo.set(false);
          // Gỡ banner nhắc và Card cờ NGAY (Screens/03 "Thao tác xong") — không đợi GET dưới, nó có
          // thể hỏng. `version` giữ nguyên: bản mới chỉ đến từ GET đó.
          this.hoSo.update((hs) => (hs ? { ...hs, coDacQuyen: false } : hs));
          this.toast.thanhCong(this.translate.instant('hoSo.coDacQuyen.thongBao.thanhCong'));
          // Tập quyền vừa đổi: `me` VÀ menu (Design/Screens/03, bảng "Thao tác xong").
          void this.auth.lamMoiPhien({ napLaiMenu: true });
          this.taiHoSo();
        },
        error: (err: unknown) => {
          this.taiLaiHoSoKhiDongHopTuBo =
            err instanceof ApiFailureError &&
            err.body?.error.code === 'CORE.PROFILE.PERMISSION_BYPASS_NOT_HELD';
          if (this.taiLaiHoSoKhiDongHopTuBo) {
            // Máy chủ nói cờ đã không còn: phiên và menu làm mới NGAY; GET hồ sơ chờ đóng hộp (Screens/03).
            void this.auth.lamMoiPhien({ napLaiMenu: true });
          }
          // Hộp xác nhận không có form: câu của banner do fe-ui-conventions.md §6.2 quyết.
          this.loiTuBo.set(
            err instanceof ApiFailureError
              ? fieldErrorsText(this.translate, err)
              : this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'),
          );
        },
      });
  }
}
