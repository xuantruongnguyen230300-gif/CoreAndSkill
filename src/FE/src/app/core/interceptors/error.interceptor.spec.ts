import localeVi from '@angular/common/locales/vi';
import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslateService,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, firstValueFrom, from, of } from 'rxjs';

import { provideCoreI18n } from '../config/core-i18n';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../auth/auth.service';
import { SessionExpiryHandler } from '../auth/session-expiry.handler';
import { XsrfTokenStore } from '../auth/xsrf-token.store';
import { CORE_ROUTES } from '../config/core-routes';
import {
  ApiFailure,
  ApiFailureError,
  BO_QUA_HET_PHIEN,
  BO_QUA_TOAST_LOI,
  CO_TAC_DUNG_PHU,
} from '../http/api-result.model';
import { ToastService } from '../toast/toast.service';

/**
 * Bản dịch cố định cho test — CHỈ chứa hai khoá phía client mà interceptor tự tra (mất kết nối, 5xx
 * không envelope — be-cqrs-handler.md §7.4). Mọi mã lỗi khác KHÔNG có khoá, để bài kiểm chứng đúng
 * cơ chế đường lùi `dichLoi()`.
 */
class FakeTranslateLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({
      loi: {
        CORE: {
          CLIENT: {
            NO_CONNECTION: 'Không kết nối được tới máy chủ.',
            SERVER_UNAVAILABLE: 'Máy chủ tạm thời không phản hồi.',
          },
        },
      },
    });
  }
}

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toast: ToastService;
  let hetPhien: SessionExpiryHandler;
  let auth: AuthService;
  let xsrf: XsrfTokenStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
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
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
    hetPhien = TestBed.inject(SessionExpiryHandler);
    auth = TestBed.inject(AuthService);
    xsrf = TestBed.inject(XsrfTokenStore);
    // Nạp xong bản dịch trước khi test đọc translate.instant() — FakeTranslateLoader phát đồng bộ.
    TestBed.inject(TranslateService);
  });

  afterEach(() => httpMock.verify());

  it('lỗi nghiệp vụ chung → toast hiện ĐÚNG message của BE khi FE CHƯA có khoá dịch cho mã đó', (done) => {
    // Envelope dưới đây là bản SAO Y NGUYÊN kết quả một lần gọi thật tới endpoint thử của B0
    // (docs/contracts/diagnostics.md — GET /api/v1/core/diagnostics/probe?outcome=failure),
    // gọi bằng curl lúc thi công F0 — không phải hình dạng tự đoán.
    const loiSpy = spyOn(toast, 'loi').and.callThrough();
    const envelope: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.DIAGNOSTICS.PROBE_FAILURE_REQUESTED',
        type: 'BusinessRule',
        message: 'Nhánh lỗi được yêu cầu qua tham số outcome=failure.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'd710dcf2ca94a7fc3c1aaf665283a1e5',
    };

    http.get('/thu-loi').subscribe({
      error: (err: unknown) => {
        expect(err).toBeInstanceOf(ApiFailureError);
        expect((err as ApiFailureError).message).toBe('CORE.DIAGNOSTICS.PROBE_FAILURE_REQUESTED');
        expect((err as ApiFailureError).body).toEqual(envelope);
        // Không có khoá `loi.CORE.DIAGNOSTICS.PROBE_FAILURE_REQUESTED` trong vi.json ở F0 ⇒
        // dichLoi() phải rơi về ĐÚNG message do BE gửi — đây chính là mục nghiệm thu F0 §7.
        expect(loiSpy).toHaveBeenCalledWith(
          'Nhánh lỗi được yêu cầu qua tham số outcome=failure.',
          'd710dcf2ca94a7fc3c1aaf665283a1e5',
        );
        done();
      },
    });

    httpMock
      .expectOne('/thu-loi')
      .flush(envelope, { status: 422, statusText: 'Unprocessable Entity' });
  });

  it('mất mạng (không envelope) → toast hiện câu dự phòng, không phải undefined hay chuỗi rỗng', (done) => {
    const loiSpy = spyOn(toast, 'loi').and.callThrough();

    http.get('/thu-loi').subscribe({
      error: (err: unknown) => {
        expect(err).toBeInstanceOf(ApiFailureError);
        expect((err as ApiFailureError).body).toBeNull();
        expect(loiSpy).toHaveBeenCalledWith('Không kết nối được tới máy chủ.', null);
        done();
      },
    });

    httpMock.expectOne('/thu-loi').error(new ProgressEvent('network error'), { status: 0 });
  });

  it('401 → gọi SessionExpiryHandler.handle() đúng một lần, KHÔNG toast', (done) => {
    const handleSpy = spyOn(hetPhien, 'handle');
    const loiSpy = spyOn(toast, 'loi');

    http.get('/thu-loi').subscribe({
      error: () => {
        expect(handleSpy).toHaveBeenCalledTimes(1);
        expect(loiSpy).not.toHaveBeenCalled();
        done();
      },
    });

    httpMock.expectOne('/thu-loi').flush(null, { status: 401, statusText: 'Unauthorized' });
  });

  it('401 mang BO_QUA_HET_PHIEN → KHÔNG gọi SessionExpiryHandler.handle()', (done) => {
    const handleSpy = spyOn(hetPhien, 'handle');
    const ctx = new HttpContext().set(BO_QUA_HET_PHIEN, true);

    http.get('/thu-loi', { context: ctx }).subscribe({
      error: () => {
        expect(handleSpy).not.toHaveBeenCalled();
        done();
      },
    });

    httpMock.expectOne('/thu-loi').flush(null, { status: 401, statusText: 'Unauthorized' });
  });

  it('lỗi mang fieldErrors → KHÔNG toast, để màn tự gắn lỗi vào từng ô', (done) => {
    const loiSpy = spyOn(toast, 'loi');
    const envelope = {
      success: false,
      data: null,
      error: {
        code: 'CORE.VALIDATION.FAILED',
        type: 'Validation',
        message: 'Dữ liệu không hợp lệ.',
        messageParams: null,
        fieldErrors: { email: [{ code: 'REQUIRED', messageParams: null }] },
      },
      traceId: 'trace-field',
    };

    http.post('/thu-loi', {}).subscribe({
      error: () => {
        expect(loiSpy).not.toHaveBeenCalled();
        done();
      },
    });

    httpMock.expectOne('/thu-loi').flush(envelope, { status: 400, statusText: 'Bad Request' });
  });

  it('BO_QUA_TOAST_LOI → màn tự lo hiển thị, interceptor không bắn toast cho request đó', (done) => {
    const loiSpy = spyOn(toast, 'loi');
    const ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);
    const envelope = {
      success: false,
      data: null,
      error: {
        code: 'MODULE.SOME_ERROR',
        type: 'BadRequest',
        message: 'Câu message thật của BE.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'trace-tat-toast',
    };

    http.get('/thu-loi', { context: ctx }).subscribe({
      error: () => {
        expect(loiSpy).not.toHaveBeenCalled();
        done();
      },
    });

    httpMock.expectOne('/thu-loi').flush(envelope, { status: 400, statusText: 'Bad Request' });
  });

  it('403 CORE.AUTH.FORBIDDEN → làm mới quyền VÀ toast (rơi xuống nhánh chung), KHÔNG điều hướng', (done) => {
    const lamMoiQuyenSpy = spyOn(auth, 'lamMoiQuyen');
    const loiSpy = spyOn(toast, 'loi').and.callThrough();
    const envelope: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.AUTH.FORBIDDEN',
        type: 'Forbidden',
        message: 'Bạn không có quyền thực hiện thao tác này.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'trace-forbidden',
    };

    http.get('/thu-loi').subscribe({
      error: () => {
        expect(lamMoiQuyenSpy).toHaveBeenCalledTimes(1);
        expect(loiSpy).toHaveBeenCalledWith(
          'Bạn không có quyền thực hiện thao tác này.',
          'trace-forbidden',
        );
        done();
      },
    });

    httpMock.expectOne('/thu-loi').flush(envelope, { status: 403, statusText: 'Forbidden' });
  });

  it('403 CSRF_REJECTED → lấy token mới rồi gửi lại ĐÚNG MỘT lần, không toast khi lần gửi lại thành công', (done) => {
    spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
    const loiSpy = spyOn(toast, 'loi');
    const envelope: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.AUTH.CSRF_REJECTED',
        type: 'Forbidden',
        message: 'Phiên xác thực chống giả mạo đã hết hạn.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'trace-csrf',
    };

    http.post('/thu-loi', {}).subscribe({
      next: (res) => {
        expect(res).toEqual({ success: true, data: null, error: null, traceId: 'trace-ok' });
        expect(loiSpy).not.toHaveBeenCalled();
        done();
      },
    });

    const lanDau = httpMock.expectOne('/thu-loi');
    lanDau.flush(envelope, { status: 403, statusText: 'Forbidden' });

    const lanHai = httpMock.expectOne('/thu-loi');
    expect(lanHai.request.headers.get('X-XSRF-TOKEN')).toBe('token-moi');
    lanHai.flush({ success: true, data: null, error: null, traceId: 'trace-ok' });
  });

  it('một HttpContext DÙNG CHUNG qua hai lần CSRF_REJECTED liên tiếp → lần nào cũng được gửi lại một lần', () => {
    // Service singleton giữ một HttpContext cho mọi lệnh ghi (mẫu `private readonly ctx`).
    // HttpContext.set() sửa map TẠI CHỖ — nếu interceptor gắn cờ "đã thử lại" vào chính instance đó,
    // cờ nằm lại vĩnh viễn và lần hết hạn CSRF kế tiếp không còn được thử lại.
    spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
    spyOn(toast, 'loi');
    const ctxDungChung = new HttpContext().set(BO_QUA_TOAST_LOI, true);
    const envelope: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.AUTH.CSRF_REJECTED',
        type: 'Forbidden',
        message: 'Phiên xác thực chống giả mạo đã hết hạn.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'trace-csrf',
    };
    const ketQua: unknown[] = [];
    const loi: unknown[] = [];

    for (let lan = 1; lan <= 2; lan++) {
      http.post('/thu-loi', {}, { context: ctxDungChung }).subscribe({
        next: (res) => ketQua.push(res),
        error: (e: unknown) => loi.push(e),
      });
      httpMock.expectOne('/thu-loi').flush(envelope, { status: 403, statusText: 'Forbidden' });
      // Lần gửi lại PHẢI xuất hiện — expectOne ném lỗi nếu không có.
      httpMock
        .expectOne('/thu-loi')
        .flush({ success: true, data: null, error: null, traceId: `trace-ok-${lan}` });
    }

    expect(loi).toEqual([]);
    expect(ketQua.length).toBe(2);
    // Context của người gọi không bị gắn cờ nội bộ nào ở trạng thái bật — chỉ token của chính họ.
    // (HttpContext.get() có thể ghi giá trị MẶC ĐỊNH `false` của một token chưa có vào map; đó là
    // hành vi của Angular và vô hại — thứ phải vắng mặt là một cờ đang bật ngoài token của người gọi.)
    expect(ctxDungChung.get(BO_QUA_TOAST_LOI)).toBeTrue();
    const cacTokenBat = Array.from(ctxDungChung.keys()).filter(
      (k) => k !== BO_QUA_TOAST_LOI && ctxDungChung.get(k) === true,
    );
    expect(cacTokenBat).toEqual([]);
  });

  it('lần gửi lại sau CSRF_REJECTED vẫn giữ các token trên context của request gốc', () => {
    spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
    const ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true).set(BO_QUA_HET_PHIEN, true);

    http.post('/thu-loi', {}, { context: ctx }).subscribe();
    httpMock.expectOne('/thu-loi').flush(
      {
        success: false,
        data: null,
        error: {
          code: 'CORE.AUTH.CSRF_REJECTED',
          type: 'Forbidden',
          message: 'x',
          messageParams: null,
          fieldErrors: null,
        },
        traceId: 't',
      },
      { status: 403, statusText: 'Forbidden' },
    );
    const lanHai = httpMock.expectOne('/thu-loi');
    expect(lanHai.request.context.get(BO_QUA_TOAST_LOI)).toBeTrue();
    expect(lanHai.request.context.get(BO_QUA_HET_PHIEN)).toBeTrue();
    lanHai.flush({ success: true, data: null, error: null, traceId: 'ok' });
  });

  describe('lần gửi lại sau CSRF_REJECTED THẤT BẠI — lỗi vẫn phải đi qua đúng đường dịch của interceptor', () => {
    const envelopeCsrf: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.AUTH.CSRF_REJECTED',
        type: 'Forbidden',
        message: 'Phiên xác thực chống giả mạo đã hết hạn.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'trace-csrf',
    };

    it('gửi lại bị 422 kèm fieldErrors → tới người gọi dạng ApiFailureError, không toast', () => {
      spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
      const loiSpy = spyOn(toast, 'loi');
      const envelope: ApiFailure = {
        success: false,
        data: null,
        error: {
          code: 'CORE.TENANT.CODE_DUPLICATE',
          type: 'Conflict',
          message: 'Mã đơn vị đã tồn tại.',
          messageParams: null,
          fieldErrors: { code: [{ code: 'DUPLICATE', messageParams: null }] },
        },
        traceId: 'trace-trung-ma',
      };
      const loi: unknown[] = [];

      http.post('/thu-loi', {}).subscribe({ error: (e: unknown) => loi.push(e) });
      httpMock.expectOne('/thu-loi').flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });
      httpMock
        .expectOne('/thu-loi')
        .flush(envelope, { status: 422, statusText: 'Unprocessable Entity' });

      expect(loi.length).toBe(1);
      expect(loi[0]).toBeInstanceOf(ApiFailureError);
      expect((loi[0] as ApiFailureError).body).toEqual(envelope);
      expect(loiSpy).not.toHaveBeenCalled();
    });

    it('gửi lại bị 401 → SessionExpiryHandler.handle() được gọi đúng một lần, lỗi là ApiFailureError', () => {
      spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
      const handleSpy = spyOn(hetPhien, 'handle');
      const loiSpy = spyOn(toast, 'loi');
      const loi: unknown[] = [];

      http.post('/thu-loi', {}).subscribe({ error: (e: unknown) => loi.push(e) });
      httpMock.expectOne('/thu-loi').flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });
      httpMock.expectOne('/thu-loi').flush(null, { status: 401, statusText: 'Unauthorized' });

      expect(handleSpy).toHaveBeenCalledTimes(1);
      expect(loiSpy).not.toHaveBeenCalled();
      expect(loi.length).toBe(1);
      expect(loi[0]).toBeInstanceOf(ApiFailureError);
    });

    it('CSRF_REJECTED lần hai → KHÔNG gửi lần ba, toast một lần, lỗi là ApiFailureError', () => {
      const lamMoiSpy = spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
      const loiSpy = spyOn(toast, 'loi').and.callThrough();
      const loi: unknown[] = [];

      http.post('/thu-loi', {}).subscribe({ error: (e: unknown) => loi.push(e) });
      httpMock.expectOne('/thu-loi').flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });
      httpMock.expectOne('/thu-loi').flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });

      // afterEach → httpMock.verify() thất bại nếu còn request lần ba treo.
      expect(lamMoiSpy).toHaveBeenCalledTimes(1);
      expect(loiSpy).toHaveBeenCalledTimes(1);
      expect(loiSpy).toHaveBeenCalledWith('Phiên xác thực chống giả mạo đã hết hạn.', 'trace-csrf');
      expect(loi.length).toBe(1);
      expect(loi[0]).toBeInstanceOf(ApiFailureError);
    });
  });

  // Dùng XsrfTokenStore THẬT (không spy lamMoi) — spy che mất đường đệ quy: lamMoi() là một GET đi
  // qua chính errorInterceptor, nên nếu GET token cũng bị coi là 'CSRF_REJECTED thì thử lại' thì nó
  // gọi lamMoi() → GET mới → ... vô hạn và im lặng. BE chỉ kiểm CSRF trên lệnh KHÔNG an toàn.
  describe('CSRF_REJECTED chỉ được thử lại cho lệnh ghi — không cho GET/HEAD/OPTIONS/TRACE', () => {
    const envelopeCsrf: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.AUTH.CSRF_REJECTED',
        type: 'Forbidden',
        message: 'Phiên xác thực chống giả mạo đã hết hạn.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'trace-csrf',
    };
    const URL_TOKEN = '/core/antiforgery/token';

    it('lệnh ghi bị CSRF_REJECTED, rồi chính GET lấy token cũng bị CSRF_REJECTED → dừng ở đó, không GET lần hai, lỗi tới màn', () => {
      const loi: unknown[] = [];

      http.post('/thu-loi', {}).subscribe({ error: (e: unknown) => loi.push(e) });
      httpMock.expectOne('/thu-loi').flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });
      httpMock.expectOne(URL_TOKEN).flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });

      httpMock.expectNone(URL_TOKEN);
      expect(loi.length).toBe(1);
      expect(loi[0]).toBeInstanceOf(ApiFailureError);
    });

    for (const phuongThuc of ['GET', 'HEAD', 'OPTIONS', 'TRACE'] as const) {
      it(`${phuongThuc} nhận CSRF_REJECTED → KHÔNG lấy token mới, KHÔNG gửi lại; lỗi tới màn`, () => {
        const lamMoiSpy = spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
        const loi: unknown[] = [];

        http.request(phuongThuc, '/thu-loi').subscribe({ error: (e: unknown) => loi.push(e) });
        httpMock
          .expectOne('/thu-loi')
          .flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });

        expect(lamMoiSpy).not.toHaveBeenCalled();
        httpMock.expectNone('/thu-loi');
        expect(loi[0]).toBeInstanceOf(ApiFailureError);
      });
    }

    for (const phuongThuc of ['POST', 'PUT', 'PATCH', 'DELETE'] as const) {
      it(`${phuongThuc} nhận CSRF_REJECTED → vẫn được lấy token mới và gửi lại một lần`, () => {
        spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));

        http.request(phuongThuc, '/thu-loi', { body: {} }).subscribe();
        httpMock
          .expectOne('/thu-loi')
          .flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });

        const lanHai = httpMock.expectOne('/thu-loi');
        expect(lanHai.request.method).toBe(phuongThuc);
        expect(lanHai.request.headers.get('X-XSRF-TOKEN')).toBe('token-moi');
        lanHai.flush({ success: true, data: null, error: null, traceId: 't' });
      });
    }
  });
  // Tải tệp xuất đi bằng responseType:'blob' (fe-api-client.md §6.4, ADR-0062). Angular giao MỌI
  // thân phản hồi dưới dạng Blob khi đó — kể cả thân lỗi 4xx/5xx dù server gửi application/json.
  describe("tải tệp xuất — responseType:'blob'", () => {
    const URL_XUAT = '/core/users/export';

    /** Thân lỗi như trình duyệt giao cho một request blob: JSON nằm TRONG một Blob. */
    function thanBlob(envelope: unknown): Blob {
      return new Blob([JSON.stringify(envelope)], { type: 'application/json' });
    }

    /** Chờ tới khi có request tới `url` — nhánh bóc Blob là bất đồng bộ (Blob.text()). */
    async function choYeuCau(url: string) {
      for (let i = 0; i < 100; i++) {
        const dsach = httpMock.match(url);
        if (dsach.length > 0) {
          return dsach[0];
        }
        await new Promise((r) => setTimeout(r, 5));
      }
      throw new Error(`không thấy request tới ${url}`);
    }

    function goiXuat(context = new HttpContext().set(CO_TAC_DUNG_PHU, true)) {
      return new Promise<unknown>((resolve) => {
        http
          .get(URL_XUAT, { responseType: 'blob', observe: 'response', context })
          .subscribe({ error: (e: unknown) => resolve(e) });
      });
    }

    const envelope422: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.EXPORT.TOO_MANY_ROWS',
        type: 'BusinessRule',
        message: 'Export exceeded the configured row cap.',
        messageParams: { MaxRows: '50000' },
        fieldErrors: null,
      },
      traceId: 'trace-xuat',
    };

    it('422 có thân JSON nằm trong Blob → toast ĐÚNG câu của mã lỗi, KHÔNG phải câu mất kết nối', async () => {
      TestBed.inject(TranslateService).setTranslation(
        'vi',
        {
          loi: {
            CORE: { EXPORT: { TOO_MANY_ROWS: 'Kết quả vượt {{MaxRows}} dòng. Lọc hẹp lại.' } },
          },
        },
        true,
      );
      const loiSpy = spyOn(toast, 'loi');
      const ketQua = goiXuat();

      httpMock
        .expectOne(URL_XUAT)
        .flush(thanBlob(envelope422), { status: 422, statusText: 'Unprocessable Entity' });
      const err = await ketQua;

      expect(err).toBeInstanceOf(ApiFailureError);
      expect((err as ApiFailureError).body).toEqual(envelope422);
      expect(loiSpy).toHaveBeenCalledOnceWith(
        'Kết quả vượt 50000 dòng. Lọc hẹp lại.',
        'trace-xuat',
      );
    });

    it('422 trong Blob mà FE chưa có khoá dịch → rơi về message của BE, vẫn không phải câu mất kết nối', async () => {
      const loiSpy = spyOn(toast, 'loi');
      const ketQua = goiXuat();

      httpMock
        .expectOne(URL_XUAT)
        .flush(thanBlob(envelope422), { status: 422, statusText: 'Unprocessable Entity' });
      await ketQua;

      expect(loiSpy).toHaveBeenCalledOnceWith(
        'Export exceeded the configured row cap.',
        'trace-xuat',
      );
    });

    // 502 không envelope là lớp xuyên suốt: câu SERVER_UNAVAILABLE, không phải câu mất kết nối (§2.2).
    it('Blob không phải JSON (proxy trả HTML 502) → câu SERVER_UNAVAILABLE, body null', async () => {
      const loiSpy = spyOn(toast, 'loi');
      const ketQua = goiXuat();

      httpMock.expectOne(URL_XUAT).flush(new Blob(['<html>502</html>'], { type: 'text/html' }), {
        status: 502,
        statusText: 'Bad Gateway',
      });
      const err = await ketQua;

      expect((err as ApiFailureError).body).toBeNull();
      expect(loiSpy).toHaveBeenCalledOnceWith('Máy chủ tạm thời không phản hồi.', null);
    });

    it('CSRF_REJECTED trong Blob trên GET mang dấu → lấy token mới và gửi lại ĐÚNG MỘT lần', async () => {
      const lamMoiSpy = spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
      const envelopeCsrf: ApiFailure = {
        success: false,
        data: null,
        error: {
          code: 'CORE.AUTH.CSRF_REJECTED',
          type: 'Forbidden',
          message: 'Phiên xác thực chống giả mạo đã hết hạn.',
          messageParams: null,
          fieldErrors: null,
        },
        traceId: 'trace-csrf-xuat',
      };

      void goiXuat();
      httpMock
        .expectOne(URL_XUAT)
        .flush(thanBlob(envelopeCsrf), { status: 403, statusText: 'Forbidden' });

      const lanHai = await choYeuCau(URL_XUAT);
      expect(lamMoiSpy).toHaveBeenCalledTimes(1);
      expect(lanHai.request.method).toBe('GET');
      expect(lanHai.request.headers.get('X-XSRF-TOKEN')).toBe('token-moi');

      lanHai.flush(thanBlob(envelopeCsrf), { status: 403, statusText: 'Forbidden' });
      await new Promise((r) => setTimeout(r, 20));
      httpMock.expectNone(URL_XUAT);
      expect(lamMoiSpy).toHaveBeenCalledTimes(1);
    });

    it('CSRF_REJECTED trên GET KHÔNG mang dấu → không lấy token mới, không gửi lại', async () => {
      const lamMoiSpy = spyOn(xsrf, 'lamMoi').and.returnValue(of('token-moi'));
      const envelopeCsrf: ApiFailure = {
        success: false,
        data: null,
        error: {
          code: 'CORE.AUTH.CSRF_REJECTED',
          type: 'Forbidden',
          message: 'Phiên xác thực chống giả mạo đã hết hạn.',
          messageParams: null,
          fieldErrors: null,
        },
        traceId: 'trace-csrf-thuong',
      };
      const ketQua = goiXuat(new HttpContext());

      httpMock
        .expectOne(URL_XUAT)
        .flush(thanBlob(envelopeCsrf), { status: 403, statusText: 'Forbidden' });
      const err = await ketQua;

      expect(lamMoiSpy).not.toHaveBeenCalled();
      httpMock.expectNone(URL_XUAT);
      expect(err).toBeInstanceOf(ApiFailureError);
    });

    it('401 trả thân Blob → vẫn vào nhánh hết phiên, không toast', async () => {
      const handleSpy = spyOn(hetPhien, 'handle');
      const loiSpy = spyOn(toast, 'loi');
      const ketQua = goiXuat();

      httpMock.expectOne(URL_XUAT).flush(new Blob([''], { type: 'application/json' }), {
        status: 401,
        statusText: 'Unauthorized',
      });
      await ketQua;

      expect(handleSpy).toHaveBeenCalledTimes(1);
      expect(loiSpy).not.toHaveBeenCalled();
    });
  });

  // Lớp lỗi xuyên suốt: mọi 5xx, cộng 403 `CORE.AUTH.*` trừ `PASSWORD_CHANGE_REQUIRED`. Luôn toast
  // kèm `traceId`, KỂ CẢ khi request mang BO_QUA_TOAST_LOI — màn không hiện lớp này ở khu lỗi của
  // nó. Mất kết nối, 429 và 409 vẫn tôn trọng cờ.
  describe('lớp lỗi xuyên suốt — request MANG BO_QUA_TOAST_LOI', () => {
    const coTatToast = (): HttpContext => new HttpContext().set(BO_QUA_TOAST_LOI, true);

    function envelope(code: string, traceId: string): ApiFailure {
      return {
        success: false,
        data: null,
        error: { code, type: 'X', message: `msg:${code}`, messageParams: null, fieldErrors: null },
        traceId,
      };
    }

    /** Gửi một POST mang cờ, trả lỗi `status` với `than`; trả về lỗi người gọi nhận. */
    function guiLoi(status: number, than: ApiFailure | string): unknown[] {
      const loi: unknown[] = [];
      http.post('/thu-loi', {}, { context: coTatToast() }).subscribe({
        error: (e: unknown) => loi.push(e),
      });
      httpMock.expectOne('/thu-loi').flush(than, { status, statusText: 'X' });
      return loi;
    }

    it('500 CORE.SYSTEM.UNEXPECTED → toast kèm traceId; lỗi tới màn mang status 500', () => {
      const loiSpy = spyOn(toast, 'loi').and.callThrough();

      const loi = guiLoi(500, envelope('CORE.SYSTEM.UNEXPECTED', 'trace-500'));

      expect(loiSpy).toHaveBeenCalledOnceWith('msg:CORE.SYSTEM.UNEXPECTED', 'trace-500');
      expect(loi[0]).toBeInstanceOf(ApiFailureError);
      expect((loi[0] as unknown as { status?: number }).status).toBe(500);
    });

    // fe-api-client.md §2.2: 5xx không envelope toast câu của `CORE.CLIENT.SERVER_UNAVAILABLE`
    // (be-cqrs-handler.md §7.4), không traceId — KHÔNG phải câu mất kết nối: máy chủ có trả lời.
    it('503 không envelope (proxy trả HTML) → VẪN toast dù mang cờ; câu SERVER_UNAVAILABLE, traceId null', () => {
      const loiSpy = spyOn(toast, 'loi').and.callThrough();

      const loi = guiLoi(503, '<html>Service Unavailable</html>');

      expect(loiSpy).toHaveBeenCalledOnceWith('Máy chủ tạm thời không phản hồi.', null);
      expect((loi[0] as unknown as { status?: number }).status).toBe(503);
    });

    it('403 CORE.AUTH.FORBIDDEN → toast kèm traceId VÀ làm mới quyền như cũ', () => {
      const lamMoiQuyenSpy = spyOn(auth, 'lamMoiQuyen');
      const loiSpy = spyOn(toast, 'loi').and.callThrough();

      guiLoi(403, envelope('CORE.AUTH.FORBIDDEN', 'trace-403'));

      expect(lamMoiQuyenSpy).toHaveBeenCalledTimes(1);
      expect(loiSpy).toHaveBeenCalledOnceWith('msg:CORE.AUTH.FORBIDDEN', 'trace-403');
    });

    it('403 CORE.AUTH.ORIGIN_REJECTED → toast kèm traceId, không gửi lại', () => {
      const loiSpy = spyOn(toast, 'loi').and.callThrough();

      guiLoi(403, envelope('CORE.AUTH.ORIGIN_REJECTED', 'trace-origin'));

      httpMock.expectNone('/thu-loi');
      expect(loiSpy).toHaveBeenCalledOnceWith('msg:CORE.AUTH.ORIGIN_REJECTED', 'trace-origin');
    });

    it('403 CORE.AUTH.PASSWORD_CHANGE_REQUIRED → KHÔNG toast (nhánh riêng giữ nguyên)', () => {
      spyOn(auth, 'lamMoiPhien').and.resolveTo();
      const loiSpy = spyOn(toast, 'loi');

      guiLoi(403, envelope('CORE.AUTH.PASSWORD_CHANGE_REQUIRED', 't'));

      expect(loiSpy).not.toHaveBeenCalled();
    });

    it('403 mang mã nghiệp vụ → KHÔNG toast (màn tự xử lý)', () => {
      const loiSpy = spyOn(toast, 'loi');

      guiLoi(403, envelope('CORE.USER.KHONG_DUOC', 't'));

      expect(loiSpy).not.toHaveBeenCalled();
    });

    it('409 CORE.CONCURRENCY.CONFLICT → KHÔNG toast (vẫn tôn trọng cờ)', () => {
      const loiSpy = spyOn(toast, 'loi');

      guiLoi(409, envelope('CORE.CONCURRENCY.CONFLICT', 't'));

      expect(loiSpy).not.toHaveBeenCalled();
    });

    it('429 CORE.RATE_LIMIT.EXCEEDED → KHÔNG toast (vẫn tôn trọng cờ)', () => {
      const loiSpy = spyOn(toast, 'loi');

      guiLoi(429, envelope('CORE.RATE_LIMIT.EXCEEDED', 't'));

      expect(loiSpy).not.toHaveBeenCalled();
    });

    it('mất kết nối (status 0) → KHÔNG toast (vẫn tôn trọng cờ)', () => {
      const loiSpy = spyOn(toast, 'loi');

      http.post('/thu-loi', {}, { context: coTatToast() }).subscribe({ error: () => undefined });
      httpMock.expectOne('/thu-loi').error(new ProgressEvent('network error'), { status: 0 });

      expect(loiSpy).not.toHaveBeenCalled();
    });
  });

  describe('5xx không envelope — request KHÔNG mang cờ', () => {
    it('502 proxy trả HTML → toast câu SERVER_UNAVAILABLE đúng một lần, traceId null — không phải câu mất kết nối', () => {
      const loiSpy = spyOn(toast, 'loi').and.callThrough();
      const loi: unknown[] = [];

      http.get('/thu-loi').subscribe({ error: (e: unknown) => loi.push(e) });
      httpMock
        .expectOne('/thu-loi')
        .flush('<html>Bad Gateway</html>', { status: 502, statusText: 'Bad Gateway' });

      expect(loiSpy).toHaveBeenCalledOnceWith('Máy chủ tạm thời không phản hồi.', null);
      expect(loi[0]).toBeInstanceOf(ApiFailureError);
      expect((loi[0] as ApiFailureError).body).toBeNull();
      expect((loi[0] as ApiFailureError).status).toBe(502);
    });
  });

  // fe-ui-conventions.md §6.2 "Toast và banner không đi cùng nhau": lần lấy token ở nhánh CSRF là một
  // GET đi qua CHÍNH interceptor này, nên lỗi của nó được dịch ở lượt của GET đó — lượt ấy phải biết
  // request gốc đã tắt toast, nếu không màn tự hiện lỗi (banner) mà toast vẫn bắn: một lỗi, hai chỗ.
  // Dùng XsrfTokenStore THẬT — spy lamMoi() che mất đúng lượt đi qua interceptor đó.
  describe('CSRF_REJECTED rồi chính lần lấy token hỏng — lỗi đó tuân cờ BO_QUA_TOAST_LOI của request gốc', () => {
    const envelopeCsrf: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.AUTH.CSRF_REJECTED',
        type: 'Forbidden',
        message: 'Phiên xác thực chống giả mạo đã hết hạn.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 'trace-csrf',
    };
    const URL_TOKEN = '/core/antiforgery/token';

    function guiGhi(context?: HttpContext): unknown[] {
      const loi: unknown[] = [];
      http.post('/thu-loi', {}, context ? { context } : {}).subscribe({
        error: (e: unknown) => loi.push(e),
      });
      httpMock.expectOne('/thu-loi').flush(envelopeCsrf, { status: 403, statusText: 'Forbidden' });
      return loi;
    }

    it('request gốc MANG cờ, lấy token mất kết nối → KHÔNG toast; màn nhận đúng một lỗi (status 0) để tự hiện', () => {
      const loiSpy = spyOn(toast, 'loi');

      const loi = guiGhi(new HttpContext().set(BO_QUA_TOAST_LOI, true));
      httpMock.expectOne(URL_TOKEN).error(new ProgressEvent('network error'), { status: 0 });

      expect(loiSpy).not.toHaveBeenCalled();
      httpMock.expectNone('/thu-loi');
      expect(loi.length).toBe(1);
      expect(loi[0]).toBeInstanceOf(ApiFailureError);
      expect((loi[0] as ApiFailureError).status).toBe(0);
    });

    it('request gốc KHÔNG mang cờ, lấy token mất kết nối → toast ĐÚNG MỘT lần', () => {
      const loiSpy = spyOn(toast, 'loi').and.callThrough();

      guiGhi();
      httpMock.expectOne(URL_TOKEN).error(new ProgressEvent('network error'), { status: 0 });

      expect(loiSpy).toHaveBeenCalledOnceWith('Không kết nối được tới máy chủ.', null);
    });

    it('request gốc MANG cờ, lấy token 503 không envelope → VẪN toast đúng một lần (lớp xuyên suốt)', () => {
      const loiSpy = spyOn(toast, 'loi').and.callThrough();

      guiGhi(new HttpContext().set(BO_QUA_TOAST_LOI, true));
      httpMock
        .expectOne(URL_TOKEN)
        .flush('<html>Service Unavailable</html>', { status: 503, statusText: 'X' });

      expect(loiSpy).toHaveBeenCalledOnceWith('Máy chủ tạm thời không phản hồi.', null);
    });
  });
});

