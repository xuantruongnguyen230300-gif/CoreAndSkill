---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0033 — Handler đăng nhập trả `LoginOutcome`; `Core.Web` dựng principal từ kết quả đó; permission không vào cookie

> **Trạng thái:** Đã chấp nhận (2026-09-16) · Sửa một phần [`0026-ranh-gioi-identity-va-cookie.md`](0026-ranh-gioi-identity-va-cookie.md) · Bổ sung [`0006-pipeline-behavior.md`](0006-pipeline-behavior.md)
>
> **Bổ sung 0006.** Hai behavior và thứ tự của chúng giữ nguyên; ADR này thêm marker `INoTransaction` — một lối thoát có tên khỏi `TransactionBehavior`, dùng cho đúng `LoginCommand`.

## Bối cảnh

Ngày 2026-09-16, trước khi có `src/`, luồng đăng nhập được đi thử trên giấy theo đúng
[`0026`](0026-ranh-gioi-identity-va-cookie.md) và các file quy ước. Ba chỗ không khép được:

1. **Handler không trả được principal cho controller dùng.** ADR-0026 để handler ở `Core.Application`
   gọi `ISessionPrincipalFactory` bằng `userId` rồi trả `ClaimsPrincipal`. Nhưng response đăng nhập cần
   `SessionDto` — thứ handler vừa tra xong — nên controller phải gọi lại query của `me` để có nó: hai lần
   đọc cùng dữ liệu, và là chỗ thứ hai có thể trả khác đi. Factory nhận `userId` phải truy vấn lại người
   dùng, và security stamp mà principal phải mang thì `IIdentityService.CheckCredentialsAsync` (khi đó trả
   `Guid`) không đưa ra — không có đường nào từ bước kiểm mật khẩu tới claim ngoài một lần đọc nữa.
2. **Bộ đếm khoá bị rollback.** `LoginCommand` là command nên đi qua `TransactionBehavior`
   ([`0006`](0006-pipeline-behavior.md)); sai mật khẩu trả `Result` thất bại, behavior rollback — kể cả
   lần tăng bộ đếm và mốc khoá mà `UserManager.AccessFailedAsync` vừa ghi. Khoá tài khoản sau N lần sai
   **không bao giờ xảy ra**, đúng dấu hiệu *"test khoá tài khoản phải được nới mới xanh"* mà 0026 tự liệt kê.
