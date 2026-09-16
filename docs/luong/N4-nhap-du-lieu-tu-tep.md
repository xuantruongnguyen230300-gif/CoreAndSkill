---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N4` — Nhập dữ liệu từ tệp

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Nhập **luôn** chạy nền sau khi qua trần số dòng: request trả `jobId`, kết quả hỏi qua bản ghi việc. Không có đường đồng bộ để chọn nhầm.

---

## 1. Ai bắt đầu, ở đâu

Người dùng đã đăng nhập, từ nút **Nhập** trên một màn danh sách.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền ghi của tài nguyên đó | Nhập là ghi hàng loạt, không phải đọc |
| Tệp đúng định dạng | CSV hoặc Excel |
| Số dòng không vượt trần `Core:Import:MaxRows` | Vượt thì bị từ chối ngay, không tạo việc — giá trị ở [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6 |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người dùng | `POST /api/v1/<khu>/<tài nguyên>/import` kèm tệp | [`../contracts/exports.md`](../contracts/exports.md) §2 |
| 2 | BE | Đếm số dòng **trước** mọi việc khác; vượt trần ⇒ dừng và báo | cùng trên |
| 3 | BE | Lưu tệp gốc vào kho tạm, tạo bản ghi việc, trả `jobId` | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §10.3 |
| 4 | Việc nền | Kiểm từng dòng, thu **lỗi theo từng dòng** thay vì dừng ở dòng đầu tiên hỏng; ghi theo lô | [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §4, §6 |
| 5 | FE | Hỏi `GET /api/v1/core/jobs/{id}` theo chu kỳ tới khi việc kết thúc; kết quả: số dòng thành công và danh sách dòng hỏng kèm lý do | [`../contracts/jobs.md`](../contracts/jobs.md) §1 |
| 6 | Người dùng | Sửa các dòng hỏng, nhập lại phần đó | |

### Vì sao lỗi theo từng dòng, không dừng ở dòng đầu tiên

Một tệp 500 dòng sai 3 dòng mà dừng ở dòng đầu tiên hỏng thì người dùng phải nhập lại **sáu lần** để tìm hết lỗi. Trả cả danh sách một lượt là khác biệt giữa một công cụ dùng được và một công cụ ai cũng né.

### Một ràng buộc dễ quên

**Tệp gốc phải được giữ lại tới khi việc kết thúc** ([`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §6) — nếu không, việc chạy lại sau một lần hỏng sẽ không còn gì để đọc.

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/exports.md`](../contracts/exports.md) §2. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Vượt trần số dòng | `CORE.IMPORT.TOO_MANY_ROWS` | Nên nêu số dòng thực tế và giới hạn |
| Vài dòng sai dữ liệu | không phải lỗi của request lẫn của việc | Danh sách dòng hỏng kèm **số dòng** và lý do từng dòng, trong `result.failed` của việc |
| Tệp sai định dạng | lỗi kiểm hợp lệ | |
| Tệp không đọc được giữa chừng | việc `failed`, lý do ở `error` của việc | [`../contracts/jobs.md`](../contracts/jobs.md) §1 |
| Nhập hai lần cùng một tệp | `CORE.IMPORT.DUPLICATE_ROW` trên từng dòng của `result.failed` | Dòng trùng khoá tự nhiên bị bỏ qua, không nhân đôi — [`../contracts/exports.md`](../contracts/exports.md) §2 |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Mọi dòng ghi ra đều mang `tenant_id` của người nhập.

Rủi ro riêng: tệp nhập có thể **chứa sẵn một cột định danh**, và nếu định danh đó trỏ tới bản ghi của đơn vị khác thì thao tác nhập trở thành thao tác **sửa dữ liệu đơn vị khác**. Bộ lọc đơn vị chặn được khi đọc; phần ghi phải kiểm tường minh.

Việc nền chạy **ngoài** request nên không thừa hưởng ngữ cảnh đơn vị từ phiên — cùng câu hỏi của luồng `N5`, trả lời ở §6.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Ngữ cảnh đơn vị và người kích hoạt đi theo dòng outbox tới việc nền ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1, mục *Danh tính và đơn vị khi không có request*); chống trùng: §4; theo dõi việc nền: `GET /api/v1/core/jobs/{id}` ([`../contracts/jobs.md`](../contracts/jobs.md)).
