---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0089 — Vi phạm unique `23505` khi ghi đồng thời được dịch ngay tại chỗ ghi, theo tên ràng buộc, thành đúng lỗi mà chỗ đó trả khi tự bắt được bản trùng; với tài khoản, chỗ đó là kho người dùng của Identity

> **Trạng thái:** Đã chấp nhận (2026-09-24) · Bổ sung bởi ADR-0092 (2026-09-25)
>
> Bổ sung bởi [`0092-dong-interceptor-them-sau-luot-luu-hong-ghi-no-chua-don.md`](0092-dong-interceptor-them-sau-luot-luu-hong-ghi-no-chua-don.md).

## Bối cảnh

`backend-expert` báo một lỗi: hai lượt tạo, hoặc hai lượt sửa, người dùng cùng tên đăng nhập hay cùng email chạy đồng thời thì lượt sau nhận **500**. Người dùng chốt ngày 2026-09-24: **sửa**.

Đường ghi tài khoản có ba lớp kiểm trùng, nối tiếp nhau:

| Lớp | Ở đâu | Bắt được thì trả |
| --- | --- | --- |
| 1 | Phép kiểm trước — handler (`CreateUserCommandHandler`) hoặc service (`UserAdminService.UpdateAsync`) | 409, mã gốc `USERNAME_DUPLICATED` / `EMAIL_DUPLICATED` |
| 2 | Bộ kiểm của Identity, bên trong `UserManager` | `IdentityResult` mang `DuplicateUserName` / `DuplicateEmail` ⇒ `IdentityErrorMapper` ⇒ 422 `CREATE_FAILED` / `UPDATE_FAILED`, kèm `fieldErrors` có `messageParams` |
| 3 | Index duy nhất `UserNameIndex`, `EmailIndex` trên `core.app_user` | `DbUpdateException` bọc `PostgresException` `23505` |

Ở mức cô lập READ COMMITTED, hai lượt cùng qua được lớp 1 và lớp 2: không lượt nào thấy dòng chưa commit của lượt kia. Lượt sau dừng ở lớp 3. Tầng lưu trữ người dùng mặc định của Identity chỉ bắt `DbUpdateConcurrencyException` — đổi nó thành `ConcurrencyFailure`, xem `IdentityConcurrency.cs`. Mọi `DbUpdateException` khác đi xuyên qua `UserManager` và ra thành 500.

Đọc code ngày 2026-09-24 thấy thêm:

- **Lớp 3 là chỗ duy nhất có mặt ở mọi đường ghi tài khoản.** `UserManager.CreateAsync` / `UpdateAsync` được gọi ở `UserAdminService`, `TenantProvisioningService` (tạo đơn vị và tạo quản trị bổ sung), `UserProfileService` và `IdentityService`. Mỗi chỗ có một bộ ánh xạ lỗi riêng, theo card của endpoint đó. Ví dụ: [`../contracts/tenants.md`](../contracts/tenants.md) §6 gộp mọi ca thành một mã để chống dò tên đăng nhập.
- **Đã có tiền lệ cùng khuôn:** `IdentityConcurrency` chuyển một lỗi của tầng lưu trữ thành `IdentityResult` ngay tại ranh giới Identity, để mọi chỗ gọi xử lý nó như một lỗi Identity bình thường.
- **`UserManager.CreateAsync` ghi luôn cả dòng đơn vị** mà `TenantProvisioningService.CreateTenantAsync` đã `Add` trước đó. Vì vậy hai lượt tạo đơn vị trùng mã chạy đồng thời cũng vỡ bên trong `UserManager`, nhưng ở ràng buộc khác: `uq_tenant_code`.

## Quyết định

Người dùng chốt **sửa** ngày 2026-09-24. Kiến trúc sư chốt cách sửa:

