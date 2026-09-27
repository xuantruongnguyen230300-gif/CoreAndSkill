import { DatePipe } from '@angular/common';
import { LOCALE_ID } from '@angular/core';

import { appConfig } from './app.config';

/**
 * Luật F36 / ADR-0063 — `LOCALE_ID` và dữ liệu locale đến từ seam `CORE_I18N`, KHÔNG cấp riêng ở
 * đây. Quên đăng ký locale thì pipe `date` im lặng rơi về `en-US` và "10/09" bị đọc thành tháng 10
 * (fe-ui-conventions.md §5.5, 08-i18n.md §6.2); cấp `LOCALE_ID` riêng thì dự án hạ nguồn đổi
 * `defaultLanguage` sẽ có câu chữ một ngôn ngữ còn ngày tháng ngôn ngữ khác.
 */
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

  it('nạp app.config.ts là đã đăng ký locale vi (pipe date ra dạng ngày/tháng/năm)', () => {
    // createdAt = 2026-09-10T03:12:44Z, múi giờ UTC để kết quả không phụ thuộc máy chạy test.
    // Locale vi được đăng ký khi provideCoreI18n chạy lúc dựng appConfig — không còn lời gọi
    // registerLocaleData nào ngoài seam.
    const ra = new DatePipe('vi').transform('2026-09-10T03:12:44Z', 'short', 'UTC');

    expect(ra).toContain('10/09/2026');
    expect(ra).not.toContain('9/10/26');
  });
});