3. **Tập claim chưa chốt.** Chưa có chỗ nào nói permission có vào cookie hay không, trong khi
   [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §5 đòi quyền đổi phải có
   hiệu lực ở request kế tiếp.

## Quyết định

> **Handler đăng nhập trả `Result<LoginOutcome>`. Controller ở `Core.Web` gọi
> `ISessionPrincipalFactory.Create(outcome, issuedAt)` rồi `HttpContext.SignInAsync`. Factory không truy vấn.**

Bốn phần đi kèm:

1. **Nguồn stamp là kết quả kiểm mật khẩu.** `IIdentityService.CheckCredentialsAsync` và `ChangePasswordAsync`
   trả `CredentialCheck`; `LoginOutcome` lấy stamp từ đó, không tra thêm.
2. **Tên claim khai một lần ở `CoreClaimTypes`.** Cookie mang danh tính, đơn vị, stamp, thời điểm phát, cờ
   buộc đổi mật khẩu và cờ vận hành hệ thống (`[RequireSystemOperator]` đọc claim, không truy DB). **Permission không vào cookie** — kiểm mỗi request qua `IPermissionChecker`.
3. **`LoginCommand` cài `INoTransaction`** — ca duy nhất được thoát `TransactionBehavior`.
4. **`issuedAt` là tham số của factory.** Đăng nhập truyền `now`; cấp lại cookie sau đổi mật khẩu truyền giá
   trị đọc từ principal cũ, nên trần tuyệt đối của phiên không reset.

Chữ ký và mã: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4 ·
[`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §7.1 ·
[`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §5.3. ADR này không chép chúng.

### Phần nào của ADR-0026 còn hiệu lực

| Trong 0026 | Nay |
| --- | --- |
| Quyết định trung tâm và bảng ranh giới ba project | **Còn nguyên** |
| Hệ quả 1, 3, 4, 5 (seam Identity không phát phiên; tự kiểm stamp; cấp lại cookie qua factory; mở phạm vi bằng `TenantId` từ claim) | **Còn nguyên** — hệ quả 4 nay truyền thêm `issuedAt` của phiên cũ |
| Phương án đã loại A–G | **Còn nguyên** |
| Dòng `Core.Application` của bảng Quyết định: factory dựng principal *từ `userId`* | Hết hiệu lực — đầu vào là `LoginOutcome` |
| Hệ quả 2: handler gọi factory trong phạm vi đơn vị rồi trả principal | Hết hiệu lực — handler trả `LoginOutcome`; factory không truy vấn nên không cần phạm vi |
| Hệ quả tích cực *"handler trả một `ClaimsPrincipal`"* | Hết hiệu lực — handler trả `LoginOutcome`, vẫn khẳng định được bằng giá trị |

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ nguyên 0026: handler trả `ClaimsPrincipal`

**Được:** không sửa ADR nào. **Mất:** controller gọi lại `me`; factory truy vấn lại; stamp phải có thêm một
đường tra. **Vì sao loại:** ba lần đọc cho một lần đăng nhập, và kiểu trả của handler là một kiểu bảo mật
của BCL thay vì DTO — test handler phải mổ claim thay vì so giá trị.

### Phương án B — Đổi `LoginCommand` thành `IQuery` để thoát transaction

**Được:** không cần marker mới; behavior không đổi. **Mất:** một thao tác ghi (tăng bộ đếm, đặt mốc khoá,
xoá bộ đếm) mang nhãn query. **Vì sao loại:** `TransactionBehavior` loại query bằng ràng buộc kiểu — tức nhãn
là thứ duy nhất nói một request có ghi hay không. Một query có tác dụng phụ là tiền lệ mà không cổng nào bắt
được lần thứ hai; marker tường minh thì đọc `LoginCommand` là thấy ngoại lệ.

### Phương án C — Chụp permission vào claim lúc đăng nhập

**Được:** không đọc quyền ở mỗi request. **Mất:** quyền cũ sống tới khi đăng nhập lại. **Vì sao loại:** trái
yêu cầu *"có hiệu lực ở request kế tiếp"* ở `02-identity-auth.md` §5; muốn giữ yêu cầu đó thì phải đổi stamp
mỗi khi vai trò đổi — đá mọi phiên của mọi người mang vai trò đó chỉ để làm mới một danh sách.

### Phương án D — Thêm `GetSecurityStampAsync(userId)` vào seam thay vì `CredentialCheck`

**Được:** chữ ký `CheckCredentialsAsync` giữ `Result<Guid>`. **Mất:** một thao tác nữa trên seam, gọi ngay
sau thao tác vừa đọc đúng bản ghi đó. **Vì sao loại:** stamp có thể đổi giữa hai lời gọi (khoá từ tab khác);
trả cùng lần đọc thì claim và DB đồng ý tại một thời điểm duy nhất.

### Phương án E — Factory tự lấy giờ hiện tại làm `IssuedAt`

**Được:** chữ ký ngắn hơn một tham số. **Vì sao loại:** cấp lại cookie sau đổi mật khẩu là một lần gọi factory,
và mỗi lần gọi là một lần reset trần phiên — đổi mật khẩu định kỳ thành cách giữ phiên vô hạn.

## Hệ quả

### Tích cực

- Một lần đăng nhập đọc dữ liệu người dùng **một** lần; response và cookie dựng từ cùng một `LoginOutcome`.
- Handler và factory test được bằng giá trị thuần, không cần HTTP, không cần DB.
- Khoá tài khoản sau N lần sai hoạt động — cái giá "tự đếm lần sai" mà 0026 nhận nay có đường đi đúng.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **`INoTransaction` là một lỗ có tên trong `TransactionBehavior`** | Lệnh thứ hai cài marker này sẽ không có gì chặn ngoài review; mọi tác dụng phụ của nó phải tự lưu |
| **Quyền đọc DB ở mọi request** | Là cái giá đã nhận ở `02-identity-auth.md` §5; ADR này chỉ khoá không cho lối tắt |
| **Controller đăng nhập không còn "chỉ `Send` rồi `HandleResult`"** | Nó gọi factory và `SignInAsync` — ngoại lệ có tên ở `be-api-controller.md` §3; ngoại lệ thứ hai cùng dạng là một quyết định mới |
| **Đọc `IssuedAt` từ principal cũ là code viết tay ở controller** | Parse sai hoặc claim thiếu thì cấp lại cookie thất bại đúng lúc vừa đổi mật khẩu xong |

## Liên quan

- [`0026-ranh-gioi-identity-va-cookie.md`](0026-ranh-gioi-identity-va-cookie.md) — ADR bị sửa một phần
- [`0006-pipeline-behavior.md`](0006-pipeline-behavior.md) — ADR được bổ sung: behavior mà `INoTransaction` tạo ngoại lệ
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.4 — seam, claim, luồng đăng nhập và cấp lại cookie
- [`../quy-uoc/be-entity-domain.md`](../quy-uoc/be-entity-domain.md) §7.1 — `IIdentityService`, `CredentialCheck`
- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §5.3 — `INoTransaction`
- [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §4.2, §5 — thứ tự kiểm khi đăng nhập, thu hồi phiên
