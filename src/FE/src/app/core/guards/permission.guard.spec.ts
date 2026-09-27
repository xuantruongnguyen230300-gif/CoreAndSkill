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
import { permissionGuard } from './permission.guard';
import { AuthService } from '../auth/auth.service';
import { NguoiDungHienTai } from '../auth/nguoi-dung-hien-tai.model';
import { CORE_ROUTES } from '../config/core-routes';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

describe('permissionGuard', () => {
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

  it('có đủ mọi quyền yêu cầu → cho qua', () => {
    dat(auth, ['core.user.read', 'core.user.write']);
    const guard = permissionGuard('core.user.read', 'core.user.write');
    const kq = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
    expect(kq).toBeTrue();
  });

  it('thiếu MỘT trong nhiều quyền yêu cầu (VÀ, không phải HOẶC) → về /khong-co-quyen', () => {
    dat(auth, ['core.user.read']);
    const guard = permissionGuard('core.user.read', 'core.user.write');
    const kq = TestBed.runInInjectionContext(() => guard({} as never, {} as never)) as UrlTree;
    expect(router.serializeUrl(kq)).toBe(
      router.serializeUrl(router.createUrlTree(['/khong-co-quyen'])),
    );
  });

  it('không có quyền nào → về /khong-co-quyen, KHÔNG về trang chủ', () => {
    dat(auth, []);
    const guard = permissionGuard('core.user.read');
    const kq = TestBed.runInInjectionContext(() => guard({} as never, {} as never)) as UrlTree;
    expect(router.serializeUrl(kq)).toBe(
      router.serializeUrl(router.createUrlTree(['/khong-co-quyen'])),
    );
  });
});

function dat(auth: AuthService, quyen: readonly string[]): void {
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
    quyen,
  };
  (auth as unknown as { _nguoiDung: { set: (v: NguoiDungHienTai) => void } })['_nguoiDung'].set(
    nguoiDung,
  );
}