/** Nạp ĐÚNG `public/i18n/vi.json` — bản dịch giả ở trên dùng câu thử, không thấy câu thật. */
class ViJsonThatLoader extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return from(fetch('/i18n/vi.json').then((r) => r.json() as Promise<TranslationObject>));
  }
}

describe('errorInterceptor — câu thật trong vi.json cho 5xx không envelope', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
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
          loader: provideTranslateLoader(ViJsonThatLoader),
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
  });

  // Câu do Design/Screens/00-khung-ung-dung.md khai (dòng "Toast 5xx không envelope").
  it('504 proxy trả HTML → toast đúng câu đã khai cho CORE.CLIENT.SERVER_UNAVAILABLE', async () => {
    await firstValueFrom(TestBed.inject(TranslateService).use('vi'));
    const http = TestBed.inject(HttpClient);
    const httpMock = TestBed.inject(HttpTestingController);
    const loiSpy = spyOn(TestBed.inject(ToastService), 'loi');

    http.get('/thu-loi').subscribe({ error: () => undefined });
    httpMock
      .expectOne('/thu-loi')
      .flush('<html>Gateway Timeout</html>', { status: 504, statusText: 'Gateway Timeout' });

    expect(loiSpy).toHaveBeenCalledOnceWith(
      'Máy chủ tạm thời không phản hồi, vui lòng thử lại sau.',
      null,
    );
    httpMock.verify();
  });
});
