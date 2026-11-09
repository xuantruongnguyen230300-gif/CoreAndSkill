---
kind: luat
scope: core
verified: 2026-10-06
---

# `N1` — Tạo một người dùng mới

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Vì hệ **không có đăng ký**, đây là đường **duy nhất** một tài khoản thường ra đời. Ba đường còn lại sinh tài khoản đều là đường đặc quyền: lệnh bootstrap (`V1`), tạo đơn vị (`V2`), và tạo quản trị mới cho đơn vị đã có ([`../contracts/tenants.md`](../contracts/tenants.md) §6).

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn danh sách người dùng.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.write` | Thêm `core.user.role.assign` nếu gán vai trò ngay lúc tạo |
| Bộ vai trò đã có quyền | Luồng `P1`. Tạo người rồi gán một vai trò rỗng là tạo một tài khoản không làm được gì |
| Mật khẩu tạm do quản trị tự gõ, đạt chính sách | Chính sách **giống nhau ở mọi môi trường**, luật **S9** |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /api/v1/core/users` với tên đăng nhập, email, họ tên và **mật khẩu tạm do quản trị tự gõ** | [`../contracts/users.md`](../contracts/users.md) §5 |
| 2 | BE | Tạo tài khoản qua `UserManager` với mật khẩu tạm đó | cùng trên |
| 3 | BE | Bật cờ buộc đổi mật khẩu | cùng trên |
| 4 | BE | Trả định danh tài khoản mới — **không** trả mật khẩu | cùng trên |
| 5 | Quản trị | Chuyển mật khẩu tạm cho người dùng | thủ công — xem luồng `D4` §6 |
| 6 | Quản trị | Gán vai trò: luồng `P2` — hoặc gửi kèm vai trò ngay ở bước 1, cùng một transaction | [`../contracts/users.md`](../contracts/users.md) §5 |
| 7 | Người dùng | Đăng nhập (`D1`), rồi buộc đổi mật khẩu (`D2`) | |

### Vì sao quản trị tự gõ mật khẩu tạm

Hệ **không giả định có kênh gửi email**. Việc cấp tài khoản không phụ thuộc vào SMTP có chạy hay không — quan trọng với một hệ nội bộ, nơi việc cấp tài khoản thường diễn ra khi hai người đang ngồi cạnh nhau.

Mật khẩu tạm **không** đi ra ngoài qua phản hồi hay log: nó đi từ người gõ tới `UserManager` và dừng ở đó. Chất lượng của nó do chính sách mật khẩu kiểm — cùng một cách với luồng `D4` và `V2`.

Đánh đổi: mật khẩu tạm đi qua tay quản trị, nên nó chỉ được sống tới lần đăng nhập đầu — cờ ở bước 3 là thứ ép điều đó.

### Vì sao không có endpoint xoá người dùng

[`../contracts/users.md`](../contracts/users.md) §10 khai rõ lý do. Tóm tắt: một tài khoản đã thao tác thì tên nó nằm trong nhật ký kiểm toán và trong các bản ghi nghiệp vụ; xoá nó làm những bản ghi đó trỏ vào hư không. Đường đúng là **khoá** (luồng `D5`).

## 4. Hỏng ở đâu — và ai thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/users.md`](../contracts/users.md) §5. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Identity từ chối | `CORE.USER.CREATE_FAILED` | Lý do ở `fieldErrors` — thường là mật khẩu tạm không đạt chính sách |
| Tên đăng nhập hoặc email đã có | `CORE.USER.USERNAME_DUPLICATED` · `CORE.USER.EMAIL_DUPLICATED` | Trùng **trong đơn vị này** — hai đơn vị có thể cùng có một tên đăng nhập ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11) |
| Tên đăng nhập là tên dành riêng của hệ | `CORE.VALIDATION.FAILED`, mã `CORE.USER.USERNAME_RESERVED` ở `fieldErrors` | Lỗi tại ô tên đăng nhập. Tên nào là tên dành riêng: [`../contracts/users.md`](../contracts/users.md) §5, mục *Ghi chú* |
| Thiếu quyền | `CORE.AUTH.FORBIDDEN` | Gồm cả ca gửi kèm vai trò mà thiếu `core.user.role.assign` (§2) |
| Gửi kèm vai trò ở bước 1 (bước 6), danh sách có một vai trò lặp lại | `CORE.USER.DUPLICATE_ROLE_ENTRY` | Xét trước mọi mã riêng của card §5, kể cả trước phép kiểm `core.user.role.assign`. Không tạo tài khoản nào |
| Gửi kèm vai trò, một vai trò không tồn tại | `CORE.USER.ROLE_NOT_FOUND` | Không tạo tài khoản nào — tạo và gán vai trò chung một transaction (bước 6) |
| Gửi kèm vai trò mang quyền người tạo không có | `CORE.USER.ROLE_ESCALATION_FORBIDDEN` | Luật 1 ở [`../contracts/users.md`](../contracts/users.md) §2. Không tạo tài khoản nào |
| Tạo xong, quên bước 6 | không có mã lỗi | 🛑 Tài khoản đăng nhập được, đổi mật khẩu xong, rồi **không làm được gì**. Cùng triệu chứng với `P1` bước 5 và `V2` bước 5 — ba nguyên nhân khác nhau, một biểu hiện |

