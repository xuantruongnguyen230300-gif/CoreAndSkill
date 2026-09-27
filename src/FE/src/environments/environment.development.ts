// Dùng khi `ng serve` (cấu hình `development` — fileReplacements khai ở angular.json). FE và API
// ở hai cổng khác nhau ngay cả trên máy dev, đúng hình dạng của môi trường thật
// (17-phuc-vu-va-trien-khai.md §4.2) — không dùng proxy máy chủ dev để giấu ranh giới.
//
// 🛑 Cổng dưới đây CHƯA được đối chiếu với BE (pha B0 đang dựng song song) — cần backend-expert
// xác nhận cổng HTTPS thật của BE dev server và thêm origin FE dev vào allowlist CORS của
// `appsettings.Development.json` trước khi coi phần "gọi endpoint thử B0" của F0 là xong.
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7100/api/v1',
};
