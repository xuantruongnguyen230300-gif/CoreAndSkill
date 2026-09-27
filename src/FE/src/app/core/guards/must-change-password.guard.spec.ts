import localeVi from '@angular/common/locales/vi';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { provideCoreI18n } from '../config/core-i18n';
import { mustChangePasswordGuard } from './must-change-password.guard';
import { AuthService } from '../auth/auth.service';
import { NguoiDungHienTai } from '../auth/nguoi-dung-hien-tai.model';
import { CORE_ROUTES } from '../config/core-routes';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const CORE_ROUTES_GIA = {
  dangNhap: '/dang-nhap',
  doiMatKhauBatBuoc: '/doi-mat-khau-bat-buoc',
  khongCoQuyen: '/khong-co-quyen',
  sauDangNhap: '/trang-chu',
};

describe('mustChangePasswordGuard', () => {
  let auth: AuthService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideCoreI18n({
          languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
          defaultLanguage: 'vi',
          sources: ['/i18n/'],
        }),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
        { provide: CORE_ROUTES, useValue: CORE_ROUTES_GIA },
      ],
    });
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('KHÔNG bị ép đổi + gõ URL bình thường → cho qua', () => {
    dat(auth, false);
    const kq = TestBed.runInInjectionContext(() =>
      mustChangePasswordGuard({} as never, { url: '/ho-so' } as never),
    );
    expect(kq).toBeTrue();
  });

  it('KHÔNG bị ép đổi + tự gõ /doi-mat-khau-bat-buoc → đẩy về đích sau đăng nhập', () => {
    dat(auth, false);
    const kq = TestBed.runInInjectionContext(() =>
      mustChangePasswordGuard({} as never, { url: '/doi-mat-khau-bat-buoc' } as never),
    ) as UrlTree;
    expect(router.serializeUrl(kq)).toBe(router.serializeUrl(router.createUrlTree(['/trang-chu'])));
  });

  it('BỊ ép đổi + gõ URL bất kỳ → đẩy về màn đổi mật khẩu bắt buộc', () => {
    dat(auth, true);
    const kq = TestBed.runInInjectionContext(() =>
      mustChangePasswordGuard({} as never, { url: '/quan-tri/nguoi-dung' } as never),
    ) as UrlTree;
    expect(router.serializeUrl(kq)).toBe(
      router.serializeUrl(router.createUrlTree(['/doi-mat-khau-bat-buoc'])),
    );
  });

  it('BỊ ép đổi + ĐANG ở chính màn đổi mật khẩu → cho qua (KHÔNG lặp vô hạn)', () => {
    dat(auth, true);
    const kq = TestBed.runInInjectionContext(() =>
      mustChangePasswordGuard({} as never, { url: '/doi-mat-khau-bat-buoc' } as never),
    );
    expect(kq).toBeTrue();
  });
});

function dat(auth: AuthService, phaiDoiMatKhau: boolean): void {
  const nguoiDung: NguoiDungHienTai = {
    id: 'u-1',
    userName: 'a',
    email: null,
    fullName: 'A',
    roles: [],
    mustChangePassword: phaiDoiMatKhau,
    isSystemOperator: false,
    sessionMinutes: 30,
    preferredLanguage: 'vi',
    tenantCode: 'T',
    tenantName: 'T',
    quyen: [],
  };
  (auth as unknown as { _nguoiDung: { set: (v: NguoiDungHienTai) => void } })['_nguoiDung'].set(
    nguoiDung,
  );
}
