---
kind: luat
scope: core
verified: 2026-10-06
---

# `D4` — Quản trị đặt lại mật khẩu cho người khác

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Luồng này **chấm dứt mọi phiên đang mở** của người bị tác động. Đó là phần quan trọng nhất, không phải phần phụ — xem §3.

---

## 1. Ai bắt đầu, ở đâu

Quản trị đơn vị, từ màn chi tiết một người dùng.

Luồng này **không** dành cho hai ca: đổi mật khẩu của **chính mình** (đi màn hồ sơ — đổi tự nguyện ở [`../contracts/auth.md`](../contracts/auth.md) §6; khi cờ buộc đổi mật khẩu đang bật thì đi luồng `D2`), và đặt lại cho tài khoản **mang cờ đặc quyền** (đi luồng `V4`).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.reset-password` | **Khoá riêng**, tách khỏi `core.user.write` |
| Người dùng đích thuộc cùng đơn vị | |
| Đích qua được **Luật 5** | [`../contracts/users.md`](../contracts/users.md) §2 — chặn đích vượt tư cách người gọi, và chặn tự đặt lại cho chính mình |
| Mật khẩu tạm do quản trị tự gõ, đạt chính sách | Chính sách **giống nhau ở mọi môi trường**, luật **S9** |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /api/v1/core/users/{id}/reset-password` kèm **mật khẩu tạm do quản trị tự gõ** và `version` của bản ghi đang xem | [`../contracts/users.md`](../contracts/users.md) §9 |
| 2 | BE | Kiểm Luật 5 **trước khi** chạm tầng ghi, rồi đặt mật khẩu tạm qua `UserManager` | cùng trên · §2 |
| 3 | BE | Bật cờ buộc đổi mật khẩu | cùng trên |
| 4 | BE | 🛑 **Chấm dứt mọi phiên đang mở của tài khoản đó** — đổi `security_stamp`, phiên trượt **ở request kế tiếp** | cùng trên |
| 5 | Quản trị | Chuyển mật khẩu tạm cho người dùng | thủ công — xem §6 |
| 6 | Người dùng | Đăng nhập, rồi đi tiếp bằng luồng `D2` | |

### Vì sao bước 4 là phần quan trọng nhất

Lý do người ta đặt lại mật khẩu cho người khác thường là **tài khoản nghi bị chiếm**. Đổi mật khẩu mà không đá phiên cũ ra thì kẻ đang giữ phiên vẫn thao tác bình thường — và người quản trị tin rằng mình đã xử lý xong.

Cùng cơ chế và cùng nhịp với việc khoá tài khoản ở luồng `D5`: thao tác đổi `security_stamp` tường minh trong cùng thao tác ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4), phiên đang mở trượt ở request kế tiếp — và **bỏ qua bước đó nghĩa là phiên đang chạy vẫn sống**.

### Vì sao khoá quyền tách riêng

Đặt lại được mật khẩu của người khác là đặt lại được mật khẩu của **quản trị khác**. Gộp nó vào `core.user.write` thì mọi người sửa được email cũng chiếm được tài khoản bất kỳ.

