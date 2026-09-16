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

### Request — query string

| Tham số | Mặc định | Ghi chú |
| --- | --- | --- |
| `page` · `pageSize` · `sortBy` · `sortDescending` | | Theo quy ước chung, [`README.md`](README.md) §8 |
| `sortBy` allowlist | `name` (tăng dần) | `name`, `createdAt` |
| `searchText` | — | Khớp một phần trên `name`; không phân biệt hoa thường. Tối đa 200 ký tự |

### Response 200

```json
{
  "success": true,
  "data": {
    "items": [
      { "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234", "name": "Quản trị hệ thống", "isSystem": true, "userCount": 3, "createdAt": "2026-09-10T03:12:44Z" }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  },
  "error": null,
  "traceId": "5d5705f6a8446bb7e0a8119e513f5fd5"
}
```

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `name` | string | Tên hiển thị |
| `isSystem` | bool | Vai trò hệ thống — không sửa, không xoá (§3, §4) |
| `userCount` | int | Số người dùng đang mang vai trò này |
| `createdAt` | string \| null | Ứng với cột `created_at` cho phép NULL ([`../database/schema-core.md`](../database/schema-core.md) §4.2, §3.2) |

`userCount` có mặt để màn hình cảnh báo **trước** khi người dùng bấm xoá, thay vì để họ bấm rồi nhận lỗi.

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Tham số danh sách ngoài khoảng hợp lệ, `searchText` quá dài ([`README.md`](README.md) §8). `fieldErrors` mang khoá của tham số sai |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Thiếu quyền `core.role.read` |

---

## 2. `POST /api/v1/core/roles`

**Status:** DRAFT
**Quyền:** `core.role.write`

### Request

```json
{ "name": "Kế toán trưởng" }
```

| Field | Ghi chú |
| --- | --- |
| `name` | Tên hiển thị, đổi được. **Duy nhất trong đơn vị**, so trên tên đã chuẩn hoá — tức không phân biệt hoa thường (`RoleNameIndex`, [`../database/schema-core.md`](../database/schema-core.md) §4.2) |

Vai trò mới được tạo **không có quyền nào**. Cấp quyền là việc của màn ma trận ([`permissions.md`](permissions.md)) — hai bước tách nhau, vì trộn chúng vào một lời gọi thì một nửa hỏng là cả hai không rõ đã tới đâu.

### Response 201

```json
{
  "success": true,
  "data": { "id": "0192f3c2-1111-7000-8000-000000000002" },
  "error": null,
  "traceId": "9c2f0ad5b4e1476a8c3d1e7f5a0b2c64"
}
```

Kèm header `Location: /api/v1/core/roles/0192f3c2-1111-7000-8000-000000000002`. Cùng khuôn với tạo người dùng ([`users.md`](users.md) §5): `data` chỉ mang `id`, FE gọi lại §1 nếu cần dòng đầy đủ.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.ROLE.NAME_DUPLICATE` | `Conflict` | 409 | `name` trùng tên một vai trò khác trong đơn vị này |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu `name`. Kèm `fieldErrors["Name"]` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu quyền `core.role.write` |

---

## 3. `PUT /api/v1/core/roles/{id}`

**Status:** DRAFT
**Quyền:** `core.role.write`

Chỉ đổi `name`.

### Request

```json
{ "name": "Kế toán trưởng" }
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `name` | ✔ | Cùng ràng buộc với §2 — duy nhất trong đơn vị, so trên tên đã chuẩn hoá |

