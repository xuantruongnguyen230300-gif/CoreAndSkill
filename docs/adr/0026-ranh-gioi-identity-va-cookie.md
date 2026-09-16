---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0026 — Kho người dùng của Identity ở `Core.Infrastructure`, phiên cookie ở `Core.Web`; `ClaimsPrincipal` dựng qua seam `ISessionPrincipalFactory`

> **Trạng thái:** Đã chấp nhận (2026-09-14) · Sửa một phần bởi ADR-0033 (2026-09-16)
>
> Phần nào còn hiệu lực, phần nào đã đổi: [`0033-luong-dang-nhap-outcome-va-claim.md`](0033-luong-dang-nhap-outcome-va-claim.md)

## Bối cảnh

[`0004-giu-aspnet-identity.md`](0004-giu-aspnet-identity.md) chốt giữ ASP.NET Core Identity và khoanh `AppUser`/`AppRole` trong `Core.Infrastructure`. Nó không chia Identity ra phần nào nằm ở đâu — và Identity không phải một khối. Một nửa là **kho người dùng**: store, `UserManager`, băm mật khẩu, khoá tài khoản, security stamp. Nửa kia là **phiên HTTP**: scheme cookie, `SignInManager`, trình kiểm security stamp định kỳ. Nửa sau đọc và ghi `HttpContext`.

Tài liệu hiện có đặt hai nửa đó vào những chỗ không chứa được chúng. Số dòng là số dòng ngày 2026-09-14.

| Tài liệu | Nói gì | Va vào |
| --- | --- | --- |
| `docs/quy-uoc/be-architecture.md:440` | Nhóm đăng ký của `Core.Infrastructure` lo *"Identity, cookie, permission"* | `docs/kien-truc-core-module.md:35` — `Core.Infrastructure` **cấm** mọi thứ phụ thuộc `HttpContext` |
| `docs/quy-uoc/be-entity-domain.md:639` | Seam `IIdentityService` ở `Core.Application` có thao tác đăng nhập, đăng xuất; hiện thực ở `Core.Infrastructure` | Đăng nhập bằng cookie là **ghi cookie vào response** — hiện thực đó buộc phải chạm `HttpContext` |
| `docs/contracts/auth.md:379` | Sau khi đổi mật khẩu gọi `SignInManager.RefreshSignInAsync` | `SignInManager<AppUser>` vừa cần `HttpContext` (nên không ở `Core.Infrastructure` được), vừa mang `AppUser` (nên không ở `Core.Web` được — luật S5, `docs/RULES.md:130`) |
| `docs/quy-uoc/be-api-controller.md:620` | Cấu hình cookie và hai sự kiện chuyển hướng | Không nói thuộc project nào |
| `docs/wiki-core/be/02-identity-auth.md:120` | Một hình dạng `IIdentityService` **khác** hình dạng ở `docs/quy-uoc/be-entity-domain.md:564` | Hai định nghĩa của cùng một seam |

Viết theo đúng tài liệu hiện có thì **không project nào đặt được thao tác đăng nhập mà không phá một luật**. Và luật canh chiều `Core.Infrastructure → HttpContext` hiện chỉ nằm ở danh sách nợ, chờ `src/` (`docs/RULES.md:260`) — lỗi này sẽ không bị máy bắt ở lần thi công đầu.

Hai ràng buộc có thật khác: kiểm security stamp **mỗi request** đã được chấp nhận làm cái giá của thu hồi phiên tức thì (`docs/wiki-core/be/02-identity-auth.md:319`); và bước đăng nhập nạp đơn vị vào một phạm vi ngữ cảnh thực thi trước khi chạm `UserManager` (`docs/quy-uoc/be-architecture.md:90`).

## Quyết định

> **`Core.Infrastructure` giữ kho người dùng của Identity. `Core.Web` giữ phiên HTTP. Hai bên nối với nhau qua seam `ISessionPrincipalFactory` khai ở `Core.Application`.**

