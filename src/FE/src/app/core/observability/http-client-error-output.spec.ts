import {
  HttpHandlerFn,
  HttpInterceptorFn,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ErrorHandler } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { XsrfTokenStore } from '../auth/xsrf-token.store';
import { API_BASE_URL } from '../http/api-base-url';
import { ClientErrorReport } from './client-error-report';
import { HttpClientErrorOutput } from './http-client-error-output';

const DIA_CHI = 'https://api.test/api/v1/core/client-errors';

const BAO_CAO: ClientErrorReport = {
  kind: 'TypeError',
  message: "Cannot read properties of undefined (reading 'ten')",
  stack: 'at PhieuListPage.hienThi (main-A7F2.js:1:2481)',
  duongDan: '/phieu/danh-sach',
  traceId: 'c1807b11710f43c1964ec2988f532e56',
  phienBanApp: 'a1b2c3d',
};

/** Cho một lượt vòng lặp sự kiện chạy — một lỗi chưa bắt của RxJS bật ra ở `setTimeout`. */
const doiMotLuot = (): Promise<void> => new Promise((xong) => setTimeout(xong));

describe('HttpClientErrorOutput', () => {
  let token: string | null;
  let soLanQuaInterceptor: number;
  let loiChoErrorHandler: jasmine.Spy;
  let dauRa: HttpClientErrorOutput;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    token = 'xsrf-abc';
    soLanQuaInterceptor = 0;
    loiChoErrorHandler = jasmine.createSpy('handleError');
    const demInterceptor: HttpInterceptorFn = (req, next: HttpHandlerFn) => {
      soLanQuaInterceptor++;
      return next(req);
    };

    TestBed.configureTestingModule({
      providers: [
        HttpClientErrorOutput,
        provideHttpClient(withInterceptors([demInterceptor])),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://api.test/api/v1' },
        { provide: XsrfTokenStore, useValue: { token: () => token } },
        { provide: ErrorHandler, useValue: { handleError: loiChoErrorHandler } },
      ],
    });
    dauRa = TestBed.inject(HttpClientErrorOutput);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    jasmine.clock().uninstall();
    httpMock.verify();
  });

  describe('hợp đồng client-errors.md', () => {
    it('POST đúng đường, kèm cookie phiên và X-XSRF-TOKEN, thân đúng sáu field', () => {
      dauRa.send(BAO_CAO);

      const req = httpMock.expectOne(DIA_CHI);
      expect(req.request.method).toBe('POST');
      expect(req.request.withCredentials).toBeTrue();
      expect(req.request.headers.get('X-XSRF-TOKEN')).toBe('xsrf-abc');
      expect(req.request.body).toEqual({
        kind: 'TypeError',
        message: "Cannot read properties of undefined (reading 'ten')",
        stack: 'at PhieuListPage.hienThi (main-A7F2.js:1:2481)',
        duongDan: '/phieu/danh-sach',
        traceId: 'c1807b11710f43c1964ec2988f532e56',
        phienBanApp: 'a1b2c3d',
      });
      req.flush({ success: true, data: null, error: null, traceId: 'x' });
    });

    it('stack và traceId vắng thì không có trong thân (không gửi null)', () => {
      dauRa.send({ kind: 'Error', message: 'm', duongDan: '/', phienBanApp: 'v' });

      const req = httpMock.expectOne(DIA_CHI);
      expect(Object.keys(req.request.body as object).sort()).toEqual(
        ['duongDan', 'kind', 'message', 'phienBanApp'].sort(),
      );
      req.flush(null);
    });

    it('field thừa trên ClientErrorReport KHÔNG tự đi lên server', () => {
      const thua = { ...BAO_CAO, noiDungForm: 'bí mật' } as ClientErrorReport;

      dauRa.send(thua);

      const req = httpMock.expectOne(DIA_CHI);
      expect(JSON.stringify(req.request.body)).not.toContain('bí mật');
      req.flush(null);
    });

    it('chưa có token CSRF → bỏ báo cáo, KHÔNG xin token và không gửi', () => {
      token = null;

      dauRa.send(BAO_CAO);

      expect(httpMock.match(() => true)).toEqual([]);
    });
  });

  describe('đi vòng chuỗi interceptor — chỗ chặn vòng lặp', () => {
    it('request không qua interceptor nào của app', () => {
      dauRa.send(BAO_CAO);

      httpMock.expectOne(DIA_CHI).flush(null);
      expect(soLanQuaInterceptor).toBe(0);
    });
  });

  describe('§4.1 ràng buộc 3 — thất bại của việc gửi không làm hỏng app', () => {
    for (const trangThai of [0, 400, 403, 404, 500, 503]) {
      it(`phản hồi ${trangThai} → nuốt, không thử lại, không đẩy ngược vào ErrorHandler`, async () => {
        dauRa.send(BAO_CAO);

        const req = httpMock.expectOne(DIA_CHI);
        if (trangThai === 0) {
          req.error(new ProgressEvent('error'));
        } else {
          req.flush(
            {
              success: false,
              data: null,
              error: { code: 'CORE.AUTH.CSRF_REJECTED' },
              traceId: 't',
            },
            { status: trangThai, statusText: 'x' },
          );
        }
        await doiMotLuot();

        expect(httpMock.match(() => true)).toEqual([]); // không thử lại
        expect(loiChoErrorHandler).not.toHaveBeenCalled();
      });
    }

    it('gửi quá 5 giây không trả lời → huỷ request, không lỗi lọt ra ngoài', async () => {
      jasmine.clock().install();
      dauRa.send(BAO_CAO);
      const req = httpMock.expectOne(DIA_CHI);

      jasmine.clock().tick(5_001);
      jasmine.clock().uninstall();
      await doiMotLuot();

      expect(req.cancelled).toBeTrue();
      expect(loiChoErrorHandler).not.toHaveBeenCalled();
    });
  });

  describe('429 — dừng gửi, không biến hạn mức thành vòng lặp (luồng N8 §4)', () => {
    function biSiet(retryAfter: string | null): void {
      dauRa.send(BAO_CAO);
      httpMock.expectOne(DIA_CHI).flush(
        { success: false, data: null, error: { code: 'CORE.RATE_LIMIT.EXCEEDED' }, traceId: 't' },
        {
          status: 429,
          statusText: 'Too Many Requests',
          headers: retryAfter === null ? {} : { 'Retry-After': retryAfter },
        },
      );
    }

    it('nghỉ đúng Retry-After rồi gửi lại được', () => {
      jasmine.clock().install();
      jasmine.clock().mockDate(new Date(2026, 10, 6, 9, 0, 0));
      biSiet('30');

      jasmine.clock().tick(29_000);
      dauRa.send(BAO_CAO);
      expect(httpMock.match(() => true)).toEqual([]);

      jasmine.clock().tick(1_001);
      dauRa.send(BAO_CAO);
      const req = httpMock.expectOne(DIA_CHI);
      expect(req.request.method).toBe('POST');
      req.flush(null);
    });

    it('không đọc được Retry-After → nghỉ một cửa sổ mặc định (60 giây)', () => {
      jasmine.clock().install();
      jasmine.clock().mockDate(new Date(2026, 10, 6, 9, 0, 0));
      biSiet(null);

      jasmine.clock().tick(59_000);
      dauRa.send(BAO_CAO);
      expect(httpMock.match(() => true)).toEqual([]);

      jasmine.clock().tick(1_001);
      dauRa.send(BAO_CAO);
      const req = httpMock.expectOne(DIA_CHI);
      expect(req.request.method).toBe('POST');
      req.flush(null);
    });

    it('Retry-After quá lớn bị chặn trần — không tắt báo lỗi cả buổi', () => {
      jasmine.clock().install();
      jasmine.clock().mockDate(new Date(2026, 10, 6, 9, 0, 0));
      biSiet('99999');

      jasmine.clock().tick(601_000);
      dauRa.send(BAO_CAO);
      const req = httpMock.expectOne(DIA_CHI);
      expect(req.request.method).toBe('POST');
      req.flush(null);
    });
  });
});
