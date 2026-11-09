---
kind: luat
scope: core
verified: 2026-10-06
---

# `P2` — Gán vai trò cho một người dùng

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

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
| 2 | Quản trị | `PUT /api/v1/core/users/{id}/roles` với **toàn bộ** tập vai trò mong muốn, kèm `version` của tài khoản từ bước 1 | cùng trên §7 |
| 3 | BE | Chặn `roleIds` trùng, tìm người dùng trong đơn vị, rồi **so `version`** với token hiện tại của tài khoản — lệch thì dừng ở đây, **trước** mọi luật bảo vệ. Phép so này là đọc rồi so, không thay bước 5 | [`../contracts/users.md`](../contracts/users.md) §7 *Thứ tự lỗi* |
| 4 | BE | Kiểm vai trò được thêm có tồn tại, và các luật bảo vệ tài khoản quản trị trên cả phần thêm lẫn phần gỡ — **trước khi** chạm tầng ghi | [`../contracts/users.md`](../contracts/users.md) §2 |
| 5 | BE | So-và-đổi token của tài khoản — chốt cuối: lượt ghi chen vào giữa bước 3 và bước này chỉ lộ ra ở đây — rồi thay thế tập vai trò, không cộng dồn; ghi một dòng nhật ký `core.user.role_assign` | [`../contracts/users.md`](../contracts/users.md) §7 |

### Vì sao gửi toàn bộ tập, không gửi "thêm/bớt"

Gửi thao tác thêm–bớt thì hai người sửa cùng lúc sẽ chồng lên nhau theo cách không ai đoán được. Gửi cả tập **kèm `version`** thì lượt ghi dựa trên bản chụp cũ nhận 409 thay vì âm thầm gỡ vai trò người kia vừa cấp ([ADR-0082](../adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md)); người dùng tải lại, thấy tập mới, rồi quyết.

Kèm theo: `roleIds` trùng lặp là lỗi kiểm hợp lệ, **không** phải "tự lọc trùng" — một mảng có khoá trùng là dấu hiệu FE đang dựng sai, và im lặng bỏ qua nó là giấu lỗi ([`../contracts/users.md`](../contracts/users.md) §7).

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/users.md`](../contracts/users.md) §7 và §2. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Thiếu `core.user.role.assign` | `CORE.AUTH.FORBIDDEN` | Người này sửa được email nhưng không gán được vai trò — đúng thiết kế |
| `roleIds` có phần tử trùng | `CORE.USER.DUPLICATE_ROLE_ENTRY` | Lỗi rõ, không tự dọn hộ |
| Người dùng không có, hoặc thuộc đơn vị khác | `CORE.USER.NOT_FOUND` | Bộ lọc đơn vị làm người dùng của đơn vị khác **không tồn tại** với người gọi |
| Người khác vừa ghi tài khoản này — đổi vai trò, sửa, khoá | `CORE.CONCURRENCY.CONFLICT` | Cảnh báo xung đột, tải lại tập vai trò hiện tại; không gì bị ghi. Đứng trước mọi mã bên dưới — bước 3 |
| Vai trò không thuộc đơn vị này | `CORE.USER.ROLE_NOT_FOUND` | Bộ lọc đơn vị làm vai trò của đơn vị khác **không tồn tại** với người gọi |
| Gán **hoặc gỡ** một vai trò cấp quyền mà người gọi không có | `CORE.USER.ROLE_ESCALATION_FORBIDDEN` | Luật 1 ở [`../contracts/users.md`](../contracts/users.md) §2 — áp cả chiều thêm lẫn chiều gỡ. Ca dễ gặp nhất với người quản trị không giữ đủ quyền của vai trò họ đang chạm tới |
| Tự gỡ vai trò hệ thống của chính mình | `CORE.USER.SELF_SYSTEM_ROLE_REMOVAL_FORBIDDEN` | Luật 2 ở [`../contracts/users.md`](../contracts/users.md) §2 — một thao tác vô ý không được làm mất quyền quản trị mà không lỗi nào bật ra |
| Gán vai trò rỗng quyền | không có mã lỗi | 🛑 Người dùng có vai trò, đăng nhập được, bấm gì cũng bị từ chối. Cùng triệu chứng với `P1` bước 5 bị quên |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Cả người dùng lẫn vai trò đều mang `tenant_id`, và bước 3–4 phải kiểm **cả hai cùng thuộc đơn vị của người đang thao tác** — người dùng ở bước 3, vai trò ở bước 4.

Kiểm một vế là chưa đủ: một quản trị của đơn vị A tra đúng người dùng của đơn vị mình nhưng truyền vào định danh một vai trò của đơn vị B thì tạo ra một dòng gán **xuyên đơn vị** — và dòng đó không vi phạm khoá ngoại nào.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Tài khoản mang `has_permission_bypass` gán vai trò được, BE không chặn ([`../contracts/users.md`](../contracts/users.md) §7).

---

## Đối chiếu với `src/`

Neo của `P2`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/platform/quan-tri/nguoi-dung/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Bước 2 — route, khoá quyền riêng | `CoreAndSkill.Core.Web/Controllers/UsersController.cs`: `[HttpPut("{id:guid}/roles")]`, `[RequirePermission(CorePermissions.UserRoleAssign)]` |
| Bước 3 — trùng, tìm người dùng, so `version` trước mọi luật | `CoreAndSkill.Core.Application/Users/AssignUserRolesCommandHandler.cs`, theo thứ tự: `UserRoleAssignmentRules.EnsureNoDuplicateRoleIds(command.RoleIds)`, `return Result.Failure(UserErrors.NotFound);`, `if (command.Version is null \|\| !string.Equals(command.Version, target.Version, StringComparison.Ordinal))` |
| Bước 4 — vai trò tồn tại, Luật 1 cả hai chiều, Luật 2 | cùng tệp: `UserErrors.RoleNotFound`, `escalation.Check(roleId)` trong vòng `added`, `escalation.Check(role.Id)` và `UserPrivilegeGuard.EnsureNotRemovingOwnSystemRole` trong vòng `removed` |
| Bước 5 — so-và-đổi token, thay cả tập | `CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`, hàm `AssignRolesAsync`: `if (!await TryClaimAccountVersionAsync(userId, version, ct))`, `db.UserRoles.RemoveRange(rowsToRemove);`, `db.UserRoles.Add(new AppUserRole { UserId = userId, RoleId = roleId });` |
| Bước 5 — một dòng nhật ký | `CoreAndSkill.Core.Infrastructure/Persistence/Interceptors/AuditLogInterceptor.cs`: `AuditActionCodes.UserRoleAssign` trong vòng `foreach (var change in userRoleChanges)` |
| FE — gán từ màn chi tiết, gửi cả tập kèm `version` | `pages/chi-tiet/chi-tiet-nguoi-dung.page.html`: `<app-hop-gan-vai-tro`; `state/hop-chi-tiet-nguoi-dung.store.ts`: `.ganVaiTro(nd.id, { roleIds: [...tapDich], version: nd.version })` |
