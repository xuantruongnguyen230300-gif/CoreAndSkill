---
kind: luat
scope: core
verified: 2026-10-06
---

# `N9` — Người dùng sửa hồ sơ của chính mình

✅ **CÓ THẬT** — đối chiếu trọn tệp với `src/` ngày 2026-10-06, chỉ bằng đọc mã, không chạy ứng dụng. Neo từng bước ở mục *Đối chiếu với `src/`* cuối tệp.

> Luồng duy nhất trong Core mà **mọi người dùng đều gọi được, không cần quyền nào**. Đó là quyết định, không phải sơ suất — xem §3.

---

## 1. Ai bắt đầu, ở đâu

Bất kỳ người dùng đã đăng nhập nào, từ menu tài khoản.

## 2. Điều kiện trước

| Cần có | Ghi chú |
| --- | --- |
| Phiên hợp lệ | Không cần khoá quyền nào |
| Token chống giả mạo | Sửa hồ sơ là request ghi |

## 3. Các bước

| # | Ai làm | Hệ thống làm gì | Chi tiết ở |
| --- | --- | --- | --- |
| 1 | Người dùng | `GET /api/v1/core/profile` | [`../contracts/profile.md`](../contracts/profile.md) §1 |
| 2 | Người dùng | Sửa họ tên, số điện thoại, ngôn ngữ ưa thích | cùng trên §2 |
| 3 | Người dùng | `PUT /api/v1/core/profile` kèm `version` nhận ở bước 1 | cùng trên |
| 4 | BE | Ghi **chỉ bản ghi của chính người đang gọi** — định danh lấy từ phiên, không lấy từ thân request | cùng trên |

### Vì sao endpoint "của chính mình" không kiểm quyền

Ma trận phân quyền trả lời câu *"người này có được tác động lên **người khác** không"*. Với thao tác lên chính mình, câu đó không có nghĩa — và nếu bắt phải có quyền thì một tài khoản mới tạo, chưa gán vai trò, sẽ **không đổi được mật khẩu của chính nó**, tức không thoát khỏi luồng `D2`.

Đây cũng là lời giải cho tài khoản vận hành hệ thống: nó bị luật **M9** cấm giữ vai trò nghiệp vụ, nên nếu các endpoint "của chính mình" đòi quyền thì nó không xem nổi hồ sơ của chính nó.

> 🛑 **Ranh giới phải hẹp và phải viết ra.** "Của chính mình" nghĩa là thao tác lấy định danh **từ phiên**, không nhận định danh từ thân request. Một endpoint nhận `userId` trong thân rồi tự tin đó là người gọi thì **không** thuộc nhóm này — đó là đường leo thang quyền cổ điển nhất.

> 📖 Nhóm endpoint "của chính mình" khai bằng mức `[AuthenticatedOnly]` của luật **S11**: [`../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md`](../adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md).

## 4. Hỏng ở đâu — và người dùng thấy gì

> 📖 Loại lỗi và HTTP status của từng mã: [`../contracts/profile.md`](../contracts/profile.md) §2 và [`../contracts/auth.md`](../contracts/auth.md) §11. Bảng dưới chỉ giữ `code`.

