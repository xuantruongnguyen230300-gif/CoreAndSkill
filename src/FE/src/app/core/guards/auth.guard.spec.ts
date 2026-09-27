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
import { authGuard } from './auth.guard';
import { AuthService } from '../auth/auth.service';
import { NguoiDungHienTai } from '../auth/nguoi-dung-hien-tai.model';
import { CORE_ROUTES } from '../config/core-routes';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

describe('authGuard', () => {
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
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/doi-mat-khau-bat-buoc',
            khongCoQuyen: '/khong-co-quyen',
            sauDangNhap: '/',
          },
        },
      ],
    });
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('đã đăng nhập → cho qua (true)', () => {
    dat(auth, true);
    const ketQua = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/ho-so' } as never),
    );
    expect(ketQua).toBeTrue();
  });

  it('chưa đăng nhập → UrlTree về /dang-nhap kèm returnUrl bằng chính URL đang gõ', () => {
    dat(auth, false);
    const ketQua = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/quan-tri/nguoi-dung' } as never),
    ) as UrlTree;

    expect(ketQua).toBeInstanceOf(UrlTree);
    const cay = router.createUrlTree(['/dang-nhap'], {
      queryParams: { returnUrl: '/quan-tri/nguoi-dung' },
    });
    expect(router.serializeUrl(ketQua)).toBe(router.serializeUrl(cay));
  });
});

function dat(auth: AuthService, daDangNhap: boolean): void {
  if (!daDangNhap) return;
  const nguoiDung: NguoiDungHienTai = {
    id: 'u-1',
    userName: 'a',
    email: null,
    fullName: 'A',
    roles: [],
    mustChangePassword: false,
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
