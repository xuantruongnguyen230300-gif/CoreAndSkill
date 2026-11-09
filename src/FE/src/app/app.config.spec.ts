import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { DATE_PIPE_DEFAULT_TIMEZONE, DatePipe } from '@angular/common';
import { Component, LOCALE_ID, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { environment } from '../environments/environment';
import { APP_I18N, appConfig, provideDauRaLoiTheoMoiTruong } from './app.config';
import { provideCoreI18n } from './core/config/core-i18n';
import { TENANT_CODE } from './core/config/tenant-code';
import { API_BASE_URL } from './core/http/api-base-url';
import {
  CLIENT_ERROR_OUTPUT,
  ClientErrorOutput,
  ConsoleClientErrorOutput,
} from './core/observability/client-error-output';
import { HttpClientErrorOutput } from './core/observability/http-client-error-output';

/**
 * Luật F36 / ADR-0063 — `LOCALE_ID` và dữ liệu locale đến từ seam `CORE_I18N`, KHÔNG cấp riêng ở
 * đây. Quên đăng ký locale thì pipe `date` im lặng rơi về `en-US` và "10/09" bị đọc thành tháng 10
 * (fe-ui-conventions.md §5.5, 08-i18n.md §6.2); cấp `LOCALE_ID` riêng thì dự án hạ nguồn đổi
 * `defaultLanguage` sẽ có câu chữ một ngôn ngữ còn ngày tháng ngôn ngữ khác.
 */
@Component({
  selector: 'app-chu-ngay-gio',
  imports: [DatePipe],
  template: `{{ thoiDiem | date }}`,
})
class ChuNgayGioComponent {
  readonly thoiDiem = '2026-09-10T03:12:44Z';
}

describe('appConfig — locale', () => {
  it('KHÔNG cấp LOCALE_ID trực tiếp — giá trị đó đến từ provideCoreI18n', () => {
    const nhaCungCap = appConfig.providers.find(
      (p): p is { provide: unknown; useValue: unknown } =>
        typeof p === 'object' && p !== null && 'provide' in p && p.provide === LOCALE_ID,
    );

    expect(nhaCungCap)
      .withContext('app.config.ts không được cấp LOCALE_ID riêng — luật F36')
      .toBeUndefined();
  });

  it('nạp app.config.ts là đã đăng ký locale vi và khuôn ngày giờ vi (pipe date không đối số ra giờ:phút ngày/tháng/năm)', () => {
    // createdAt = 2026-09-10T03:12:44Z, múi giờ UTC (chỉ trong test) để kết quả không phụ thuộc máy
    // chạy. Dựng lại từ `APP_I18N` — chính giá trị `appConfig` đưa cho provideCoreI18n.
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideCoreI18n(APP_I18N),
        { provide: DATE_PIPE_DEFAULT_TIMEZONE, useValue: 'UTC' },
      ],
    });
    const fixture = TestBed.createComponent(ChuNgayGioComponent);
    fixture.detectChanges();

    const ra = (fixture.nativeElement as HTMLElement).textContent.trim();
    expect(ra).toBe('03:12 10/09/2026');
    expect(ra).not.toContain('09/10');
  });
});

describe('TENANT_CODE', () => {
  it('app.config cấp đúng environment.tenantCode', () => {
    const provider = appConfig.providers.find(
      (p): p is { provide: unknown; useValue: unknown } =>
        typeof p === 'object' && p !== null && 'provide' in p && p.provide === TENANT_CODE,
    );
    expect(provider?.useValue).toBe(environment.tenantCode);
    expect(environment.tenantCode.length).toBeGreaterThan(0);
  });
});

/**
 * Đầu ra báo lỗi theo môi trường — người dùng chốt 2026-11-06: bản production gửi về BE, dev giữ
 * console. Test hàm chọn nhánh thay vì nạp cả `appConfig`: `environment.production` của `ng test`
 * luôn là `true` (environment.ts) nên qua `appConfig` chỉ chạm được một nhánh.
 */
describe('provideDauRaLoiTheoMoiTruong', () => {
  function dung(production: boolean): ClientErrorOutput {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://api.test.invalid/api/v1' },
        provideDauRaLoiTheoMoiTruong(production),
      ],
    });
    return TestBed.inject(CLIENT_ERROR_OUTPUT);
  }

  it('production: đầu ra là HttpClientErrorOutput', () => {
    expect(dung(true)).toBeInstanceOf(HttpClientErrorOutput);
  });

  it('không phải production: giữ đầu ra console mặc định', () => {
    const dauRa = dung(false);

    expect(dauRa).toBeInstanceOf(ConsoleClientErrorOutput);
    expect(dauRa).not.toBeInstanceOf(HttpClientErrorOutput);
  });
});
