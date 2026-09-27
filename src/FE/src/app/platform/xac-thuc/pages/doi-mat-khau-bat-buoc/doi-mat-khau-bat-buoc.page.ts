import { ChangeDetectionStrategy, Component, afterNextRender, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AuthService } from '../../../../core/auth/auth.service';
import { DoiMatKhauService } from '../../../../core/auth/doi-mat-khau.service';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';
import { ApiFailureError } from '../../../../core/http/api-result.model';
import { applyFormFailure } from '../../../../shared/forms/apply-form-failure';
import { fieldErrorText } from '../../../../shared/forms/field-error-text';
import { nhapLaiPhaiKhop } from '../../../../shared/forms/nhap-lai-phai-khop';
import { AuthCardComponent } from '../../../../shared/components/auth-card/auth-card.component';
import { AuthFieldComponent } from '../../../../shared/components/auth-field/auth-field.component';
import { FooterComponent } from '../../../../shared/components/footer/footer.component';
import { ButtonComponent } from '../../../../shared/components/button/button.component';

type TenTruong = 'currentPassword' | 'newPassword' | 'confirmNewPassword';

/**
 * Design/Screens/01-dang-nhap.md, mục "Đổi mật khẩu bắt buộc". Guard: `authGuard` +
 * `mustChangePasswordGuard` (xac-thuc.routes.ts). Không bao giờ bắt đăng nhập lại sau khi thành
 * công — BE cấp lại cookie cho phiên đang gọi (auth.md §7, Ghi chú).
 */
@Component({
  selector: 'app-doi-mat-khau-bat-buoc',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, AuthCardComponent, AuthFieldComponent, FooterComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './doi-mat-khau-bat-buoc.page.html',
})
export class DoiMatKhauBatBuocPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly doiMatKhau = inject(DoiMatKhauService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly routes = inject(CORE_ROUTES);
  private readonly translate = inject(TranslateService);
  private readonly branding = inject(CORE_BRANDING);

  private readonly oMatKhauHienTai = viewChild<AuthFieldComponent>('oMatKhauHienTai');

  protected readonly tenDangNhap = this.auth.nguoiDung()?.userName ?? '';
  protected readonly namHienTai = new Date().getFullYear();
  protected readonly tenHeThong = this.branding.name;

  // Kiểm khớp CHỈ ở FE (CORE.CLIENT.VALIDATION_MISMATCH) — mật khẩu mới khác mật khẩu hiện tại
  // là luật của BE (CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT), FE không kiểm luật đó (fe-ui-conventions.md §6.3).
  protected readonly form = this.fb.group(
    {
      currentPassword: this.fb.control('', [Validators.required]),
      newPassword: this.fb.control('', [Validators.required]),
      confirmNewPassword: this.fb.control('', [Validators.required]),
    },
    { validators: nhapLaiPhaiKhop('newPassword', 'confirmNewPassword') },
  );

  protected readonly dangGui = signal(false);
  protected readonly daGui = signal(false);
  protected readonly loiChung = signal<string | null>(null);

  constructor() {
    afterNextRender(() => this.oMatKhauHienTai()?.focus());
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
    const gt = this.form.getRawValue();
    this.doiMatKhau
      .doiBatBuoc({ currentPassword: gt.currentPassword, newPassword: gt.newPassword })
      .subscribe({
        next: () => this.sauKhiDoiThanhCong(),
        error: (err: unknown) => {
          this.dangGui.set(false);
          this.xuLyLoi(err);
        },
      });
  }

  private sauKhiDoiThanhCong(): void {
    // BE cấp lại cookie cho phiên đang gọi; gọi lại me TRƯỚC khi điều hướng (auth.md §7, Ghi chú;
    // fe-routing-guard.md §5.3 điều 4) — mustChangePassword phải về false trước khi rời màn này.
    void this.auth.lamMoiPhien().then(() => {
      this.dangGui.set(false);
      // returnUrl được mang SANG từ màn đăng nhập (dang-nhap.page.ts) khi có; guard chặn trực
      // tiếp KHÔNG mang nó, và lúc đó sauDangNhap là đích đúng (fe-routing-guard.md §5.2).
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
      const hopLe = returnUrl !== null && returnUrl.startsWith('/') && !returnUrl.startsWith('//');
      void this.router.navigateByUrl(hopLe ? returnUrl : this.routes.sauDangNhap);
    });
  }

  protected dangXuat(): void {
    this.auth.dangXuat().subscribe({
      next: () => void this.router.navigate([this.routes.dangNhap]),
      error: () => undefined, // interceptor đã toast; ở lại màn, thử lại được
    });
  }

  private xuLyLoi(err: unknown): void {
    if (!(err instanceof ApiFailureError)) {
      this.loiChung.set(this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'));
      return;
    }

    if (err.body?.error.code === 'CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED') {
      // Nhánh luồng, không phải nhánh hiển thị: cờ đã hạ ở chỗ khác — rời màn như khi thành công.
      this.sauKhiDoiThanhCong();
      return;
    }

    // Mọi mã còn lại, KHÔNG lọc theo danh sách mã (fe-ui-conventions.md §6.2).
    this.loiChung.set(applyFormFailure(this.form, err, this.translate));
  }
}
