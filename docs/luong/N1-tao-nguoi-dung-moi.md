---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `N1` — Tạo một người dùng mới

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Vì hệ **không có đăng ký**, đây là đường **duy nhất** một tài khoản thường ra đời. Hai đường còn lại sinh tài khoản đều là đường đặc quyền: lệnh bootstrap (`V1`) và tạo đơn vị (`V2`).

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn danh sách người dùng.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.write` | |
| Bộ vai trò đã có quyền | Luồng `P1`. Tạo người rồi gán một vai trò rỗng là tạo một tài khoản không làm được gì |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /users` với tên đăng nhập, email, họ tên | [`../contracts/users.md`](../contracts/users.md) §5 |
| 2 | BE | Tạo tài khoản qua `UserManager`, sinh **mật khẩu tạm** | cùng trên |
| 3 | BE | Bật cờ buộc đổi mật khẩu | cùng trên |
| 4 | BE | Trả `tempPassword` **trong thân phản hồi** | cùng trên |
| 5 | Quản trị | Đọc mật khẩu tạm trên màn, chuyển cho người dùng | thủ công — xem §6 |
| 6 | Quản trị | Gán vai trò: luồng `P2` | |
| 7 | Người dùng | Đăng nhập (`D1`), rồi buộc đổi mật khẩu (`D2`) | |

### Vì sao mật khẩu tạm trả về trong phản hồi

Hệ **không giả định có kênh gửi email**. Trả mật khẩu tạm ngay trên màn nghĩa là việc cấp tài khoản không phụ thuộc vào SMTP có chạy hay không — quan trọng với một hệ nội bộ, nơi việc cấp tài khoản thường diễn ra khi hai người đang ngồi cạnh nhau.

Đánh đổi: mật khẩu tạm hiện trên màn hình, nên nó chỉ sống tới lần đăng nhập đầu — cờ ở bước 3 là thứ ép điều đó.

### Vì sao không có endpoint xoá người dùng

[`../contracts/users.md`](../contracts/users.md) §10 khai rõ lý do. Tóm tắt: một tài khoản đã thao tác thì tên nó nằm trong nhật ký kiểm toán và trong các bản ghi nghiệp vụ; xoá nó làm những bản ghi đó trỏ vào hư không. Đường đúng là **khoá** (luồng `D5`).

## 4. Hỏng ở đâu — và ai thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Identity từ chối | `CORE.USER.CREATE_FAILED` (422) | Lý do ở `fieldErrors` — thường là mật khẩu tạm không đạt chính sách |
| Thiếu quyền | `CORE.AUTH.FORBIDDEN` (403) | |
| Tạo xong, quên bước 6 | không có mã lỗi | 🛑 Tài khoản đăng nhập được, đổi mật khẩu xong, rồi **không làm được gì**. Cùng triệu chứng với `P1` bước 5 và `V2` bước 5 — ba nguyên nhân khác nhau, một biểu hiện |

Ba luồng cho ra cùng một triệu chứng là lý do màn quản trị nên phân biệt được **"chưa có vai trò"** với **"vai trò chưa có quyền"**.

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Tài khoản mới mang `tenant_id` của người tạo ra nó — không có ô chọn đơn vị trên form, và **không nên có**: cho quản trị đơn vị A tạo tài khoản ở đơn vị B là mở đúng cánh cửa mà mô hình này đóng.

## 6. Câu chưa trả lời được

- **Mật khẩu tạm do hệ sinh hay quản trị gõ?** Hợp đồng `POST /users` trả `tempPassword` về, tức **hệ sinh**. Nhưng luồng `D4` (đặt lại mật khẩu) lại đòi quản trị **gõ vào** `tempPassword`. Hai luồng cùng làm một việc theo hai cách ngược nhau, và không file nào giải thích vì sao.
- **Mật khẩu tạm có vào nhật ký hoặc vào lịch sử trình duyệt không?** Nó nằm trong thân phản hồi HTTP. Nếu tầng ghi log có ghi thân phản hồi thì mật khẩu tạm vào log.
- **Tên đăng nhập trùng được kiểm ở phạm vi nào?** Duy nhất **trong một đơn vị**, theo mô hình multi-tenant — nhưng hợp đồng không nêu mã lỗi cho ca trùng.
