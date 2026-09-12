---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N3` — Xuất dữ liệu theo bộ lọc đang xem

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng duy nhất trong Core **đọc dữ liệu hàng loạt và giao ra ngoài hệ**. Mọi lỗi phạm vi ở đây đều rời khỏi hệ thống dưới dạng một tệp, và không lấy lại được.

---

## 1. Ai bắt đầu, ở đâu

Người dùng đã đăng nhập, từ nút **Xuất** trên thanh công cụ của một màn danh sách ([`../Design/Components/Toolbar.md`](../Design/Components/Toolbar.md)).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền xuất của đúng tài nguyên đó | Ví dụ `core.user.export` — quyền **riêng**, không gộp vào quyền đọc |
| Một bộ lọc đang áp trên màn | Xuất lấy **đúng bộ lọc đang xem**, không xuất toàn bảng |
| Số dòng kết quả nằm dưới giới hạn | Xuất chạy nền **chưa có ở v1** |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | `GET /<khu>/<tài nguyên>/export` kèm **đúng bộ tham số lọc đang hiển thị** | [`../contracts/exports.md`](../contracts/exports.md) §1 |
| 2 | BE | Kiểm quyền xuất của tài nguyên đó | [`../database/schema-core.md`](../database/schema-core.md) §5 |
| 3 | BE | Đếm số dòng khớp bộ lọc **trước khi** dựng tệp | [`../contracts/exports.md`](../contracts/exports.md) §1 |
| 4 | BE | Vượt giới hạn ⇒ dừng, trả lỗi kèm số dòng thực tế. Dưới giới hạn ⇒ dựng tệp | cùng trên |
| 5 | BE | Đọc dữ liệu theo lô bằng phân trang keyset, không nạp hết vào bộ nhớ | [`../wiki-core/be/11-performance-caching.md`](../wiki-core/be/11-performance-caching.md) |
| 6 | FE | Nhận tệp | [`../contracts/exports.md`](../contracts/exports.md) §1 |

### Vì sao bước 3 đứng trước bước 5

Đếm trước thì người dùng nhận lỗi **ngay**. Dựng tệp trước rồi mới phát hiện quá lớn nghĩa là hệ đã tiêu tài nguyên cho một việc chắc chắn thất bại — và ở một hệ một instance ([`../adr/0014-mot-instance-key-ring-postgres.md`](../adr/0014-mot-instance-key-ring-postgres.md)), một lần xuất quá lớn làm chậm **mọi người**.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Thiếu quyền xuất | `CORE.AUTH.FORBIDDEN` (403) | Nút Xuất lẽ ra đã không hiện — nếu nó hiện thì FE đang dựng giao diện không theo tập quyền |
| Vượt giới hạn số dòng | `CORE.EXPORT.TOO_MANY_ROWS` (422) | Thông điệp phải nêu **số dòng thực tế và giới hạn**, để người dùng biết cần lọc hẹp thêm bao nhiêu |
| Bộ lọc gửi lên khác bộ lọc đang hiển thị | không có mã lỗi | 🛑 Tệp xuất ra **đúng cú pháp nhưng sai nội dung**. Không lỗi nào bắn ra; chỉ có người đọc tệp phát hiện, và thường là muộn |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị — và đây là luồng rủi ro nhất trong nhóm.**

Bộ lọc đơn vị phải áp ở bước 5 **như mọi truy vấn khác**. Rủi ro riêng của luồng này: đọc hàng loạt là chỗ người ta hay với tới `IgnoreQueryFilters` vì lý do hiệu năng, và luật **M5** buộc mọi lời gọi đó nằm trong allowlist đã khai, còn luật **M6** buộc **nêu tên filter** được bỏ.

Gọi `IgnoreQueryFilters` **không tham số** bỏ **cả hai** filter cùng lúc: người viết định bỏ lọc xoá mềm lại bỏ luôn lọc đơn vị. Kết quả là một tệp chứa dữ liệu của mọi đơn vị, giao ra ngoài hệ.

## 6. Câu chưa trả lời được

- **Giới hạn số dòng khai ở đâu và theo cái gì?** [`../contracts/exports.md`](../contracts/exports.md) §1 nói giới hạn là bắt buộc vì chưa có xuất chạy nền, nhưng không nói giá trị nằm ở cấu hình hay hằng số, và có khác nhau theo đơn vị không.
- **Tệp xuất ra có được lưu lại không?** Nếu có thì nó là một tệp theo luồng `N2` và thừa hưởng mọi câu hỏi chưa trả lời của luồng đó. Nếu không thì người dùng mất kết nối giữa chừng là phải xuất lại từ đầu.
- **Có ghi nhật ký kiểm toán khi ai đó xuất dữ liệu không?** Xuất là thao tác mang dữ liệu ra khỏi hệ. [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) không xếp nó vào danh sách thao tác phải ghi.
