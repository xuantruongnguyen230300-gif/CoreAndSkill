import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  provideRouter,
  Router,
  Routes,
  withNavigationErrorHandler,
  NavigationError,
} from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

import { ToastService } from '../toast/toast.service';
import { laLoiTaiPhanMa, LoiTaiPhanMaService } from './loi-tai-phan-ma';

describe('laLoiTaiPhanMa — mỗi mẫu câu lỗi của trình duyệt một ca', () => {
  it('Chromium: Failed to fetch dynamically imported module', () => {
    const loi = new TypeError(
      'Failed to fetch dynamically imported module: http://localhost:4300/chunk-ABC.js',
    );
    expect(laLoiTaiPhanMa(loi)).toBeTrue();
  });

  it('Firefox: error loading dynamically imported module', () => {
    const loi = new TypeError(
      'error loading dynamically imported module: http://localhost:4300/chunk-ABC.js',
    );
    expect(laLoiTaiPhanMa(loi)).toBeTrue();
  });

  it('Safari: Importing a module script failed.', () => {
    expect(laLoiTaiPhanMa(new TypeError('Importing a module script failed.'))).toBeTrue();
  });

  it('chỉ cần có message dạng chuỗi — không đòi là instance của Error (đa khung iframe)', () => {
    expect(laLoiTaiPhanMa({ message: 'Importing a module script failed.' })).toBeTrue();
  });

  it('lỗi khác → false', () => {
    expect(laLoiTaiPhanMa(new Error('Guard ném lỗi bất kỳ'))).toBeFalse();
    expect(laLoiTaiPhanMa(new TypeError('x is not a function'))).toBeFalse();
  });

  it('không phải lỗi → false, không ném', () => {
    expect(laLoiTaiPhanMa(null)).toBeFalse();
    expect(laLoiTaiPhanMa(undefined)).toBeFalse();
    expect(laLoiTaiPhanMa('Importing a module script failed.')).toBeFalse();
    expect(laLoiTaiPhanMa(42)).toBeFalse();
    expect(laLoiTaiPhanMa({ message: 7 })).toBeFalse();
  });
});

describe('LoiTaiPhanMaService — Router thật', () => {
  const CHUNK_HONG = () =>
    Promise.reject(
      new TypeError('Failed to fetch dynamically imported module: http://x/chunk-ABC.js'),
    );
  const routes: Routes = [
    { path: 'on', children: [] },
    { path: 'hong', loadComponent: CHUNK_HONG },
    {
      path: 'guard-ne',
      canActivate: [() => Promise.reject(new Error('lỗi của guard'))],
      children: [],
    },
    { path: 'chan', canActivate: [() => false], children: [] },
  ];

  let router: Router;
  let location: Location;
  let toast: ToastService;
  let service: LoiTaiPhanMaService;
  let loiConLai: unknown[];

  beforeEach(() => {
    loiConLai = [];
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideLocationMocks(),
        // Router.navigate từ chối promise khi NavigationError; handler này giữ cho test thấy lỗi "vẫn tới nơi khác".
        provideRouter(
          routes,
          withNavigationErrorHandler((e: NavigationError) => {
            loiConLai.push(e.error);
          }),
        ),
        {
          provide: TranslateService,
          useValue: { instant: (khoa: string) => `[${khoa}]` },
        },
      ],
    });
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    toast = TestBed.inject(ToastService);
    service = TestBed.inject(LoiTaiPhanMaService);
  });

  it('phần mã hỏng TRƯỚC NavigationEnd đầu → khối toàn trang, thanh địa chỉ giữ URL đích', async () => {
    await router.navigateByUrl('/hong?a=1').catch(() => undefined);

    expect(service.khoiToanTrang()).toBeTrue();
    expect(location.path()).toBe('/hong?a=1');
    expect(toast.hangDoi().length).toBe(0);
  });

  it('phần mã hỏng SAU NavigationEnd đầu → toast danger kèm nút "Tải lại trang", không khối', async () => {
    await router.navigateByUrl('/on');
    await router.navigateByUrl('/hong').catch(() => undefined);

    expect(service.khoiToanTrang()).toBeFalse();
    const [thongBao] = toast.hangDoi();
    expect(thongBao.severity).toBe('error');
    expect(thongBao.summary).toBe('[loi.CORE.CLIENT.RESOURCE_LOAD_FAILED]');
    expect(thongBao.hanhDong?.nhan).toBe('[trangLoi.khongTaiDuoc.taiLai]');
  });

  it('nút trong toast gọi tải lại toàn trang đúng một lần', async () => {
    const taiLai = spyOn(service, 'taiLaiTrang').and.stub();
    await router.navigateByUrl('/on');
    await router.navigateByUrl('/hong').catch(() => undefined);

    toast.hangDoi()[0].hanhDong?.chay();

    expect(taiLai).toHaveBeenCalledTimes(1);
  });

  it('lỗi KHÁC (guard ném) ở lần điều hướng đầu → không khối, không toast', async () => {
    await router.navigateByUrl('/guard-ne').catch(() => undefined);

    expect(service.khoiToanTrang()).toBeFalse();
    expect(toast.hangDoi().length).toBe(0);
  });

  it('guard trả false ở lần đầu (NavigationCancel) chưa tính là có màn → lần sau hỏng vẫn ra khối', async () => {
    await router.navigateByUrl('/chan');
    await router.navigateByUrl('/hong').catch(() => undefined);

    expect(service.khoiToanTrang()).toBeTrue();
  });

  it('không nuốt sự kiện — NavigationError vẫn tới nơi khác đang nghe', async () => {
    await router.navigateByUrl('/hong').catch(() => undefined);

    expect(loiConLai.length).toBe(1);
  });
});
