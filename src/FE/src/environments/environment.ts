// Chỉ dùng cho `ng test`, `ng lint` và làm kiểu của `environment`. KHÔNG BAO GIỜ vào artifact
// sản xuất: cấu hình `production` thay tệp này bằng `environment.ban-cai.ts` của từng bản cài
// (docs/quy-uoc/fe-architecture.md §1.1; ADR-0116 QC4). Mọi giá trị dưới đây là placeholder.
export const environment = {
  production: true,
  apiBaseUrl: 'https://api.core-and-skill.invalid/api/v1',
  tenantCode: 'PLACEHOLDER-KHONG-DUNG-CHO-SAN-XUAT',
};
