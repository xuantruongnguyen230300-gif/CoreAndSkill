// Dùng cho `ng build` mặc định (cấu hình `production`). Giá trị BẮT BUỘC cấu hình vì FE và API
// khác nguồn ở mọi môi trường (17-phuc-vu-va-trien-khai.md §4.1, §5.2) — một artifact riêng cho
// mỗi môi trường triển khai thật.
//
// 🛑 F0 CHƯA có môi trường triển khai nào tồn tại. Giá trị dưới là PLACEHOLDER — phải thay bằng
// origin API thật trước khi build cho một môi trường thật; đừng triển khai artifact build từ
// placeholder này.
export const environment = {
  production: true,
  apiBaseUrl: 'https://api.core-and-skill.invalid/api/v1',
};
