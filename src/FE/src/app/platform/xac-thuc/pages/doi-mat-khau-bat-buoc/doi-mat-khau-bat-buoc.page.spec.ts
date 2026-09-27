import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { Router, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { DoiMatKhauService } from '../../../../core/auth/doi-mat-khau.service';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';
import { ApiFailure, ApiFailureError } from '../../../../core/http/api-result.model';
import { DoiMatKhauBatBuocPage } from './doi-mat-khau-bat-buoc.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

function loiApi(code: string, fieldErrors: ApiFailure['error']['fieldErrors']): ApiFailureError {
  return new ApiFailureError({
    success: false,
    data: null,
    error: { code, type: 'Validation', message: `msg:${code}`, messageParams: null, fieldErrors },
    traceId: 't',
  });
}

interface PageTest {
  form: FormGroup<{
    currentPassword: FormControl<string>;
    newPassword: FormControl<string>;
    confirmNewPassword: FormControl<string>;
  }>;
  gui(): void;
  loiChung(): string | null;
}

/** `DoiMatKhauService` tắt toast (BO_QUA_TOAST_LOI) nên màn là nơi DUY NHẤT hiện lỗi. */
describe('DoiMatKhauBatBuocPage — lỗi 422 không gắn được vào ô nào vẫn phải HIỆN', () => {
  let fixture: ComponentFixture<DoiMatKhauBatBuocPage>;
  let doiMatKhau: jasmine.SpyObj<DoiMatKhauService>;
  let lamMoiPhien: jasmine.Spy<() => Promise<void>>;
  let page: PageTest;

  beforeEach(async () => {
    doiMatKhau = jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiBatBuoc']);
    lamMoiPhien = jasmine.createSpy('lamMoiPhien').and.resolveTo();
    await TestBed.configureTestingModule({
      imports: [DoiMatKhauBatBuocPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ userName: 'an' }), lamMoiPhien },
        },
        { provide: DoiMatKhauService, useValue: doiMatKhau },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/d',
            khongCoQuyen: '/k',
            sauDangNhap: '/',
          },
        },
        { provide: CORE_BRANDING, useValue: { name: 'X', shortName: 'X' } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DoiMatKhauBatBuocPage);
    page = fixture.componentInstance as unknown as PageTest;
    fixture.detectChanges();
    page.form.setValue({ currentPassword: 'cu', newPassword: 'moi', confirmNewPassword: 'moi' });
  });

  for (const fieldErrors of [null, {}]) {
    it(`CHANGE_PASSWORD_FAILED với fieldErrors = ${JSON.stringify(fieldErrors)} → khu lỗi chung mang câu của mã`, async () => {
      doiMatKhau.doiBatBuoc.and.returnValue(
        throwError(() => loiApi('CORE.AUTH.CHANGE_PASSWORD_FAILED', fieldErrors)),
      );

      page.gui();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(page.loiChung()).toBe('msg:CORE.AUTH.CHANGE_PASSWORD_FAILED');
      expect(fixture.nativeElement.textContent).toContain('msg:CORE.AUTH.CHANGE_PASSWORD_FAILED');
    });
  }

  // fe-ui-conventions.md §6.2: mã gốc không nói thay mã con; mã chưa hiện ở ô nào lên khu lỗi của
  // AuthCard, mỗi mã một dòng — khu lỗi giữ ngắt dòng.
  it('CHANGE_PASSWORD_FAILED chỉ có khoá lạ → khu lỗi mang câu của từng MÃ CON, mỗi mã một dòng', async () => {
    doiMatKhau.doiBatBuoc.and.returnValue(
      throwError(() =>
        loiApi('CORE.AUTH.CHANGE_PASSWORD_FAILED', {
          KhoaLa: [
            { code: 'CORE.VALIDATION.REQUIRED', messageParams: null },
            { code: 'CORE.USER.PASSWORD_TOO_SHORT', messageParams: null },
          ],
        }),
      ),
    );

    page.gui();
    fixture.detectChanges();
    await fixture.whenStable();

    const haiDong = 'loi.CORE.VALIDATION.REQUIRED\nloi.CORE.USER.PASSWORD_TOO_SHORT';
    expect(page.loiChung()).toBe(haiDong);
    const khuLoi = (fixture.nativeElement as HTMLElement).querySelector('.auth-card__khu-loi');
    expect(khuLoi?.textContent).toContain(haiDong);
    expect(getComputedStyle(khuLoi as Element).whiteSpace).toBe('pre-line');
  });

  // fe-ui-conventions.md §6.1/§6.2: mọi mã đi qua `applyFormFailure`, không lọc theo danh sách mã.
  it('mã NGOÀI danh sách cũ kèm fieldErrors khoá khớp (NewPassword) → lỗi vào ô, khu lỗi chung trống', async () => {
    doiMatKhau.doiBatBuoc.and.returnValue(
      throwError(() =>
        loiApi('CORE.AUTH.MA_NGOAI_DANH_SACH', {
          NewPassword: [{ code: 'CORE.AUTH.PASSWORD_TOO_SHORT', messageParams: null }],
        }),
      ),
    );

    page.gui();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(page.form.controls.newPassword.errors?.['server']).toEqual([
      'loi.CORE.AUTH.PASSWORD_TOO_SHORT',
    ]);
    expect(page.loiChung()).toBeNull();
  });

  // Nhánh luồng, không phải nhánh hiển thị: cờ đã hạ ở chỗ khác — rời màn như thành công, kể cả
  // khi phản hồi mang `fieldErrors` (không ô nào bị tô, không khu lỗi).
  it('PASSWORD_CHANGE_NOT_REQUIRED → gọi lại me rồi rời màn, không hiện lỗi', async () => {
    const router = TestBed.inject(Router);
    const dieuHuong = spyOn(router, 'navigateByUrl').and.resolveTo(true);
    doiMatKhau.doiBatBuoc.and.returnValue(
      throwError(() =>
        loiApi('CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED', {
          NewPassword: [{ code: 'CORE.AUTH.PASSWORD_TOO_SHORT', messageParams: null }],
        }),
      ),
    );

    page.gui();
    expect(lamMoiPhien).toHaveBeenCalledTimes(1);
    // Page nối `.then` vào CHÍNH promise này trước — chờ nó xong là phần điều hướng đã chạy.
    await lamMoiPhien.calls.mostRecent().returnValue;

    expect(lamMoiPhien).toHaveBeenCalledTimes(1);
    expect(dieuHuong).toHaveBeenCalledOnceWith('/');
    expect(page.loiChung()).toBeNull();
    expect(page.form.controls.newPassword.errors).toBeNull();
  });
});

