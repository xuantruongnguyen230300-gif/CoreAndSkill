---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Quản trị vai trò

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Ma trận vai trò × quyền: [`permissions.md`](permissions.md). Gán vai trò cho người dùng: [`users.md`](users.md). Bảng dữ liệu: [`../database/schema-core.md`](../database/schema-core.md) §4.2.

> **Vai trò là DỮ LIỆU, không phải hằng số trong code** ([`../adr/0005-permission-based.md`](../adr/0005-permission-based.md)). Thêm một vai trò là thêm một dòng, không phải một lần triển khai. Đó là lý do file này tồn tại.

---

## 1. `GET /api/v1/core/roles`

**Status:** DRAFT
**Quyền:** `core.role.read`

Danh sách vai trò của đơn vị hiện hành, phân trang theo khuôn chung ([`README.md`](README.md) §8).

### Response 200

```json
{
  "success": true,
  "data": {
    "items": [
      { "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234", "code": "QUAN_TRI", "name": "Quản trị hệ thống", "isSystem": true, "userCount": 3 }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000061"
}
```

`userCount` có mặt để màn hình cảnh báo **trước** khi người dùng bấm xoá, thay vì để họ bấm rồi nhận lỗi.

---

## 2. `POST /api/v1/core/roles`

**Status:** DRAFT
**Quyền:** `core.role.write`

### Request

```json
{ "code": "KE_TOAN_TRUONG", "name": "Kế toán trưởng" }
```

| Field | Ghi chú |
| --- | --- |
| `code` | Mã ổn định, chữ HOA và gạch dưới. **Không đổi được** sau khi tạo — nó là thứ dữ liệu khác tham chiếu tới |
| `name` | Tên hiển thị, đổi được |

Vai trò mới được tạo **không có quyền nào**. Cấp quyền là việc của màn ma trận ([`permissions.md`](permissions.md)) — hai bước tách nhau, vì trộn chúng vào một lời gọi thì một nửa hỏng là cả hai không rõ đã tới đâu.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu field, `code` sai khuôn |
| `CORE.AUTH.FORBIDDEN` | `Forbidden` | 403 | Thiếu quyền `core.role.write` |
| `CORE.ROLE.CODE_DUPLICATE` | `Conflict` | 409 | `code` đã tồn tại trong đơn vị này |

---

## 3. `PUT /api/v1/core/roles/{id}`

**Status:** DRAFT
**Quyền:** `core.role.write`

Chỉ đổi `name`. `code` không đổi được.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.ROLE.NAME_REQUIRED` | `Validation` | 400 | `name` rỗng |
| `CORE.ROLE.NOT_FOUND` | `NotFound` | 404 | Không có vai trò đó trong đơn vị này |
| `CORE.ROLE.SYSTEM_IMMUTABLE` | `BusinessRule` | 422 | Vai trò hệ thống — không đổi tên |

---

## 4. `DELETE /api/v1/core/roles/{id}`

**Status:** DRAFT
**Quyền:** `core.role.write`

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.ROLE.NOT_FOUND` | `NotFound` | 404 | Không có vai trò đó |
| `CORE.ROLE.SYSTEM_IMMUTABLE` | `BusinessRule` | 422 | Vai trò hệ thống — không xoá |
| `CORE.ROLE.IN_USE` | `BusinessRule` | 422 | Còn người dùng mang vai trò này. Thông báo **phải** nói rõ còn bao nhiêu người |

### Ghi chú

**Không xoá vai trò đang có người dùng** — và đây là lý do `userCount` có mặt ở §1. Cách đúng khi muốn bỏ một vai trò: gỡ vai trò khỏi từng người ([`users.md`](users.md)) rồi mới xoá. Tự động gỡ hộ là âm thầm hạ quyền của một nhóm người mà không ai thấy.

**Vai trò hệ thống không sửa, không xoá.** Đó là vai trò mà bước seed dựng lên để hệ thống còn có người quản trị; xoá được nó là tự khoá mình ra ngoài.

**Mọi thao tác ở file này ghi nhật ký kiểm toán.** Đổi vai trò là đổi ai làm được gì.
