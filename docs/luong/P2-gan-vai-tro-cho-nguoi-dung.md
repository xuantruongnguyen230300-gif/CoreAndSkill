---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `P2` — Gán vai trò cho một người dùng

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng ngắn nhất trong nhóm phân quyền, và là luồng có **bốn luật bảo vệ riêng** — đọc [`../contracts/users.md`](../contracts/users.md) §2 trước.

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn chi tiết một người dùng.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.role.assign` | **Khoá riêng**, tách khỏi `core.user.write` |
| Vai trò đã tồn tại và đã có quyền | Luồng `P1`. Gán một vai trò rỗng là gán một cái tên |
| Người dùng thuộc **cùng đơn vị** | |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | Mở chi tiết người dùng, xem tập vai trò hiện có | [`../contracts/users.md`](../contracts/users.md) §4 |
| 2 | Quản trị | `PUT /users/{id}/roles` với **toàn bộ** tập vai trò mong muốn | cùng trên §7 |
| 3 | BE | Thay thế tập vai trò, không cộng dồn | cùng trên |
| 4 | BE | Kiểm bốn luật bảo vệ tài khoản quản trị | [`../contracts/users.md`](../contracts/users.md) §2 |

### Vì sao gửi toàn bộ tập, không gửi "thêm/bớt"

Gửi thao tác thêm–bớt thì hai người sửa cùng lúc sẽ chồng lên nhau theo cách không ai đoán được. Gửi cả tập thì lần ghi sau thắng, và đó là một hành vi **giải thích được cho người dùng**.

Kèm theo: `roleIds` trùng lặp là lỗi 400, **không** phải "tự lọc trùng" — một mảng có khoá trùng là dấu hiệu FE đang dựng sai, và im lặng bỏ qua nó là giấu lỗi ([`../contracts/users.md`](../contracts/users.md) §7).

## 4. Hỏng ở đâu — và người dùng thấy gì

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Thiếu `core.user.role.assign` | `CORE.AUTH.FORBIDDEN` (403) | Người này sửa được email nhưng không gán được vai trò — đúng thiết kế |
| `roleIds` có phần tử trùng | `CORE.VALIDATION.FAILED` (400) | Lỗi rõ, không tự dọn hộ |
| Vai trò không thuộc đơn vị này | `CORE.ROLE.NOT_FOUND` (404) | |
| Tự gỡ vai trò cuối cùng của chính mình | xem §2 của hợp đồng | Bốn luật bảo vệ tồn tại để ngăn một đơn vị **tự khoá mình ra ngoài** |
| Gán vai trò rỗng quyền | không có mã lỗi | 🛑 Người dùng có vai trò, đăng nhập được, bấm gì cũng bị từ chối. Cùng triệu chứng với `P1` bước 5 bị quên |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Cả người dùng lẫn vai trò đều mang `tenant_id`, và bước 3 phải kiểm **cả hai cùng thuộc đơn vị của người đang thao tác**.

Kiểm một vế là chưa đủ: một quản trị của đơn vị A tra đúng người dùng của đơn vị mình nhưng truyền vào định danh một vai trò của đơn vị B thì tạo ra một dòng gán **xuyên đơn vị** — và dòng đó không vi phạm khoá ngoại nào.

## 6. Câu chưa trả lời được

- **Gán vai trò cho người đang có phiên mở thì phiên đó đổi quyền lúc nào?** Cùng câu hỏi chưa trả lời với luồng `P1`.
- **Tài khoản mang cờ `has_permission_bypass` có gán vai trò được không?** Luật **M9** cấm gán vai trò nghiệp vụ cho tài khoản mang `is_system_operator`, nhưng không nói gì về cờ còn lại. Gán vai trò cho một tài khoản vốn đã bỏ qua mọi kiểm quyền là vô nghĩa, nhưng "vô nghĩa" khác "bị chặn".