| Ca | Mã lỗi | Thấy gì |
| --- | --- | --- |
| Họ tên rỗng hoặc quá dài; số điện thoại sai khuôn; mã ngôn ngữ sai khuôn | `CORE.VALIDATION.FAILED` | Lỗi tại đúng ô đang sai |
| Thiếu token chống giả mạo | `CORE.AUTH.CSRF_REJECTED` | |
| Chưa đăng nhập | `CORE.AUTH.NOT_AUTHENTICATED` | |
| Giữa bước 1 và bước 3, một thao tác khác đã ghi vào bản ghi tài khoản — ví dụ quản trị sửa thông tin người này ([`../contracts/users.md`](../contracts/users.md) §6) hoặc gán vai trò cho họ (`P2`), hay chính người dùng vừa lưu ở một tab khác — `version` gửi lên đã lệch | `CORE.CONCURRENCY.CONFLICT` | Dữ liệu đang nhập **giữ nguyên**; tải lại rồi lưu lần nữa. Không tự gửi lại — [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §6 |
| Giữa bước 1 và bước 3, quản trị khoá hoặc đặt lại mật khẩu tài khoản này (`D5`, `D4`) | `CORE.AUTH.NOT_AUTHENTICATED` | **Không** phải `CORE.CONCURRENCY.CONFLICT`: hai thao tác đó chấm dứt phiên, nên request lưu bị từ chối trước khi tới bước 4. Người dùng về màn đăng nhập như phiên hết hạn — cùng hàng *phiên đang mở* ở §4 của `D4` và `D5` |
| Bước 4 lấy định danh từ thân request | không có mã lỗi | 🛑 Bất kỳ ai sửa được hồ sơ của bất kỳ ai. Không lỗi, không log bất thường — chỉ là một dòng dữ liệu đổi chủ |

## 5. Quan hệ với đơn vị

**Thuộc đơn vị**, nhưng theo cách chặt hơn mọi luồng khác: phạm vi không phải "đơn vị của tôi" mà là **"đúng một bản ghi, là tôi"**.

Bộ lọc đơn vị vẫn áp, nhưng nó **không phải** hàng rào chính ở đây — hàng rào chính là việc định danh lấy từ phiên. Một lỗi ở bước 4 vẫn bị bộ lọc đơn vị chặn lại ở ranh giới đơn vị, nghĩa là thiệt hại giới hạn trong cùng đơn vị. Đó là một lớp phòng thủ thứ hai tình cờ có, không phải lớp được thiết kế cho ca này.

## 6. Câu chưa trả lời được

Không còn câu riêng của luồng này. Ngôn ngữ của tệp xuất là câu của luồng `N3` §6.

---

## Đối chiếu với `src/`

Neo của `N9`, đối chiếu 2026-10-06. Đường dẫn BE tính từ `src/BE/Core/`, FE từ `src/FE/src/app/platform/`.

| Bước, ca | Neo bằng chuỗi |
| --- | --- |
| Bước 1, 3 — hai endpoint mức `[AuthenticatedOnly]`, không khoá quyền | `CoreAndSkill.Core.Web/Controllers/ProfileController.cs`: `[AuthenticatedOnly("Hồ sơ của chính người gọi — định danh lấy từ phiên")]`, `[AuthenticatedOnly("Sửa hồ sơ của chính người gọi — định danh lấy từ phiên")]` |
| Bước 4 — định danh lấy từ phiên | `CoreAndSkill.Core.Application/Profile/UpdateProfileCommandHandler.cs`: `var userId = currentUser.UserId`; thân request không có trường định danh — `ProfileController.cs`: `public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber, string? PreferredLanguage, string? Version);` |
| Ô sai → `CORE.VALIDATION.FAILED` | `CoreAndSkill.Core.Application/Profile/UpdateProfileCommandValidator.cs`: `MaximumLength(200)`, `PhoneNumberPattern`, `LanguagePattern` |
| `version` lệch → `CORE.CONCURRENCY.CONFLICT`, không ghi gì | `CoreAndSkill.Core.Infrastructure/Identity/UserProfileService.cs`: `if (!IdentityConcurrency.VersionMatches(user, input.Version))` |
| Quản trị sửa người dùng hay gán vai trò làm lệch `version` | Gán vai trò đổi token tài khoản — `CoreAndSkill.Core.Infrastructure/Identity/UserAdminService.cs`: `.SetProperty(u => u.ConcurrencyStamp, newStamp)` trong `TryClaimAccountVersionAsync`; sửa thông tin đi `userManager.UpdateAsync`, vốn đổi `ConcurrencyStamp` |
| Bị khoá hay bị đặt lại mật khẩu giữa chừng → `CORE.AUTH.NOT_AUTHENTICATED` trước khi tới bước 4 | `CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`: `var sessionValid = await identityService.IsSessionValidAsync(userId, stamp, ct);` |
| Thiếu token chống giả mạo | `CoreAndSkill.Core.Web/Security/SecurityErrors.cs`: `"CORE.AUTH.CSRF_REJECTED"` |
| FE — vào từ menu tài khoản; nhánh 409 giữ dữ liệu đang nhập | `shell/shell.component.ts`: `{ key: 'ho-so', label: this.translate.instant('hoSo.tieuDe'), icon: 'user' }`; `ho-so/pages/ho-so/ho-so.page.ts`: `if (err.body?.error.code === 'CORE.CONCURRENCY.CONFLICT') {`, `this.xungDotDongThoi.set(true);` |
