---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0090 — Tên đăng nhập `system` là tên dành riêng: mọi đường tạo tài khoản từ chối nó bằng một lỗi validation, so như `NormalizedUserName`; tài khoản đã có không bị đổi

> **Trạng thái:** Đã chấp nhận (2026-09-24)

## Bối cảnh

Khi không có người thực hiện (job nền, lệnh bootstrap), Core ghi danh tính hệ thống vào `created_by`, `updated_by` và `actor_display`. Giá trị đó là `system`, khai ở `src/BE/Core/CoreAndSkill.Core.Application/Identity/SystemActor.cs` — `public const string UserName = "system";`.

`created_by` giữ **tên đăng nhập**, và có chỗ so nó với tên đăng nhập của người đang gọi: `IsUploader` trong `src/BE/Core/CoreAndSkill.Core.Application/Files/FileAccessPolicy.cs`. Với tệp chưa gắn chủ, phép so đó quyết định ai được đọc và ghi tệp. Vì vậy một tài khoản tên `system` sẽ mang cùng danh tính với mọi dòng hệ thống đã ghi trong đơn vị của nó.

Trước quyết định này, không đường tạo tài khoản nào chặn tên đó. Bốn đường tạo tài khoản đang có:

| Đường | Khoá mang tên đăng nhập |
| --- | --- |
| [`../contracts/users.md`](../contracts/users.md) §5 | `UserName` |
| [`../contracts/tenants.md`](../contracts/tenants.md) §2 | `AdminUserName` |
| [`../contracts/tenants.md`](../contracts/tenants.md) §6 | `UserName` |
| Lệnh `core bootstrap` ([`../database/script-runbook.md`](../database/script-runbook.md) §3.3, bước 4) | `Core:Bootstrap:OperatorUserName`, `Core:Bootstrap:AdminUserName` |

Giá trị `system` đã nằm trong dữ liệu. Script `database/scripts/core/0002__core__seed-permission-catalog.sql` ghi `'system'` vào hai cột `created_by` và `updated_by`, và script đã chạy thì không sửa ([`../database/script-runbook.md`](../database/script-runbook.md) §2).

## Quyết định

Người dùng chốt ngày 2026-09-24:

1. **`system` là tên dành riêng.** Không đường tạo tài khoản nào nhận nó làm tên đăng nhập.
2. **Qua HTTP, lỗi có mã `CORE.USER.USERNAME_RESERVED`, loại `Validation`, không `messageParams`.** Mã này đi trong `fieldErrors`, dưới mã gốc 400 `CORE.VALIDATION.FAILED`, ở đúng khoá của bảng Bối cảnh. Phép kiểm đứng ở validator, vì điều kiện tính được từ payload.
3. **Lệnh bootstrap thoát mã 1 và không ghi dòng nào.** Phép kiểm chạy ở đầu lệnh, cùng chỗ với phép kiểm giá trị thiếu.
4. **So như Identity so tên đăng nhập:** cắt khoảng trắng hai đầu, rồi `Normalize().ToUpperInvariant()` — đúng phép sinh `NormalizedUserName`. `System` và ` SYSTEM ` đều bị từ chối.
5. **Giá trị có đúng một nguồn:** `SystemActor.UserName`.
6. **Tài khoản đã mang tên `system` từ trước không bị đổi.** Phép chặn chỉ đứng ở đường **tạo**.

Mã riêng này không phá luật gộp mã chống dò tên của [`../contracts/tenants.md`](../contracts/tenants.md) §6. Tên bị từ chối ở mọi đơn vị, bất kể database có gì, nên lỗi này không cho biết gì về dữ liệu.

## Phương án đã cân nhắc và vì sao loại

Kiến trúc sư ghi các phương án dưới đây ngày 2026-09-25, lúc viết ADR này — sau khi người dùng đã chốt.

### Phương án A — Đổi danh tính hệ thống sang một chuỗi không thể là tên đăng nhập

Ví dụ `(system)`: dấu ngoặc nằm ngoài tập ký tự tên đăng nhập mặc định của Identity.

**Được:** không cần chặn gì, vì không thể có va chạm. Không đường tạo nào phải nhớ một phép kiểm.

**Mất:**

- Dữ liệu đã ghi giữ `system`, còn dòng mới mang giá trị khác. Hai giá trị cùng nghĩa sẽ nằm chung một cột.
- Sự an toàn dựa vào một thiết lập cấu hình, `AllowedUserNameCharacters`. Ai nới tập ký tự đó thì va chạm quay lại, và không có gì báo.

