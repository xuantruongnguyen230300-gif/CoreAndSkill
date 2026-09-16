---
kind: luat
scope: core
verified: chua-doi-chieu
---

# Contract card — Quản trị đơn vị (khu hệ thống)

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Mọi card trong file này mang `Status: DRAFT`.
>
> Quyết định nền: [`../adr/0017-khu-quan-tri-he-thong.md`](../adr/0017-khu-quan-tri-he-thong.md). Service tạo đơn vị dùng chung: [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md). Khôi phục tài khoản quản trị đơn vị: [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md). Vòng đời đơn vị và seed: [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10, §11.4. Envelope và mã lỗi: [`README.md`](README.md).

> 🛑 **Mọi endpoint trong file này mang mức `[RequireSystemOperator]`** (luật S11, [`../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md)) — cờ vận hành hệ thống trên tài khoản, không phải một quyền trong ma trận quyền. Tài khoản thường — kể cả tài khoản quản trị của một đơn vị — nhận **403** ở mọi endpoint dưới đây.

---

## 1. `GET /api/v1/core/system/tenants`

**Status:** DRAFT
**Quyền:** `[RequireSystemOperator]`

Danh sách đơn vị, có phân trang theo khuôn chung ([`README.md`](README.md) §8). Đơn vị hệ thống **không** nằm trong kết quả.

### Request — query string

| Tham số | Mặc định | Ghi chú |
| --- | --- | --- |
| `page` · `pageSize` · `sortBy` · `sortDescending` | | Theo quy ước chung, [`README.md`](README.md) §8 |
| `sortBy` allowlist | `name` (tăng dần) | `code`, `name`, `createdAt` |
| `searchText` | — | Khớp một phần trên `code`, `name`; không phân biệt hoa thường. Tối đa 200 ký tự |

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
  "traceId": "4a8ed7907683feea0220fa2acb2d8bed"
}
```

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `code` · `name` | string | Mã và tên đơn vị |
| `isActive` | bool | Cột `is_active` ([`../database/schema-core.md`](../database/schema-core.md) §1.3) |
| `createdAt` | string \| null | Cột `created_at` cho phép NULL (cùng mục trên, §3.2). FE phải dựng được lưới khi ô này rỗng |

### Lỗi

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Tham số danh sách ngoài khoảng hợp lệ, `sortBy` ngoài allowlist, `searchText` quá dài ([`README.md`](README.md) §8). `fieldErrors` mang khoá của tham số sai |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.FORBIDDEN` | Tài khoản không mang cờ vận hành |

---

## 2. `POST /api/v1/core/system/tenants`

**Status:** DRAFT
**Quyền:** `[RequireSystemOperator]`

Tạo một đơn vị qua **service tạo đơn vị dùng chung** — cùng service mà lệnh bootstrap dùng lúc cài đặt lần đầu ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)). Đây là **một** thao tác đối với người dùng, dù bên trong gồm nhiều bước — và cũng là **một transaction**: hỏng ở bất kỳ bước nào thì không dòng nào còn lại, kể cả dòng đơn vị, tài khoản quản trị và nhật ký kiểm toán.

### Request

```json
{
  "code": "SYT-HN",
  "name": "Sở Y tế Hà Nội",
  "adminUserName": "quantri.syt-hn",
  "adminEmail": "quantri@syt-hn.gov.vn",
  "adminFullName": "Nguyễn Văn An",
  "adminTempPassword": "…"
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `code` | ✅ | Mã đơn vị người dùng gõ ở màn đăng nhập ([`auth.md`](auth.md) §3). **Khuôn:** chỉ gồm chữ HOA `A`–`Z`, chữ số `0`–`9`, `-` và `_`; ký tự đầu phải là chữ hoặc số; tối đa **50** ký tự — trần của cột `core.tenant.code` ([`../database/schema-core.md`](../database/schema-core.md) §1.3). **Chuẩn hoá về chữ HOA trước khi kiểm khuôn, trước khi lưu và trước khi so trùng** — `syt-hn` và `SYT-HN` là một mã, và `CORE.TENANT.CODE_DUPLICATE` so trên bản đã chuẩn hoá. Biểu thức kiểm sau chuẩn hoá: `^[A-Z0-9][A-Z0-9_-]{0,49}$`. FE kiểm cùng khuôn ở ô nhập, nhưng BE mới là chỗ quyết |
| `name` | ✅ | Tên hiển thị |
| `adminUserName` · `adminEmail` · `adminFullName` | ✅ | Tài khoản quản trị đầu tiên của đơn vị. Tạo qua `UserManager`, mang **hai** cờ: `must_change_password` **và** `has_permission_bypass` ([`../database/schema-core.md`](../database/schema-core.md) §4.1). Cờ thứ hai là thứ cho quản trị đơn vị dựng được vai trò và cấp quyền: khi không nguồn seed nào khai bộ vai trò mặc định (seam `ITenantSeedSource`, [`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)), đơn vị vừa tạo **không có vai trò nào**, và thiếu cờ thì quản trị đăng nhập được nhưng **không làm được gì**. Cờ chỉ được đặt lúc **tạo** tài khoản (luật M12), qua service tạo đơn vị: lệnh bootstrap, endpoint này, và §6 |
| `adminTempPassword` | ✅ | Mật khẩu tạm của tài khoản quản trị đầu tiên — **người vận hành tự gõ**. Chính sách mật khẩu áp như mọi tài khoản (luật S9). **Không** xuất hiện trong response, log hay `messageParams` |

### Response 200

`data` là đơn vị vừa tạo, cùng hình dạng một phần tử ở §1. **Không** trả mật khẩu trong thân phản hồi.

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.TENANT.CODE_DUPLICATE` | `Conflict` | 409 | `code` đã tồn tại |
| `CORE.TENANT.ADMIN_CREATE_FAILED` | `BusinessRule` | 422 | Identity từ chối tài khoản quản trị đầu tiên — `adminTempPassword` không đạt chính sách, hoặc tên đăng nhập/email không hợp lệ. Lý do ở `fieldErrors`, khoá `AdminTempPassword` / `AdminUserName` / `AdminEmail` |
| `CORE.TENANT.SEED_FAILED` | `BusinessRule` | 422 | Seed hỏng giữa chừng. **Không dòng nào được ghi** — không đơn vị, không tài khoản, không nhật ký. `code` vẫn còn trống: gửi lại sau khi sửa nguyên nhân không vướng `CORE.TENANT.CODE_DUPLICATE` |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu field; `code` sai khuôn hoặc quá 50 ký tự (khuôn ở bảng Request); `name` rỗng. `fieldErrors` mang khoá theo field request: `Code` / `Name` / `AdminUserName` / `AdminEmail` / `AdminFullName` / `AdminTempPassword` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Không mang cờ vận hành |

---

## 3. `PUT /api/v1/core/system/tenants/{id}/active`

**Status:** DRAFT
**Quyền:** `[RequireSystemOperator]`

Ngưng hoạt động hoặc bật lại một đơn vị. Thân request mang đúng một trường trạng thái.

### Request

```json
{ "isActive": false }
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `isActive` | ✅ | `false` — ngưng hoạt động; `true` — bật lại. Ứng với cột `is_active` của bảng đơn vị ([`../database/schema-core.md`](../database/schema-core.md) §1.3) |

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "7c1e4b9a02d35f68e0a4c7b19d2f5e3a" }
```

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.TENANT.NOT_FOUND` | `NotFound` | 404 | Không có đơn vị đó |
| `CORE.TENANT.SYSTEM_IMMUTABLE` | `BusinessRule` | 422 | Không được ngưng hoạt động **đơn vị hệ thống** |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu `isActive`. Kèm `fieldErrors["IsActive"]` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Không mang cờ vận hành |

### Ghi chú

**Ngưng hoạt động có hiệu lực ở hai chỗ**, không phải một: bước đăng nhập, và bước dựng danh tính của mỗi request ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §10). Chặn chỉ ở đăng nhập thì người đang mở phiên vẫn dùng tiếp tới khi phiên hết.

**Không có endpoint xoá đơn vị** — xoá là thao tác bị loại, không phải bị hoãn (§10 của file trên).

**Ngưng và bật lại ghi nhật ký kiểm toán hai dòng** — §5.

---

## 4. `POST /api/v1/core/system/tenants/{id}/recovery-reset-password`

**Status:** DRAFT
**Quyền:** `[RequireSystemOperator]`

Mở lại **cửa quản trị** của một đơn vị: đặt mật khẩu tạm cho một tài khoản của đơn vị `{id}` đang mang `has_permission_bypass` **hoặc** đang giữ một vai trò `is_system` của đơn vị đó ([`../database/schema-core.md`](../database/schema-core.md) §4.1, §4.2). Chỉ nhận hai loại tài khoản đó — đặt lại mật khẩu cho người dùng thường là việc của quản trị trong chính đơn vị ([`users.md`](users.md) §9). Lý do và phương án đã loại: [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md).

### Request

```json
{
  "userName": "quantri.syt-hn",
  "tempPassword": "…"
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `userName` | ✅ | Tên đăng nhập của tài khoản đích **trong đơn vị `{id}`** |
| `tempPassword` | ✅ | Mật khẩu tạm — **người vận hành tự gõ**. Chính sách mật khẩu áp như mọi tài khoản (luật S9). **Không** xuất hiện trong response, log hay `messageParams` |

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "2211bc7167825e8ab25660eb26688289" }
```

**Không trả dữ liệu nào về tài khoản đích.** Sau lệnh này tài khoản đích có `mustChangePassword = true` và **mọi phiên đang mở của nó bị chấm dứt ở request kế tiếp** — `SecurityStamp` đổi. Lần đăng nhập kế tiếp đi qua `POST /api/v1/core/auth/change-password-required` ([`auth.md`](auth.md) §7).

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.TENANT.NOT_FOUND` | `NotFound` | 404 | Không có đơn vị `{id}` |
| `CORE.TENANT.RECOVERY_TARGET_NOT_ELIGIBLE` | `BusinessRule` | 422 | Không có tài khoản `userName` trong đơn vị đó, **hoặc** có nhưng không mang `has_permission_bypass` và không giữ vai trò `is_system` nào của đơn vị — **hai ca, một mã** |
| `CORE.TENANT.RECOVERY_RESET_FAILED` | `BusinessRule` | 422 | Identity từ chối — `tempPassword` không đạt chính sách. Lý do ở `fieldErrors` |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu `userName` hoặc `tempPassword`. Kèm `fieldErrors["UserName"]` / `["TempPassword"]` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Tài khoản không mang cờ vận hành |

### Ghi chú

**Hai ca gộp một mã là cố ý** — cùng khuôn với bốn ca của `CORE.AUTH.INVALID_CREDENTIALS` ([`auth.md`](auth.md) §3). Tách "không có tài khoản này" khỏi "có nhưng không đủ điều kiện" là cho người gọi dò được tên đăng nhập bên trong một đơn vị mà họ không được thấy dữ liệu.

**Vì sao nhận cả người giữ vai trò hệ thống.** Đơn vị đã từ bỏ cờ bypass ([`profile.md`](profile.md) §3) thì cửa quản trị nằm ở vai trò: vai trò `is_system` không bao giờ mất `core.permission.write` ([`../database/schema-core.md`](../database/schema-core.md) §5.3). Chỉ nhận đích mang cờ thì đơn vị làm đúng khuyến nghị vận hành lại mất lưới an toàn.

Hệ quả cho thứ tự kiểm: `CORE.TENANT.RECOVERY_RESET_FAILED` không được để lộ điều mà mã gộp đang che. Chính sách mật khẩu kiểm **trước** khi tra tài khoản đích, nên mã đó không nói gì về việc đích có tồn tại hay không.

**Khôi phục chính tài khoản vận hành hệ thống không đi qua HTTP** — đó là lệnh chạy tay trên máy chủ ([`../adr/0023-dich-vu-tao-don-vi-dung-chung.md`](../adr/0023-dich-vu-tao-don-vi-dung-chung.md)).

---

## 5. Nhật ký kiểm toán — thao tác xuyên đơn vị

**Mọi thao tác ở file này ghi nhật ký kiểm toán**, kèm danh tính tài khoản vận hành đã thực hiện.

Các thao tác tác động lên một đơn vị nghiệp vụ — tạo đơn vị (§2), ngưng và bật lại (§3), khôi phục tài khoản quản trị (§4), tạo tài khoản quản trị mới (§6) — ghi **hai** dòng, không phải một:

| Dòng | Đơn vị của dòng | Người thực hiện |
| --- | --- | --- |
| 1 | Đơn vị hệ thống — nhật ký của người vận hành | Tài khoản vận hành |
| 2 | Đơn vị đích | Tài khoản vận hành, **đánh dấu là thao tác xuyên đơn vị** |

Cột của bảng nhật ký: [`../database/schema-core.md`](../database/schema-core.md) §9.4. Quyết định: [`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md).

---

## 6. `POST /api/v1/core/system/tenants/{id}/admins`

**Status:** DRAFT
**Quyền:** `[RequireSystemOperator]`

Tạo một tài khoản quản trị **mới** cho đơn vị `{id}`, mang `has_permission_bypass` và `must_change_password` — qua service tạo đơn vị dùng chung, cùng đường gán cờ với §2. Là lối khôi phục cuối khi đơn vị không còn tài khoản nào đủ điều kiện của §4 ([`../luong/V4-khoi-phuc-quan-tri-don-vi.md`](../luong/V4-khoi-phuc-quan-tri-don-vi.md)).

### Request

```json
{
  "userName": "quantri2.syt-hn",
  "email": "quantri2@syt-hn.gov.vn",
  "fullName": "Trần Thị Bình",
  "tempPassword": "…"
}
```

| Field | Bắt buộc | Ghi chú |
| --- | --- | --- |
| `userName` · `email` · `fullName` | ✅ | Tài khoản tạo qua `UserManager` **trong phạm vi đơn vị `{id}`**; `userName` duy nhất trong đơn vị đó ([`../database/schema-core.md`](../database/schema-core.md) §4.1) |
| `tempPassword` | ✅ | Mật khẩu tạm — **người vận hành tự gõ**. Chính sách mật khẩu áp như mọi tài khoản (luật S9). **Không** xuất hiện trong response, log hay `messageParams` |

### Response 200

```json
{ "success": true, "data": null, "error": null, "traceId": "5f0c2d8e91a74b3c8e6d1f2a0b9c7d4e" }
```

**Không trả dữ liệu nào về tài khoản vừa tạo.** Lần đăng nhập đầu của nó đi qua `POST /api/v1/core/auth/change-password-required` ([`auth.md`](auth.md) §7).

### Lỗi

| `code` | `type` | HTTP | Khi nào |
| --- | --- | ---: | --- |
| `CORE.TENANT.NOT_FOUND` | `NotFound` | 404 | Không có đơn vị `{id}` |
| `CORE.TENANT.ADMIN_CREATE_FAILED` | `BusinessRule` | 422 | Identity từ chối tạo tài khoản — **mọi ca, một mã**, không nêu nguyên nhân liên quan `userName`. Chỉ lý do chính sách mật khẩu được trả ở `fieldErrors["TempPassword"]` |

**Mã dùng chung** — `type` và HTTP tra ở [`auth.md`](auth.md) §11:

| `code` | Khi nào |
| --- | --- |
| `CORE.VALIDATION.FAILED` | Thiếu field. Kèm `fieldErrors["UserName"]` / `["Email"]` / `["FullName"]` / `["TempPassword"]` |
| `CORE.AUTH.NOT_AUTHENTICATED` | Chưa đăng nhập |
| `CORE.AUTH.CSRF_REJECTED` | Thiếu hoặc sai `X-XSRF-TOKEN` |
| `CORE.AUTH.ORIGIN_REJECTED` | Header `Origin` ngoài allowlist |
| `CORE.AUTH.FORBIDDEN` | Tài khoản không mang cờ vận hành |

### Ghi chú

**Ghi nhật ký kiểm toán hai dòng** — §5. Phạm vi đơn vị `{id}` mở **bên trong** service tạo đơn vị, không thêm mục allowlist A12.

**Không sửa tài khoản đã có.** Endpoint chỉ **tạo mới**; bật cờ lên tài khoản đang tồn tại là việc luật M12 cấm ở mọi đường.

**Mọi ca thất bại tạo tài khoản gộp một mã là cố ý** — cùng nguyên tắc với §4: tách "`userName` đã có" ra một lý do riêng là cho người vận hành dò được tên đăng nhập trong đơn vị. Chính sách mật khẩu kiểm **trước** khi gọi `UserManager`, nên `fieldErrors["TempPassword"]` không nói gì về tên đăng nhập.