/**
 * fe-ui-conventions.md §6.3 + Design/Screens/01 "kiểm tra dữ liệu": ô nhập lại phải khớp ô mật khẩu
 * mới, kiểm ở FE; lỗi `mismatch` nằm trên CHÍNH ô nhập lại, câu `VALIDATION_MISMATCH` dưới ô đó.
 * Gõ qua DOM; `AuthField` đánh dấu touched khi rời ô. Không `setValue` sẵn ở `beforeEach`: một ca cần
 * ô mật khẩu mới CHƯA từng đổi giá trị.
 */
describe('DoiMatKhauBatBuocPage — ô nhập lại mật khẩu mới, kiểm khớp ở FE', () => {
  let fixture: ComponentFixture<DoiMatKhauBatBuocPage>;
  let doiMatKhau: jasmine.SpyObj<DoiMatKhauService>;
  let page: PageTest;

  beforeEach(async () => {
    doiMatKhau = jasmine.createSpyObj<DoiMatKhauService>('DoiMatKhauService', ['doiBatBuoc']);
    await TestBed.configureTestingModule({
      imports: [DoiMatKhauBatBuocPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { nguoiDung: signal({ userName: 'an' }), lamMoiPhien: () => Promise.resolve() },
        },
        { provide: DoiMatKhauService, useValue: doiMatKhau },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/d',
            khongCoQuyen: '/k',
            sauDangNhap: '/',
          },
        },
        { provide: CORE_BRANDING, useValue: { name: 'X', shortName: 'X' } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DoiMatKhauBatBuocPage);
    page = fixture.componentInstance as unknown as PageTest;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function veLai(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function o(id: string): HTMLInputElement {
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      `input#${id}`,
    );
    if (!input) throw new Error(`Không thấy ô #${id}`);
    return input;
  }

  /** Gõ rồi rời ô — như người dùng thật. */
  function go(id: string, giaTri: string): void {
    o(id).value = giaTri;
    o(id).dispatchEvent(new Event('input'));
    o(id).dispatchEvent(new Event('blur'));
  }

  function loiNhapLai(): string | undefined {
    return (fixture.nativeElement as HTMLElement)
      .querySelector('#doi-mat-khau-bat-buoc-nhap-lai-error')
      ?.textContent?.trim();
  }

  it('nhập lại khác mật khẩu mới → lỗi không khớp dưới ô nhập lại, viền lỗi ở ô đó; gửi không gọi API', async () => {
    go('doi-mat-khau-bat-buoc-hien-tai', 'cu');
    go('doi-mat-khau-bat-buoc-moi', 'Moi-12345');
    go('doi-mat-khau-bat-buoc-nhap-lai', 'Moi-54321');
    await veLai();

    expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');
    expect(o('doi-mat-khau-bat-buoc-nhap-lai').getAttribute('aria-invalid')).toBe('true');
    expect(o('doi-mat-khau-bat-buoc-moi').getAttribute('aria-invalid')).toBeNull();

    page.gui();
    await veLai();

    expect(doiMatKhau.doiBatBuoc).not.toHaveBeenCalled();
    expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');
  });

  it('sửa ô mật khẩu mới cho khớp → lỗi không khớp biến mất; gửi thì gọi API', async () => {
    doiMatKhau.doiBatBuoc.and.returnValue(new Observable<void>());
    go('doi-mat-khau-bat-buoc-hien-tai', 'cu');
    go('doi-mat-khau-bat-buoc-moi', 'Moi-12345');
    go('doi-mat-khau-bat-buoc-nhap-lai', 'Moi-54321');
    await veLai();
    expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');

    go('doi-mat-khau-bat-buoc-moi', 'Moi-54321');
    await veLai();
    expect(loiNhapLai()).toBeUndefined();

    page.gui();
    expect(doiMatKhau.doiBatBuoc).toHaveBeenCalledOnceWith({
      currentPassword: 'cu',
      newPassword: 'Moi-54321',
    });
  });

  // Luật ba nhánh (Screens/01 "kiểm tra dữ liệu"): ô đã rời mang giá trị không khớp thì hiện lỗi,
  // bất kể người dùng gõ ô nào trước.
  it('gõ ô nhập lại khi ô mật khẩu mới CHƯA từng gõ → rời ô → lỗi không khớp hiện dưới ô nhập lại', async () => {
    go('doi-mat-khau-bat-buoc-nhap-lai', 'Moi-54321');
    await veLai();

    expect(loiNhapLai()).toBe('loi.CORE.CLIENT.VALIDATION_MISMATCH');
  });
});