**Vì sao loại:** dữ liệu bị tách làm hai giá trị, và một cấu hình lặng lẽ trở thành thứ gánh an toàn.

### Phương án B — Chặn ở bộ kiểm người dùng của Identity (`IUserValidator<AppUser>`)

**Được:** một chỗ phủ mọi lệnh ghi qua `UserManager`, kể cả đường tạo viết sau này. Không validator nào phải nhớ gọi phép kiểm.

**Mất:**

- Lỗi ra theo hình dạng của Identity: 422 `CREATE_FAILED` / `ADMIN_CREATE_FAILED`. Ở [`../contracts/tenants.md`](../contracts/tenants.md) §6, nó còn bị gộp vào mã chung không nêu lý do, nên người vận hành không biết vì sao bị từ chối.
- Bộ kiểm Identity chạy cả khi **sửa**. Mọi lần sửa một tài khoản `system` đã có sẽ bị chặn, trái với quyết định 6.
- Phép kiểm chạy sau các lần đọc database của phép kiểm trước, dù điều kiện tính được ngay từ payload.

**Vì sao loại:** hình dạng lỗi không khớp điều người dùng chốt, và phương án chạm cả đường sửa.

## Hệ quả

### Tích cực

- Không tài khoản mới nào mang được danh tính hệ thống, ở mọi đơn vị.
- FE nhận một lý do cụ thể dưới đúng ô tên đăng nhập, cùng khuôn với mọi lỗi validation khác.
- Mỗi đường tạo hôm nay có test ghim hành vi. Khoá cấu hình của lệnh bootstrap có `BootstrapReservedUserNameTests`. Đường HTTP của [`../contracts/users.md`](../contracts/users.md) §5 có `CreateUserReservedUserNameEndpointTests`. Hai validator của [`../contracts/tenants.md`](../contracts/tenants.md) có `TenantValidatorsTests`.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Tài khoản `system` có từ trước vẫn va chạm** | `IsUploader` so phân biệt hoa thường, nên tài khoản tên đúng `system` vẫn được coi là người tải lên mọi tệp chưa gắn chủ mà hệ thống ghi trong đơn vị của nó. Câu `SELECT tenant_id, user_name FROM core.app_user WHERE normalized_user_name = 'SYSTEM'` tìm được mọi tài khoản mang tên dành riêng, ở mọi dạng hoa thường. Xử lý chúng là việc tay của người vận hành |
| **Đường tạo tài khoản viết sau này có thể quên phép kiểm** | Phép kiểm đứng ở từng validator, không ở một chỗ chung. Test hiện có chỉ ghim các đường đang có; không cổng nào bắt một đường mới thiếu nó |
| **Phép so lặp lại logic của Identity** | Tầng Application không tham chiếu Identity, nên `IsReservedUserName` tự làm lại phép chuẩn hoá. `SystemActorNormalizationTests` đối chiếu với bộ chuẩn hoá thật của host; đổi bộ chuẩn hoá mà quên chỗ này thì test đỏ |
| **Một mã lỗi mới FE phải dịch** | `CORE.USER.USERNAME_RESERVED` cần một khoá dịch |

### Rút lui nếu sai

1. Gỡ lời gọi `NotReservedUserName()` khỏi ba validator và gỡ thuộc tính `[NotReservedUserName]` khỏi hai khoá cấu hình của `CoreBootstrapOptions`.
2. Gỡ mã `CORE.USER.USERNAME_RESERVED` khỏi `UserErrors`, khỏi hai hợp đồng, và gỡ khoá dịch ở FE.

Không có dữ liệu nào cần chuyển: quyết định này không ghi gì, chỉ từ chối.

### Dấu hiệu quyết định này bắt đầu sai

- Một đường tạo tài khoản mới xuất hiện — tự đăng ký, nhập tài khoản hàng loạt, module tạo tài khoản — mà validator của nó không gọi phép kiểm. Lúc đó cần một cổng chung, hoặc xét lại phương án B.
- Có thêm tên cần dành riêng. Lúc đó một hằng không còn đủ.
- Có chỗ bắt đầu nhận ra hàng do hệ thống ghi bằng cách so `created_by` với `system`, thay vì bằng một cờ.

## Liên quan

- [`../RULES.md`](../RULES.md) S22 — cổng canh quyết định 5
- [`0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md`](0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md) — cùng các đường ghi tài khoản
- [`../contracts/users.md`](../contracts/users.md) §5 · [`../contracts/tenants.md`](../contracts/tenants.md) §2, §6
- [`../database/script-runbook.md`](../database/script-runbook.md) §3.3