## 4. Hỏng ở đâu — và ai thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/users.md`](../contracts/users.md) §9 và §2, [`../contracts/auth.md`](../contracts/auth.md) §11. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Thiếu quyền | `CORE.AUTH.FORBIDDEN` | |
| Thiếu mật khẩu tạm trong request | `CORE.VALIDATION.FAILED` | |
| Mật khẩu tạm không đạt chính sách | `CORE.USER.RESET_PASSWORD_FAILED` | Lý do ở `fieldErrors` |
| Đích có tập quyền vượt người gọi, hoặc mang cờ đặc quyền | `CORE.USER.RESET_PASSWORD_TARGET_FORBIDDEN` | Luật 5. Quản trị đơn vị **không** tự mở được tài khoản mang cờ đặc quyền — đường đó là `V4` |
| Đích chính là người gọi | `CORE.USER.CANNOT_RESET_OWN_PASSWORD` | Luật 5. Đổi mật khẩu của chính mình đi đường hồ sơ, không đi luồng này |
| Bản ghi đích đã bị thao tác khác ghi sau khi quản trị mở hộp — `version` gửi lên lệch | `CORE.CONCURRENCY.CONFLICT` | Không ghi gì, phiên của đích **chưa** bị chấm dứt. Tải lại chi tiết rồi thao tác lại — [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6 |
| Người bị đặt lại đang có phiên mở | `CORE.AUTH.NOT_AUTHENTICATED` | Request kế tiếp đưa về màn đăng nhập như phiên hết hạn; đăng nhập bằng mật khẩu tạm thì đi tiếp luồng `D2` |
| Bước 4 bị bỏ | không có mã lỗi | 🛑 Phiên của người bị nghi vẫn sống. Quản trị thấy "đã đặt lại thành công" và tin là xong |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Cả người thao tác lẫn người bị tác động phải thuộc cùng một đơn vị.

Khác luồng `V2`, `V3` và `V4` ở đúng điểm này: ở đó người thao tác cố ý đứng ngoài đơn vị bị tác động; ở đây, đứng ngoài là **lỗi**.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Kênh chuyển mật khẩu tạm (bước 5) là **quy trình vận hành ngoài phần mềm** — cờ buộc đổi mật khẩu là hàng rào duy nhất ([`../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md`](../adr/0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)).

> Câu *"có ghi nhật ký kiểm toán không"* **đã có lời đáp**: **có**. Danh sách thao tác phải ghi ở [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §5.4 gồm *đổi mật khẩu*, không phân biệt người tự đổi hay quản trị đặt lại hộ.

---

## Đối chiếu với `src/`

Neo của `D4`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/platform/quan-tri/nguoi-dung/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Bước 1 — route, khoá quyền riêng, thân mang mật khẩu tạm và `version` | `CoreAndSkill.Core.Web/Controllers/UsersController.cs`: `[HttpPost("{id:guid}/reset-password")]`, `[RequirePermission(CorePermissions.UserResetPassword)]`; `CoreAndSkill.Core.Application/Permissions/CorePermissions.cs`: `UserResetPassword = "core.user.reset-password"` |
| Thiếu mật khẩu tạm | `CoreAndSkill.Core.Application/Users/ResetPasswordCommandValidator.cs`: `RuleFor(x => x.TempPassword).NotEmpty()` |
| Bước 2 — Luật 5 trước tầng ghi | `CoreAndSkill.Core.Application/Users/ResetPasswordCommandHandler.cs`: `privilegeGuard.EnsureCanResetPasswordAsync(callerId, command.UserId, ct)` rồi `userAdmin.ResetPasswordAsync`; hai mã của Luật 5 ở `UserPrivilegeGuard.cs`: `UserErrors.CannotResetOwnPassword`, `UserErrors.ResetPasswordTargetForbidden` |
| Bước 2–4 — so `version`, đặt mật khẩu tạm qua `UserManager`, bật cờ, đổi stamp | `CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`, hàm `ResetPasswordAsync`: `IdentityConcurrency.VersionMatches(user, version)`, `user.RequirePasswordChange();`, `userManager.AddPasswordAsync(user, tempPassword)`, `userManager.UpdateSecurityStampAsync(user)`. Cờ đặt trên đối tượng trước lời gọi mật khẩu và được lưu cùng lượt — cả lệnh nằm trong một transaction chỉ commit khi thành công (`CoreAndSkill.Core.Application/Common/Behaviors/TransactionBehavior.cs`: `return new TransactionOutcome<TResponse>(response, response.IsSuccess);`), nên ca `version` lệch không ghi gì |
| Phiên đang mở của đích trượt ở request kế tiếp | `CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`: `var sessionValid = await identityService.IsSessionValidAsync(userId, stamp, ct);` |
| Nhật ký kiểm toán cho lần đặt lại | `CoreAndSkill.Core.Infrastructure/Persistence/Interceptors/AuditLogInterceptor.cs`: `ValueChanged(entry, nameof(AppUser.PasswordHash))` ghi `AuditActionCodes.UserPasswordChange` |
| Chính sách mật khẩu chung mọi môi trường | Khối `"Password"` chỉ có ở `src/BE/CoreAndSkill.Api/appsettings.json`; `appsettings.Development.json` không ghi đè |
| FE — nút ở màn chi tiết, sau quyền riêng | `pages/chi-tiet/chi-tiet-nguoi-dung.page.html`: `*appHasPermission="'core.user.reset-password'"`, `<app-hop-dat-lai-mat-khau`; `services/nguoi-dung.service.ts`: `` `${this.duongDan}/${id}/reset-password` `` |
