---
kind: luat
scope: core
verified: 2026-10-06
---

# `D5` — Khoá và mở khoá một tài khoản

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Có **hai** đường làm tài khoản bị khoá, và chúng khác nhau ở chỗ ai mở lại được. Lẫn hai đường này là nguồn của phần lớn hiểu nhầm ở màn quản trị người dùng.

---

## 1. Ai bắt đầu, ở đâu

Hai khởi điểm khác nhau:

| Đường | Ai bắt đầu |
| --- | --- |
| **Khoá tự động** | Không ai — hệ khoá sau một số lần đăng nhập sai liên tiếp, tự mở khi hết thời gian khoá. Ngưỡng và thời gian khoá khai ở cấu hình `Core:Identity:Lockout`; giá trị mặc định: [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2 |
| **Khoá thủ công** | Quản trị đơn vị, từ màn chi tiết người dùng |

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Quyền `core.user.lock` | Chỉ cho đường thủ công |
| Người dùng đích thuộc cùng đơn vị | |
| `lock`: đích không phải chính người gọi | **Luật 3** — [`../contracts/users.md`](../contracts/users.md) §2 |
| `lock`: đích qua được **Luật 4** | [`../contracts/users.md`](../contracts/users.md) §2 — chặn khoá tài khoản quản trị khi người gọi không đủ tư cách. `unlock` cố ý không áp luật nào — cùng mục đó, *Mở khoá* |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Quản trị | `POST /api/v1/core/users/{id}/lock` hoặc `/unlock`, kèm `version` của bản ghi đang xem | [`../contracts/users.md`](../contracts/users.md) §8 |
| 2 | BE | `lock`: kiểm Luật 3 rồi Luật 4 **trước khi** chạm tầng ghi — trước cả phép tra đích và phép so `version`. Rồi đặt mốc hết khoá qua `UserManager` và cờ `locked_by_admin` — **không ghi cột bằng SQL tay** | [`../contracts/users.md`](../contracts/users.md) §2 · [`../database/schema-core.md`](../database/schema-core.md) §4.1 |
| 3 | BE | `lock`: đổi `security_stamp` tường minh trong cùng thao tác ⇒ phiên đang chạy bị chấm dứt **ở request kế tiếp**. `unlock`: **không** đổi stamp | [`../contracts/users.md`](../contracts/users.md) §8 |

### Vì sao không ghi cột khoá bằng SQL

Thao tác khoá đặt mốc hết khoá qua `UserManager` **và** đổi `security_stamp` tường minh trong cùng thao tác đó ([`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4). Ghi thẳng vào cột bằng SQL thì chỉ làm được nửa đầu — và nửa sau mới là nửa đá người đang đăng nhập ra.

Kết quả của việc bỏ nửa sau: tài khoản hiện là "đã khoá" trên màn quản trị, còn người bị khoá vẫn thao tác bình thường.

### Ba điều dễ sai của khoá tự động

| # | Điều | Vì sao |
| --- | --- | --- |
| 1 | **Khoá theo tài khoản là chưa đủ** | Kẻ tấn công thử một mật khẩu phổ biến trên hàng nghìn tài khoản thì không tài khoản nào chạm ngưỡng |
| 2 | **Khoá theo tài khoản mở đường cho tấn công từ chối dịch vụ nhắm vào một người** | Biết tên đăng nhập là khoá được người đó |
| 3 | **Thông điệp không được tiết lộ tài khoản có tồn tại hay không** | [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) |

Điều 1 và 2 kéo theo: khoá theo tài khoản phải đi **cùng** hạn mức theo nhịp gọi, không thay thế cho nó.

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/auth.md`](../contracts/auth.md) §3 và §11, [`../contracts/users.md`](../contracts/users.md) §8. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Tài khoản bị khoá, đăng nhập **đúng** mật khẩu | `CORE.AUTH.LOCKED_OUT` | **Câu riêng**, khác "sai thông tin đăng nhập". Người dùng thật cần biết để đi tìm quản trị — lý do ở [`../contracts/auth.md`](../contracts/auth.md) §3 |
| Tài khoản bị khoá, đăng nhập **sai** mật khẩu | `CORE.AUTH.INVALID_CREDENTIALS` | Cùng câu với mọi ca sai thông tin — báo khoá ở đây là xác nhận tài khoản có thật cho người không biết mật khẩu |
| Chạm hạn mức theo nhịp gọi | `CORE.RATE_LIMIT.EXCEEDED` | **Khác** `CORE.AUTH.LOCKED_OUT`: mã này tự hết sau `Retry-After`; `LOCKED_OUT` hết khi quản trị mở khoá, hoặc khi hết thời gian của khoá tự động |
| Người bị khoá đang có phiên mở | `CORE.AUTH.NOT_AUTHENTICATED` | Request kế tiếp đưa về màn đăng nhập như phiên hết hạn; đăng nhập lại **đúng** mật khẩu thì rơi vào dòng `CORE.AUTH.LOCKED_OUT` |
| Thiếu quyền khoá | `CORE.AUTH.FORBIDDEN` | |
| `lock` nhắm vào chính người gọi | `CORE.USER.CANNOT_LOCK_SELF` | Luật 3, xét **trước** Luật 4 và trước cả phép tra đích (bước 2) |
| `lock` nhắm vào tài khoản quản trị mà người gọi không đủ tư cách khoá | `CORE.USER.SYSTEM_ROLE_LOCK_FORBIDDEN` | Luật 4, xét ngay sau Luật 3 — trước phép tra đích và phép so `version` |
| Không có người dùng đích | `CORE.USER.NOT_FOUND` | Cả `lock` lẫn `unlock` |
| Identity từ chối thao tác | `CORE.USER.LOCK_FAILED` | Cả `lock` lẫn `unlock` |
| Bản ghi đích đã bị thao tác khác ghi sau khi quản trị mở màn — `version` gửi lên lệch | `CORE.CONCURRENCY.CONFLICT` | Không ghi gì. Tải lại chi tiết rồi thao tác lại — [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6 |
| Khoá bằng SQL tay | không có mã lỗi | 🛑 Màn quản trị hiện "đã khoá", người bị khoá vẫn làm việc bình thường |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị.** Người thao tác và người bị tác động cùng đơn vị, như luồng `D4`.

Khoá tự động thì không có người thao tác, nhưng bản ghi vẫn nằm trong một đơn vị — và ngưỡng khoá áp cho tài khoản đó, không áp cho toàn hệ.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Màn quản trị phân biệt hai đường khoá bằng `lockedByAdmin` ([`../contracts/users.md`](../contracts/users.md) §3); mở khoá không đá phiên (bước 3).

---

## Đối chiếu với `src/`

Neo của `D5`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/platform/quan-tri/nguoi-dung/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Bước 1 — hai route, quyền `core.user.lock`, thân mang `version` | `CoreAndSkill.Core.Web/Controllers/UsersController.cs`: `[HttpPost("{id:guid}/lock")]`, `[HttpPost("{id:guid}/unlock")]`, `[RequirePermission(CorePermissions.UserLock)]`, `[FromBody] VersionedRequest body` |
| Bước 2 — Luật 3 rồi Luật 4 trước tầng ghi; `unlock` không qua luật nào | `CoreAndSkill.Core.Application/Users/LockUserCommandHandler.cs`: `UserPrivilegeGuard.EnsureNotLockingSelf(callerId, command.UserId)`, rồi `privilegeGuard.EnsureCanLockAsync(callerId, command.UserId, ct)`, rồi `userAdmin.LockAsync`; `UnlockUserCommandHandler.cs`: `=> userAdmin.UnlockAsync(command.UserId, command.Version, ct);` |
| Bước 2–3 — tra đích, so `version`, cờ `locked_by_admin`, mốc khoá qua `UserManager`, đổi stamp chỉ khi khoá | `CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`, hàm `SetLockoutAsync`: `return Result.Failure(UserErrors.NotFound);`, `IdentityConcurrency.VersionMatches(user, version)`, `user.MarkLockedByAdmin();`, `userManager.SetLockoutEndDateAsync(user, lockoutEnd)`, `await userManager.UpdateSecurityStampAsync(user);` trong khối `if (locked)` |
| Khoá tự động; đăng nhập khi đang khoá | `CoreAndSkill.Core.Infrastructure/Identity/IdentityService.cs`: `await userManager.AccessFailedAsync(user);` và `return Result.Failure<CredentialCheck>(AuthErrors.LockedOut); // Ca 3.`; khoá cấu hình ở `CoreAndSkill.Core.Application/Configuration/CoreIdentityLockoutOptions.cs`: `"Core:Identity:Lockout"` |
| Phiên đang mở bị chấm dứt ở request kế tiếp | `CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`: `var sessionValid = await identityService.IsSessionValidAsync(userId, stamp, ct);` |
| Hạn mức theo nhịp gọi trả `Retry-After` | cùng tệp: `context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds;` |
| FE — khoá, mở khoá từ màn chi tiết; hiện cờ `lockedByAdmin` | `services/nguoi-dung.service.ts`: `` `${this.duongDan}/${id}/lock` ``, `` `${this.duongDan}/${id}/unlock` ``; `pages/chi-tiet/chi-tiet-nguoi-dung.page.ts`: `protected xacNhanKhoa(): void {`, `protected moKhoa(): void {`; `components/the-thong-tin-nguoi-dung/the-thong-tin-nguoi-dung.component.html`: `nd.lockedByAdmin` |
