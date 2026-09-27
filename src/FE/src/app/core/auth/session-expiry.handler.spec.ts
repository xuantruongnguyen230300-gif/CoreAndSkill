import localeVi from '@angular/common/locales/vi';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { provideCoreI18n } from '../config/core-i18n';
import { AuthService } from './auth.service';
import { SessionExpiryHandler } from './session-expiry.handler';
import { NguoiDungHienTai } from './nguoi-dung-hien-tai.model';
import { KHOA_BAO_TAB_PHIEN_KET_THUC } from './tab-session-broadcast.service';
import { CORE_ROUTES } from '../config/core-routes';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

describe('SessionExpiryHandler', () => {
  let handler: SessionExpiryHandler;
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
    handler = TestBed.inject(SessionExpiryHandler);
  });

  it('handle() không làm gì khi chưa đăng nhập — chặn 401 đến sau khi phiên đã dọn rồi', () => {
    const navigateSpy = spyOn(router, 'navigate');
    handler.handle();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('handle() dọn phiên và điều hướng về màn đăng nhập kèm returnUrl khi đang đăng nhập', () => {
    dangNhapGia(auth);

    const navigateSpy = spyOn(router, 'navigate');
    handler.handle();

    expect(auth.daDangNhap()).toBeFalse();
    expect(navigateSpy).toHaveBeenCalledWith(
      ['/dang-nhap'],
      jasmine.objectContaining({ queryParams: jasmine.any(Object) }),
    );
  });

  it('gọi handle() hai lần liên tiếp chỉ điều hướng một lần — lần hai bị chặn vì phiên đã dọn', () => {
    dangNhapGia(auth);
    const navigateSpy = spyOn(router, 'navigate');

    handler.handle();
    handler.handle();

    expect(navigateSpy).toHaveBeenCalledTimes(1);
  });

  it('tab KHÁC ghi khoá bao-tab (đăng xuất chủ động hoặc 401 ở tab đó) → tab này tự dọn, KHÔNG ghi lại (chặn vòng lặp)', () => {
    dangNhapGia(auth);
    const navigateSpy = spyOn(router, 'navigate');
    const setItemSpy = spyOn(localStorage, 'setItem').and.callThrough();

    window.dispatchEvent(
      new StorageEvent('storage', {
        key: KHOA_BAO_TAB_PHIEN_KET_THUC,
        newValue: String(Date.now()),
      }),
    );

    expect(auth.daDangNhap()).toBeFalse();
    expect(navigateSpy).toHaveBeenCalledTimes(1);
    expect(setItemSpy).not.toHaveBeenCalled();
  });
});

const NGUOI_DUNG_GIA: NguoiDungHienTai = {
  id: 'u-1',
  userName: 'an.nv',
  email: null,
  fullName: 'Nguyễn Văn An',
  roles: [],
  mustChangePassword: false,
  isSystemOperator: false,
  sessionMinutes: 30,
  preferredLanguage: 'vi',
  tenantCode: 'SO-GD',
  tenantName: 'Sở Giáo dục',
  quyen: [],
};

/** Đặt trực tiếp signal nội bộ — AuthService F2 không có seam nào khác để ép trạng thái trong test. */
function dangNhapGia(auth: AuthService): void {
  (auth as unknown as { _nguoiDung: { set: (v: NguoiDungHienTai) => void } })['_nguoiDung'].set(
    NGUOI_DUNG_GIA,
  );
}
