// Dùng khi `ng serve` (cấu hình `development` — fileReplacements khai ở angular.json). FE và API
// ở hai cổng khác nhau ngay cả trên máy dev, đúng hình dạng của môi trường thật
// (17-phuc-vu-va-trien-khai.md §4.2) — không dùng proxy máy chủ dev để giấu ranh giới.
//
// Quy ước ba tệp môi trường: docs/quy-uoc/fe-architecture.md §1.1.
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7100/api/v1',
  tenantCode: 'DEMO',
};
