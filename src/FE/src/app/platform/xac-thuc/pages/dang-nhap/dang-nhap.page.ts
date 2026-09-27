import { ChangeDetectionStrategy, Component, afterNextRender, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';
import { ApiFailureError } from '../../../../core/http/api-result.model';
import { dichLoiChoMan } from '../../../../core/http/dich-loi';
import { applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { AuthCardComponent } from '../../../../shared/components/auth-card/auth-card.component';
import { AuthFieldComponent } from '../../../../shared/components/auth-field/auth-field.component';
import { FooterComponent } from '../../../../shared/components/footer/footer.component';
import { ButtonComponent } from '../../../../shared/components/button/button.component';

type TenTruong = 'tenantCode' | 'userName' | 'password';

/**
 * Design/Screens/01-dang-nhap.md. Không có `authGuard` — nơi duy nhất người chưa đăng nhập vào
 * được. `returnUrl` chỉ nhận đường dẫn NỘI BỘ (`/`, không `//`) — 07-auth-identity.md §2.2, chặn
 * chuyển hướng mở.
 */
@Component({
  selector: 'app-dang-nhap',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, AuthCardComponent, AuthFieldComponent, FooterComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dang-nhap.page.html',
})
export class DangNhapPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly routes = inject(CORE_ROUTES);
  private readonly translate = inject(TranslateService);
  private readonly branding = inject(CORE_BRANDING);

  protected readonly namHienTai = new Date().getFullYear();
  protected readonly tenHeThong = this.branding.name;

  private readonly oMaDonVi = viewChild<AuthFieldComponent>('oMaDonVi');

  protected readonly form = this.fb.group({
    tenantCode: this.fb.control('', [Validators.required]),
    userName: this.fb.control('', [Validators.required]),
    password: this.fb.control('', [Validators.required]),
  });

  protected readonly dangGui = signal(false);
  protected readonly daGui = signal(false);
  protected readonly loiChung = signal<string | null>(null);

  constructor() {
    afterNextRender(() => this.oMaDonVi()?.focus());
  }

  protected loiCua(ten: TenTruong): string | null {
    return fieldErrorText(this.form.controls[ten], this.daGui(), this.translate);
  }

  protected gui(): void {
    this.daGui.set(true);
    this.loiChung.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dangGui.set(true);
    this.auth
      .dangNhap(this.form.getRawValue())
      .pipe(finalize(() => this.dangGui.set(false)))
      .subscribe({
        next: () => this.dieuHuongSauDangNhap(),
        error: (err: unknown) => this.xuLyLoi(err),
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
      void this.router.navigate([this.routes.doiMatKhauBatBuoc], hopLe ? { queryParams: { returnUrl } } : {});
      return;
    }

    void this.router.navigateByUrl(hopLe ? returnUrl : this.routes.sauDangNhap);
  }

  private xuLyLoi(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiChung.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }

    const ma = err.body?.error.code;
    // CORE.VALIDATION.FAILED gắn vào đúng ô theo khoá TenantCode/UserName/Password — không tô
    // đỏ riêng ô mã đơn vị khi INVALID_CREDENTIALS (không lộ mã đơn vị nào tồn tại — auth.md §3).
    if (ma === 'CORE.VALIDATION.FAILED') {
      this.loiChung.set(applyFormFailure(this.form, err, this.translate));
      return;
    }

    // CORE.RATE_LIMIT.EXCEEDED (429): `ApiFailureError` chỉ mang envelope, không mang header —
    // đủ dùng vì BE gửi CÙNG số giây trong `messageParams.RetryAfterSeconds` (auth.md §10).
    // Lớp lỗi xuyên suốt (5xx, 403 CORE.AUTH.*) → khu lỗi trống: interceptor đã toast kèm traceId.
    this.loiChung.set(dichLoiChoMan(this.translate, err));
  }
}
