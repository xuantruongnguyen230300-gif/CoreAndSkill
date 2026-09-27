import localeVi from '@angular/common/locales/vi';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router, provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';

import { provideCoreI18n } from '../../core/config/core-i18n';
import { AuthService } from '../../core/auth/auth.service';
import { CORE_BRANDING } from '../../core/config/core-branding';
import { CORE_ROUTES } from '../../core/config/core-routes';
import { ApiFailureError } from '../../core/http/api-result.model';
import { errorInterceptor } from '../../core/interceptors/error.interceptor';
import { MenuStore } from '../../core/menu/menu.store';
import { ToastService } from '../../core/toast/toast.service';
import { UnsavedChangesService } from '../../core/unsaved-changes/unsaved-changes.service';
import { SidebarComponent } from '../../shared/components/sidebar/sidebar.component';
import { TopbarComponent } from '../../shared/components/topbar/topbar.component';
import { ShellComponent } from './shell.component';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const LOI_500 = {
  success: false,
  data: null,
  error: {
    code: 'CORE.INTERNAL',
    type: 'internal',
    message: 'x',
    messageParams: null,
    fieldErrors: null,
  },
  traceId: 't-loi',
};

/** Chỉ phần `ShellComponent` đọc — phiên đã có người dùng (authGuard đã qua). */
const authGia = {
  nguoiDung: signal({ id: 'u-1', fullName: 'A', userName: 'a', email: 'a@x', tenantName: 'T' }),
  dangXuat: () => of(undefined),
};

/** Tệp dịch về MUỘN — khung đã dựng xong trước khi bảng dịch có mặt (V-01). */
const tepDichCham = new Subject<TranslationObject>();

class LoaderCham extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return tepDichCham;
  }
}

@Component({ selector: 'app-trong', template: '' })
class TrongComponent {}

describe('ShellComponent — bảng dịch về muộn (V-01)', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'a', title: 'trang.a', component: TrongComponent }]),
        provideCoreI18n({
          languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
          defaultLanguage: 'vi',
          sources: ['/i18n/'],
        }),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(LoaderCham),
        }),
        { provide: AuthService, useValue: authGia },
        { provide: CORE_BRANDING, useValue: { name: 'X', shortName: 'X' } },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/doi',
            khongCoQuyen: '/403',
            sauDangNhap: '/',
          },
        },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('nút tài khoản, tiêu đề tuyến và nhãn menu đổi sang câu đã dịch khi bảng dịch về', async () => {
    await TestBed.inject(Router).navigateByUrl('/a');
    const fixture = TestBed.createComponent(ShellComponent);
    httpMock.expectOne('/core/meta/menu').flush({
      success: true,
      data: [
        {
          id: '1',
          parentId: null,
          code: 'trang-chu',
          labelKey: 'menu.trangChu',
          icon: null,
          route: '/trang-chu',
          displayOrder: 10,
        },
      ],
      error: null,
      traceId: 't-1',
    });
    fixture.detectChanges();
    await fixture.whenStable();

    tepDichCham.next({
      hoSo: { tieuDe: 'Hồ sơ' },
      xacThuc: { dangXuat: 'Đăng xuất' },
      trang: { a: 'Trang A' },
      menu: { trangChu: 'Trang chủ' },
    });
    tepDichCham.complete();
    fixture.detectChanges();
    await fixture.whenStable();

    const topbar = fixture.debugElement.query(By.directive(TopbarComponent))
      .componentInstance as TopbarComponent;
    const sidebar = fixture.debugElement.query(By.directive(SidebarComponent))
      .componentInstance as SidebarComponent;
    expect(topbar.menuItems().map((m) => m.label)).toEqual(['Hồ sơ', 'Đăng xuất']);
    // Design/Icons.md §5: input `icon` nhận TÊN TRẦN — shell là vế "nơi gọi" của luật đó, còn
    // topbar là vế ghép tiền tố. Truyền `pi-user` ở đây thì DOM ra `pi pi-pi-user`, không vẽ gì.
    expect(topbar.menuItems().map((m) => m.icon)).toEqual(['user', 'sign-out']);
    const iconHoSo = (fixture.nativeElement as HTMLElement).querySelector('.topbar__muc i');
    expect(iconHoSo?.classList.contains('pi-user')).toBeTrue();
    expect(iconHoSo?.classList.contains('pi-pi-user')).toBeFalse();
    expect(topbar.title()).toBe('Trang A');
    expect(sidebar.items().map((m) => m.label)).toEqual(['Trang chủ']);
  });
});

