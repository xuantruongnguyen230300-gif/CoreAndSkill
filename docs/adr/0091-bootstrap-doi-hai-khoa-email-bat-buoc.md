---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0091 — Lệnh `core bootstrap` đòi hai khoá email bắt buộc, `Core:Bootstrap:OperatorEmail` và `Core:Bootstrap:AdminEmail`; Core không tự sinh email khi thiếu

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

Vòng `core-reviewer` ngày 2026-09-25 báo một lỗi mức Chặn: lệnh `core bootstrap` thất bại trên mọi host, ngay ở tài khoản đầu tiên.

Kiến trúc sư đọc code ngày 2026-09-25 và xác nhận chuỗi nhân quả:

| # | Sự thật | Neo |
| --- | --- | --- |
| 1 | Core bật tuỳ chọn email duy nhất của Identity | `src/BE/Core/CoreAndSkill.Core.Infrastructure/DependencyInjection/CoreInfrastructureServiceCollectionExtensions.cs`, chuỗi `options.User.RequireUniqueEmail = true`. Lý do: [`../database/schema-core.md`](../database/schema-core.md) §4.1 |
| 2 | Khi tuỳ chọn đó bật, bộ kiểm người dùng của Identity từ chối tài khoản có email rỗng, trước mọi lệnh lưu | Hành vi của `UserValidator<TUser>` trong ASP.NET Core Identity |
| 3 | Trước ADR này, nhóm khoá `Core:Bootstrap:*` không có khoá email nào | `src/BE/Core/CoreAndSkill.Core.Application/Configuration/CoreBootstrapOptions.cs`, đọc sáng 2026-09-25: không thuộc tính nào chứa chữ `Email`. Bảng khoá ở [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 bước 4 cũng không có |
| 4 | Lệnh dựng đầu vào cho service tạo đơn vị mà không truyền email, nên email nhận giá trị mặc định `null` | Đọc sáng 2026-09-25: hàm `RunBootstrapAsync` trong `src/BE/Core/CoreAndSkill.Core.Web/Commands/CoreCommandRunner.cs` không truyền email; tham số email của `CreateTenantInput` trong `src/BE/Core/CoreAndSkill.Core.Application/Tenants/ITenantProvisioningService.cs` khi đó là tuỳ chọn, mặc định `null`. Cùng ngày, lượt thi công ADR này đổi nó thành bắt buộc |

Hệ quả là Identity từ chối tài khoản vận hành và lệnh thoát mã 1. Không dòng nào được ghi, vì đơn vị hệ thống mới chỉ nằm trong bộ theo dõi. Chưa bản cài nào qua được bước 5 của runbook.

Không test nào bắt được lỗi này. Test không database duy nhất chạy trọn đường thành công dựng `UserManager` bằng tay, với `IdentityOptions` mặc định, tức email duy nhất đang **tắt**. Lệnh cũng chưa từng chạy trên PostgreSQL thật (bảng đầu tệp [`../database/script-runbook.md`](../database/script-runbook.md), hàng §3.3).

Endpoint tạo đơn vị ([`../contracts/tenants.md`](../contracts/tenants.md) §2) đã đòi `adminEmail` từ trước. Hai đường cùng gọi một service ([ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md)), nhưng chỉ đường HTTP truyền email.

## Quyết định

Người dùng chốt ngày 2026-09-25:

1. **Nhóm khoá `Core:Bootstrap:*` có thêm hai khoá bắt buộc:** `Core:Bootstrap:OperatorEmail` cho tài khoản vận hành hệ thống, `Core:Bootstrap:AdminEmail` cho tài khoản quản trị đơn vị. Cả hai khoá đều không phải bí mật.
2. **Thiếu hoặc rỗng một trong hai khoá thì lệnh thoát mã 1 và không ghi dòng nào.** Phép kiểm đứng ở đầu lệnh, cùng chỗ với phép kiểm giá trị thiếu của các khoá khác ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4.4).
3. **Lệnh truyền hai giá trị đó vào service tạo đơn vị.** Core **không** tự sinh email khi thiếu.

Kiến trúc sư chốt thêm hai chi tiết thi công:

4. **Lệnh `core reset-operator-password` không đòi hai khoá mới.** Lệnh đó không tạo tài khoản ([`../database/script-runbook.md`](../database/script-runbook.md) §8).
5. **Phép kiểm đầu lệnh bắt cả email sai định dạng và email dài quá độ rộng cột `core.app_user.email`**, không chỉ giá trị rỗng. Phép kiểm định dạng là đúng phép mà bộ kiểm người dùng của Identity chạy, nên giá trị qua được đầu lệnh không bị Identity trả lại lỗi email. Người vận hành nhận lỗi kèm tên khoá, không phải một mã lỗi Identity.

Tên khoá và nguồn giá trị theo môi trường nằm ở định nghĩa gốc: [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 bước 4.

Hai email được phép trùng nhau. `EmailIndex` chỉ duy nhất theo cặp với `tenant_id`, và hai tài khoản nằm ở hai đơn vị khác nhau.

## Phương án đã cân nhắc và vì sao loại

Kiến trúc sư ghi các phương án dưới đây ngày 2026-09-25, sau khi người dùng đã chốt.

### Phương án A — Một khoá email dùng chung cho cả hai tài khoản

Đây là phương án đơn giản nhất.

**Được:** một khoá thay vì hai. Về dữ liệu thì hợp lệ, vì email chỉ duy nhất trong phạm vi một đơn vị.

**Mất:** tài khoản vận hành và tài khoản quản trị đơn vị là hai vai của hai phía: một bên vận hành bản cài, một bên thuộc đơn vị sử dụng ([ADR-0017](0017-khu-quan-tri-he-thong.md)). Có một khoá chung thì người vận hành sẽ điền email của chính mình cho cả tài khoản của đơn vị. Từ đó mọi thư gửi cho tài khoản quản trị đơn vị đều tới tay người vận hành.

**Vì sao loại:** bớt được một dòng cấu hình, nhưng đổi lại một tài khoản mang email của người khác ngay từ lúc tạo.

### Phương án B — Khoá email tuỳ chọn; thiếu thì Core sinh một địa chỉ giữ chỗ

Ví dụ `<tên đăng nhập>@<mã đơn vị>.invalid`.

**Được:** lệnh không bao giờ vấp vì email. Lợi ích *"cấu hình cũ vẫn chạy"* thì hôm nay bằng không, vì chưa bản cài nào chạy lệnh thành công.

**Mất:**

- `core.app_user` là bảng người dùng duy nhất ([`../database/schema-core.md`](../database/schema-core.md) §4.1). Nó sẽ nhận một giá trị trông như dữ liệu thật mà không ai nhập. Giá trị đó hiện ở màn hồ sơ và là đích của mọi thư gửi tới tài khoản. Thư sẽ đi vào một tên miền không tồn tại mà không ai hay.
- Core thành nơi quyết định khuôn dạng email cho dự án hạ nguồn.

Họ tên thì hôm nay Core **có** thay bằng tên đăng nhập khi thiếu (chuỗi `input.AdminFullName ?? input.AdminUserName` trong `TenantProvisioningService.cs`). Hai trường khác loại nhau: họ tên chỉ để hiển thị, còn email là địa chỉ nhận thư.

**Vì sao loại:** phương án này giấu một cấu hình thiếu sau một giá trị giả. Nhóm khoá này được kiểm ở đầu lệnh chính là để tránh loại lỗi im lặng đó.

### Phương án C — Tắt tuỳ chọn email duy nhất của Identity

**Được:** email thành tuỳ chọn ở mọi đường, và lệnh chạy được mà không cần khoá mới.

**Mất:**

- Hành vi của **mọi** đường ghi tài khoản đổi theo, chỉ để sửa một lệnh chạy một lần.
- `EmailIndex` phải bỏ `UNIQUE`, tức cần một script schema mới ([`../database/schema-core.md`](../database/schema-core.md) §4.1).
- Hai tài khoản cùng đơn vị có thể mang cùng email. Khi đó mọi tính năng tra tài khoản theo email sau này, như quên mật khẩu tự phục vụ (ngoài v1 theo [ADR-0029](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)), không còn xác định được đúng một tài khoản.

**Vì sao loại:** phạm vi ảnh hưởng quá lớn so với lỗi cần sửa. Đây là một quyết định lược đồ, không phải một chỗ sửa cấu hình.

## Hệ quả

### Tích cực

- Lệnh bootstrap qua được bộ kiểm người dùng thật, và áp cùng luật email với endpoint tạo đơn vị.
- Thiếu email lộ ra ngay đầu lệnh, kèm tên khoá. Người vận hành không phải đọc một mã lỗi Identity sau khi đầu vào đã được dựng.
- Không đổi lược đồ, và không đổi hợp đồng HTTP nào.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Người vận hành điền thêm hai giá trị ở mọi bản cài, kể cả máy dev** | Trên máy dev, hai khoá này nằm trong `appsettings.Development.json`, là tệp vào repo. Ai điền email thật của mình vào đó thì đưa dữ liệu cá nhân vào lịch sử git. Giá trị dev phải là địa chỉ ví dụ (tên miền `example.com`), và **không cổng nào ép điều này** |
| **Sửa khoá sau lần chạy đầu không đổi email của tài khoản** | Chạy lại lệnh thì tài khoản đã có được bỏ qua (tính chất *"chạy lại không nhân đôi"*). Vì vậy sửa `OperatorEmail` rồi chạy lại không có tác dụng gì, và lệnh cũng không báo. Muốn đổi email thì đi qua màn hồ sơ |
| **Phép kiểm định dạng ở đầu lệnh là bản lặp của phép kiểm trong Identity** | Hai phép khớp nhau chỉ vì cùng dùng `EmailAddressAttribute`. Một bản nâng cấp Identity đổi phép kiểm thì hai bên lệch: một email qua được đầu lệnh vẫn có thể bị Identity từ chối. Lúc đó lệnh vẫn thoát mã 1 và không ghi gì, nhưng thông báo mất tên khoá |
| **Lỗi này đã lọt qua mọi cổng cho tới vòng review** | Chỉ hai thứ bắt được nó: một lượt chạy lệnh trên PostgreSQL thật, hoặc một test không database dựng Identity bằng đúng đăng ký của Core. Test cũ dựng `UserManager` tay nên xanh mà không phủ gì |

### Rút lui nếu sai

1. Gỡ hai thuộc tính email khỏi `CoreBootstrapOptions`, gỡ hai dòng khỏi bảng khoá ở runbook §3.3, và gỡ đoạn truyền email trong `RunBootstrapAsync`.
2. Gỡ xong bước 1 thì lệnh lại thất bại như trước ADR này. Vì vậy rút lui **buộc** phải chọn cùng lúc phương án B hoặc C, bằng một ADR mới.

Không dữ liệu nào cần chuyển: tài khoản đã tạo giữ nguyên email. Khoá thừa trong cấu hình của các bản cài không gây lỗi, vì bộ bind cấu hình mặc định bỏ qua khoá không khớp thuộc tính nào.

### Dấu hiệu quyết định này bắt đầu sai

- Lệnh bootstrap cần thêm một trường bắt buộc nữa cho tài khoản, như họ tên thật hay số điện thoại. Lúc đó nhóm khoá phẳng nên đổi thành một khối con cho mỗi tài khoản.
- Một dự án hạ nguồn không dùng email và đề nghị bỏ bắt buộc. Câu hỏi thật lúc đó là phương án C ở cấp dự án, và cần ADR mới.
- Có người đề xuất sinh email giữ chỗ *"cho máy dev thôi"*. Đó là phương án B, với cùng cái giá.

## Việc thi công

- **`backend-expert`:** thêm hai thuộc tính bắt buộc vào `CoreBootstrapOptions`, và truyền chúng vào hai lời gọi `CreateTenantAsync` của `RunBootstrapAsync`. Thêm giá trị địa chỉ ví dụ vào `appsettings.Development.json`. Không đụng `RunResetOperatorPasswordAsync` (quyết định 4). `backend-expert` làm song song với lúc ADR này được viết; trạng thái thi công theo dõi ở bảng đầu tệp [`../database/script-runbook.md`](../database/script-runbook.md), hàng §3.3, không ở đây.
- **Test:** đường thành công của lệnh phải chạy với Identity dựng bằng `AddCoreIdentity()`, không dựng `UserManager` tay. Mỗi khoá email thiếu, và mỗi khoá email sai định dạng, là một ca riêng: thoát mã 1, không ghi dòng nào.

## Liên quan

- [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 — nơi khai tên khoá
- [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md) — service tạo đơn vị dùng chung
- [`0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md`](0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md) — cũng dựa vào `EmailIndex` duy nhất
- [`0090-ten-dang-nhap-system-la-ten-danh-rieng-chan-o-duong-tao.md`](0090-ten-dang-nhap-system-la-ten-danh-rieng-chan-o-duong-tao.md) — phép kiểm khác ở cùng đầu lệnh
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4.4 · [`../contracts/tenants.md`](../contracts/tenants.md) §2