| Project | Giữ | Không được giữ |
| --- | --- | --- |
| `Core.Infrastructure` | `AddIdentityCore<AppUser>` — không phải `AddIdentity`; store EF; `UserManager`; chính sách mật khẩu; khoá tài khoản; **phép kiểm security stamp theo `userId`**; hiện thực của `ISessionPrincipalFactory` | Scheme cookie, `SignInManager`, `IHttpContextAccessor` |
| `Core.Web` | Đăng ký scheme cookie và tuỳ chọn cookie; các sự kiện cookie — chưa xác thực, bị từ chối, kiểm principal mỗi request; lời gọi `HttpContext.SignInAsync` và `HttpContext.SignOutAsync` | `AppUser`, `UserManager`, `SignInManager<AppUser>` |
| `Core.Application` | Seam `ISessionPrincipalFactory`: dựng `ClaimsPrincipal` của một phiên từ `userId`. Tên các loại claim của phiên khai **một lần**, ở `CoreClaimTypes` | Mọi kiểu của Identity và của ASP.NET Core. `ClaimsPrincipal` là kiểu của BCL nên được phép |

Năm hệ quả nối, là một phần của quyết định:

1. **`IIdentityService` không có thao tác đăng nhập hay đăng xuất.** Nó giữ kiểm thông tin đăng nhập (mật khẩu, khoá tài khoản), đổi mật khẩu, và phép kiểm một phiên còn hiệu lực theo `userId` cùng security stamp.
2. **Principal dựng bên trong phạm vi đơn vị của bước đăng nhập.** Handler đăng nhập ở `Core.Application` mở phạm vi, kiểm thông tin, gọi `ISessionPrincipalFactory`, rồi trả principal; controller ở `Core.Web` gọi `HttpContext.SignInAsync`. Dựng principal **sau** khi phạm vi đã đóng thì truy vấn người dùng chạy khi chưa có đơn vị (`docs/wiki-core/be/17-multi-tenant.md:329`).
3. **Không dùng trình kiểm security stamp dựng sẵn của Identity** — nó đi kèm `AddIdentity` và phụ thuộc `SignInManager`. Sự kiện kiểm principal của cookie ở `Core.Web` gọi phép kiểm phiên của `IIdentityService` ở mọi request.
4. **Cấp lại cookie sau khi đổi mật khẩu** là việc `Core.Web` gọi lại `ISessionPrincipalFactory` rồi `HttpContext.SignInAsync` — thay cho `SignInManager.RefreshSignInAsync`.
5. **Phép kiểm security stamp mở phạm vi ngữ cảnh thực thi bằng `TenantId` lấy từ claim của chính principal đang kiểm.** Sự kiện kiểm principal chạy **trước** khi `HttpContext.User` được gán, nên `ITenantContext` chưa có giá trị. Phép kiểm này là **một mục** trong allowlist của luật A12 ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1). Bên trong phạm vi, truy vấn người dùng đi qua bộ lọc đơn vị như mọi truy vấn khác: claim mang đơn vị không khớp đơn vị của tài khoản thì truy vấn không tìm thấy ai, và phiên trượt.

Envelope mà hai sự kiện *chưa xác thực* / *bị từ chối* ghi ra: [`0027-errortype-unauthorized.md`](0027-errortype-unauthorized.md).

## Phương án đã cân nhắc và vì sao loại

### Phương án A — `AddIdentity` đầy đủ ở `Core.Infrastructure`

**Được:** ít code tự viết nhất. `SignInManager`, trình kiểm security stamp, khoá tài khoản khi sai mật khẩu, cấp lại cookie đều có sẵn. Đây là cách mọi hướng dẫn Identity chính thức làm, và là phương án người mới sẽ tưởng là đúng.

**Vì sao loại:** `SignInManager` lấy `HttpContext` qua `IHttpContextAccessor`, nên `Core.Infrastructure` phải kéo `Microsoft.AspNetCore.Http` vào — đúng dòng ranh giới *"hay bị vi phạm nhất"* (`docs/wiki-core/be/ly-do/kien-truc-core-module.md:47`). Hệ quả không dừng ở giấy: job nền, bộ phát outbox và lệnh vận hành cùng tham chiếu `Core.Infrastructure`, và chúng không có `HttpContext`.