describe('ShellComponent — menu', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideZonelessChangeDetection(),
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
        { provide: AuthService, useValue: authGia },
        { provide: CORE_BRANDING, useValue: { name: 'X', shortName: 'X' } },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/doi',
            khongCoQuyen: '/403',
            sauDangNhap: '/',
          },
        },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // 500 thuộc lớp lỗi xuyên suốt: toast kèm traceId dù request menu mang BO_QUA_TOAST_LOI;
  // `Sidebar` vẫn tự hiện trạng thái `error` + Thử lại.
  it('menu lỗi 500 → Sidebar ở trạng thái error, bấm Thử lại thì tải lại menu; toast kèm traceId', async () => {
    const fixture = TestBed.createComponent(ShellComponent);
    const menu = TestBed.inject(MenuStore);
    const toast = TestBed.inject(ToastService);

    httpMock
      .expectOne('/core/meta/menu')
      .flush(LOI_500, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(menu.state()).toBe('error');
    expect(toast.latest()?.traceId).toBe('t-loi');

    const nutThuLai = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      'app-sidebar button',
    );
    expect(nutThuLai)
      .withContext('Sidebar phải hiện nút Thử lại ở trạng thái error')
      .not.toBeNull();
    nutThuLai?.click();

    const req = httpMock.expectOne('/core/meta/menu');
    req.flush({
      success: true,
      data: [
        {
          id: '1',
          parentId: null,
          code: 'trang-chu',
          labelKey: 'menu.trang-chu',
          icon: null,
          route: '/trang-chu',
          displayOrder: 10,
        },
      ],
      error: null,
      traceId: 't-2',
    });

    expect(menu.state()).toBe('idle');
    expect(menu.items().length).toBe(1);
  });
});

/**
 * Design/Screens/00-khung-ung-dung.md, bảng "Người dùng chọn → Khung làm gì" và §Trạng thái: đăng
 * xuất hỏng → ở lại màn, thử lại được, KHÔNG dọn trạng thái — kể cả lớp bảo vệ thay đổi chưa lưu mà
 * người dùng vừa đồng ý bỏ cho một lần rời trang RỒI KHÔNG XẢY RA (fe-routing-guard.md §4.1).
 */
describe('ShellComponent — đăng xuất hỏng sau khi đã chọn "Rời đi"', () => {
  let httpMock: HttpTestingController;
  const dangXuat = jasmine.createSpy('dangXuat');

  beforeEach(() => {
    dangXuat.calls.reset();
    dangXuat.and.returnValue(throwError(() => new ApiFailureError(null, 0)));
    TestBed.configureTestingModule({
      imports: [ShellComponent],
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
        { provide: AuthService, useValue: { nguoiDung: authGia.nguoiDung, dangXuat } },
        { provide: CORE_BRANDING, useValue: { name: 'X', shortName: 'X' } },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/doi',
            khongCoQuyen: '/403',
            sauDangNhap: '/',
          },
        },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('đăng ký thay đổi chưa lưu còn sống, không điều hướng; bấm đăng xuất lại thì HỎI lại', async () => {
    const thayDoi = TestBed.inject(UnsavedChangesService);
    thayDoi.dangKy(() => true);
    const fixture = TestBed.createComponent(ShellComponent);
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: [], error: null, traceId: 't' });
    const navigateSpy = spyOn(TestBed.inject(Router), 'navigate');
    const shell = fixture.componentInstance as unknown as { khiChonMucTaiKhoan(k: string): void };

    shell.khiChonMucTaiKhoan('dang-xuat');
    expect(thayDoi.dangHoi()).toBeTrue();
    thayDoi.xacNhanRoiDi();
    await fixture.whenStable();

    expect(dangXuat).toHaveBeenCalledTimes(1);
    expect(navigateSpy).not.toHaveBeenCalled();
    expect(thayDoi.coThayDoiChuaLuu()).toBeTrue();

    shell.khiChonMucTaiKhoan('dang-xuat');
    expect(thayDoi.dangHoi()).withContext('lần thử lại vẫn phải hỏi').toBeTrue();
    thayDoi.huy();
  });
});
