import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';
import { TENANT_CODE } from '../../../../core/config/tenant-code';
import { ApiFailure, ApiFailureError } from '../../../../core/http/api-result.model';
import { DangNhapPage } from './dang-nhap.page';

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

const MA_CAU_HINH = 'CAUHINH';
const LOI_TENANT_CODE = {
  TenantCode: [{ code: 'CORE.VALIDATION.MAX_LENGTH', messageParams: { MaxLength: '50' } }],
};

/** ADR-0116: hai chế độ trên một tuyến; chế độ vận hành bật bằng `?cheDo=van-hanh`. */
describe('DangNhapPage — hai chế độ', () => {
  let harness: RouterTestingHarness;
  let fixture: ComponentFixture<unknown>;
  let el: HTMLElement;
  let auth: jasmine.SpyObj<AuthService>;
  let router: Router;

  const o = (id: string): HTMLInputElement | null => el.querySelector(`#${id}`);
  const link = (): HTMLAnchorElement => el.querySelector('a[app-button]') as HTMLAnchorElement;
  const khuLoi = (): string =>
    el.querySelector('.auth-card__khu-loi .notice-banner__than')?.textContent?.trim() ?? '';

  async function oen(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }
  async function vao(url: string): Promise<void> {
    await harness.navigateByUrl(url, DangNhapPage);
    await oen();
  }
  function go(id: string, v: string): void {
    const i = o(id) as HTMLInputElement;
    i.value = v;
    i.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }
  function gui(): void {
    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
  }
  async function nhan(a: HTMLElement): Promise<void> {
    a.click();
    await oen();
    await oen();
  }

  beforeEach(async () => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['dangNhap', 'phaiDoiMatKhau']);
    await TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([{ path: 'dang-nhap', component: DangNhapPage }]),
        { provide: AuthService, useValue: auth },
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
        { provide: TENANT_CODE, useValue: MA_CAU_HINH },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
    fixture = harness.fixture;
    el = fixture.nativeElement as HTMLElement;
  });

  describe('chế độ thường (không tham số)', () => {
    beforeEach(() => vao('/dang-nhap'));

    it('không có ô mã đơn vị, có hai ô còn lại, không có mô tả', () => {
      expect(o('dang-nhap-ma-don-vi')).toBeNull();
      expect(o('dang-nhap-ten-dang-nhap')).not.toBeNull();
      expect(o('dang-nhap-mat-khau')).not.toBeNull();
      expect(el.querySelector('.auth-card__mo-ta')).toBeNull();
    });

    it('focus vào ô tên đăng nhập', () => {
      expect(document.activeElement).toBe(o('dang-nhap-ten-dang-nhap'));
    });

    it('body gửi mang mã cấu hình', () => {
      auth.dangNhap.and.returnValue(new Subject());
      go('dang-nhap-ten-dang-nhap', 'an');
      go('dang-nhap-mat-khau', 'x');
      gui();
      expect(auth.dangNhap).toHaveBeenCalledOnceWith({
        tenantCode: MA_CAU_HINH,
        userName: 'an',
        password: 'x',
      });
    });

    it('INVALID_CREDENTIALS dùng câu của mã (không nhắc mã đơn vị) và không tô ô nào', async () => {
      auth.dangNhap.and.returnValue(
        throwError(() => loiApi('CORE.AUTH.INVALID_CREDENTIALS', null)),
      );
      go('dang-nhap-ten-dang-nhap', 'an');
      go('dang-nhap-mat-khau', 'x');
      gui();
      await oen();
      expect(khuLoi()).toContain('msg:CORE.AUTH.INVALID_CREDENTIALS');
      expect(el.querySelectorAll('[aria-invalid="true"]').length).toBe(0);
    });

    it('fieldErrors.TenantCode → khu lỗi với câu cấu hình, không ô nào bị tô', async () => {
      auth.dangNhap.and.returnValue(
        throwError(() => loiApi('CORE.VALIDATION.FAILED', LOI_TENANT_CODE)),
      );
      go('dang-nhap-ten-dang-nhap', 'an');
      go('dang-nhap-mat-khau', 'x');
      gui();
      await oen();
      expect(khuLoi()).toContain('xacThuc.dangNhap.loiCauHinhMaDonVi');
      expect(khuLoi()).not.toContain('MAX_LENGTH');
      expect(el.querySelectorAll('[aria-invalid="true"]').length).toBe(0);
    });

    it('liên kết là <a> có href, đứng sau nút chính trong thứ tự Tab', () => {
      expect(link().tagName).toBe('A');
      expect(link().getAttribute('href')).toContain('cheDo=van-hanh');
      const nut = el.querySelector('button[type="submit"]') as HTMLElement;
      expect(nut.compareDocumentPosition(link()) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
      expect(link().textContent).toContain('xacThuc.dangNhap.lienKetCheDoVanHanh');
    });

    it('lúc đang gửi: aria-disabled, không href, bấm không đổi URL, ô giữ giá trị và bị khoá', async () => {
      auth.dangNhap.and.returnValue(new Subject());
      go('dang-nhap-ten-dang-nhap', 'an');
      go('dang-nhap-mat-khau', 'x');
      gui();
      fixture.detectChanges();
      expect(link().getAttribute('aria-disabled')).toBe('true');
      expect(link().hasAttribute('href')).toBeFalse();
      expect(o('dang-nhap-ten-dang-nhap')?.disabled).toBeTrue();
      expect(o('dang-nhap-ten-dang-nhap')?.value).toBe('an');
      const truoc = router.url;
      await nhan(link());
      expect(router.url).toBe(truoc);
    });
  });

  describe('chế độ vận hành (?cheDo=van-hanh)', () => {
    beforeEach(() => vao('/dang-nhap?cheDo=van-hanh'));

    it('ba ô, ô mã đơn vị đứng đầu, có mô tả, focus vào ô mã đơn vị', () => {
      const ids = Array.from(el.querySelectorAll('form input')).map((i) => i.id);
      expect(ids).toEqual(['dang-nhap-ma-don-vi', 'dang-nhap-ten-dang-nhap', 'dang-nhap-mat-khau']);
      expect(el.querySelector('.auth-card__mo-ta')?.textContent).toContain(
        'xacThuc.dangNhap.moTaCheDoVanHanh',
      );
      expect(document.activeElement).toBe(o('dang-nhap-ma-don-vi'));
      expect(link().textContent).toContain('xacThuc.dangNhap.lienKetCheDoThuong');
    });

    it('body gửi mang mã gõ tay, không phải mã cấu hình', () => {
      auth.dangNhap.and.returnValue(new Subject());
      go('dang-nhap-ma-don-vi', 'SYS');
      go('dang-nhap-ten-dang-nhap', 'op');
      go('dang-nhap-mat-khau', 'x');
      gui();
      expect(auth.dangNhap).toHaveBeenCalledOnceWith({
        tenantCode: 'SYS',
        userName: 'op',
        password: 'x',
      });
    });

    it('INVALID_CREDENTIALS dùng câu riêng của chế độ vận hành, không tô ô nào', async () => {
      auth.dangNhap.and.returnValue(
        throwError(() => loiApi('CORE.AUTH.INVALID_CREDENTIALS', null)),
      );
      go('dang-nhap-ma-don-vi', 'SYS');
      go('dang-nhap-ten-dang-nhap', 'op');
      go('dang-nhap-mat-khau', 'x');
      gui();
      await oen();
      expect(khuLoi()).toContain('xacThuc.dangNhap.loiSaiThongTinVanHanh');
      expect(el.querySelectorAll('[aria-invalid="true"]').length).toBe(0);
    });

    it('fieldErrors.TenantCode → lỗi dưới ô mã đơn vị', async () => {
      auth.dangNhap.and.returnValue(
        throwError(() => loiApi('CORE.VALIDATION.FAILED', LOI_TENANT_CODE)),
      );
      go('dang-nhap-ma-don-vi', 'SYS');
      go('dang-nhap-ten-dang-nhap', 'op');
      go('dang-nhap-mat-khau', 'x');
      gui();
      await oen();
      expect(o('dang-nhap-ma-don-vi')?.getAttribute('aria-invalid')).toBe('true');
      expect(khuLoi()).toBe('');
    });
  });

  describe('chuyển chế độ', () => {
    it('giữ returnUrl, tên đăng nhập, mật khẩu; ô mã đơn vị trống; xoá lỗi; focus ô đầu còn trống', async () => {
      await vao('/dang-nhap?returnUrl=%2Fho-so');
      auth.dangNhap.and.returnValue(throwError(() => loiApi('CORE.AUTH.LOCKED_OUT', null)));
      go('dang-nhap-ten-dang-nhap', 'an');
      go('dang-nhap-mat-khau', 'x');
      gui();
      await oen();
      expect(khuLoi()).not.toBe('');

      await nhan(link());
      expect(router.url).toContain('returnUrl=%2Fho-so');
      expect(router.url).toContain('cheDo=van-hanh');
      expect(o('dang-nhap-ten-dang-nhap')?.value).toBe('an');
      expect(o('dang-nhap-mat-khau')?.value).toBe('x');
      expect(o('dang-nhap-ma-don-vi')?.value).toBe('');
      expect(khuLoi()).toBe('');
      expect(document.activeElement).toBe(o('dang-nhap-ma-don-vi'));

      // Quay lại chế độ thường: returnUrl còn; không ô nào trống nên focus vào ô đầu tiên.
      go('dang-nhap-ma-don-vi', 'SYS');
      await nhan(link());
      expect(router.url).toBe('/dang-nhap?returnUrl=%2Fho-so');
      expect(o('dang-nhap-ma-don-vi')).toBeNull();
      expect(document.activeElement).toBe(o('dang-nhap-ten-dang-nhap'));
      expect(o('dang-nhap-mat-khau')?.value).toBe('x');
    });
  });
});