### Phương án B — Chuyển toàn bộ Identity sang `Core.Web`

**Được:** `SignInManager` và `UserManager` ở cùng một chỗ; dùng được mọi thứ dựng sẵn.

**Vì sao loại:** `AppUser` rời `Core.Infrastructure` — phá luật S5 và chính quyết định của ADR-0004. `Core.Web` khi đó chứa store EF, tức tầng HTTP tự truy cập database.

### Phương án C — `Core.Web` dùng `SignInManager<AppUser>`, `Core.Infrastructure` giữ store

**Được:** chia gần giống quyết định này mà vẫn dùng `SignInManager` dựng sẵn.

**Vì sao loại:** `SignInManager<AppUser>` mang `AppUser` trong chính tên kiểu. `Core.Web` nhắc tới `AppUser` là phá luật S5, dù không chạm store.

### Phương án D — Không có seam; `Core.Web` tự dựng `ClaimsPrincipal` từ DTO người dùng

**Được:** bớt một interface.

**Vì sao loại:** `Core.Web` phải biết security stamp — một giá trị nội bộ của Identity — nên DTO tra cứu người dùng phải mang nó ra `Core.Application`. Và tập claim được **dựng** ở `Core.Web` trong khi được **kiểm** ở `Core.Infrastructure`: hai chỗ phải đồng ý về cùng một tập giá trị mà không có gì ép chúng đồng ý.

### Phương án E — Bỏ cookie, dùng token để đăng nhập không cần `HttpContext`

