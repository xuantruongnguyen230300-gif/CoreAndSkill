---
kind: luat
scope: core
verified: 2026-10-06
---

# `D2` — Đổi mật khẩu bắt buộc ở lần đăng nhập đầu

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Trạng thái **nửa vời có chủ đích**: đăng nhập đã thành công, nhưng gần như mọi cửa đều đóng. Bốn cửa mở là bốn cửa đã cân nhắc từng cái.

---

## 1. Ai bắt đầu, ở đâu

Người dùng vừa đăng nhập thành công (luồng `D1`) với cờ buộc đổi mật khẩu đang bật.

Các đường dẫn tới trạng thái này — mọi tài khoản mới, và mọi lần đặt mật khẩu hộ người khác: tài khoản `admin` và tài khoản vận hành do lệnh bootstrap tạo (`V1`), tài khoản quản trị của đơn vị mới (`V2`), quản trị mới tạo cho đơn vị đã có ([`../contracts/tenants.md`](../contracts/tenants.md) §6), tài khoản do quản trị tạo (`N1`), tài khoản vừa bị quản trị đặt lại mật khẩu (`D4`), tài khoản quản trị đơn vị vừa được vận hành khôi phục (`V4`), và tài khoản vận hành vừa được đặt lại bằng lệnh `core reset-operator-password` ([`../database/script-runbook.md`](../database/script-runbook.md) §8).

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ | Đăng nhập **vẫn thành công** — nếu chặn ngay ở đăng nhập thì người dùng không có đường nào để đổi |
| Mật khẩu mới đạt chính sách | Chính sách **giống nhau ở mọi môi trường**, luật **S9** |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | FE | Thấy `mustChangePassword = true` trong DTO phiên — từ response `login` ([`D1-dang-nhap.md`](D1-dang-nhap.md) bước 8) hoặc từ `GET /api/v1/core/auth/me` lúc khởi động app | [`../contracts/auth.md`](../contracts/auth.md) §3, §5 |
| 2 | FE | Chuyển sang màn đổi mật khẩu và **không cho thoát ra** | [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.3 |
| 3 | Người dùng | `POST /api/v1/core/auth/change-password-required` | [`../contracts/auth.md`](../contracts/auth.md) §7 |
| 4 | BE | Đổi mật khẩu, hạ cờ, **cấp lại cookie** cho phiên đang dùng; mọi phiên khác của tài khoản bị chấm dứt | cùng trên |
| 5 | FE | Vào ứng dụng bình thường | |

### Bốn cửa mở trong lúc cờ còn bật

Mọi endpoint **có danh tính** khác trả `CORE.AUTH.PASSWORD_CHANGE_REQUIRED`, trừ đúng bốn đường dưới. Endpoint ẩn danh — như đăng nhập, hay FE báo lỗi về server (luồng `N8`) — **không** bị kiểm ([`../contracts/auth.md`](../contracts/auth.md) §1.2); chặn cả chúng thì màn đổi mật khẩu mất đường báo lỗi.

| Đường | Vì sao phải mở |
| --- | --- |
| Lấy token chống giả mạo | Không có nó thì không gửi được request ghi — kể cả request đổi mật khẩu |
| `GET /api/v1/core/auth/me` | FE cần biết trạng thái để điều hướng đúng |
| `POST /api/v1/core/auth/change-password-required` | **Đường thoát duy nhất** |
| `POST /api/v1/core/auth/logout` | Luôn phải thoát được |

Bỏ sót bất kỳ cửa nào trong bốn cửa này tạo ra một tài khoản **bị nhốt**: đăng nhập được, không đổi được mật khẩu, không thoát được.

### Vì sao chặn ở middleware, không để FE tự điều hướng

FE điều hướng là **trải nghiệm**; middleware chặn là **bảo mật**. Người dùng gọi thẳng endpoint bằng công cụ dòng lệnh là bỏ qua được mọi điều hướng của FE.

Đây là cùng một nguyên tắc với luồng `P3`: menu ẩn một mục không phải hàng rào.

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/auth.md`](../contracts/auth.md) §7 và §11. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Người dùng thấy |
| --- | --- | --- |
| Gọi bất kỳ endpoint có danh tính nào ngoài bốn cửa | `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | FE phải nhận mã này và điều hướng, không hiện màn lỗi chung |
| Mật khẩu mới không đạt chính sách | `CORE.AUTH.CHANGE_PASSWORD_FAILED`, lý do ở `fieldErrors` | BE trả **mã lỗi**, không trả câu tiếng Việt — câu chữ do FE ghép |
| Mật khẩu hiện tại — mật khẩu tạm — gõ sai | `CORE.AUTH.CHANGE_PASSWORD_FAILED`, lý do ở `fieldErrors` của ô mật khẩu hiện tại | Lỗi tại ô mật khẩu hiện tại. Lỗi đi theo mã, không theo endpoint — [`../contracts/auth.md`](../contracts/auth.md) §6 |
| Mật khẩu mới trùng mật khẩu hiện tại | `CORE.VALIDATION.FAILED`, mã `CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT` ở `fieldErrors` | Lỗi tại ô mật khẩu mới. Thiếu luật này thì luồng thành hình thức: gõ lại đúng mật khẩu tạm là hạ được cờ — [`../contracts/auth.md`](../contracts/auth.md) §7, *Ghi chú* |
| Cờ đã hạ ở chỗ khác — ví dụ một tab khác vừa đổi xong | `CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED` | FE coi như đã xong: rời màn như khi đổi thành công, không hiện lỗi — [`../Design/Screens/01-dang-nhap.md`](../Design/Screens/01-dang-nhap.md), mục *Đổi mật khẩu bắt buộc → Trạng thái* |
| FE không xử lý mã `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` | không có mã lỗi mới | 🛑 Người dùng thấy màn lỗi chung ở mọi thao tác, và **không ai nói cho họ biết phải đổi mật khẩu** |
| Bước 4 đổi mật khẩu mà không cấp lại cookie | không có mã lỗi riêng | 🛑 Đổi mật khẩu thành công rồi bị đá về màn đăng nhập ngay ở request kế tiếp — đúng lúc người dùng nghĩ mình vừa vào được |

## 5. Quan hệ với đơn vị

**Không áp dụng** — luồng này chỉ chạm bản ghi của chính người đang đăng nhập, trong đơn vị đã được thiết lập ở luồng `D1` bước 4. Nó không đọc và không ghi dữ liệu của ai khác.

## 6. Câu chưa trả lời được

Không còn.

---

## Đối chiếu với `src/`

Neo của `D2`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| §1 — mọi tài khoản mới mang cờ; đặt mật khẩu hộ bật lại cờ | `CoreAndSkill.Core.Infrastructure/Identity/AppUser.cs`: `MustChangePassword = true,` trong `NewAccount`; `public void RequirePasswordChange() => MustChangePassword = true;` — gọi ở `UserAdminService.cs` (`ResetPasswordAsync`) và `Tenants/TenantProvisioningService.cs` (`ResetPasswordWritesAsync`) |
| Bước 1–2 — FE ép sang màn đổi mật khẩu | `app.routes.ts`: `canActivate: [authGuard, mustChangePasswordGuard]`; `platform/xac-thuc/xac-thuc.routes.ts`: `path: 'doi-mat-khau-bat-buoc'` |
| Bốn cửa mở; endpoint ẩn danh không bị kiểm | `CoreAndSkill.Core.Web/Middleware/PasswordChangeRequiredMiddleware.cs`: bốn mục của `Allowlist` — `CoreRoutes.AntiforgeryToken`, `CoreRoutes.AuthMe`, `CoreRoutes.AuthChangePasswordRequired`, `CoreRoutes.AuthLogout`; `var allowsAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;` |
| Bước 3–4 — đổi mật khẩu, hạ cờ, cấp lại cookie | `CoreAndSkill.Core.Web/Controllers/AuthController.cs`: `[HttpPost(CoreRoutes.AuthChangePasswordRequired)]`, `await SignInAsync(result.Value, ReadIssuedAt(User));`; `CoreAndSkill.Core.Infrastructure/Identity/IdentityService.cs`: `userManager.ChangePasswordAsync(user, currentPassword, newPassword)`, `user.ClearPasswordChangeRequirement();`, `var freshStamp = await userManager.GetSecurityStampAsync(user);` |
| Phiên khác bị chấm dứt | stamp mới vào cookie mới; phiên cũ trượt ở `CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`: `var sessionValid = await identityService.IsSessionValidAsync(userId, stamp, ct);` |
| Mật khẩu mới trùng mật khẩu hiện tại | `CoreAndSkill.Core.Application/Auth/ChangePasswordRequiredCommandValidator.cs`: `.NotEqual(x => x.CurrentPassword).WithErrorCode(AuthErrors.NewPasswordSameAsCurrent.Code)` |
| Sai mật khẩu hiện tại, mật khẩu mới không đạt chính sách | `IdentityService.cs`: `IdentityErrorMapper.Subject.CurrentPassword => "CurrentPassword"`, `IdentityErrorMapper.Subject.NewPassword => "NewPassword"` |
| Cờ đã hạ ở chỗ khác | BE: `CoreAndSkill.Core.Application/Auth/ChangePasswordRequiredCommandHandler.cs`: `return Result.Failure<LoginOutcome>(AuthErrors.PasswordChangeNotRequired);`; FE: `platform/xac-thuc/pages/doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page.ts`: `if (err.body?.error.code === 'CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED') {` rồi `this.sauKhiDoiThanhCong();` |
| FE nhận `CORE.AUTH.PASSWORD_CHANGE_REQUIRED` và điều hướng | `core/interceptors/error.interceptor.ts`: `err.status === 403 && body?.error.code === 'CORE.AUTH.PASSWORD_CHANGE_REQUIRED'` rồi `void auth.lamMoiPhien();` |
