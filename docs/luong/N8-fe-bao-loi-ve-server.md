---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N8` — FE báo lỗi về server

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng duy nhất mà **dữ liệu đi ngược chiều**: trình duyệt kể cho server nghe một chuyện server không chứng kiến. Vì vậy mọi thứ nó gửi đều là **dữ liệu không tin được**.

---

## 1. Ai bắt đầu, ở đâu

FE, tự động, khi bắt được một lỗi chưa xử lý trong trình duyệt.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Token chống giả mạo | Đây là request ghi |
| Mã lần gọi nếu có | Nối lỗi phía trình duyệt với dấu vết phía server |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | Bắt lỗi chưa xử lý ở lớp xử lý lỗi toàn cục | [`../quy-uoc/fe-api-client.md`](../quy-uoc/fe-api-client.md) |
| 2 | FE | `POST /api/v1/core/client-errors` với thông điệp, dấu vết ngăn xếp, đường dẫn màn, mã lần gọi | [`../contracts/client-errors.md`](../contracts/client-errors.md) §1 |
| 3 | BE | Kiểm hợp lệ và **áp hạn mức theo nhịp gọi** | cùng trên |
| 4 | BE | Ghi vào log cùng mã lần gọi | [`../wiki-core/be/07-observability.md`](../wiki-core/be/07-observability.md) |

### Vì sao hạn mức là bắt buộc, không phải tuỳ chọn

Một lỗi lặp trong vòng lặp vẽ lại giao diện có thể bắn **hàng nghìn** request mỗi phút từ **một** tab trình duyệt. Không có hạn mức thì một lỗi FE nhỏ làm ngập log và có thể làm chết chính hệ thống nó đang báo lỗi.

### Vì sao nội dung gửi lên là dữ liệu không tin được

Thân request do trình duyệt soạn, và trình duyệt nằm dưới quyền người dùng. Hệ quả bắt buộc: nội dung đó **chỉ được ghi log**, không bao giờ được dùng làm điều kiện rẽ nhánh, và phải được xử lý như văn bản chứ không như mã.

## 4. Hỏng ở đâu — và ai thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/client-errors.md`](../contracts/client-errors.md) §1. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Thiếu trường bắt buộc | `CORE.VALIDATION.FAILED` | Người dùng **không thấy gì** — đây là luồng chạy ngầm, không được hiện lỗi lên màn |
| Thiếu token chống giả mạo | `CORE.AUTH.CSRF_REJECTED` | |
| Vượt hạn mức | `CORE.RATE_LIMIT.EXCEEDED` kèm `Retry-After` | FE **phải dừng gửi**, không thử lại ngay — thử lại ngay là cách biến hạn mức thành một vòng lặp |
| Bản thân việc báo lỗi lại gây lỗi | không có mã lỗi | 🛑 Vòng lặp vô hạn: lỗi → gửi báo cáo → gửi hỏng → sinh lỗi mới → gửi báo cáo. Lớp xử lý lỗi toàn cục phải tự loại trừ chính nó |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị khi người gửi đã đăng nhập; dùng chung toàn hệ khi chưa.**

Đây là luồng duy nhất chạy được ở **cả hai** trạng thái: một lỗi ở màn đăng nhập cũng cần báo về, và lúc đó chưa có đơn vị nào trong ngữ cảnh.

Hệ quả: bản ghi log phải chịu được `tenant_id` rỗng, và **không** dùng bản ghi này cho bất kỳ truy vấn nào giả định luôn có đơn vị.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Đích là log có cấu trúc (giữ theo chính sách log), BE cắt `stack` và lọc trường nhạy cảm trước khi ghi, chỉ người vận hành đọc ở log — [`../contracts/client-errors.md`](../contracts/client-errors.md) §1, Ghi chú.
