import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { CORE_ROUTES } from './core/config/core-routes';

/**
 * Điều hướng URL gốc `/` qua cấu hình route THẬT (`routes`). Kiểm HÀNH VI — URL cuối sau điều hướng —
 * không kiểm cấu trúc cấu hình. Giá trị CORE_ROUTES chép từ `provideCoreRoutes` ở app.config.ts.
 */
describe('app.routes — điều hướng URL gốc và URL của cả khu', () => {
  const daDangNhap = signal(false);
  const phaiDoiMatKhau = signal(false);
  const quyenCo = signal<readonly string[]>([]);
  const vanHanhHeThong = signal(false);
  let router: Router;

  beforeEach(() => {
    daDangNhap.set(false);
    phaiDoiMatKhau.set(false);
    quyenCo.set([]);
    vanHanhHeThong.set(false);
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter(routes),
        {
          provide: AuthService,
          useValue: {
            daDangNhap,
            phaiDoiMatKhau,
            laVanHanhHeThong: vanHanhHeThong,
            coQuyen: (ma: string) => quyenCo().includes(ma),
          },
        },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/doi-mat-khau-bat-buoc',
            khongCoQuyen: '/khong-co-quyen',
            sauDangNhap: '/trang-chu',
          },
        },
      ],
    });
    router = TestBed.inject(Router);
  });

  it('chưa đăng nhập, mở "/" → về /dang-nhap?returnUrl=%2Ftrang-chu', async () => {
    await router.navigateByUrl('/');

    expect(router.url).toBe('/dang-nhap?returnUrl=%2Ftrang-chu');
  });

  it('đã đăng nhập, không bị buộc đổi mật khẩu, mở "/" → /trang-chu', async () => {
    daDangNhap.set(true);

    await router.navigateByUrl('/');

    expect(router.url).toBe('/trang-chu');
  });

  it('chưa đăng nhập, mở "/trang-chu" → /dang-nhap?returnUrl=%2Ftrang-chu', async () => {
    await router.navigateByUrl('/trang-chu');

    expect(router.url).toBe('/dang-nhap?returnUrl=%2Ftrang-chu');
  });

  it('đã đăng nhập nhưng bị buộc đổi mật khẩu, mở "/" → /doi-mat-khau-bat-buoc', async () => {
    daDangNhap.set(true);
    phaiDoiMatKhau.set(true);

    await router.navigateByUrl('/');

    expect(router.url).toBe('/doi-mat-khau-bat-buoc');
  });

  it('đã đăng nhập, mở "/quan-tri" → /trang-chu', async () => {
    daDangNhap.set(true);

    await router.navigateByUrl('/quan-tri');

    expect(router.url).toBe('/trang-chu');
  });

  it('đã đăng nhập, mở "/quan-tri?x=1" (có query) → /trang-chu', async () => {
    daDangNhap.set(true);

    await router.navigateByUrl('/quan-tri?x=1');

    expect(router.url).toBe('/trang-chu');
  });

  it('đã đăng nhập là vận hành hệ thống, mở "/he-thong" → /trang-chu', async () => {
    daDangNhap.set(true);
    vanHanhHeThong.set(true);

    await router.navigateByUrl('/he-thong');

    expect(router.url).toBe('/trang-chu');
  });

  it('đã đăng nhập, có quyền, mở "/quan-tri/nguoi-dung" → vẫn tới đúng màn', async () => {
    daDangNhap.set(true);
    quyenCo.set(['core.user.read']);

    await router.navigateByUrl('/quan-tri/nguoi-dung');

    expect(router.url).toBe('/quan-tri/nguoi-dung');
  });

  it('đã đăng nhập là vận hành hệ thống, mở "/he-thong/don-vi" → vẫn tới đúng màn', async () => {
    daDangNhap.set(true);
    vanHanhHeThong.set(true);

    await router.navigateByUrl('/he-thong/don-vi');

    expect(router.url).toBe('/he-thong/don-vi');
  });
});
