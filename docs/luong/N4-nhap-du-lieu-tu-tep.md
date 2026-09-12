---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N4` — Nhập dữ liệu từ tệp

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng có **hai đường**: đồng bộ cho tệp nhỏ, chạy nền cho tệp lớn. Chọn nhầm đường không gây lỗi — nó gây một request treo.

---

## 1. Ai bắt đầu, ở đâu

Người dùng đã đăng nhập, từ nút **Nhập** trên một màn danh sách.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền ghi của tài nguyên đó | Nhập là ghi hàng loạt, không phải đọc |
| Tệp đúng định dạng | CSV hoặc Excel |
| Số dòng dưới giới hạn của đường đồng bộ | Vượt thì phải đi đường chạy nền |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người dùng | `POST /<khu>/<tài nguyên>/import` kèm tệp | [`../contracts/exports.md`](../contracts/exports.md) §2 |
| 2 | BE | Đếm số dòng **trước khi** xử lý | cùng trên |
| 3 | BE | Vượt giới hạn ⇒ dừng và báo; dưới giới hạn ⇒ xử lý đồng bộ | [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §3 |
| 4 | BE | Kiểm từng dòng, thu **lỗi theo từng dòng** thay vì dừng ở dòng đầu tiên hỏng | cùng trên §4 |
| 5 | BE | Trả kết quả: số dòng thành công, và danh sách dòng hỏng kèm lý do | cùng trên |
| 6 | Người dùng | Sửa các dòng hỏng, nhập lại phần đó | |

### Vì sao lỗi theo từng dòng, không dừng ở dòng đầu tiên

Một tệp 500 dòng sai 3 dòng mà dừng ở dòng đầu tiên hỏng thì người dùng phải nhập lại **sáu lần** để tìm hết lỗi. Trả cả danh sách một lượt là khác biệt giữa một công cụ dùng được và một công cụ ai cũng né.

### Đường chạy nền, và một ràng buộc dễ quên

Với tệp lớn, việc xử lý chuyển sang một công việc nền theo lô. Ràng buộc kèm theo: **tệp gốc phải được giữ lại tới khi công việc kết thúc** ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6) — nếu không, công việc chạy lại sau một lần hỏng sẽ không còn gì để đọc.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Vượt giới hạn số dòng | `CORE.IMPORT.TOO_MANY_ROWS` (422) | Nên nêu số dòng thực tế và giới hạn |
| Vài dòng sai dữ liệu | không phải lỗi cả request | Danh sách dòng hỏng kèm **số dòng** và lý do từng dòng |
| Tệp sai định dạng | lỗi kiểm hợp lệ | |
| Nhập hai lần cùng một tệp | không có mã lỗi | 🛑 Không file nào nói việc nhập có chống trùng hay không. Nhập lại một tệp đã nhập có thể nhân đôi dữ liệu |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Mọi dòng ghi ra đều mang `tenant_id` của người nhập.

Rủi ro riêng: tệp nhập có thể **chứa sẵn một cột định danh**, và nếu định danh đó trỏ tới bản ghi của đơn vị khác thì thao tác nhập trở thành thao tác **sửa dữ liệu đơn vị khác**. Bộ lọc đơn vị chặn được khi đọc; phần ghi phải kiểm tường minh.

Với đường chạy nền, thêm đúng câu hỏi của luồng `N5`: công việc chạy **ngoài** request, nên nó không thừa hưởng ngữ cảnh đơn vị từ phiên.

## 6. Câu chưa trả lời được

- **Công việc nền mở ngữ cảnh đơn vị bằng cách nào?** Cùng lỗ với luồng `N5` §6. Ở đây hậu quả nặng hơn vì công việc nền **ghi** dữ liệu, không chỉ đọc.
- **Nhập có chống trùng không?** Không file nào nêu. Với nhập liệu, chạy lại được mà không nhân đôi dữ liệu là yêu cầu cơ bản — cùng nguyên tắc idempotent mà [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md) đặt cho seed.
- **Người dùng theo dõi một công việc nền ở đâu?** Không endpoint nào trong `contracts/` trả trạng thái công việc. Hôm nay một lần nhập lớn là một thao tác **không quan sát được** sau khi request trả về.
