import localeVi from '@angular/common/locales/vi';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
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
import { XsrfTokenStore } from './xsrf-token.store';
import { MenuStore } from '../menu/menu.store';
import { errorInterceptor } from '../interceptors/error.interceptor';
import { CORE_ROUTES } from '../config/core-routes';
import { PhienDto } from './phien.dto';
import { NguoiDungHienTai } from './nguoi-dung-hien-tai.model';
import { sangNguoiDungHienTai } from './phien.mapper';
import { KHOA_BAO_TAB_PHIEN_KET_THUC } from './tab-session-broadcast.service';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const PHIEN_DTO: PhienDto = {
  id: 'u-1',
  userName: 'an.nv',
  email: 'an.nv@vd.vn',
  fullName: 'Nguyễn Văn An',
  roles: ['Quản trị hệ thống'],
  permissions: ['core.user.read', 'core.user.write'],
  mustChangePassword: false,
  isSystemOperator: false,
  sessionMinutes: 30,
  preferredLanguage: 'vi',
  tenantCode: 'SO-GD',
  tenantName: 'Sở Giáo dục và Đào tạo',
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let xsrf: XsrfTokenStore;
  let menu: MenuStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        // Chuỗi thật (không chỉ HttpClient trần) để 401 của napPhienKhoiDong/dangXuat đi đúng
        // nhánh BO_QUA_HET_PHIEN của errorInterceptor — đúng hệ đang chạy thật, không mô phỏng tay.
        provideHttpClient(withInterceptors([errorInterceptor])),
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
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    xsrf = TestBed.inject(XsrfTokenStore);
    menu = TestBed.inject(MenuStore);
  });

  afterEach(() => httpMock.verify());

  it('daDangNhap() và coQuyen() mặc định false/không có quyền nào', () => {
    expect(service.daDangNhap()).toBeFalse();
    expect(service.coQuyen('core.user.read')).toBeFalse();
  });

  describe('napPhienKhoiDong() — lời gọi me DUY NHẤT lúc daDangNhap() còn false', () => {
    it('me trả 200 → nạp người dùng, quyền đọc được từ tập permissions', (done) => {
      service.napPhienKhoiDong().subscribe(() => {
        expect(service.daDangNhap()).toBeTrue();
        expect(service.nguoiDung()?.fullName).toBe('Nguyễn Văn An');
        expect(service.coQuyen('core.user.write')).toBeTrue();
        expect(service.coQuyen('core.user.delete')).toBeFalse();
        done();
      });

      httpMock
        .expectOne('/core/auth/me')
        .flush({ success: true, data: PHIEN_DTO, error: null, traceId: 't-me' });
    });

    it('me trả 401 → giữ null, KHÔNG lỗi ra ngoài (401 là trạng thái bình thường lúc khởi động)', (done) => {
      service.napPhienKhoiDong().subscribe({
        next: () => {
          expect(service.daDangNhap()).toBeFalse();
          done();
        },
        error: () => fail('napPhienKhoiDong() không được lỗi khi me trả 401'),
      });

      httpMock.expectOne('/core/auth/me').flush(
        {
          success: false,
          data: null,
          error: {
            code: 'CORE.AUTH.NOT_AUTHENTICATED',
            type: 'Unauthorized',
            message: 'x',
            messageParams: null,
            fieldErrors: null,
          },
          traceId: 't-401',
        },
        { status: 401, statusText: 'Unauthorized' },
      );
    });

    it('me trả lỗi khác 401 → ném tiếp cho nơi gọi (app.config.ts tự bọc catchError)', (done) => {
      service.napPhienKhoiDong().subscribe({
        next: () => fail('phải lỗi, không phải next'),
        error: (err: unknown) => {
          expect(err).toBeTruthy();
          expect(service.daDangNhap()).toBeFalse();
          done();
        },
      });

      httpMock.expectOne('/core/auth/me').flush(null, { status: 500, statusText: 'Server Error' });
    });
  });

  describe('dangNhap()', () => {
    it('login 200 → nạp người dùng TỪ RESPONSE (không gọi lại me), rồi lấy lại token XSRF', () => {
      let xong = false;
      service
        .dangNhap({ tenantCode: 'SO-GD', userName: 'an.nv', password: '***' })
        .subscribe(() => {
          xong = true;
        });

      // Cả pipeline chạy đồng bộ trên HttpClientTesting — không cần done()/fakeAsync.
      httpMock
        .expectOne('/core/auth/login')
        .flush({ success: true, data: PHIEN_DTO, error: null, traceId: 't-login' });
      httpMock.expectNone('/core/auth/me');

      httpMock
        .expectOne('/core/antiforgery/token')
        .flush({ success: true, data: { token: 'token-moi' }, error: null, traceId: 't-xsrf' });

      expect(xong).toBeTrue();
      expect(service.daDangNhap()).toBeTrue();
      expect(xsrf.token()).toBe('token-moi');
    });
  });

  describe('dangXuat()', () => {
    beforeEach(() => localStorage.removeItem(KHOA_BAO_TAB_PHIEN_KET_THUC));
    afterEach(() => localStorage.removeItem(KHOA_BAO_TAB_PHIEN_KET_THUC));

    it('logout 200 → dọn phiên, lấy lại token XSRF, VÀ báo các tab khác qua localStorage', () => {
      xacLapPhienGia(service);
      let xong = false;
      service.dangXuat().subscribe(() => (xong = true));

      httpMock
        .expectOne('/core/auth/logout')
        .flush({ success: true, data: null, error: null, traceId: 't-out' });
      expect(service.daDangNhap()).toBeFalse();
      expect(localStorage.getItem(KHOA_BAO_TAB_PHIEN_KET_THUC)).not.toBeNull();
      // Token mới phải về TRƯỚC khi nơi gọi được báo xong (rồi mới điều hướng) — fe-api-client.md §2.1.
      expect(xong).withContext('chưa có token mới thì chưa được báo xong').toBeFalse();

      httpMock
        .expectOne('/core/antiforgery/token')
        .flush({ success: true, data: { token: 't2' }, error: null, traceId: 't-xsrf2' });
      expect(xong).toBeTrue();
      expect(xsrf.token()).toBe('t2');
    });

    it('logout 200 nhưng lấy token XSRF lỗi → VẪN đăng xuất xong, không ném lỗi ra nơi gọi', () => {
      xacLapPhienGia(service);
      let xong = false;
      let loi = false;
      service.dangXuat().subscribe({ next: () => (xong = true), error: () => (loi = true) });

      httpMock
        .expectOne('/core/auth/logout')
        .flush({ success: true, data: null, error: null, traceId: 't-out' });
      httpMock
        .expectOne('/core/antiforgery/token')
        .flush(null, { status: 500, statusText: 'Server Error' });

      expect(loi).toBeFalse();
      expect(xong).toBeTrue();
      expect(service.daDangNhap()).toBeFalse();
    });

    it('logout 401 → VẪN coi là đã đăng xuất (phiên có thể đã hết trước khi bấm)', () => {
      xacLapPhienGia(service);
      let loi = false;
      let xong = false;
      service.dangXuat().subscribe({ next: () => (xong = true), error: () => (loi = true) });

      httpMock.expectOne('/core/auth/logout').flush(
        {
          success: false,
          data: null,
          error: {
            code: 'CORE.AUTH.NOT_AUTHENTICATED',
            type: 'Unauthorized',
            message: 'x',
            messageParams: null,
            fieldErrors: null,
          },
          traceId: 't-401',
        },
        { status: 401, statusText: 'Unauthorized' },
      );

      expect(loi).toBeFalse();
      expect(service.daDangNhap()).toBeFalse();
      expect(xong).toBeFalse();
      httpMock
        .expectOne('/core/antiforgery/token')
        .flush({ success: true, data: { token: 't3' }, error: null, traceId: 't-xsrf3' });
      expect(xong).toBeTrue();
    });

    it('logout hỏng kiểu khác (500) → KHÔNG dọn trạng thái, lỗi ném ra để màn giữ nguyên', () => {
      xacLapPhienGia(service);
      let loi = false;
      service.dangXuat().subscribe({ next: () => fail('phải lỗi'), error: () => (loi = true) });

      httpMock
        .expectOne('/core/auth/logout')
        .flush(null, { status: 500, statusText: 'Server Error' });

      expect(loi).toBeTrue();
      expect(service.daDangNhap()).toBeTrue();
    });
  });

  describe('lamMoiQuyen() — 403 CORE.AUTH.FORBIDDEN', () => {
    it('gọi lại me, thay tập quyền, VÀ nạp lại menu BỎ QUA CACHE trong cùng bước', () => {
      xacLapPhienGia(service);
      // userId không đổi giữa hai lần — phải qua thuLai() (xoá cache rồi mới nạp), KHÔNG
      // phải lamMoi() thẳng, nếu không menu cũ trong cache sẽ được trả lại nguyên (07-auth-identity.md §5.4).
      const thuLaiMenuSpy = spyOn(menu, 'thuLai');
      service.lamMoiQuyen();

      const req = httpMock.expectOne('/core/auth/me');
      req.flush({
        success: true,
        data: { ...PHIEN_DTO, permissions: ['core.user.read'] },
        error: null,
        traceId: 't-refresh',
      });

      expect(service.coQuyen('core.user.read')).toBeTrue();
      expect(service.coQuyen('core.user.write')).toBeFalse();
      expect(thuLaiMenuSpy).toHaveBeenCalledWith('u-1');
    });
  });

  describe('lamMoiPhien()', () => {
    it('gọi lại me và KHÔNG nạp lại menu (không phải nhánh lamMoiQuyen)', async () => {
      xacLapPhienGia(service);
      const lamMoiMenuSpy = spyOn(menu, 'lamMoi');

      const promise = service.lamMoiPhien();
      httpMock
        .expectOne('/core/auth/me')
        .flush({ success: true, data: PHIEN_DTO, error: null, traceId: 't' });
      await promise;

      expect(lamMoiMenuSpy).not.toHaveBeenCalled();
    });

    it('napLaiMenu: true → gọi lại me, rồi nạp lại menu BỎ QUA CACHE', async () => {
      xacLapPhienGia(service);
      const thuLaiMenuSpy = spyOn(menu, 'thuLai');

      const promise = service.lamMoiPhien({ napLaiMenu: true });
      httpMock
        .expectOne('/core/auth/me')
        .flush({ success: true, data: PHIEN_DTO, error: null, traceId: 't' });
      await promise;

      expect(thuLaiMenuSpy).toHaveBeenCalledOnceWith('u-1');
    });

    it('napLaiMenu: true khi đang có lời gọi me cũ → HUỶ lời gọi cũ, gửi lại, menu nạp theo lời gọi mới', async () => {
      xacLapPhienGia(service);
      const thuLaiMenuSpy = spyOn(menu, 'thuLai');
      void service.lamMoiPhien();
      const cu = httpMock.expectOne('/core/auth/me');

      const promise = service.lamMoiPhien({ napLaiMenu: true });

      expect(cu.cancelled).toBeTrue();
      httpMock
        .expectOne('/core/auth/me')
        .flush({ success: true, data: PHIEN_DTO, error: null, traceId: 't' });
      await promise;
      expect(thuLaiMenuSpy).toHaveBeenCalledOnceWith('u-1');
    });

    it('mustChangePassword = true → điều hướng lại URL hiện tại để guard chạy lại', async () => {
      xacLapPhienGia(service);
      const router = TestBed.inject(Router);
      const navSpy = spyOn(router, 'navigateByUrl');

      const promise = service.lamMoiPhien();
      httpMock.expectOne('/core/auth/me').flush({
        success: true,
        data: { ...PHIEN_DTO, mustChangePassword: true },
        error: null,
        traceId: 't',
      });
      await promise;

      expect(navSpy).toHaveBeenCalledWith(router.url, { onSameUrlNavigation: 'reload' });
    });
  });

  it('donPhien() xoá người dùng VÀ dọn menu', () => {
    xacLapPhienGia(service);
    const donMenuSpy = spyOn(menu, 'donPhien');

    service.donPhien();

    expect(service.daDangNhap()).toBeFalse();
    expect(donMenuSpy).toHaveBeenCalled();
  });
});

function xacLapPhienGia(service: AuthService): void {
  (service as unknown as { _nguoiDung: { set: (v: NguoiDungHienTai) => void } })['_nguoiDung'].set(
    sangNguoiDungHienTai(PHIEN_DTO),
  );
}
