import localeVi from '@angular/common/locales/vi';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { unsavedChangesGuard } from './unsaved-changes.guard';
import { AuthService } from '../auth/auth.service';
import { NguoiDungHienTai } from '../auth/nguoi-dung-hien-tai.model';
import { SessionExpiryHandler } from '../auth/session-expiry.handler';
import { KHOA_BAO_TAB_PHIEN_KET_THUC } from '../auth/tab-session-broadcast.service';
import { provideCoreI18n } from '../config/core-i18n';
import { CORE_ROUTES } from '../config/core-routes';
import { UnsavedChangesService } from '../unsaved-changes/unsaved-changes.service';

/** Gọi guard trong injection context của TestBed — guard không đọc tham số nào. */
function chayGuard(): ReturnType<typeof unsavedChangesGuard> {
  return TestBed.runInInjectionContext(() =>
    unsavedChangesGuard({} as never, {} as never, {} as never, {} as never),
  );
}

/** Kết quả của guard, hoặc `'treo'` nếu nó chưa trả lời sau một nhịp — để test không treo theo nó. */
async function ketQuaHoacTreo(ketQua: unknown): Promise<unknown> {
  return Promise.race([
    Promise.resolve(ketQua),
    new Promise((xong) => setTimeout(() => xong('treo'), 50)),
  ]);
}

describe('unsavedChangesGuard', () => {
  let service: UnsavedChangesService;
  const daDangNhap = signal(true);

  beforeEach(() => {
    daDangNhap.set(true);
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthService, useValue: { daDangNhap } },
      ],
    });
    service = TestBed.inject(UnsavedChangesService);
  });

  it('không có thay đổi chưa lưu → cho rời NGAY (true), không mở hộp', async () => {
    service.dangKy(() => false);

    const ketQua = chayGuard();

    expect(await ketQua).toBeTrue();
    expect(service.dangHoi()).toBeFalse();
  });

  it('còn thay đổi chưa lưu, chọn "Rời đi" → guard trả true', async () => {
    service.dangKy(() => true);

    const cho = chayGuard() as Promise<boolean>;
    expect(service.dangHoi()).toBeTrue();
    service.xacNhanRoiDi();

    expect(await cho).toBeTrue();
  });

  it('còn thay đổi chưa lưu, chọn "Ở lại" → guard trả false, không rời route', async () => {
    service.dangKy(() => true);

    const cho = chayGuard() as Promise<boolean>;
    service.huy();

    expect(await cho).toBeFalse();
  });

  // Router chạy lại canDeactivate của route đang rời khi một canActivate phía sau trả UrlTree
  // (chuyển hướng): câu trả lời "Rời đi" dùng luôn cho lần chạy lại đó — không hỏi lần hai.
  it('"Rời đi" qua guard → Router chạy lại guard cho CÙNG lần rời (chuyển hướng) → KHÔNG hỏi lần hai', async () => {
    service.dangKy(() => true);
    const cho = chayGuard() as Promise<boolean>;
    service.xacNhanRoiDi();
    expect(await cho).toBeTrue();

    const lanHai = chayGuard();

    expect(service.dangHoi()).toBeFalse();
    expect(await ketQuaHoacTreo(lanHai)).toBeTrue();
  });

  // fe-routing-guard.md §8: 401 luôn về màn đăng nhập, kể cả khi form dở. Cùng một điều kiện phủ
  // đăng xuất chủ động đã xong: `AuthService.dangXuat()` dọn phiên TRƯỚC khi khung điều hướng, và
  // khung đã hỏi một lần rồi (00-khung-ung-dung.md: "không hỏi lần hai").
  it('phiên đã kết thúc + còn thay đổi chưa lưu → cho rời NGAY, không mở hộp', async () => {
    service.dangKy(() => true);
    daDangNhap.set(false);

    const ketQua = chayGuard();

    expect(service.dangHoi()).toBeFalse();
    expect(await ketQuaHoacTreo(ketQua)).toBeTrue();
  });

  it('hộp "Rời trang?" đang mở (lượt hỏi trước) mà phiên kết thúc → guard đóng hộp, lượt cũ nhận "ở lại"', async () => {
    service.dangKy(() => true);
    const luotCu = service.xinRoiTrang();
    expect(service.dangHoi()).toBeTrue();
    daDangNhap.set(false);

    const ketQua = chayGuard();

    expect(service.dangHoi()).toBeFalse();
    expect(await ketQuaHoacTreo(luotCu)).toBeFalse();
    expect(await ketQuaHoacTreo(ketQua)).toBeTrue();
  });
});

@Component({ selector: 'app-trang-form', template: '' })
class TrangForm {}

@Component({ selector: 'app-trang-dang-nhap', template: '' })
class TrangDangNhap {}

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

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

/** Đặt thẳng signal nội bộ — AuthService không có seam nào khác để ép trạng thái trong test. */
function dangNhapGia(auth: AuthService): void {
  (auth as unknown as { _nguoiDung: { set: (v: NguoiDungHienTai) => void } })['_nguoiDung'].set(
    NGUOI_DUNG_GIA,
  );
}

/** Nhường vài nhịp cho Router chạy xong một điều hướng không ai giữ Promise của nó. */
async function nhuongNhip(): Promise<void> {
  for (let i = 0; i < 10; i++) {
    await new Promise((xong) => setTimeout(xong, 0));
  }
}

/**
 * fe-routing-guard.md §8 — "401 luôn điều hướng về màn đăng nhập, kể cả khi form đang nhập dở":
 * Router thật, guard thật, `SessionExpiryHandler` thật. Hộp "Rời trang?" không được chen vào — chọn
 * "Ở lại" ở đó là giữ người dùng trên một màn chết mà mọi thao tác đều 401.
 */
describe('unsavedChangesGuard — phiên kết thúc khi form đang dở (Router thật)', () => {
  let router: Router;
  let auth: AuthService;
  let thayDoi: UnsavedChangesService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'ho-so', component: TrangForm, canDeactivate: [unsavedChangesGuard] },
          { path: 'dang-nhap', component: TrangDangNhap },
        ]),
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
    router = TestBed.inject(Router);
    auth = TestBed.inject(AuthService);
    thayDoi = TestBed.inject(UnsavedChangesService);

    dangNhapGia(auth);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/ho-so');
    thayDoi.dangKy(() => true);
  });

  it('form dirty, gặp 401 → tới được /dang-nhap, không mở hộp "Rời trang?"', async () => {
    TestBed.inject(SessionExpiryHandler).handle();
    await nhuongNhip();

    expect(router.url.startsWith('/dang-nhap')).withContext(router.url).toBeTrue();
    expect(thayDoi.dangHoi()).toBeFalse();
  });

  it('form dirty, tab khác báo hết phiên → tới được /dang-nhap, không mở hộp', async () => {
    TestBed.inject(SessionExpiryHandler);
    window.dispatchEvent(
      new StorageEvent('storage', {
        key: KHOA_BAO_TAB_PHIEN_KET_THUC,
        newValue: String(Date.now()),
      }),
    );
    await nhuongNhip();

    expect(router.url.startsWith('/dang-nhap')).withContext(router.url).toBeTrue();
    expect(thayDoi.dangHoi()).toBeFalse();
  });
});
