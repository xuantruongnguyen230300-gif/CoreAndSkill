/**
 * Mã commit ĐẦY ĐỦ của bản dựng — hằng số của bản dựng, không gọi API, không đọc lúc chạy
 * (Design/Components/Footer.md §Phiên bản hiển thị, Q59).
 *
 * `null` ở cây nguồn đã commit: `Footer` ẩn hẳn phần phiên bản khi chưa có mã. Hai nơi GHI ĐÈ tệp này
 * bằng mã commit thật ngay trước `ng build`: CI (`.github/workflows/docs-gate.yml`, job FE) và bản
 * cài dựng tay (`docs/wiki-core/fe/17-phuc-vu-va-trien-khai.md` §8 bước 2, hoàn nguyên sau khi build;
 * dòng 2026-11-05 "Bản cài FE dựng tay…" ở `docs/adr/nhat-ky-quyet-dinh.md`). Không dùng
 * `package.json` vì nó mang `0.0.0`. Tệp chỉ giữ MỘT hằng này; đừng sửa tay giá trị rồi commit, và
 * đừng thêm gì khác vào đây (cả hai nơi ghi đè cả tệp).
 */
export const MA_COMMIT: string | null = null;