1. **Nguyên tắc:** một vi phạm `23505` trên ràng buộc mà hợp đồng có mã trùng riêng được dịch **tại chỗ thực hiện lệnh ghi**. Kết quả phải là **đúng lỗi mà chỗ đó trả khi chính nó bắt được bản trùng**: cùng mã, cùng `fieldErrors`, cùng `messageParams`. Vi phạm `23505` trên ràng buộc khác vẫn là 500, vì đó là lỗi lập trình.
2. **Nhận ra bằng tên ràng buộc**, không bằng câu thông báo: `PostgresException` có `SqlState` bằng `PostgresErrorCodes.UniqueViolation` và `ConstraintName` nằm trong một tập tên khai ở **một** chỗ. Mỗi tên trong tập được một test đối chiếu với tên index trong model EF, để đổi tên index mà quên sửa tập thì test đỏ.
3. **Với tài khoản, chỗ ghi là kho người dùng của Identity.** Core thay kho mặc định bằng một lớp con. Lớp con bắt `23505` trên `UserNameIndex` và `EmailIndex`, rồi trả `IdentityResult.Failed` mang **đúng** lỗi mà bộ kiểm Identity dựng ra — `ErrorDescriber.DuplicateUserName(…)` / `DuplicateEmail(…)`. Từ đó `IdentityErrorMapper` và bộ ánh xạ riêng của từng endpoint chạy như cũ, không sửa gì.
4. **Sau khi dịch, không chạy thêm lệnh SQL nào trong transaction đó.** PostgreSQL đã đánh dấu transaction là hỏng. Chỗ gọi trả thất bại ngay, `TransactionBehavior` rollback. Lớp con gỡ bản ghi vừa hỏng khỏi bộ theo dõi, để không lần lưu nào sau đó thử ghi lại nó.
5. **Phạm vi áp ngay:** hai index của `core.app_user` — đúng phạm vi người dùng đã chốt. Nó tự phủ mọi đường ghi tài khoản qua `UserManager`, gồm cả tạo quản trị bổ sung ở `tenants.md` §6.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Bắt ở bộ xử lý ngoại lệ chung (`IExceptionHandler`)

**Được:** một chỗ cho mọi đường ghi. Không đụng Identity.

**Mất:**

- Bộ xử lý chung không biết giá trị bị từ chối. `messageParams` sẽ thiếu, và FE hiện nguyên chữ `{{UserName}}` — đúng lỗi đã gặp ở hộp *Sửa người dùng* hôm nay. Muốn có giá trị thì phải bóc `Detail` của `PostgresException`, tức là đọc câu thông báo. Giá trị lấy được cũng đã bị chuẩn hoá thành chữ hoa, không phải giá trị người dùng gõ.
- Nó không biết endpoint nào đang chạy, nên không biết khoá `fieldErrors` riêng của endpoint (`UserName` hay `AdminUserName`), và không biết luật gộp mã của `tenants.md` §6. Nó sẽ làm lộ tên đăng nhập đúng ở chỗ card đang che.

**Vì sao loại:** không trả được lỗi "giống hệt ca trùng thường", mà đó là yêu cầu gốc.

### Phương án B — Bắt ở `UnitOfWork`

**Được:** gần tầng lưu trữ, một chỗ cho các lệnh ghi qua `DbContext`.

**Mất:** kho Identity tự lưu ngay bên trong `UserManager`, trước khi `UnitOfWork` commit. Vì vậy `23505` của tài khoản không bao giờ tới `UnitOfWork`.

**Vì sao loại:** không bắt được chính ca đang cần sửa.

### Phương án C — Bọc từng lời gọi `UserManager` ở từng service

**Được:** tường minh; mỗi chỗ gọi thấy rõ nhánh của mình.

**Mất:** có ít nhất năm chỗ gọi, và mỗi chỗ lặp lại cùng một khối bắt lỗi. `IdentityConcurrency.cs` ghi lại đúng hậu quả của khuôn này với lỗi đồng thời: khối kiểm bị lặp ở ba nơi và bị sửa sai hai lần. Chỗ gọi thứ sáu viết sau này sẽ quên.

**Vì sao loại:** cùng bài toán, đã có lời giải ở ranh giới Identity; lặp lại khuôn cũ là lặp lại lỗi cũ.

### Phương án D — Trả 409 mã gốc, giống lớp 1, thay vì hình dạng của lớp 2

**Được:** trùng thật hay trùng do đồng thời đều ra 409.

**Mất:** lớp 2 hôm nay đã trả 422 kèm `fieldErrors`. Muốn thống nhất về 409 thì phải đổi cả lớp 2, tức là đổi hợp đồng và đổi `IdentityErrorMapper`. Với `tenants.md` §6, trả mã gốc `USERNAME_DUPLICATED` còn phá luật gộp mã chống dò.

**Vì sao loại:** lớp 3 chỉ bắt được đúng những ca mà lớp 2 để lọt vì đồng thời. Trả đúng hình dạng của lớp 2 là thay đổi nhỏ nhất, và không đổi hợp đồng nào. FE hiện hai hình dạng giống nhau: mã gốc trùng vào ô qua bảng *mã gốc → control*, `fieldErrors` vào ô qua `applyFieldErrors` ([`0086-ma-goc-vao-o-qua-bang-cua-applyformfailure.md`](0086-ma-goc-vao-o-qua-bang-cua-applyformfailure.md)).

