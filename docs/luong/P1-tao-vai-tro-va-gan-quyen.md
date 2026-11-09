---
kind: luat
scope: core
verified: 2026-10-06
---

# `P1` — Tạo vai trò và gán quyền cho nó

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Hai việc, **hai màn, hai endpoint** — cố ý không gộp. Lý do ở §3.

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn **Vai trò**, rồi sang màn **Ma trận phân quyền**.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.role.write` | Tạo và sửa vai trò |
| Quyền `core.permission.write` | Ghi ma trận — **khoá riêng**, không gộp với khoá trên |
| Danh mục quyền đã có trong database | Vào database bằng migration lúc áp lược đồ (luồng `V1` bước 2), dùng chung toàn hệ |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /api/v1/core/roles` với tên vai trò | [`../contracts/roles.md`](../contracts/roles.md) §2 |
| 2 | BE | Tạo vai trò **không có quyền nào** | cùng trên |
| 3 | Quản trị | Mở màn ma trận: `GET /api/v1/core/permissions/matrix` | [`../contracts/permissions.md`](../contracts/permissions.md) |
| 4 | Quản trị | Tick các ô vai trò × quyền | [`../Design/Screens/12-ma-tran-phan-quyen.md`](../Design/Screens/12-ma-tran-phan-quyen.md) |
| 5 | Quản trị | `PUT /api/v1/core/permissions/matrix` | [`../contracts/permissions.md`](../contracts/permissions.md) |
| 6 | BE | Ghi ánh xạ vai trò→quyền **của đơn vị này** | [`../database/schema-core.md`](../database/schema-core.md) §5 |
| 7 | BE, rồi FE của người mang vai trò đó | Ma trận mới có hiệu lực từ **request kế tiếp** của họ. Giao diện của họ còn giữ tập quyền cũ; FE làm mới tập quyền khi nhận 403 | [`../contracts/users.md`](../contracts/users.md) §7 |

### Vì sao tạo vai trò và cấp quyền là hai việc

Vai trò mới sinh ra **rỗng**. Gộp hai việc lại thì **mọi người sửa được tên vai trò cũng cấp được quyền** — và hai việc đó có mức rủi ro hoàn toàn khác nhau. Đổi tên một vai trò là việc hành chính; cấp thêm một quyền là việc bảo mật.

Hai khoá quyền riêng (`core.role.write` và `core.permission.write`) là cách tách đó được ép ra tới tận API.

### Danh mục quyền dùng chung, ánh xạ thuộc đơn vị

| Thứ | Phạm vi | Vì sao |
| --- | --- | --- |
| **Danh mục quyền** (`core.user.read`…) | Dùng chung toàn hệ | Chuỗi khoá nằm trong code (`[RequirePermission(...)]`) và phải khớp một dòng dữ liệu. Cho mỗi đơn vị một bản sao nghĩa là một endpoint từ chối ở đơn vị này mà cho qua ở đơn vị kia |
| **Ánh xạ vai trò → quyền** | Thuộc đơn vị | Mỗi đơn vị tự quyết vai trò của mình gồm những quyền nào |
| **Vai trò** | Thuộc đơn vị | Mỗi đơn vị đặt tên riêng, sửa riêng |

