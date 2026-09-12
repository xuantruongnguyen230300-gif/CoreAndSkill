---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `D4` — Quản trị đặt lại mật khẩu cho người khác

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng này **chấm dứt mọi phiên đang mở** của người bị tác động. Đó là phần quan trọng nhất, không phải phần phụ — xem §3.

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn chi tiết một người dùng.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.reset-password` | **Khoá riêng**, tách khỏi `core.user.write` |
| Người dùng đích thuộc cùng đơn vị | |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /users/{id}/reset-password` | [`../contracts/users.md`](../contracts/users.md) §9 |
| 2 | BE | Đặt mật khẩu tạm qua `UserManager` | cùng trên |
| 3 | BE | Bật cờ buộc đổi mật khẩu | cùng trên |
| 4 | BE | 🛑 **Chấm dứt mọi phiên đang mở của tài khoản đó** | cùng trên |
| 5 | Quản trị | Chuyển mật khẩu tạm cho người dùng | thủ công — xem §6 |
| 6 | Người dùng | Đăng nhập, rồi đi tiếp bằng luồng `D2` | |

### Vì sao bước 4 là phần quan trọng nhất

Lý do người ta đặt lại mật khẩu cho người khác thường là **tài khoản nghi bị chiếm**. Đổi mật khẩu mà không đá phiên cũ ra thì kẻ đang giữ phiên vẫn thao tác bình thường — và người quản trị tin rằng mình đã xử lý xong.

Cùng cơ chế với việc khoá tài khoản ở luồng `D5`: Identity cập nhật `security_stamp` cùng lượt, và **bỏ qua bước đó nghĩa là phiên đang chạy vẫn sống**.

### Vì sao khoá quyền tách riêng

Đặt lại được mật khẩu của người khác là đặt lại được mật khẩu của **quản trị khác**. Gộp nó vào `core.user.write` thì mọi người sửa được email cũng chiếm được tài khoản bất kỳ.

## 4. Hỏng ở đâu — và ai thấy gì

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Thiếu quyền | `CORE.AUTH.FORBIDDEN` (403) | |
| Thiếu mật khẩu tạm trong request | `CORE.VALIDATION.FAILED` (400) | |
| Mật khẩu tạm không đạt chính sách | `CORE.USER.RESET_PASSWORD_FAILED` (422) | Lý do ở `fieldErrors` |
| Bước 4 bị bỏ | không có mã lỗi | 🛑 Phiên của người bị nghi vẫn sống. Quản trị thấy "đã đặt lại thành công" và tin là xong |
| Người dùng đích là quản trị cuối cùng | xem [`../contracts/users.md`](../contracts/users.md) §2 | Bốn luật bảo vệ tồn tại để ngăn một đơn vị tự khoá mình ra ngoài |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Cả người thao tác lẫn người bị tác động phải thuộc cùng một đơn vị.

Khác luồng `V2` và `V3` ở đúng điểm này: ở đó người thao tác cố ý đứng ngoài đơn vị bị tác động; ở đây, đứng ngoài là **lỗi**.

## 6. Câu chưa trả lời được

- **Mật khẩu tạm do ai nghĩ ra?** Hợp đồng đòi `tempPassword` trong thân request, tức **quản trị tự gõ**. Nghĩa là chất lượng mật khẩu tạm phụ thuộc thói quen của từng quản trị, và chính sách mật khẩu là hàng rào duy nhất.
- **Quản trị chuyển mật khẩu tạm cho người dùng bằng cách nào?** Bước 5 hoàn toàn thủ công và không file nào mô tả. Trên thực tế nó sẽ đi qua tin nhắn hoặc lời nói — và không có gì buộc nó đổi ngay, ngoài cờ ở bước 3.
- **Có ghi nhật ký kiểm toán không?** Đây là thao tác có thể dùng để chiếm tài khoản người khác một cách hợp lệ. [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5 chưa nêu nó trong danh sách thao tác phải ghi.
