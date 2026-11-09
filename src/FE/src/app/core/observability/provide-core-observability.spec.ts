import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import {
  CLIENT_ERROR_OUTPUT,
  ClientErrorOutput,
  ConsoleClientErrorOutput,
  provideClientErrorOutput,
} from './client-error-output';
import { ClientErrorReport } from './client-error-report';
import { GlobalErrorHandler } from './global-error-handler';
import { provideCoreObservability } from './provide-core-observability';

class DauRaGhi implements ClientErrorOutput {
  readonly daGui: ClientErrorReport[] = [];

  send(baoCao: ClientErrorReport): void {
    this.daGui.push(baoCao);
  }
}

/**
 * Cửa sổ giả: chỉ là một `EventTarget`. Hai bộ nghe của Angular (`error`, `unhandledrejection`) gắn
 * vào `DOCUMENT.defaultView` — dùng cửa sổ giả để phát sự kiện thật vào chúng mà KHÔNG chạm
 * `window.onerror` của Jasmine (nó sẽ coi mọi sự kiện lỗi là spec hỏng).
 */
function dungTaiLieuGia(): { taiLieu: unknown; cuaSo: EventTarget } {
  const cuaSo = new EventTarget();
  return {
    cuaSo,
    taiLieu: { defaultView: cuaSo, location: { pathname: '/man/thu' } },
  };
}

function suKienTuChoi(ly: unknown): Event {
  return Object.assign(new Event('unhandledrejection', { cancelable: true }), { reason: ly });
}

function suKienLoi(loi: unknown, message: string): Event {
  return Object.assign(new Event('error', { cancelable: true }), { error: loi, message });
}

describe('provideCoreObservability — ba nguồn lỗi của 10-observability.md §3, đi hết đường tới đầu ra', () => {
  let dauRa: DauRaGhi;
  let cuaSo: EventTarget;

  beforeEach(() => {
    const gia = dungTaiLieuGia();
    cuaSo = gia.cuaSo;
    dauRa = new DauRaGhi();
    TestBed.configureTestingModule({
      // TestBed mặc định ném lại mọi lỗi đi qua bộ nghe toàn cục của Angular để spec hỏng; ở đây chính
      // đường đó là thứ đang được kiểm, nên để nó tới ErrorHandler của app.
      rethrowApplicationErrors: false,
      providers: [
        provideZonelessChangeDetection(),
        { provide: DOCUMENT, useValue: gia.taiLieu },
        provideCoreObservability(),
        { provide: CLIENT_ERROR_OUTPUT, useValue: dauRa },
      ],
    });
    TestBed.inject(ErrorHandler); // chạy initializer của môi trường
    spyOn(console, 'error');
  });

  it('ErrorHandler của app là GlobalErrorHandler', () => {
    expect(TestBed.inject(ErrorHandler)).toBeInstanceOf(GlobalErrorHandler);
  });

  it('promise bị từ chối mà không ai bắt (sự kiện unhandledrejection) → tới đầu ra', () => {
    cuaSo.dispatchEvent(suKienTuChoi(new RangeError('async/await không bắt')));

    expect(dauRa.daGui.length).toBe(1);
    expect(dauRa.daGui[0].kind).toBe('RangeError');
    expect(dauRa.daGui[0].message).toBe('async/await không bắt');
    expect(dauRa.daGui[0].duongDan).toBe('/man/thu');
  });

  it('lỗi ngoài vùng Angular (sự kiện error của cửa sổ, có e.error) → tới đầu ra', () => {
    cuaSo.dispatchEvent(suKienLoi(new TypeError('từ thư viện bên thứ ba'), 'Uncaught TypeError'));

    expect(dauRa.daGui.length).toBe(1);
    expect(dauRa.daGui[0].kind).toBe('TypeError');
  });

  it('sự kiện error không kèm đối tượng lỗi → vẫn tới đầu ra, với thông điệp của sự kiện', () => {
    cuaSo.dispatchEvent(suKienLoi(undefined, 'Script error.'));

    expect(dauRa.daGui.length).toBe(1);
    expect(dauRa.daGui[0].message).toContain('Script error.');
  });

  it('lỗi HTTP đã qua interceptor → KHÔNG thành báo cáo lỗi runtime', () => {
    cuaSo.dispatchEvent(suKienTuChoi(new HttpErrorResponse({ status: 400 })));

    expect(dauRa.daGui.length).toBe(0);
  });

  it('ErrorHandler.handleError trực tiếp (lỗi trong template/handler của Angular) → tới đầu ra', () => {
    TestBed.inject(ErrorHandler).handleError(new Error('lỗi khi vẽ'));

    expect(dauRa.daGui.map((b) => b.message)).toEqual(['lỗi khi vẽ']);
  });
});

describe('đầu ra của ClientErrorReporter — mặc định và cắm được', () => {
  it('mặc định là console — không cấu hình gì cũng không có đường ra mạng', () => {
    TestBed.configureTestingModule({});

    expect(TestBed.inject(CLIENT_ERROR_OUTPUT)).toBeInstanceOf(ConsoleClientErrorOutput);
  });

  it('console in ĐÚNG bản báo cáo đã dựng, kèm ghi chú suy luận khi có traceId trước đó', () => {
    const warn = spyOn(console, 'warn');
    const baoCao: ClientErrorReport = { kind: 'K', message: 'm', duongDan: '/', phienBanApp: 'v' };
    const dauRa = new ConsoleClientErrorOutput();

    dauRa.send(baoCao, []);
    dauRa.send(baoCao, ['t1', 't2']);

    expect(warn.calls.argsFor(0)[1]).toBe(baoCao);
    expect(warn.calls.argsFor(0).length).toBe(2);
    expect(String(warn.calls.argsFor(1)[2])).toContain('t1, t2');
    expect(String(warn.calls.argsFor(1)[2])).toContain('suy luận');
  });

  it('provideClientErrorOutput thay được đầu ra', () => {
    TestBed.configureTestingModule({ providers: [provideClientErrorOutput(DauRaGhi)] });

    expect(TestBed.inject(CLIENT_ERROR_OUTPUT)).toBeInstanceOf(DauRaGhi);
  });
});