Không xét lại. Cookie đã chốt vì thu hồi phiên tức thì ([`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §1.3, [`../contracts/auth.md`](../contracts/auth.md) §1). Ghi ra để không ai đề xuất nó như lối tắt cho một vấn đề ranh giới project.

### Phương án F — Kiểm security stamp bằng truy vấn bỏ đúng bộ lọc đơn vị, rồi so `TenantId` của bản ghi với claim

**Được:** không mở phạm vi ngữ cảnh; một truy vấn theo `userId`.

**Vì sao loại:** thêm một mục vào allowlist của luật M5, chạy ở **mọi** request đã đăng nhập. Phép so `TenantId` là code viết tay: thiếu nó thì phiên của đơn vị này khớp với bản ghi của đơn vị khác và không gì báo — hỏng im lặng theo chiều cách ly đơn vị, việc bộ lọc làm sẵn nếu không bị bỏ. Bước đăng nhập đã cố ý không đi cách này ([`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §11.1).

### Phương án G — Một câu SQL thô lọc theo `userId` và `TenantId` lấy từ claim

**Được:** không chạm allowlist nào; lọc đơn vị viết ngay trong câu lệnh, theo luật M6.

**Vì sao loại:** security stamp là giá trị nội bộ của Identity, đọc qua store EF. Câu SQL thô phải biết tên bảng và tên cột của Identity, và lệch khi nâng phiên bản — đúng loại soát tay mà mục *Tiêu cực* đã phải nhận cho phần tự viết. Nó cũng là đường truy vấn duy nhất của Identity không đi qua EF.

## Hệ quả

### Tích cực

- Thao tác đăng nhập có một chỗ đặt hợp lệ, và bảng ranh giới project giữ nguyên không ngoại lệ.
- `Core.Infrastructure` dùng được từ job nền, bộ phát outbox, lệnh vận hành mà không kéo theo phụ thuộc web.
- Tập claim của phiên được dựng ở **một** chỗ, tên claim khai ở **một** chỗ; lớp đọc danh tính ở `Core.Web` đọc đúng tên mà factory ghi.
- Handler đăng nhập test được không cần HTTP: nó trả một `ClaimsPrincipal`, thứ khẳng định được bằng giá trị.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Tự viết lại ba thứ Identity có sẵn** | Kiểm security stamp mỗi request; cấp lại cookie sau đổi mật khẩu; và **đếm lần sai, khoá tài khoản khi đăng nhập** — `SignInManager` làm việc cuối trong một lời gọi, không có nó thì code phải tự gọi đúng thứ tự các thao tác tăng và xoá bộ đếm của `UserManager`. Đây là code bảo mật tự viết, đúng loại ADR-0004 muốn tránh. Sai thứ tự thì hoặc không bao giờ khoá, hoặc khoá cả người nhập đúng |
| **Trái thói quen ngành** | Mọi hướng dẫn Identity dùng `AddIdentity` và `SignInManager`. Người mới thấy một bản tự viết của thứ framework có sẵn, và có động cơ rất tự nhiên để "sửa lại cho chuẩn" — tức phương án A |
| **Thêm một seam và một bộ hằng số claim vào bề mặt Core** | Hai project cùng phụ thuộc tên claim. Đổi một tên claim là mọi phiên đang mở trượt ở request kế tiếp |
| **Ranh giới quan trọng nhất chưa có cổng** | Luật canh `Core.Infrastructure` không tham chiếu `Microsoft.AspNetCore.Http` vẫn ở danh sách nợ. Tới khi ArchTest đó tồn tại, chỉ review canh quyết định này |
| **Nâng phiên bản Identity phải soát phần tự viết** | Thay đổi về khoá tài khoản hay security stamp trong framework không tự đi vào code của ta |
| **Allowlist A12 có một mục chạy ở MỌI request đã đăng nhập** | Các mục khác chạy ở job nền, lệnh vận hành, lúc đăng nhập — hiếm, và mỗi lần một chỗ. Mục này nằm trên đường nóng: phạm vi đóng không đúng thì đơn vị của phép kiểm rò sang phần còn lại của request, ở mọi request. Hàng rào là cơ chế khôi phục phạm vi trước đó ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1, và chưa có test nào canh nó |
| **Đơn vị của phép kiểm tin đúng bằng mức tin cookie** | `TenantId` dùng để mở phạm vi đến từ cookie của chính request. Nó chỉ an toàn chừng nào cookie không giả được — tức chừng nào key ring bảo vệ dữ liệu chưa lộ |

## Điều kiện lật quyết định

1. **Core chuyển sang nhà cung cấp danh tính bên ngoài** (đăng nhập tập trung qua OIDC). Phần kho người dùng ở `Core.Infrastructure` bị thay; phần phiên ở `Core.Web` có thể giữ. Viết ADR mới, trích ADR này.
2. **ASP.NET Core Identity tách phần đăng nhập khỏi `HttpContext`** thành một thành phần dùng được ở tầng không có web. Lý do tự viết lại ba thứ ở mục Tiêu cực khi đó biến mất.

### Dấu hiệu quyết định này bắt đầu sai

- `IHttpContextAccessor` hoặc `Microsoft.AspNetCore.Http` xuất hiện trong `Core.Infrastructure`.
- `SignInManager` xuất hiện ở bất kỳ đâu.
- Một chuỗi tên claim viết tay ngoài `CoreClaimTypes`, hoặc một chỗ thứ hai dựng `ClaimsPrincipal`.
- Test khoá tài khoản sau nhiều lần sai phải được "nới" mới xanh.
- Phép kiểm phiên gọi `IgnoreQueryFilters` "cho chắc", hoặc mở phạm vi bằng một giá trị không lấy từ claim của principal đang kiểm.

## Liên quan

- [`0004-giu-aspnet-identity.md`](0004-giu-aspnet-identity.md) — quyết định mà ADR này chia ra theo project
- [`0027-errortype-unauthorized.md`](0027-errortype-unauthorized.md) — envelope của sự kiện cookie
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2 — bảng ranh giới project
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1 — danh sách seam, phạm vi ngữ cảnh thực thi
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7 — cookie, CORS, antiforgery
- [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §5, §6 — thu hồi phiên, đơn vị trong luồng xác thực
- [`../RULES.md`](../RULES.md) — luật S5, A12, M5, M6, và mục nợ B4
