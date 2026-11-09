import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ApiFailureError } from '../http/api-result.model';
import { CLIENT_ERROR_OUTPUT, ClientErrorOutput } from './client-error-output';
import { ClientErrorReporter } from './client-error-reporter';
import { GlobalErrorHandler } from './global-error-handler';

describe('GlobalErrorHandler', () => {
  describe('với bộ thu lỗi giả', () => {
    let capture: jasmine.Spy;
    let handler: GlobalErrorHandler;
    let consoleError: jasmine.Spy;

    beforeEach(() => {
      capture = jasmine.createSpy('capture');
      TestBed.configureTestingModule({
        providers: [GlobalErrorHandler, { provide: ClientErrorReporter, useValue: { capture } }],
      });
      handler = TestBed.inject(GlobalErrorHandler);
      consoleError = spyOn(console, 'error');
    });

    it('lỗi runtime → đưa cho bộ thu lỗi VÀ vẫn in ra console', () => {
      const loi = new TypeError("Cannot read properties of undefined (reading 'ten')");

      handler.handleError(loi);

      expect(capture).toHaveBeenCalledOnceWith(loi);
      expect(consoleError).toHaveBeenCalledWith(loi);
    });

    it('lỗi HTTP đã qua interceptor → KHÔNG đếm hai lần (HttpErrorResponse)', () => {
      const loi = new HttpErrorResponse({ status: 400 });

      handler.handleError(loi);

      expect(capture).not.toHaveBeenCalled();
    });

    it('lỗi HTTP đã qua interceptor → KHÔNG đếm hai lần (ApiFailureError)', () => {
      handler.handleError(new ApiFailureError(null, 503));

      expect(capture).not.toHaveBeenCalled();
    });

    it('promise bị từ chối bằng giá trị không phải Error vẫn được đưa đi', () => {
      handler.handleError('chuỗi bị ném');

      expect(capture).toHaveBeenCalledOnceWith('chuỗi bị ném');
    });

    it('bộ thu lỗi ném → handleError không ném', () => {
      capture.and.throwError('bộ thu lỗi hỏng');

      expect(() => handler.handleError(new Error('x'))).not.toThrow();
    });

    it('console.error ném → handleError không ném', () => {
      consoleError.and.throwError('console hỏng');

      expect(() => handler.handleError(new Error('x'))).not.toThrow();
    });
  });

  describe('với bộ thu lỗi thật — không vòng lặp', () => {
    class DauRaDem implements ClientErrorOutput {
      soLanGui = 0;
      khiGui: () => void = () => undefined;

      send(): void {
        this.soLanGui++;
        this.khiGui();
      }
    }

    let dauRa: DauRaDem;
    let handler: ErrorHandler;

    beforeEach(() => {
      dauRa = new DauRaDem();
      TestBed.configureTestingModule({
        providers: [
          { provide: ErrorHandler, useClass: GlobalErrorHandler },
          { provide: CLIENT_ERROR_OUTPUT, useValue: dauRa },
        ],
      });
      handler = TestBed.inject(ErrorHandler);
      spyOn(console, 'error');
    });

    it('đầu ra ném lỗi RỒI đẩy lỗi đó ngược vào ErrorHandler → đúng một lần gửi, không vòng lặp', () => {
      dauRa.khiGui = () => {
        const loi = new Error('lỗi của chính việc báo lỗi');
        handler.handleError(loi);
        throw loi;
      };

      expect(() => handler.handleError(new Error('lỗi gốc'))).not.toThrow();

      expect(dauRa.soLanGui).toBe(1);
    });
  });
});
