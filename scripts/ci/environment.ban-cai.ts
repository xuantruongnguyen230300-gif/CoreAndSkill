// Tệp môi trường của bản cài GIẢ — chỉ để job frontend-gate của CI build `production` đúng đường mà
// một bản cài thật đi (ADR-0116 QC4, luật F54; docs/wiki-core/fe/trien-khai/05-gate.md §5 bước 4b).
// Nằm NGOÀI `src/` để không bị biên dịch; CI chép nó vào `src/environments/` rồi xoá sau build.
// Giá trị giả: `apiBaseUrl` trên miền `.invalid`, `tenantCode` không trùng mã đơn vị thật nào.
import { khaiBaoMoiTruong } from '../app/core/config/khai-bao-moi-truong';

export const environment = khaiBaoMoiTruong({
  production: true,
  apiBaseUrl: 'https://api.ci-gia.invalid/api/v1',
  tenantCode: 'CI-GIA',
});
