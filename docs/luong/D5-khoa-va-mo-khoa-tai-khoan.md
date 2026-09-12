---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `D5` — Khoá và mở khoá một tài khoản

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Có **hai** đường làm tài khoản bị khoá, và chúng khác nhau ở chỗ ai mở lại được. Lẫn hai đường này là nguồn của phần lớn hiểu nhầm ở màn quản trị người dùng.

---

## 1. Ai bắt đầu, ở đâu

Hai khởi điểm khác nhau:

| Đường | Ai bắt đầu |
| --- | --- |
| **Khoá tự động** | Không ai — hệ khoá sau N lần đăng nhập sai liên tiếp |
| **Khoá thủ công** | Quản trị đơn vị, từ màn chi tiết người dùng |

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.lock` | Chỉ cho đường thủ công |
| Người dùng đích thuộc cùng đơn vị | |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /users/{id}/lock` hoặc `/unlock` | [`../contracts/users.md`](../contracts/users.md) §8 |
| 2 | BE | Đặt mốc hết khoá qua `UserManager` — **không ghi cột bằng SQL tay** | [`../database/schema-core.md`](../database/schema-core.md) §4.1 |
| 3 | BE | Identity cập nhật `security_stamp` cùng lượt ⇒ phiên đang chạy bị vô hiệu | cùng trên |

### Vì sao không ghi cột khoá bằng SQL

`UserManager` đặt mốc hết khoá **và** cập nhật `security_stamp` trong cùng một thao tác. Ghi thẳng vào cột bằng SQL thì chỉ làm được nửa đầu — và nửa sau mới là nửa đá người đang đăng nhập ra.

Kết quả của việc bỏ nửa sau: tài khoản hiện là "đã khoá" trên màn quản trị, còn người bị khoá vẫn thao tác bình thường.

### Ba điều dễ sai của khoá tự động

| # | Điều | Vì sao |
| --- | --- | --- |
| 1 | **Khoá theo tài khoản là chưa đủ** | Kẻ tấn công thử một mật khẩu phổ biến trên hàng nghìn tài khoản thì không tài khoản nào chạm ngưỡng |
| 2 | **Khoá theo tài khoản mở đường cho tấn công từ chối dịch vụ nhắm vào một người** | Biết tên đăng nhập là khoá được người đó |
| 3 | **Thông điệp không được tiết lộ tài khoản có tồn tại hay không** | [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) |

Điều 1 và 2 kéo theo: khoá theo tài khoản phải đi **cùng** hạn mức theo nhịp gọi, không thay thế cho nó.

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Tài khoản bị khoá, cố đăng nhập | `CORE.AUTH.LOCKED_OUT` (422) | **Câu riêng**, khác "sai thông tin đăng nhập". Người dùng thật cần biết để đi tìm quản trị — đánh đổi có chủ ý |
| Chạm hạn mức theo nhịp gọi | `CORE.RATE_LIMIT.EXCEEDED` (429) | **Khác** 422: 429 tự hết sau `Retry-After`, 422 cần quản trị mở |
| Thiếu quyền khoá | `CORE.AUTH.FORBIDDEN` (403) | |
| Khoá bằng SQL tay | không có mã lỗi | 🛑 Màn quản trị hiện "đã khoá", người bị khoá vẫn làm việc bình thường |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Người thao tác và người bị tác động cùng đơn vị, như luồng `D4`.

Khoá tự động thì không có người thao tác, nhưng bản ghi vẫn nằm trong một đơn vị — và ngưỡng khoá áp cho tài khoản đó, không áp cho toàn hệ.

## 6. Câu chưa trả lời được

- **Ngưỡng N lần sai và thời gian khoá là bao nhiêu, khai ở đâu?** [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2 mô tả cơ chế nhưng không chốt giá trị, và không nói giá trị đó có khác nhau theo đơn vị không.
- **Khoá thủ công và khoá tự động dùng chung một cột — màn quản trị phân biệt bằng gì?** Hai đường có ý nghĩa vận hành khác nhau (một là nghi ngờ bảo mật, một là hệ tự bảo vệ), nhưng nếu chúng để lại cùng một dấu vết thì quản trị không biết mình đang nhìn ca nào.
- **Mở khoá có đá phiên không?** Bước 3 nói khoá thì đá. Mở khoá thì không cần đá, nhưng nếu `security_stamp` cũng đổi theo thì người vừa được mở khoá phải đăng nhập lại — hành vi đó nên nói rõ thay vì để người dùng phát hiện.
