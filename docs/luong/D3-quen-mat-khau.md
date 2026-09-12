---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `D3` — Quên mật khẩu và tự đặt lại

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Hệ **không có đăng ký**, nên đây là đường tự phục vụ duy nhất của người dùng. Nếu nó hỏng, mọi việc quên mật khẩu đều rơi xuống quản trị (luồng `D4`).

---

## 1. Ai bắt đầu, ở đâu

Người dùng **chưa đăng nhập**, từ liên kết "Quên mật khẩu" trên form đăng nhập.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Mã đơn vị **và** email | Hai ô, không phải một — xem §3 |
| Đơn vị đang hoạt động | Đơn vị ngưng hoạt động thì mọi đường vào đều đóng |
| Kênh gửi hoạt động được | Nếu không có kênh, luồng này không dùng được và `D4` là đường duy nhất |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người dùng | `POST /auth/forgot-password` với `(mã đơn vị, email)` | [`../contracts/auth.md`](../contracts/auth.md) §8 |
| 2 | BE | Tra đơn vị, nạp ngữ cảnh, rồi mới tra tài khoản | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.2 |
| 3 | BE | Sinh token đặt lại gắn với **đúng một** tài khoản, gửi qua kênh đã cấu hình | cùng trên |
| 4 | BE | Trả về **cùng một phản hồi** dù email có tồn tại hay không | [`../contracts/auth.md`](../contracts/auth.md) §8 |
| 5 | Người dùng | Mở liên kết, `POST /auth/reset-password` với token và mật khẩu mới | cùng trên §9 |
| 6 | BE | Kiểm token, đổi mật khẩu | |

### Vì sao hỏi mã đơn vị, không chỉ hỏi email

**Một địa chỉ email có thể ứng với nhiều tài khoản ở nhiều đơn vị.** Chỉ có email thì không có câu trả lời đúng cho câu hỏi *"đặt lại mật khẩu cho tài khoản nào"*.

Đây là cùng bộ ô với form đăng nhập, và đó là chủ đích: người dùng đã quen gõ mã đơn vị ở màn trước.

### Vì sao bước 4 trả lời giống nhau trong mọi ca

Phân biệt được "email này có tài khoản" với "email này không có" là một công cụ **dò tài khoản**. Kẻ tấn công không cần mật khẩu để dùng nó — chỉ cần biết ai có mặt trong hệ.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Biểu hiện |
| --- | --- |
| Email không tồn tại trong đơn vị đó | **Cùng phản hồi** với ca thành công. Người gõ nhầm sẽ đợi một email không bao giờ tới — đó là cái giá đã chấp nhận để đổi lấy việc không rò danh sách tài khoản |
| Gõ sai quá nhiều lần | `CORE.RATE_LIMIT.EXCEEDED` (429) kèm `Retry-After` |
| Token hết hạn hoặc đã dùng | Mã lỗi ở [`../contracts/auth.md`](../contracts/auth.md) §9 |
| Kênh gửi chết | 🛑 **Không ai thấy gì.** Phản hồi vẫn là "đã gửi nếu email tồn tại", và người dùng đợi mãi. Cùng lớp lỗi với tiến trình phát nền ở luồng `N5` |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị** — và đây là một trong ba luồng chạy **trước khi** phiên tồn tại, nên ngữ cảnh đơn vị phải được nạp thủ công ở bước 2, y hệt bước 4 của luồng `D1`.

Bỏ bước đó thì việc tra email chạy trên phạm vi sai: hoặc không thấy ai, hoặc thấy tài khoản trùng email **ở đơn vị khác** — và gửi liên kết đặt lại mật khẩu cho nhầm người.

## 6. Câu chưa trả lời được

- **Kênh gửi là kênh nào, và ai cấu hình nó?** Không file nào trong `contracts/` hay `wiki-core/be/` khai kênh gửi email của luồng này. Với một hệ nội bộ, giả định "có SMTP" là một giả định lớn.
- **Token đặt lại sống bao lâu, và dùng được mấy lần?** Identity có cơ chế sẵn, nhưng giá trị cụ thể chưa khai ở đâu.
- **Đặt lại mật khẩu có đá các phiên đang mở không?** Luồng `D4` nói rõ là **có**. Luồng này không nói — mà nếu tài khoản đang bị chiếm thì đây đúng là ca cần đá phiên nhất.