**Xoá vai trò không có luồng riêng:** một endpoint, một luật chặn — còn người mang vai trò thì `CORE.ROLE.IN_USE` ([`../contracts/roles.md`](../contracts/roles.md) §4).

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/roles.md`](../contracts/roles.md) §2–§3 và [`../contracts/permissions.md`](../contracts/permissions.md) §6. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Tên vai trò trùng, khi tạo hoặc khi đổi tên | `CORE.ROLE.NAME_DUPLICATE` | Trùng **trong phạm vi đơn vị này**, không phải toàn hệ |
| Sửa một vai trò hệ thống | `CORE.ROLE.SYSTEM_IMMUTABLE` | Vai trò hệ thống không đổi tên được |
| Người khác vừa đổi tên vai trò này, khi đổi tên | `CORE.CONCURRENCY.CONFLICT` | Cảnh báo xung đột, tải lại; không gì bị ghi ([`../contracts/roles.md`](../contracts/roles.md) §3) |
| Thiếu `core.permission.write` | `CORE.AUTH.FORBIDDEN` | Người này sửa được tên vai trò nhưng **không** cấp được quyền — đúng thiết kế |
| Người khác vừa lưu ma trận, ở bước 5 | `CORE.PERMISSION.VERSION_MISMATCH` | Cảnh báo xung đột, tải lại ma trận; không gì bị ghi. Mã **riêng** của ma trận, không phải `CORE.CONCURRENCY.CONFLICT` ([`../contracts/permissions.md`](../contracts/permissions.md) §3.2, §6) — màn ma trận bắt xung đột theo mã dùng chung thì nhánh *tải lại* không bao giờ chạy |
| Ma trận gửi lên gỡ `core.permission.write` khỏi một vai trò hệ thống | `CORE.PERMISSION.SYSTEM_ROLE_CANNOT_LOSE_WRITE` | Lưu bị từ chối, không gì bị ghi — vai trò hệ thống không mất khoá ghi ma trận |
| Ma trận gửi lên thiếu hoặc lặp một quyền của danh mục | `CORE.PERMISSION.ENTRIES_INCOMPLETE` · `CORE.PERMISSION.DUPLICATE_ENTRY` | Lưu bị từ chối, không gì bị ghi. Dấu hiệu FE dựng payload sai, không phải thao tác của người dùng |
| FE không làm mới tập quyền khi nhận 403 ở bước 7 | `CORE.AUTH.FORBIDDEN` | 🛑 Người vừa bị gỡ quyền thấy nút cũ mãi tới lần đăng nhập sau, bấm lần nào cũng bị từ chối |
| Tạo vai trò xong, quên bước 5 | không có mã lỗi | 🛑 Vai trò tồn tại, gán được cho người, và **người đó không làm được gì**. Không lỗi nào bắn ra — cùng triệu chứng với ca thiếu cờ đặc quyền ở luồng `V1` bước 7 |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị** — trừ danh mục quyền, dùng chung toàn hệ (bảng ở §3).

Đây là chỗ dễ nhầm nhất của cả nhóm phân quyền: **ba thứ nghe giống nhau, hai phạm vi khác nhau**. Một truy vấn ma trận quên lọc đơn vị sẽ trả về ánh xạ của mọi đơn vị, và màn ma trận hiện ra các vai trò không thuộc đơn vị đang đăng nhập.

## 6. Câu chưa trả lời được

Không còn.

---

## Đối chiếu với `src/`

Neo của `P1`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/platform/quan-tri/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Hai khoá quyền riêng, hai endpoint | `CoreAndSkill.Core.Web/Controllers/RolesController.cs`: `[RequirePermission(CorePermissions.RoleWrite)]` trên `[HttpPost]`, `[HttpPut("{id:guid}")]`, `[HttpDelete("{id:guid}")]`; `CoreAndSkill.Core.Web/Controllers/PermissionsController.cs`: `[HttpPut("matrix")]` với `[RequirePermission(CorePermissions.PermissionWrite)]` |
| Bước 2 — vai trò sinh ra rỗng; tên trùng trong đơn vị | `CoreAndSkill.Core.Infrastructure/Roles/RoleAdminService.cs`, hàm `CreateAsync`: `roleManager.CreateAsync(role)`, `RoleErrors.NameDuplicate` — không ghi dòng quyền nào |
| Đổi tên: vai trò hệ thống, xung đột đồng thời | cùng tệp, hàm `RenameAsync`: `if (role.IsSystem)` → `RoleErrors.SystemImmutable`; `if (version is null \|\| !string.Equals(version, role.ConcurrencyStamp, StringComparison.Ordinal))` → `CommonErrors.ConcurrencyConflict` |
| Bước 5–6 — ghi ma trận của đơn vị, bốn mã riêng | `CoreAndSkill.Core.Infrastructure/Permissions/PermissionMatrixService.cs`, hàm `ReplaceMatrixAsync`: `PermissionErrors.EntriesIncomplete`, `PermissionErrors.VersionMismatch`, `PermissionErrors.SystemRoleCannotLoseWrite`; mã lặp ở `CoreAndSkill.Core.Application/Permissions/PermissionErrors.cs`: `"CORE.PERMISSION.DUPLICATE_ENTRY"` |
| Bước 7 — FE làm mới tập quyền khi nhận 403 | `src/FE/src/app/core/interceptors/error.interceptor.ts`: `err.status === 403 && body?.error.code === 'CORE.AUTH.FORBIDDEN'` rồi `auth.lamMoiQuyen();` |
| Màn ma trận bắt mã riêng của nó | `phan-quyen/pages/ma-tran/ma-tran-phan-quyen.page.ts`: `if (ma === 'CORE.PERMISSION.VERSION_MISMATCH') {` |
| Xoá vai trò còn người mang | `RoleAdminService.cs`: `return Result.Failure(RoleErrors.InUse.WithParams(("Count", userCount)));` |
| FE — hai màn, hai guard | `vai-tro/vai-tro.routes.ts`: `permissionGuard('core.role.read')`; `phan-quyen/phan-quyen.routes.ts`: `permissionGuard('core.permission.read')` |