Payload **không** có chỗ cho quyền: cấp quyền đi đường ma trận ([`permissions.md`](permissions.md) §6), cùng lý do tách hai bước ở §2.

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "f1a70c6b2d8e4539a0b6c4d2e8f13579" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.ROLE.NOT_FOUND` | `NotFound` | 404 | Không có vai trò đó trong đơn vị này |
| `CORE.ROLE.NAME_DUPLICATE` | `Conflict` | 409 | `name` mới trùng tên một vai trò khác trong đơn vị này |
| `CORE.ROLE.SYSTEM_IMMUTABLE` | `BusinessRule` | 422 | Vai trò hệ thống — không đổi tên |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | `name` rỗng. Kèm `fieldErrors["Name"]` — như §2 |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu quyền `core.role.write` |

---

## 4. `DELETE /api/v1/core/roles/{id}`

**Status:** DRAFT
**Quyền:** `core.role.write`

### Request

Không có body. Vai trò xác định bằng `{id}` trên đường dẫn.

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "3e5c9b1470af62d8e1c05b7a4936f2d8" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.ROLE.NOT_FOUND` | `NotFound` | 404 | Không có vai trò đó |
| `CORE.ROLE.SYSTEM_IMMUTABLE` | `BusinessRule` | 422 | Vai trò hệ thống — không xoá |
| `CORE.ROLE.IN_USE` | `BusinessRule` | 422 | Còn người dùng mang vai trò này. Kèm `messageParams` khoá `Count` — số người đang mang vai trò. Thông báo **phải** nói rõ số đó |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Thiếu quyền `core.role.write` |

```json
{
  "code": "CORE.ROLE.IN_USE",
  "type": "BusinessRule",
  "message": "Vai trò còn 3 người dùng.",
  "messageParams": { "Count": "3" },
  "fieldErrors": null
}
```

### Ghi chú

**`Count` tính lúc xoá, không phải `userCount` FE đang giữ.** Danh sách ở §1 có thể đã cũ khi người dùng bấm xoá; câu hiển thị dựng từ `messageParams` của chính lỗi này.

**Không xoá vai trò đang có người dùng** — và đây là lý do `userCount` có mặt ở §1. Cách đúng khi muốn bỏ một vai trò: gỡ vai trò khỏi từng người ([`users.md`](users.md)) rồi mới xoá. Tự động gỡ hộ là âm thầm hạ quyền của một nhóm người mà không ai thấy.

**Vai trò hệ thống không sửa, không xoá.** Vai trò mang `is_system` là vai trò mặc định do nguồn seed của dự án khai kèm cờ đó (seam `ITenantSeedSource`, [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)), dựng lúc tạo đơn vị để đơn vị còn đường quản trị; xoá được nó là tự khoá mình ra ngoài. Không nguồn nào khai vai trò thì đơn vị không có vai trò hệ thống nào — đường quản trị lúc đó là cờ `has_permission_bypass` của tài khoản quản trị đầu tiên ([`tenants.md`](tenants.md) §2).

**Mọi thao tác ở file này ghi nhật ký kiểm toán.** Đổi vai trò là đổi ai làm được gì.

---

## 5. `GET /api/v1/core/roles/{id}`

**Status:** DRAFT
**Quyền:** `core.role.read`

Một vai trò của đơn vị hiện hành, theo `{id}`. Có mặt để màn hình nhận `roleId` từ URL (ví dụ chip lọc ở màn người dùng) dựng được tên vai trò mà không phải tải cả danh sách §1.

### Request

Không có tham số ngoài `{id}` trên đường dẫn.

### Response 200

`data` là **một phần tử `items` của §1, cùng shape** — `id`, `name`, `isSystem`, `userCount`, `createdAt`. Không mang tập quyền của vai trò: thứ đó đi đường ma trận ([`permissions.md`](permissions.md)).

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.ROLE.NOT_FOUND` | `NotFound` | 404 | Không có vai trò đó trong đơn vị này — kể cả khi `{id}` là vai trò của một đơn vị khác (luật M7) |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Thiếu quyền `core.role.read` |

### Ghi chú

**`userCount` ở đây tính lúc gọi**, như ở §1 — vẫn có thể cũ khi người dùng bấm xoá; số dùng để hiển thị trong lỗi xoá là `messageParams` của `CORE.ROLE.IN_USE` (§4).
