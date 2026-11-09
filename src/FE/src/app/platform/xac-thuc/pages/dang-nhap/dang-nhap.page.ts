import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  linkedSignal,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { distinctUntilChanged, map } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { maCommitNgan } from '../../../../core/config/phien-ban';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';
import { TENANT_CODE } from '../../../../core/config/tenant-code';
import { ApiFailure, ApiFailureError } from '../../../../core/http/api-result.model';
import { cauLoiKhongPhaiApi, dichLoiChoMan } from '../../../../core/http/dich-loi';
import { applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { phanTuDangFocus, traFocus } from '../../../../shared/forms/tra-focus-sau-khi-mo-form';
import { AuthCardComponent } from '../../../../shared/components/auth-card/auth-card.component';
import { AuthFieldComponent } from '../../../../shared/components/auth-field/auth-field.component';
import { AuthPageComponent } from '../../../../shared/components/auth-page/auth-page.component';
import { FooterComponent } from '../../../../shared/components/footer/footer.component';
import { ButtonComponent } from '../../../../shared/components/button/button.component';

/** Tham số truy vấn bật chế độ vận hành — khai ở quy-uoc/fe-routing-guard.md §1 (ADR-0116 QC2). */
export const THAM_SO_CHE_DO = 'cheDo';
export const GIA_TRI_CHE_DO_VAN_HANH = 'van-hanh';

function laCheDoVanHanh(tham: ParamMap): boolean {
  return tham.get(THAM_SO_CHE_DO) === GIA_TRI_CHE_DO_VAN_HANH;
}

type TenTruong = 'tenantCode' | 'userName' | 'password';

/**
 * Design/Screens/01-dang-nhap.md. Hai chế độ trên một tuyến (ADR-0116): chế độ thường hai ô,
 * `tenantCode` lấy từ `TENANT_CODE`; chế độ vận hành (`?cheDo=van-hanh`) ba ô, mã đơn vị gõ tay.
 * Không có `authGuard` — nơi duy nhất người chưa đăng nhập vào được. `returnUrl` chỉ nhận đường dẫn
 * NỘI BỘ (`/`, không `//`) — 07-auth-identity.md §2.2, chặn chuyển hướng mở.
 */
@Component({
  selector: 'app-dang-nhap',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslatePipe,
    AuthPageComponent,
    AuthCardComponent,
    AuthFieldComponent,
    FooterComponent,
    ButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dang-nhap.page.html',
  styleUrl: './dang-nhap.page.scss',
})
export class DangNhapPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly routes = inject(CORE_ROUTES);
  private readonly translate = inject(TranslateService);
  private readonly branding = inject(CORE_BRANDING);
  private readonly maCauHinh = inject(TENANT_CODE);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  /** Phần tử giữ focus lúc khoá form — trả lại sau khi mở khoá vì gửi hỏng (AuthCard.md §Accessibility). */
  private phanTuGiuFocus: HTMLElement | null = null;

  protected readonly namHienTai = new Date().getFullYear();
  /** 7 ký tự đầu mã commit lúc build; `null` → `Footer` ẩn phần phiên bản (Footer.md §Phiên bản hiển thị). */
  protected readonly maCommit = maCommitNgan();
  protected readonly tenHeThong = this.branding.name;

  private readonly oMaDonVi = viewChild<AuthFieldComponent>('oMaDonVi');
  private readonly oTenDangNhap = viewChild<AuthFieldComponent>('oTenDangNhap');
  private readonly oMatKhau = viewChild<AuthFieldComponent>('oMatKhau');

  private readonly thamSo = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });
  protected readonly cheDoVanHanh = computed(() => laCheDoVanHanh(this.thamSo()));
  /** Liên kết chuyển chế độ chỉ đổi tham số chế độ; `returnUrl` giữ nhờ `queryParamsHandling="merge"`. */
  protected readonly thamSoChuyenCheDo = computed(() => ({
    [THAM_SO_CHE_DO]: this.cheDoVanHanh() ? null : GIA_TRI_CHE_DO_VAN_HANH,
  }));

  protected readonly form = this.fb.group({
    tenantCode: this.fb.control('', [Validators.required]),
    userName: this.fb.control('', [Validators.required]),
    password: this.fb.control('', [Validators.required]),
  });

  protected readonly dangGui = signal(false);
  /** Đổi chế độ thì về trạng thái đầu: chưa gửi lần nào, không có lỗi chung của chế độ trước. */
  protected readonly daGui = linkedSignal<boolean, boolean>({
    source: this.cheDoVanHanh,
    computation: () => false,
  });
  protected readonly loiChung = linkedSignal<boolean, string | null>({
    source: this.cheDoVanHanh,
    computation: () => null,
  });

  constructor() {
    // Vào màn và mỗi lần đổi chế độ: dọn control của chế độ trước, rồi focus ô đầu còn trống. Chỉ
    // còn phần ghi vào `FormControl`/focus — thứ ngoài signal graph — nên bám thẳng vào nguồn sự
    // kiện (tham số truy vấn) thay vì `effect()`. `daGui`, `loiChung` tự về đầu qua `linkedSignal`.
    this.route.queryParamMap
      .pipe(map(laCheDoVanHanh), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((vanHanh) => this.datCheDo(vanHanh));
  }

  private datCheDo(vanHanh: boolean): void {
    const { tenantCode, userName, password } = this.form.controls;
    // Ô mã đơn vị luôn trống khi vào chế độ vận hành; ở chế độ thường nó bị vô hiệu (không có ô).
    tenantCode.setValue('');
    if (vanHanh) {
      tenantCode.enable({ emitEvent: false });
    } else {
      tenantCode.disable({ emitEvent: false });
    }
    // Giữ giá trị tên đăng nhập và mật khẩu; xoá lỗi (cả lỗi máy chủ) và dấu đã chạm.
    for (const c of [tenantCode, userName, password]) {
      c.markAsUntouched();
      c.updateValueAndValidity({ emitEvent: false });
    }
    afterNextRender(() => this.focusO((c) => c.value === '', true), { injector: this.injector });
  }

  /**
   * Focus ô đầu tiên (theo thứ tự trên màn) thoả `chon`; không ô nào thoả thì ô đầu tiên.
   * `lucMo`: vào màn / đổi chế độ → đặt cờ ô tự nhận focus (`focus()` của AuthField); còn lại là
   * trả focus sau gửi → `focusLai()`, không cờ (fe-ui-conventions.md §6.5).
   */
  private focusO(chon: (c: AbstractControl<string>) => boolean, lucMo = false): void {
    const { tenantCode, userName, password } = this.form.controls;
    const cacO = [
      ...(this.cheDoVanHanh() ? [{ c: tenantCode, o: this.oMaDonVi() }] : []),
      { c: userName, o: this.oTenDangNhap() },
      { c: password, o: this.oMatKhau() },
    ];
    const o = (cacO.find(({ c }) => chon(c)) ?? cacO[0]).o;
    if (lucMo) o?.focus();
    else o?.focusLai();
  }

  private khoaForm(): void {
    this.phanTuGiuFocus = phanTuDangFocus(this.host.nativeElement);
    this.dangGui.set(true);
    this.form.disable({ emitEvent: false });
  }

  /** Mở khoá TRƯỚC khi gắn lỗi máy chủ: `enable` chạy lại validator và xoá lỗi `server` đã gắn. */
  private moForm(): void {
    this.dangGui.set(false);
    this.form.enable({ emitEvent: false });
    if (!this.cheDoVanHanh()) {
      this.form.controls.tenantCode.disable({ emitEvent: false });
    }
  }

  /** Sau lần render kế (ô đã hết `disabled`): về phần tử cũ, không được thì ô lỗi đầu tiên, rồi ô đầu form. */
  private traFocusSauLoi(): void {
    const truoc = this.phanTuGiuFocus;
    this.phanTuGiuFocus = null;
    afterNextRender(() => traFocus(truoc, () => this.focusO((c) => c.invalid)), {
      injector: this.injector,
    });
  }

  protected loiCua(ten: TenTruong): string | null {
    return fieldErrorText(this.form.controls[ten], this.daGui(), this.translate);
  }

  protected gui(): void {
    this.daGui.set(true);
    this.loiChung.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.focusO((c) => c.invalid);
      return;
    }

    const { tenantCode, userName, password } = this.form.getRawValue();
    this.khoaForm();
    this.auth
      .dangNhap({
        tenantCode: this.cheDoVanHanh() ? tenantCode : this.maCauHinh,
        userName,
        password,
      })
      .subscribe({
        next: () => {
          this.moForm();
          this.dieuHuongSauDangNhap();
        },
        error: (err: unknown) => {
          this.moForm();
          this.xuLyLoi(err);
          this.traFocusSauLoi();
        },
      });
  }

  private dieuHuongSauDangNhap(): void {
    // returnUrl chỉ nhận đường dẫn NỘI BỘ — chặn chuyển hướng mở (07-auth-identity.md §2.2).
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    const hopLe = returnUrl !== null && returnUrl.startsWith('/') && !returnUrl.startsWith('//');

    if (this.auth.phaiDoiMatKhau()) {
      // Mang returnUrl SANG màn đổi mật khẩu bắt buộc — nó cần dùng lại sau khi đổi xong
      // (Design/Screens/01-dang-nhap.md, luồng "đổi xong → returnUrl hợp lệ | sauDangNhap").
      // Guard tự tìm URL này khi chặn trực tiếp KHÔNG mang returnUrl — đúng vì lúc đó không có
      // đường quay lại nào để mang (fe-routing-guard.md §5.2).
      void this.router.navigate(
        [this.routes.doiMatKhauBatBuoc],
        hopLe ? { queryParams: { returnUrl } } : {},
      );
      return;
    }

    void this.router.navigateByUrl(hopLe ? returnUrl : this.routes.sauDangNhap);
  }

  private xuLyLoi(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiChung.set(cauLoiKhongPhaiApi(this.translate));
      return;
    }

    const ma = err.body?.error.code;
    const vanHanh = this.cheDoVanHanh();

    // INVALID_CREDENTIALS: câu theo chế độ, KHÔNG tô đỏ ô nào (không lộ mã đơn vị nào tồn tại —
    // auth.md §3), kể cả ô mã đơn vị ở chế độ vận hành.
    if (ma === 'CORE.AUTH.INVALID_CREDENTIALS' && vanHanh) {
      this.loiChung.set(this.translate.instant('xacThuc.dangNhap.loiSaiThongTinVanHanh'));
      return;
    }

    // Chế độ thường không có ô mã đơn vị: khoá `TenantCode` là lỗi cấu hình bản cài — vào khu lỗi
    // với câu cấu hình, không dùng câu của mã con (nói về một ô không có trên màn).
    const fieldErrors = err.body?.error.fieldErrors ?? {};
    if (ma === 'CORE.VALIDATION.FAILED' && !vanHanh && Object.hasOwn(fieldErrors, 'TenantCode')) {
      this.loiChung.set(this.cauLoiCauHinh(err, fieldErrors));
      return;
    }

    // CORE.VALIDATION.FAILED gắn vào đúng ô theo khoá UserName/Password (và TenantCode ở chế độ
    // vận hành).
    if (ma === 'CORE.VALIDATION.FAILED') {
      this.loiChung.set(applyFormFailure(this.form, err, this.translate));
      return;
    }

    // CORE.RATE_LIMIT.EXCEEDED (429): `ApiFailureError` chỉ mang envelope, không mang header —
    // đủ dùng vì BE gửi CÙNG số giây trong `messageParams.RetryAfterSeconds` (auth.md §10).
    // Lớp lỗi xuyên suốt (5xx, 403 CORE.AUTH.*) → khu lỗi trống: interceptor đã toast kèm traceId.
    this.loiChung.set(dichLoiChoMan(this.translate, err));
  }

  /** Câu cấu hình, cộng câu của các khoá khác (nếu BE gửi kèm) — các khoá đó vẫn gắn vào ô. */
  private cauLoiCauHinh(
    err: ApiFailureError,
    fieldErrors: NonNullable<ApiFailure['error']['fieldErrors']>,
  ): string {
    const cau = this.translate.instant('xacThuc.dangNhap.loiCauHinhMaDonVi') as string;
    const conLai = Object.fromEntries(
      Object.entries(fieldErrors).filter(([khoa]) => khoa !== 'TenantCode'),
    );
    if (!err.body || !Object.values(conLai).some((loi) => loi.length > 0)) {
      return cau;
    }
    const cacLoiKhac = applyFormFailure(
      this.form,
      new ApiFailureError(
        { ...err.body, error: { ...err.body.error, fieldErrors: conLai } },
        err.status,
      ),
      this.translate,
    );
    return cacLoiKhac ? `${cau}\n${cacLoiKhac}` : cau;
  }
}