Ba luồng cho ra cùng một triệu chứng. Màn người dùng chỉ phân biệt được **"chưa có vai trò"** — không response nào của danh sách người dùng mang tập quyền của vai trò; vai trò chưa có quyền kiểm ở màn ma trận quyền (luồng [`P1`](P1-tao-vai-tro-va-gan-quyen.md)).

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Tài khoản mới mang `tenant_id` của người tạo ra nó — không có ô chọn đơn vị trên form, và **không nên có**: cho quản trị đơn vị A tạo tài khoản ở đơn vị B là mở đúng cánh cửa mà mô hình này đóng.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Kênh chuyển mật khẩu tạm tới người dùng ở bước 5 là câu hỏi chung, giữ ở luồng `D4` §6.

---

## Đối chiếu với `src/`

Neo của `N1`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/platform/quan-tri/nguoi-dung/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Bước 1 — route, quyền `core.user.write`, vai trò gửi kèm tuỳ chọn | `CoreAndSkill.Core.Web/Controllers/UsersController.cs`: `[RequirePermission(CorePermissions.UserWrite)]` trên `Create`, `public sealed record CreateUserRequest(string UserName, string Email, string FullName, string TempPassword, IReadOnlyList<Guid>? RoleIds);` |
| Tên dành riêng | `CoreAndSkill.Core.Application/Users/CreateUserCommandValidator.cs`: `.NotReservedUserName();` |
| Thứ tự phép kiểm khi gửi kèm vai trò | `CoreAndSkill.Core.Application/Users/CreateUserCommandHandler.cs`: `UserRoleAssignmentRules.EnsureNoDuplicateRoleIds(command.RoleIds)` đứng đầu, rồi `if (!callerPermissions.Contains(CorePermissions.UserRoleAssign))`, rồi hai phép trùng `UserErrors.UsernameDuplicated`, `UserErrors.EmailDuplicated`, rồi `UserErrors.RoleNotFound` và `escalation.Check(roleId)` |
| Bước 2–3 — tạo qua `UserManager`, cờ buộc đổi mật khẩu | `CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`: `var user = AppUser.NewAccount(input.UserName, input.Email, input.FullName);`, `userManager.CreateAsync(user, input.TempPassword)`; cờ đặt ngay trong `NewAccount` (`AppUser.cs`: `MustChangePassword = true,`), lưu cùng lượt tạo |
| Bước 4 — chỉ trả định danh | `CoreAndSkill.Core.Application/Common/CreatedIdDto.cs`: `public sealed record CreatedIdDto(Guid Id);` |
| Bước 6 — tạo và gán vai trò cùng một transaction | `CreateUserCommandHandler.cs`: `userAdmin.GrantInitialRolesAsync(createResult.Value, command.RoleIds, ct)` trong cùng handler; transaction ở `CoreAndSkill.Core.Application/Common/Behaviors/TransactionBehavior.cs`: `return new TransactionOutcome<TResponse>(response, response.IsSuccess);` |
| FE — tạo từ màn danh sách, kèm vai trò | `pages/danh-sach/danh-sach-nguoi-dung.page.html`: `<app-hop-tao-nguoi-dung`; `state/tao-nguoi-dung.store.ts`: `roleIds: this.fb.control<readonly string[]>([]),` |
