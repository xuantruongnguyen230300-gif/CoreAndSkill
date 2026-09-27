import localeVi from '@angular/common/locales/vi';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { provideCoreI18n } from '../config/core-i18n';
import { CORE_ROUTES } from '../config/core-routes';
import { errorInterceptor } from '../interceptors/error.interceptor';
import { ToastService } from '../toast/toast.service';
import { MenuStore } from './menu.store';
import { MenuItemDto } from './menu.dto';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

const PHANG: readonly MenuItemDto[] = [
  {
    id: '1',
    parentId: null,
    code: 'trang-chu',
    labelKey: 'menu.trang-chu',
    icon: 'pi-home',
    route: '/trang-chu',
    displayOrder: 10,
  },
];

const PHANG_HAI_MUC: readonly MenuItemDto[] = [
  ...PHANG,
  {
    id: '2',
    parentId: null,
    code: 'quan-tri',
    labelKey: 'menu.quan-tri',
    icon: 'pi-cog',
    route: null,
    displayOrder: 90,
  },
];

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

describe('MenuStore', () => {
  let store: MenuStore;
  let httpMock: HttpTestingController;
  let toast: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        // errorInterceptor THẬT — để khẳng định request menu không bắn toast chung (Design/Screens/00, "Tải menu hỏng").
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
            doiMatKhauBatBuoc: '/doi',
            khongCoQuyen: '/403',
            sauDangNhap: '/',
          },
        },
      ],
    });
    store = TestBed.inject(MenuStore);
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
  });

  afterEach(() => httpMock.verify());

  it('trạng thái ban đầu: idle, không có mục nào', () => {
    expect(store.state()).toBe('idle');
    expect(store.items()).toEqual([]);
  });

  it('lamMoi() thành công → dựng cây, state idle', () => {
    store.lamMoi('u-1');

    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-1' });

    expect(store.state()).toBe('idle');
    expect(store.items().length).toBe(1);
    expect(store.items()[0].code).toBe('trang-chu');
  });

  // 500 thuộc lớp lỗi xuyên suốt: interceptor toast kèm traceId dù request menu mang
  // BO_QUA_TOAST_LOI — `Sidebar` chỉ hiện trạng thái `error` + Thử lại, không mang traceId.
  it('lamMoi() lỗi 500 → state error, KHÔNG có mục nào (không menu giả); toast lỗi hệ thống kèm traceId', () => {
    store.lamMoi('u-1');

    httpMock
      .expectOne('/core/meta/menu')
      .flush(LOI_500, { status: 500, statusText: 'Server Error' });

    expect(store.state()).toBe('error');
    expect(store.items()).toEqual([]);
    expect(toast.latest()?.traceId).toBe('t-loi');
  });

  it('lamMoi() mất kết nối → state error, không toast', () => {
    store.lamMoi('u-1');

    httpMock.expectOne('/core/meta/menu').error(new ProgressEvent('error'), { status: 0 });

    expect(store.state()).toBe('error');
    expect(store.items()).toEqual([]);
    expect(toast.latest()).toBeNull();
  });

  it('lỗi KHÔNG được ghi vào cache — lamMoi() lần sau gọi lại API', () => {
    store.lamMoi('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush(LOI_500, { status: 500, statusText: 'Server Error' });

    store.lamMoi('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-2' });

    expect(store.state()).toBe('idle');
    expect(store.items().length).toBe(1);
  });

  it('thuLai() sau lỗi → tải lại, về idle kèm cây menu', () => {
    store.lamMoi('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush(LOI_500, { status: 500, statusText: 'Server Error' });
    expect(store.state()).toBe('error');

    store.thuLai('u-1');
    expect(store.state()).toBe('loading');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-2' });

    expect(store.state()).toBe('idle');
    expect(store.items().length).toBe(1);
  });

  it('gọi lamMoi() lần hai với CÙNG người dùng → đọc từ cache, KHÔNG gọi lại API', () => {
    store.lamMoi('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-1' });

    store.lamMoi('u-1');
    httpMock.expectNone('/core/meta/menu');
    expect(store.items().length).toBe(1);
  });

  it('lamMoi() với NGƯỜI DÙNG KHÁC → khoá cache khác, gọi lại API', () => {
    store.lamMoi('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-1' });

    store.lamMoi('u-2');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: [], error: null, traceId: 't-2' });

    expect(store.items().length).toBe(0);
    expect(store.state()).toBe('empty');
  });

  it('donPhien() dọn items và state', () => {
    store.lamMoi('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-1' });

    store.donPhien();

    expect(store.items()).toEqual([]);
    expect(store.state()).toBe('idle');
  });

  it('donPhien() xoá CẢ cache — A đăng xuất rồi đăng nhập lại cùng tab thì menu tải mới, không hiện mục của quyền đã thu hồi', () => {
    // Phiên 1: A có hai mục.
    store.lamMoi('u-a');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG_HAI_MUC, error: null, traceId: 't-1' });
    expect(store.items().length).toBe(2);

    // A đăng xuất; quản trị thu hồi quyền; A đăng nhập lại trên cùng tab.
    store.donPhien();
    store.lamMoi('u-a');

    // Phải đi server — cache của phiên trước không được sống sót qua donPhien().
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-2' });
    expect(store.items().length).toBe(1);
    expect(store.items().map((m) => m.code)).toEqual(['trang-chu']);
  });

  it('thuLai() xoá cache của đúng người dùng rồi gọi lại API', () => {
    store.lamMoi('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: PHANG, error: null, traceId: 't-1' });

    store.thuLai('u-1');
    httpMock
      .expectOne('/core/meta/menu')
      .flush({ success: true, data: [], error: null, traceId: 't-2' });

    expect(store.items().length).toBe(0);
  });
});

/**
 * Khoá cache gồm người dùng + ngôn ngữ (07-auth-identity.md §6). Khi TranslateService chưa có ngôn
 * ngữ nào, phần ngôn ngữ phải là `CORE_I18N.defaultLanguage` của dự án — không phải một mã viết cứng.
 */
describe('MenuStore — ngôn ngữ trong khoá cache đến từ CORE_I18N', () => {
  it('TranslateService chưa có ngôn ngữ → khoá dùng CORE_I18N.defaultLanguage', () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService({ loader: provideTranslateLoader(FakeTranslateLoader) }),
        provideCoreI18n({
          languages: [{ code: 'xx', nativeName: 'XX', localeData: localeVi }],
          defaultLanguage: 'xx',
          sources: ['/i18n/'],
        }),
      ],
    });
    const store = TestBed.inject(MenuStore) as unknown as { khoaCache(userId: string): string };
    expect(store.khoaCache('u-1')).toBe('u-1:xx');
  });
});
