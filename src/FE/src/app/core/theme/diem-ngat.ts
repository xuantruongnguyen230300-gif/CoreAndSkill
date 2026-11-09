/**
 * Điểm ngắt cho TypeScript — bản phản chiếu DUY NHẤT của `$bp-*` ở `styles/_spacing.scss`
 * (fe-ui-conventions.md §7.1, luật F58). Điều kiện `@media` của SCSS không đọc được từ TypeScript
 * mà `matchMedia` cần chuỗi truy vấn, nên giá trị có bản thứ hai ở đây: chuỗi dựng sẵn, phép trừ
 * `0.02px` của "dưới điểm ngắt" nằm ở đúng một tệp. Đổi `$bp-*` thì đổi hằng cùng hậu tố; cổng F58
 * so từng hằng với `_spacing.scss`. Chỉ khai hằng đang có nơi dùng.
 */

/** Phản chiếu `$bp-xs` — dưới điểm ngắt. */
export const TRUY_VAN_DUOI_BP_XS = '(max-width: 479.98px)';

/** Phản chiếu `$bp-sm` — dưới điểm ngắt. */
export const TRUY_VAN_DUOI_BP_SM = '(max-width: 639.98px)';
