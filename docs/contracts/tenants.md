---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Quản trị đơn vị (khu hệ thống)

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Quyết định nền: [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md). Vòng đời đơn vị và seed: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10, §11.4. Envelope và mã lỗi: [`README.md`](README.md).

> 🛑 **Mọi endpoint trong file này yêu cầu cờ vận hành hệ thống trên tài khoản**, không phải một quyền trong ma trận quyền. Tài khoản thường — kể cả tài khoản quản trị của một đơn vị — nhận **403** ở mọi endpoint dưới đây.

---

## 1. `GET /api/v1/core/system/tenants`

**Status:** DRAFT
**Quyền:** cờ vận hành hệ thống

Danh sách đơn vị, có phân trang theo khuôn chung ([`README.md`](README.md) §8). Đơn vị hệ thống **không** nằm trong kết quả.

### Response 200

```json
{
  "success": true,
  "data": {
    "items": [
      { "id": "0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234", "code": "SYT-HN", "name": "Sở Y tế Hà Nội", "isActive": true, "createdAt": "2026-09-10T03:12:44Z" }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  },
  "error": null,
  "traceId": "0HNO9S8JAP586:00000021"
}
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.NOT_AUTHENTICATED` | `Unauthorized` | 401 | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | `Forbidden` | 403 | Tài khoản không mang cờ vận hành |

---

## 2. `POST /api/v1/core/system/tenants`

**Status:** DRAFT
**Quyền:** cờ vận hành hệ thống

Tạo một đơn vị và chạy seed cho nó. Đây là **một** thao tác đối với người dùng, dù bên trong gồm nhiều bước.

### Request

```json
{
  "code": "SYT-HN",
  "name": "Sở Y tế Hà Nội",
  "adminUserName": "quantri.syt-hn",
  "adminEmail": "quantri@syt-hn.gov.vn",
  "adminFullName": "Nguyễn Văn An"
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `code` | ✅ | Mã đơn vị người dùng gõ ở màn đăng nhập. Chuẩn hoá về chữ HOA trước khi lưu |
| `name` | ✅ | Tên hiển thị |
| `adminUserName` · `adminEmail` · `adminFullName` | ✅ | Tài khoản quản trị đầu tiên của đơn vị. Tạo qua `UserManager`, mang **hai** cờ: `must_change_password` **và** `has_permission_bypass` ([`../database/schema-core.md`](../database/schema-core.md) §4.1). Thiếu cờ thứ hai thì admin đơn vị mới đăng nhập được nhưng **không làm được gì** — chưa có vai trò nào tồn tại trong đơn vị vừa tạo. Endpoint này là một trong **đúng hai** đường được đặt cờ đó (luật M12) |

### Response 200

`data` là đơn vị vừa tạo, cùng hình dạng một phần tử ở §1. **Không** trả mật khẩu trong thân phản hồi.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.VALIDATION.FAILED` | `Validation` | 400 | Thiếu field, `code` sai khuôn |
| `CORE.AUTH.FORBIDDEN` | `Forbidden` | 403 | Không mang cờ vận hành |
| `CORE.TENANT.CODE_DUPLICATE` | `Conflict` | 409 | `code` đã tồn tại |
| `CORE.TENANT.SEED_FAILED` | `BusinessRule` | 422 | Seed hỏng giữa chừng. Đơn vị được để ở trạng thái **ngưng hoạt động**, không phải hoạt động-nhưng-thiếu-dữ-liệu |

---

## 3. `PUT /api/v1/core/system/tenants/{id}/active`

**Status:** DRAFT
**Quyền:** cờ vận hành hệ thống

Ngưng hoạt động hoặc bật lại một đơn vị. Thân request mang đúng một trường trạng thái.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.AUTH.FORBIDDEN` | `Forbidden` | 403 | Không mang cờ vận hành |
| `CORE.TENANT.NOT_FOUND` | `NotFound` | 404 | Không có đơn vị đó |
| `CORE.TENANT.SYSTEM_IMMUTABLE` | `BusinessRule` | 422 | Không được ngưng hoạt động **đơn vị hệ thống** |

### Ghi chú

**Ngưng hoạt động có hiệu lực ở hai chỗ**, không phải một: bước đăng nhập, và bước dựng danh tính của mỗi request ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10). Chặn chỉ ở đăng nhập thì người đang mở phiên vẫn dùng tiếp tới khi phiên hết.

**Không có endpoint xoá đơn vị** — xoá là thao tác bị loại, không phải bị hoãn (§10 của file trên).

**Mọi thao tác ở file này ghi nhật ký kiểm toán**, kèm danh tính tài khoản vận hành đã thực hiện.
