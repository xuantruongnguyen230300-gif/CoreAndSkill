---
kind: luat
scope: core
verified: chua-doi-chieu
---

# `P2` — Gán vai trò cho một người dùng

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.**

> Luồng ngắn nhất trong nhóm phân quyền, và là luồng chịu **các luật bảo vệ tài khoản quản trị** — đọc [`../contracts/users.md`](../contracts/users.md) §2 trước.

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
| 2 | Quản trị | `PUT /api/v1/core/users/{id}/roles` với **toàn bộ** tập vai trò mong muốn | cùng trên §7 |
| 3 | BE | Kiểm các luật bảo vệ tài khoản quản trị — **trước khi** chạm tầng ghi | [`../contracts/users.md`](../contracts/users.md) §2 |
| 4 | BE | Thay thế tập vai trò, không cộng dồn | cùng trên §7 |

### Vì sao gửi toàn bộ tập, không gửi "thêm/bớt"

Gửi thao tác thêm–bớt thì hai người sửa cùng lúc sẽ chồng lên nhau theo cách không ai đoán được. Gửi cả tập thì lần ghi sau thắng, và đó là một hành vi **giải thích được cho người dùng**.

Kèm theo: `roleIds` trùng lặp là lỗi kiểm hợp lệ, **không** phải "tự lọc trùng" — một mảng có khoá trùng là dấu hiệu FE đang dựng sai, và im lặng bỏ qua nó là giấu lỗi ([`../contracts/users.md`](../contracts/users.md) §7).

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/users.md`](../contracts/users.md) §7 và §2. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Thiếu `core.user.role.assign` | `CORE.AUTH.FORBIDDEN` | Người này sửa được email nhưng không gán được vai trò — đúng thiết kế |
| `roleIds` có phần tử trùng | `CORE.USER.DUPLICATE_ROLE_ENTRY` | Lỗi rõ, không tự dọn hộ |
| Vai trò không thuộc đơn vị này | `CORE.USER.ROLE_NOT_FOUND` | Bộ lọc đơn vị làm vai trò của đơn vị khác **không tồn tại** với người gọi |
| Tự gỡ vai trò hệ thống của chính mình | `CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN` | Luật 2 ở [`../contracts/users.md`](../contracts/users.md) §2 — một thao tác vô ý không được làm mất quyền quản trị mà không lỗi nào bật ra |
| Gán vai trò rỗng quyền | không có mã lỗi | 🛑 Người dùng có vai trò, đăng nhập được, bấm gì cũng bị từ chối. Cùng triệu chứng với `P1` bước 5 bị quên |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Cả người dùng lẫn vai trò đều mang `tenant_id`, và bước 3 phải kiểm **cả hai cùng thuộc đơn vị của người đang thao tác**.

Kiểm một vế là chưa đủ: một quản trị của đơn vị A tra đúng người dùng của đơn vị mình nhưng truyền vào định danh một vai trò của đơn vị B thì tạo ra một dòng gán **xuyên đơn vị** — và dòng đó không vi phạm khoá ngoại nào.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Tài khoản mang `has_permission_bypass` gán vai trò được, BE không chặn ([`../contracts/users.md`](../contracts/users.md) §7).
