// Dùng cho lưới giao diện Playwright (ADR-0117, QC1): cấu hình `e2e` của `ng build` và `ng serve`
// thay `environment.ts` bằng tệp này. Mọi giá trị là GIẢ — `apiBaseUrl` trên miền `.invalid`, khác
// nguồn với dev server (ADR-0015) và bị `page.route` của lưới chặn; không bao giờ tới API thật.
// Cố ý KHÔNG dùng tệp bản cài (ADR-0116): kết quả lưới không được phụ thuộc giá trị riêng của một máy.
//
// Quy ước các tệp môi trường: docs/quy-uoc/fe-architecture.md §1.1.
import { khaiBaoMoiTruong } from '../app/core/config/khai-bao-moi-truong';

export const environment = khaiBaoMoiTruong({
  production: true,
  apiBaseUrl: 'https://api.e2e-gia.invalid/api/v1',
  tenantCode: 'E2E-GIA',
});