## Hệ quả

### Tích cực

- Không còn 500 cho ca trùng do ghi đồng thời trên tài khoản, ở mọi đường ghi qua `UserManager`, kể cả các đường viết sau này.
- Không bộ ánh xạ lỗi nào phải sửa, và không hợp đồng nào đổi hình dạng.
- Đổi tên index làm một test đỏ, thay vì âm thầm quay lại trả 500.

### Tiêu cực

| Cái giá | Nghĩa là |
| --- | --- |
| **Core thay một thành phần mặc định của Identity** | Lớp con kho người dùng phải đi theo mỗi lần nâng phiên bản Identity. Nâng bản mà lớp cha đổi cách bọc ngoại lệ thì lớp con có thể mất tác dụng một cách im lặng. Chỉ test trên PostgreSQL thật mới bắt được việc này |
| **Cùng một ca "trùng" có hai hình dạng trên dây** | 409 mã gốc khi phép kiểm trước bắt được; 422 kèm `fieldErrors` khi Identity hoặc index bắt được. Hai hình dạng này có từ trước; quyết định này giữ nguyên chúng, không gộp |
| **Tên index thành một phần của hợp đồng lỗi** | `UserNameIndex`, `EmailIndex` vốn đã không được đổi tên ([`../database/schema-core.md`](../database/schema-core.md) §4.0). Nay đổi tên còn đổi cả hành vi lỗi — có test canh |
| **Chưa chứng minh được bằng máy nếu không có Docker** | Ca đồng thời chỉ tái hiện được trên PostgreSQL thật — nợ **E18** ở [`../DEBT.md`](../DEBT.md) |
| **Các ràng buộc cùng lớp khác vẫn trả 500** | `uq_tenant_code` và `RoleNameIndex` có cùng dạng lỗi nhưng nằm ngoài phạm vi đã chốt — xem mục cuối |

### Rút lui nếu sai

1. Gỡ đăng ký lớp con, quay về kho mặc định.
2. Gỡ tập tên ràng buộc cùng test của nó.

Hành vi trở lại đúng như hôm nay: ca đồng thời ra 500. Không có dữ liệu nào cần chuyển, vì thay đổi chỉ nằm trên đường trả lỗi.

### Dấu hiệu quyết định này bắt đầu sai

- Tập tên ràng buộc bắt đầu mang logic theo endpoint. Lúc đó việc dịch đã bị kéo ra khỏi chỗ ghi.
- Một bản nâng cấp Identity đổi cách kho người dùng lưu dữ liệu — ví dụ gom lệnh lưu ra ngoài `UserManager`.
- Có đề xuất bóc `Detail` của `PostgresException` để lấy giá trị.

## Câu hỏi còn mở — mở rộng sang hai ràng buộc cùng lớp

Hai ràng buộc sau có mã trùng riêng trong hợp đồng và có phép kiểm trước chạy ngoài khoá, nên cùng lỗi 500 khi ghi đồng thời:

| Ràng buộc | Đường ghi | Lỗi khi chính đường đó bắt được bản trùng |
| --- | --- | --- |
| `uq_tenant_code` | `TenantProvisioningService.CreateTenantAsync` — dòng đơn vị được `UserManager.CreateAsync` ghi cùng tài khoản đầu tiên | 409 `CORE.TENANT.CODE_DUPLICATE` kèm `Code` ([`../contracts/tenants.md`](../contracts/tenants.md) §2) |
| `RoleNameIndex` | `RoleManager` — kho vai trò của Identity | 409 `CORE.ROLE.NAME_DUPLICATE` ([`../contracts/roles.md`](../contracts/roles.md) §2, §3) |

Áp nguyên tắc 1–2 cho hai ràng buộc này là mở rộng phạm vi so với điều người dùng đã chốt. Việc này chờ người dùng quyết.

## Liên quan

- [`0086-ma-goc-vao-o-qua-bang-cua-applyformfailure.md`](0086-ma-goc-vao-o-qua-bang-cua-applyformfailure.md) — FE hiện hai hình dạng lỗi trùng như nhau
- [`0082-gan-vai-tro-dung-token-cua-tai-khoan.md`](0082-gan-vai-tro-dung-token-cua-tai-khoan.md) — lỗi ghi đồng thời khác trên cùng bảng
- [`../contracts/users.md`](../contracts/users.md) §5, §6 · [`../contracts/tenants.md`](../contracts/tenants.md) §2, §6
