import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, firstValueFrom, from, of, throwError } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';
import { ApiFailure, ApiFailureError } from '../../../../core/http/api-result.model';
import { DangNhapPage } from './dang-nhap.page';

class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

function loiApi(code: string, fieldErrors: ApiFailure['error']['fieldErrors']): ApiFailureError {
  return new ApiFailureError({
    success: false,
    data: null,
    error: { code, type: 'Validation', message: `msg:${code}`, messageParams: null, fieldErrors },
    traceId: 't',
  });
}

interface PageTest {
  form: {
    setValue(v: { tenantCode: string; userName: string; password: string }): void;
    controls: Record<string, { touched: boolean; errors: unknown }>;
  };
  gui(): void;
  loiChung(): string | null;
}

/**
 * `AuthService.dangNhap` tắt toast (BO_QUA_TOAST_LOI) nên màn là nơi DUY NHẤT hiện lỗi: 422 mà
 * không có `fieldErrors` dùng được mà màn im lặng thì người dùng chỉ thấy nút quay xong rồi thôi.
 */
describe('DangNhapPage — lỗi 422 không gắn được vào ô nào vẫn phải HIỆN', () => {
  let fixture: ComponentFixture<DangNhapPage>;
  let auth: jasmine.SpyObj<AuthService>;
  let page: PageTest;

  beforeEach(async () => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['dangNhap', 'phaiDoiMatKhau']);
    await TestBed.configureTestingModule({
      imports: [DangNhapPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/d',
            khongCoQuyen: '/k',
            sauDangNhap: '/',
          },
        },
        { provide: CORE_BRANDING, useValue: { name: 'X', shortName: 'X' } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(DangNhapPage);
    page = fixture.componentInstance as unknown as PageTest;
    fixture.detectChanges();
    page.form.setValue({ tenantCode: 'DV', userName: 'an', password: 'x' });
  });

  for (const fieldErrors of [null, {}]) {
    it(`VALIDATION.FAILED với fieldErrors = ${JSON.stringify(fieldErrors)} → khu lỗi chung mang câu của mã`, async () => {
      auth.dangNhap.and.returnValue(
        throwError(() => loiApi('CORE.VALIDATION.FAILED', fieldErrors)),
      );

      page.gui();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(page.loiChung()).toBe('msg:CORE.VALIDATION.FAILED');
      expect(fixture.nativeElement.textContent).toContain('msg:CORE.VALIDATION.FAILED');
    });
  }

  // Lớp lỗi xuyên suốt: interceptor đã toast kèm traceId (kể cả khi request mang BO_QUA_TOAST_LOI) —
  // khu lỗi của AuthCard KHÔNG hiện lần hai.
  for (const [status, code] of [
    [500, 'CORE.SYSTEM.UNEXPECTED'],
    [403, 'CORE.AUTH.ORIGIN_REJECTED'],
  ] as const) {
    it(`${status} ${code} → khu lỗi của AuthCard trống`, async () => {
      const loi = loiApi(code, null);
      auth.dangNhap.and.returnValue(throwError(() => new ApiFailureError(loi.body, status)));

      page.gui();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(page.loiChung()).toBeNull();
      expect(fixture.nativeElement.textContent).not.toContain(`msg:${code}`);
    });
  }

  // Bất biến "che thông tin" (contracts/auth.md §3): bốn ca sai (mã đơn vị, đơn vị ngưng, tên đăng nhập,
  // mật khẩu) là MỘT mã và MỘT câu gộp — không ô nào được tô đỏ riêng, vì ô nào đỏ là lộ ra thứ tồn
  // tại. Bất biến này hôm nay đúng nhờ luồng điều khiển ở `xuLyLoi`; ai cho mọi mã 422 đi qua
  // `applyFormFailure` sẽ làm lộ mà không thứ gì khác đỏ — nên khoá nó ở đây, kể cả khi BE (giả
  // định xấu nhất) gửi kèm `fieldErrors` trỏ vào TenantCode.
  describe('bất biến che thông tin: lỗi thông tin đăng nhập chỉ vào khu lỗi CHUNG, không ô nào bị tô', () => {
    function khongOnaoBiTo(): void {
      for (const [ten, c] of Object.entries(page.form.controls)) {
        expect(c.touched).withContext(`${ten}.touched`).toBeFalse();
        expect(c.errors).withContext(`${ten}.errors`).toBeNull();
      }
      const ariaInvalid = (fixture.nativeElement as HTMLElement).querySelectorAll(
        '[aria-invalid="true"]',
      );
      expect(ariaInvalid.length).withContext('ô có aria-invalid').toBe(0);
    }

    const trongCuaBE = {
      TenantCode: [{ code: 'CORE.AUTH.INVALID_CREDENTIALS', messageParams: null }],
    };

    for (const fieldErrors of [null, trongCuaBE]) {
      it(`INVALID_CREDENTIALS (fieldErrors = ${fieldErrors === null ? 'null' : 'trỏ vào TenantCode'}) → câu gộp ở khu chung, không control nào có lỗi server hay bị chạm`, async () => {
        auth.dangNhap.and.returnValue(
          throwError(() => loiApi('CORE.AUTH.INVALID_CREDENTIALS', fieldErrors)),
        );

        page.gui();
        fixture.detectChanges();
        await fixture.whenStable();

        expect(page.loiChung()).toBe('msg:CORE.AUTH.INVALID_CREDENTIALS');
        expect(fixture.nativeElement.textContent).toContain('msg:CORE.AUTH.INVALID_CREDENTIALS');
        khongOnaoBiTo();
      });
    }

    for (const ma of ['CORE.AUTH.LOCKED_OUT', 'CORE.RATE_LIMIT.EXCEEDED']) {
      for (const fieldErrors of [null, trongCuaBE]) {
        it(`${ma} (fieldErrors = ${fieldErrors === null ? 'null' : 'trỏ vào TenantCode'}) → câu ở khu chung, không ô nào bị tô`, async () => {
          auth.dangNhap.and.returnValue(throwError(() => loiApi(ma, fieldErrors)));

          page.gui();
          fixture.detectChanges();
          await fixture.whenStable();

          expect(page.loiChung()).toBe('msg:' + ma);
          khongOnaoBiTo();
        });
      }
    }
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — khoá hay tham số lệch thì câu dưới ô là khoá thô hoặc thiếu số. */
class ViJsonThatLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return from(fetch('/i18n/vi.json').then((r) => r.json() as Promise<TranslationObject>));
  }
}

/**
 * BE thêm trần độ dài cho `TenantCode` ở `POST /core/auth/login`: vượt trần thì trả
 * `CORE.VALIDATION.FAILED` kèm `fieldErrors.TenantCode = CORE.VALIDATION.MAX_LENGTH`, trần đi trong
 * `messageParams.MaxLength` (contracts/auth.md §3, khuôn của `UserName`). Câu phải hiện DƯỚI ô mã
 * đơn vị, có con số, không rơi vào khu lỗi chung.
 */
describe('DangNhapPage — TenantCode vượt trần độ dài (CORE.VALIDATION.MAX_LENGTH)', () => {
  let fixture: ComponentFixture<DangNhapPage>;
  let auth: jasmine.SpyObj<AuthService>;
  let page: PageTest;

  beforeEach(async () => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['dangNhap', 'phaiDoiMatKhau']);
    await TestBed.configureTestingModule({
      imports: [DangNhapPage],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/d',
            khongCoQuyen: '/k',
            sauDangNhap: '/',
          },
        },
        { provide: CORE_BRANDING, useValue: { name: 'X', shortName: 'X' } },
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(ViJsonThatLoader),
        }),
      ],
    }).compileComponents();
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    fixture = TestBed.createComponent(DangNhapPage);
    page = fixture.componentInstance as unknown as PageTest;
    fixture.detectChanges();
    page.form.setValue({ tenantCode: 'X'.repeat(51), userName: 'an', password: 'x' });
  });

  it('câu "Vượt quá 50 ký tự." hiện dưới ô mã đơn vị, ô mang aria-invalid, khu lỗi chung trống', async () => {
    auth.dangNhap.and.returnValue(
      throwError(
        () =>
          new ApiFailureError({
            success: false,
            data: null,
            error: {
              code: 'CORE.VALIDATION.FAILED',
              type: 'Validation',
              message: 'msg',
              messageParams: null,
              fieldErrors: {
                TenantCode: [
                  { code: 'CORE.VALIDATION.MAX_LENGTH', messageParams: { MaxLength: '50' } },
                ],
              },
            },
            traceId: 't',
          }),
      ),
    );

    page.gui();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const o = el.querySelector('#dang-nhap-ma-don-vi');
    expect(o?.getAttribute('aria-invalid')).withContext('ô mã đơn vị').toBe('true');
    const moTa = (o?.getAttribute('aria-describedby') ?? '')
      .split(/\s+/)
      .filter((id) => id.length > 0)
      .map((id) => el.querySelector(`#${id}`)?.textContent?.trim() ?? '')
      .join(' ');
    expect(moTa)
      .withContext('câu lỗi nối với ô qua aria-describedby')
      .toContain('Vượt quá 50 ký tự.');
    expect(page.loiChung()).toBeNull();
    expect(el.textContent).not.toContain('loi.CORE.VALIDATION.MAX_LENGTH');
  });
});
