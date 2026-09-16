---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N5` — Thông báo đi qua Outbox tới đúng người nhận

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng duy nhất trong Core **không bắt đầu từ một người**. Nó bắt đầu từ một sự kiện nghiệp vụ, và chạy tiếp **sau khi** request đã trả lời xong.

---

## 1. Ai bắt đầu, ở đâu

**Không ai.** Không có endpoint tạo thông báo — thông báo sinh ra từ một **sự kiện nghiệp vụ** ([`../contracts/notifications.md`](../contracts/notifications.md) §4).

Người dùng chỉ tham gia ở nửa sau: đọc danh sách, đánh dấu đã đọc.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Một thay đổi dữ liệu nghiệp vụ vừa được ghi | Bản ghi Outbox ghi **cùng transaction** với thay đổi đó |
| Tiến trình phát nền đang chạy | Nó đọc Outbox, không đọc trực tiếp dữ liệu nghiệp vụ |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Handler nghiệp vụ | Ghi thay đổi dữ liệu | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) |
| 2 | Handler nghiệp vụ | Ghi một bản ghi Outbox — 🛑 **cùng transaction** với bước 1 | [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2 |
| 3 | — | Transaction commit. Request trả lời xong tại đây | |
| 4 | Tiến trình phát nền | Đọc bản ghi Outbox chưa xử lý | [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2 |
| 5 | Tiến trình phát nền | Xác định người nhận, sinh bản ghi thông báo cho từng người | cùng trên |
| 6 | Tiến trình phát nền | Đánh dấu bản ghi Outbox đã xử lý; hỏng thì tăng số lần thử và hẹn lần sau | [`../database/schema-core.md`](../database/schema-core.md) §8 |
| 7 | Người dùng | `GET /api/v1/core/notifications` · `GET /api/v1/core/notifications/unread-count` | [`../contracts/notifications.md`](../contracts/notifications.md) §1, §2 |
| 8 | Người dùng | `PUT /api/v1/core/notifications/{id}/read` hoặc `PUT /api/v1/core/notifications/read-all` | cùng trên §3, §4 |

### Vì sao bước 2 phải cùng transaction với bước 1

Ghi ngoài transaction thì hai ca hỏng, mỗi ca một kiểu:

| Ghi Outbox **trước** khi commit dữ liệu | Ghi Outbox **sau** khi commit dữ liệu |
| --- | --- |
| Dữ liệu rollback, thông báo vẫn gửi. Người nhận được báo về một việc **chưa từng xảy ra** | Tiến trình chết giữa hai lệnh. Việc đã xảy ra mà **không ai được báo**, và không có dấu vết nào để dò lại |

Cùng transaction thì cả hai ca biến mất: hoặc cả hai cùng có, hoặc cả hai cùng không.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Biểu hiện |
| --- | --- |
| Tiến trình phát nền dừng | **Không ai thấy gì.** Dữ liệu vẫn đúng, request vẫn nhanh, chỉ là không thông báo nào tới. Bản ghi Outbox dồn lại và không có màn nào hiển thị con số đó |
| Gửi hỏng lặp lại | Số lần thử tăng, hẹn lần sau xa dần; chạm ngưỡng thì bản ghi thành `dead` và health check báo `Degraded` — [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2.5 |
| Đọc thông báo của người khác | `CORE.NOTIFICATION.NOT_FOUND` ([`../contracts/notifications.md`](../contracts/notifications.md) §3) — gộp "không có" với "không phải của bạn", cùng lý do như luồng `N2` |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị** — bản ghi Outbox và bản ghi thông báo đều mang `tenant_id`.

🛑 **Nhưng luồng này chạy NGOÀI một request** — cùng nhóm với nhập nền ở `N4` — nên nó **không** thừa hưởng ngữ cảnh đơn vị từ phiên như các luồng chạy trong request. Ngữ cảnh phải được mở **theo từng bản ghi Outbox** ở bước 4–5, đúng cách [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.4 mô tả cho seed.

Đây là cùng một lớp lỗi với bước 4 của luồng `D1`: chạy truy vấn khi ngữ cảnh đơn vị còn rỗng. Khác ở chỗ luồng `D1` hỏng ra mặt người dùng, còn ở đây nó hỏng **trong một tiến trình nền mà không ai nhìn**.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Ngữ cảnh đơn vị mở theo **từng dòng** outbox ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1, mục *Danh tính và đơn vị khi không có request*); ngưỡng thử lại, trạng thái `dead` và health check `Degraded`: [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §2.5.
