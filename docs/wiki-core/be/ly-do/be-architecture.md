---
kind: tham-chieu
scope: core
verified: chua-doi-chieu
---

# Lý do, bẫy, ví dụ mở rộng của `be-architecture.md`

> File này giữ **lý do, bẫy, ví dụ dài** của [`docs/quy-uoc/be-architecture.md`](../../../quy-uoc/be-architecture.md); luật ở đó. Số mục dưới đây trùng số mục của file luật — mục không có gì dời thì ghi "—".

---

## 1. Project layout & chiều phụ thuộc

Đây là mục quan trọng nhất của file luật.

### 1.1 Project nào chứa gì — và các seam của `Core.Application`

Mọi câu hỏi *"file này bỏ vào đâu"* trả lời bằng bảng project, và chỉ bằng bảng đó. Bảng project không
chép lại vào file luật vì hai bản sẽ lệch — khuôn [`../../../OWNERSHIP.md`](../../../OWNERSHIP.md) mô tả.
Danh sách seam phải đọc được trong một lần nhìn: đó là toàn bộ bề mặt DIP của Core.

Vì sao từng seam là interface, không phải lớp cụ thể — phần bổ sung cho cột *Vai* ở bảng seam:

| Seam | Vì sao |
| --- | --- |
| `IUnitOfWork` | Handler không được biết `DbContext` tồn tại |
| `ILoginAttemptLimiter` | Bộ đếm lẫn bộ chuẩn hoá tên đăng nhập của Identity đều là hạ tầng — không thứ nào được lộ ra `Core.Application` |
| `ITenantProvisioningService` | Hai đường vào, một đường ghi: đơn vị dựng từ dòng lệnh và đơn vị dựng từ màn hình không thể khác nhau |
| `ICacheStore` | Đổi bản cài không sửa chỗ gọi |

**Danh tính và đơn vị của request** — vì sao ba luật:

| Luật (tóm tắt) | Vì sao |
| --- | --- |
| `TenantId` chỉ trên `ITenantContext` | Hai seam cùng trả đơn vị là hai chỗ có thể trả hai giá trị khác nhau — và ở bước đăng nhập chúng **đã** khác nhau: có đơn vị, chưa có người |
| Hai seam, không gộp | Luồng đăng nhập tra đơn vị theo mã đơn vị rồi nạp vào ngữ cảnh, và chỉ sau đó mới gọi `UserManager`. Gộp hai seam làm bước đó không viết được |
| Nullable, không `Guid.Empty` | `Guid.Empty` đi lọt mọi chữ ký nhận `Guid`, vào dòng ghi và vào log mà không ai hỏi. `null` buộc chỗ đọc xử lý ca "chưa có" tường minh |

**Danh tính và đơn vị khi không có request** — những chỗ không có `HttpContext` để đọc, hoặc có mà chưa
mang đơn vị: job nền, bộ phát outbox, service tạo đơn vị cùng runner lệnh bootstrap, bước đăng nhập
(trước khi có phiếu thì chưa có claim đơn vị nào), và phép kiểm security stamp của cookie (chạy trước khi
`HttpContext.User` được gán). Hiện thực giữ giá trị trong `AsyncLocal` — không chạm `HttpContext` nên
được phép ở `Core.Infrastructure`; phạm vi lồng nhau được. Vì sao từng luật:

| Luật (tóm tắt) | Vì sao |
| --- | --- |
| Allowlist A12 đóng | Mở phạm vi ở chỗ khác là tự chọn đơn vị — đúng lỗ mà luật M2 chặn ở đường vào HTTP. Phép kiểm security stamp chạy **trước** khi `HttpContext.User` được gán nên `ITenantContext` chưa có giá trị; đơn vị nó dùng lấy từ claim của phiếu — đúng nguồn M2 cho phép |
| `Current` trên seam; hai lớp ở `Core.Web` đọc nó trước `HttpContext` | Giá trị nằm trong `AsyncLocal` của `Core.Infrastructure`; không có `Current` thì `Core.Web` phải biết hiện thực, hoặc giữ một bản đọc thứ hai. Thứ tự *phạm vi trước, `HttpContext` sau* là thứ làm bước đăng nhập và phép kiểm cookie thấy đúng đơn vị khi `HttpContext.User` chưa có |
| `Dispose` khôi phục phạm vi trước đó | Service tạo đơn vị chạy trong một phạm vi rồi mở phạm vi của đơn vị mới. Đóng mà xoá về rỗng thì phần việc còn lại của phạm vi ngoài chạy không có đơn vị — interceptor từ chối lưu (luật M8) ở một chỗ cách xa nguyên nhân |
| Đơn vị đi theo việc nền bằng dữ liệu | Nằm ở chỗ gọi thì mỗi nơi enqueue phải nhớ truyền, và nơi nào quên sẽ lặng lẽ ghi dữ liệu không danh tính |
| Không chụp vai trò hay quyền | Job chạy sau vài phút mà quyết định theo ảnh chụp thì quyền vừa bị thu hồi vẫn còn hiệu lực |
| Bộ phát outbox mỗi dòng một phạm vi | Dòng biết nó thuộc đơn vị nào và do ai kích hoạt; tiến trình phát thì không. Một phạm vi mở một lần cho cả lô gán đơn vị của dòng đầu cho mọi dòng sau |
| Không có người là hợp lệ, không có đơn vị thì không | Job hệ thống không do ai kích hoạt; nhưng dữ liệu đơn vị mà không có đơn vị là dữ liệu vô hình với mọi người |

**Danh mục khoá quyền do module cấp** — cơ chế Core giữ gồm kiểm deny-by-default, ma trận, và migration
idempotent ghi danh mục. Hai record, một cho mỗi bảng đích, cùng khuôn record phẳng tham chiếu nhau bằng
**khoá** như `ITenantSeedSource`. Ví dụ giá trị: `Key` là `"core.user"`, `"skill.course"`; `Action` là
`"read"`, `"write"`, `"lock"`; `DisplayOrder` của tài nguyên là thứ tự hàng trên ma trận, của khoá là thứ
tự trong hàng. Vì sao từng luật:

| Luật (tóm tắt) | Vì sao |
| --- | --- |
| Mỗi module một nguồn, Core gộp | Core không được biết module nào tồn tại — luật A3 |
| Kiểm lúc khởi động | Kiểm lúc dùng thì host khai sai vẫn chạy bình thường, rồi trả 500 đúng lúc ai đó mở màn phân quyền. Cùng cơ chế với kiểm cấu hình lúc khởi động ([`be-architecture.md`](../../../quy-uoc/be-architecture.md) §4) nên hai loại cấu hình sai hỏng cùng một kiểu |
| Tài nguyên và khoá cùng nguồn | FK `permission.resource_key → permission_resource.key` không phân biệt chủ, nhưng migration của module chỉ được ghi dòng của mình ([`../../../database/migration-policy.md`](../../../database/migration-policy.md) §1). Khoá trỏ sang tài nguyên của Core là một dòng module không ghi được mà cũng không kiểm được |
| Dòng vào bảng bằng migration idempotent | Lúc khởi động chỉ **kiểm**. Thứ ghi dữ liệu là migration: chạy một lần, có người nhìn, có câu nghiệm thu |
| Tiến trình không ghi danh mục | Hướng ngược lại — tiến trình tự đồng bộ danh mục vào DB — là hướng đã khoá `K52`; lý do ở [`../13-core-data-migration.md`](../13-core-data-migration.md) §8 |
| `NameKey` là khoá dịch | BE sở hữu mã, không sở hữu câu chữ |
| Không hiện thực mặc định | Hỏng ồn ào lúc khởi động thay vì một màn phân quyền trống |
| Ánh xạ vai trò → quyền không qua seam | Ánh xạ là dữ liệu của **đơn vị**, không phải hợp đồng code ↔ dữ liệu |

**Nguồn seed cho đơn vị mới** — cùng khuôn `IPermissionCatalogSource`; các mục tham chiếu nhau bằng mã vì
id do service tạo đơn vị sinh lúc ghi. Vì sao từng luật:

| Luật (tóm tắt) | Vì sao |
| --- | --- |
| Một nguồn trả ba thứ | Đó là thứ một đơn vị cần để người quản trị đầu tiên làm được việc mà không phải dựng tay |
| Nhiều đăng ký được gộp; Core tự đăng ký nguồn menu | Core không biết module nào tồn tại (luật A3), và menu Core không thuộc module nào |
| Kiểm lúc khởi động | Kiểm lúc tạo đơn vị thì lỗi lộ ra đúng lúc người vận hành đang tạo đơn vị cho một cơ quan |
| Không nguồn nào khai vai trò ⇒ chạy bằng `has_permission_bypass` | Người quản trị dựng vai trò từ màn hình |

### 1.2 Sơ đồ chiều phụ thuộc

Sơ đồ là DIP viết thành đồ thị tham chiếu — không phải quy ước bằng lời. Đọc ngược mũi tên: `Application`
biết `Domain`; `Domain` **không** biết ai. Không có cạnh `Modules.<X>.Infrastructure → Core.Infrastructure`
thì hoặc module tự viết lại hai bộ lọc (lọc đơn vị và lọc xoá mềm), tức hai bản sẽ lệch, hoặc nó **không có
bộ lọc nào** và dữ liệu của mọi đơn vị chảy qua truy vấn đầu tiên ai đó viết.

### 1.3 Danh sách cấm — mỗi dòng là một ArchTest

—

### 1.4 Vì sao giữ luật này từ slice đầu tiên

Chi phí giữ layer sạch từ commit đầu tiên gần như bằng **0**: nó chỉ là việc đặt file vào
đúng thư mục. Chi phí gỡ rối sau khi `Core.Domain` đã dính EF Core thì rất cao và thường
phải viết lại — vì lúc đó entity đã mang navigation property, lazy loading, và các thuộc
tính chỉ có nghĩa khi có `DbContext`.

### 1.5 Khi Infrastructure của Core cần dữ liệu của một module

Tình huống điển hình: một job nền dùng chung ở `Core.Infrastructure` cần dọn dữ liệu cũ của một
module cụ thể. Với interface hẹp, Core không biết module nào tồn tại; module không biết job nào gọi
mình. Thêm module thứ hai không sửa một dòng nào của Core.

---

## 2. `Core.Web` chứa gì — và vì sao nó KHÔNG được nằm ở host

Đây là mục mới so với mọi tài liệu tiền nhiệm, và là lý do tồn tại của cả layout.

### 2.1 Danh sách thành phần

`PasswordChangeRequiredMiddleware` chặn ở BE chứ không để FE điều hướng vì FE điều hướng là trải
nghiệm — một request gọi thẳng bằng `curl` đi qua được.

### 2.2 Vì sao chúng không được ở host

`Core.Web` tồn tại để **Core tự đứng được**; thiếu bất kỳ mục nào trong phép thử nghĩa là mục đó đang
nằm sai chỗ. Ở dự án tiền nhiệm, toàn bộ danh sách thành phần ở §2.1 nằm trong project host. Hệ quả đo được:
**dự án thứ hai muốn dùng lại Core phải copy-paste hơn 600 dòng `Program.cs` cộng 14 file.**

Đó không phải tái sử dụng — đó là nhân bản. Ba hậu quả kéo theo, và cả ba đều không tự
lộ ra:

1. **Mọi bugfix hạ tầng phải sửa ở từng dự án.** Bẫy antiforgery hai cookie trùng tên
   (xem [`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §7.5) sửa ở dự án A không chạm dự án B.
2. **Hai bản sao trôi khỏi nhau ngay lập tức.** Dự án B sửa một dòng cho hợp cảnh của
   mình; từ đó không còn "Core" nữa, chỉ còn hai codebase giống nhau một phần.
3. **Không có chỗ nào để viết ArchTest.** Luật *"mọi controller kế thừa `ApiControllerBase`"*
   cần một `ApiControllerBase` **được ship**, không phải một class mỗi dự án tự chép.

### 2.3 Ranh giới của `Core.Web`

`Core.Web` được biết `Core.Infrastructure` vì nó là nơi nối dây.

---

## 3. Host mỏng — composition root duy nhất

- `Program.cs` vượt 50 dòng nghĩa là có thứ lẽ ra thuộc `Core.Web` đang nằm sai chỗ.
- Kéo `AddControllers()`, `AddCors()`, `UseAuthentication()` ra host là cho phép mỗi dự án tự đặt
  lại thứ tự middleware, mà thứ tự middleware là thứ hỏng **im lặng**: đặt `UseCors` sau
  `UseAuthorization` thì preflight vẫn qua nhưng request thật bị chặn ở nơi khó đoán.
- Truyền `IHostEnvironment` vào `AddCore` là cách duy nhất để Core quyết định theo môi trường mà
  không đọc biến toàn cục.
- Runner nằm ở `Core.Web` chứ không ở host vì cùng lý do §2.2: dự án thứ hai có lệnh bootstrap mà
  không chép tệp nào.

### 3.1 Thứ tự pipeline

Thứ tự pipeline là một quyết định kiến trúc, không phải chi tiết cấu hình. Vì sao bốn chỗ không
hoán đổi được:

1. **`PasswordChangeRequiredMiddleware` sau `UseAuthorization`** — nó cần biết người gọi là ai, nên trả
   403 chứ không 401. Đặt trước `UseAuthentication` thì nó không có danh tính để kiểm và sẽ chặn cả
   người chưa đăng nhập.
2. **`AntiforgeryValidationMiddleware` sau xác thực, trước phân quyền** — sau xác thực vì
   `IAntiforgery` gắn request-token với danh tính tại thời điểm phát hành, và vì middleware cần biết
   request đã có danh tính chưa: phiên hết hạn phải ra 401 trước khi antiforgery kiểm gì. Trước phân
   quyền vì phép kiểm quyền đọc tập quyền hiệu lực từ DB ở mỗi request
   ([`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §4.3): request giả mạo bị loại
   trước lần đọc đó, và nhận mã CSRF bất kể tài khoản có quyền hay không.
3. **`UseCors` trước mọi middleware chặn** — đặt sau thì preflight vẫn qua nhưng request thật bị
   chặn ở một chỗ khó đoán.
4. **`UseForwardedHeaders` đầu tiên** — rate limit phân vùng theo IP, cookie `Secure` cần biết
   request là HTTPS, log ghi IP. Đặt muộn thì các bước đứng trước nó thấy IP và scheme **của proxy**.
   Danh sách proxy tin cậy rỗng là trạng thái đúng khi app chạy không có proxy. Hai cách hỏng, cả
   hai đều im lặng:

   | Hỏng | Hậu quả |
   | --- | --- |
   | Có proxy mà thiếu cấu hình | App tưởng mọi request là HTTP ⇒ không phát cookie mang `Secure` ⇒ endpoint phát token antiforgery lỗi, **không request ghi nào đi qua** — trong khi `/health` vẫn xanh nên triển khai vẫn báo thành công |
   | Tin mọi nguồn gửi header chuyển tiếp | Ai cũng giả được IP để tự chọn phân vùng rate limit — [`be-api-controller.md`](../../../quy-uoc/be-api-controller.md) §6.3 |

**Hai endpoint health.** Orchestrator dùng `/health/live` để quyết định restart; load balancer
dùng `/health/ready` để quyết định đưa vào luồng. Gộp làm một thì một sự cố DB tạm thời khiến
orchestrator **restart** app — làm mọi thứ tệ hơn, vì restart không sửa được DB.

**Vì sao có `EveryMiddlewareClass_IsWiredInto_Pipeline`.** Một lớp bảo vệ trên giấy là dạng lỗi
tệ nhất — nó tạo cảm giác được bảo vệ. `PasswordChangeRequiredMiddleware` từng **thiếu hẳn** khỏi
khối `UseCore()` trong khi hợp đồng auth đã mô tả nó như một hàng rào có thật; đó chính là ca luật
này sinh ra để bắt.

---

## 4. Cấu hình fail-fast — thiếu cấu hình thì không khởi động

### 4.1 Luật

—

### 4.2 Vì sao `?? []` là một cái bẫy, không phải một mặc định lịch sự

Ở dự án tiền nhiệm, allowlist CORS từng đọc thẳng `IConfiguration` và kết thúc bằng
`?? []`. Một cấu hình **thiếu** biến thành một allowlist **rỗng**, mà allowlist rỗng chặn
**mọi** origin. Kết quả: app khởi động thành công, `/health/ready` xanh, deploy báo thành công,
và FE không gọi được một API nào.

> **Một app khởi động rồi chặn sạch mọi request còn tệ hơn một app dừng lại và nói thẳng
> nó thiếu gì.**

Vì vậy `[Required]` + `[MinLength(1)]` trên `AllowedOrigins` không phải trang trí — nó là
thứ biến một sự cố ở Production thành một dòng lỗi lúc `dotnet run`.

### 4.3 `ValidateOnStart` ở MỌI môi trường

**Vì sao không miễn cho Development:** một phép kiểm chỉ bật ở Production là phép kiểm mà nhánh
báo lỗi của nó **chạy lần đầu trên bản thật** — cùng khuôn hỏng luật S9 chặn cho chính sách mật
khẩu. Còn máy dev thiếu cấu hình mà vẫn boot thì lỗi lộ ra ở lời gọi API đầu tiên, dưới dạng một
500 cách xa nguyên nhân.

### 4.4 Ngoại lệ có tên duy nhất — nhóm khoá `Core:Bootstrap:*`

Đây là ngoại lệ **có tên**, không phải một chỗ quên. Lý do là chính phạm vi dùng của nhóm khoá đó: nó
chỉ có nghĩa khi người vận hành chạy lệnh bootstrap một lần lúc cài đặt. Phép kiểm chạy trước thao tác
ghi đầu tiên nên hoặc lệnh không chạy, hoặc nó chạy trọn. Gắn `ValidateOnStart` cho nó nghĩa là **mọi** lần khởi động về sau
đều đòi một mật khẩu tài khoản quản trị đã dùng xong từ lâu — và cách duy nhất để app còn chạy
được là giữ nguyên bí mật đó trong cấu hình môi trường mãi mãi, tức biến một luật fail-fast thành
lý do để một mật khẩu không bao giờ bị gỡ.

Vế "lệnh dừng, không ghi dòng nào" là hợp đồng đã khai ở
[`../../../database/script-runbook.md`](../../../database/script-runbook.md) §3.3 bước 5 — *"thiếu
một giá trị ở bước 4 ⇒ dừng, không ghi dòng nào"*.

---

## 5. Dependency Injection

### 5.1 Đăng ký theo nhóm, không liệt kê tay ở host

`AddCore()` không phải một hàm dài — nó chỉ gọi các nhóm con.

Niêm danh sách assembly làm thứ tự sai không bao giờ thành một app chạy thiếu handler: module lắp sau
`AddCore` làm host **dừng ngay lúc khởi động**. Ca còn lại
vẫn im lặng: module **không** gọi hàm ghi nhận thì không có gì để niêm hay báo — handler của nó
không được đăng ký và lỗi lộ lúc gửi request
([`../../../adr/0025-luu-du-lieu-module-mot-transaction.md`](../../../adr/0025-luu-du-lieu-module-mot-transaction.md),
mục *Tiêu cực*).

Vì sao project sở hữu kiểu sở hữu lời đăng ký: đặt lời đăng ký repository của Infrastructure vào
`Core.Web` sẽ khiến `Core.Web` phải `using` mọi namespace nội bộ của Infrastructure — và ranh giới
nào cũng vỡ theo đúng cách đó.

Vì sao chưa quét theo convention: quét sớm đổi một danh sách đọc được lấy một phép màu khó debug.

### 5.2 Vòng đời DI

`ITenantContext` là `Scoped` không phải lựa chọn phong cách: nó mang đơn vị của **request hiện tại**, và
`CoreDbContext` cũng `Scoped` nên hai vòng đời khớp nhau. .NET bắt ca Scoped-trong-Singleton lúc khởi động
chỉ khi `ValidateScopes` bật — mặc định bật ở Development, tắt ở Production. Job nền và hosted service
không có scope của request, nên phải tự tạo.

`ITenantContext` đăng ký `Singleton` thì đơn vị của request đầu tiên được dùng lại cho mọi request
sau — tức **đơn vị A đọc dữ liệu đơn vị B**, không lỗi, không ngoại lệ, chỉ là truy vấn trả về
nhiều hơn đáng ra được thấy. Nhét `ITenantContext` vào một singleton là ca bị luật cứng
*Scoped-trong-Singleton* cấm.

`CoreDbContext` là `Scoped`; nhét nó vào một singleton nghĩa là một `DbContext` sống suốt vòng đời
app, dùng chung giữa các request, và `ChangeTracker` của nó phình vô hạn.

---

## 6. Vertical slice trong `Application`

### 6.1 Quy ước

—

### 6.2 So sánh hai cách

| | Theo loại kỹ thuật (`Commands/`, `Queries/`, `Validators/`) | Theo nghiệp vụ (chọn cách này) |
| --- | --- | --- |
| Sửa một feature | Nhảy qua 4–5 thư mục để ráp lại bức tranh | Mọi file liên quan nằm cạnh nhau |
| Xoá một feature | Phải đi tìm mảnh vụn ở từng thư mục, luôn sót | Xoá một thư mục |
| Tách thành module riêng | Phải gỡ từng file khỏi từng thư mục dùng chung | Di chuyển một thư mục |
| Xung đột merge | Cao — hai người thêm hai feature cùng chạm `Commands/` | Thấp — hai thư mục khác nhau |
| Đọc lần đầu | Thấy ngay "hệ thống có những loại kỹ thuật nào" | Thấy ngay "hệ thống làm được những việc gì" |

Cột phải thắng ở mọi hàng trừ hàng cuối, và hàng cuối là câu hỏi người ta chỉ hỏi một
lần trong đời một dự án.

`Common/` chia theo kỹ thuật là chấp nhận được vì nó chứa hạ tầng của tầng Application; ranh giới
"từ hai slice trở lên dùng" tồn tại để không biến `Common/` thành ngăn kéo rác.

---

## 7. Thêm một tính năng mới — checklist

Thứ tự này chọn để lỗi lộ ra sớm nhất có thể. Contract là thứ hai phía cùng nhìn, không phải tài liệu
viết sau. Ba tầng test theo thứ tự: unit test cho factory/mutation method của entity (không cần DB), unit
test cho handler với repository giả lập, integration test cho endpoint qua `WebApplicationFactory` +
PostgreSQL thật.

---

## 8. SOLID & OOP — áp vào đúng ngữ cảnh Core này

Không lý thuyết suông — mỗi nguyên tắc phải chỉ được một chỗ trong Core mà nó quyết định.

- **S.** Phép thử hai-lý-do-thay-đổi: sửa một luật nghiệp vụ **và** đổi công nghệ lưu trữ cùng buộc sửa
  một class X ⇒ X mang hai lý do để thay đổi. Đó là lý do repository nằm ở Infrastructure còn luật nằm ở
  Domain/Handler.
- **O.** Thêm một `ErrorDescriptor` chỉ là thêm một dòng vào catalog của feature — vì `ErrorType`
  đã phủ hết các nhánh HTTP và ánh xạ đã khai đủ. Sửa `Core.*` cho riêng một module không phải việc nhỏ.
- **L.** Ép `AppUser` kế thừa `BaseEntity` rồi bỏ qua một phần hành vi là vi phạm LSP; `AppUser`
  có vòng đời khác.
- **I.** Ba interface Identity thay vì một `IIdentityService` khổng lồ: một handler chỉ tra cứu
  người dùng không phải phụ thuộc vào bề mặt cho phép khoá tài khoản.
- **D.** Application khai interface, Infrastructure implement, và compiler không cho đi ngược.
  Không có gì thêm phải làm ngoài giữ kỷ luật đó.
- **Encapsulation.** Lý do và ranh giới của ngoại lệ field audit ở
  [`be-entity-domain.md`](../../../quy-uoc/be-entity-domain.md) §1.
- **Polymorphism.** Một `switch` ba nhánh đọc được rõ hơn ba class rải ba file.

---

## 9. Kết cấu thư mục test và ArchTest nào canh luật nào

### 9.1 Layout

Bộ lọc tenant, xoá mềm và transaction chỉ chứng minh được bằng integration test vì unit test với
repository giả lập chạy mà không có bộ lọc nào — nên nó xanh cả khi bộ lọc đã mất.

`UseInMemoryDatabase` bị cấm vì provider đó không có transaction, không có ràng buộc, không có
index lọc — nên nó không chứng minh được gì về hành vi thật, mà lại xanh.

### 9.2 ArchTest nào canh luật nào

Một tên ArchTest chép lệch là một tên không tìm thấy khi `grep`.

### 9.3 Mọi detector phải có test đối chứng

Không có test đối chứng thì một detector hỏng sẽ **xanh vĩnh viễn**, và một cổng xanh giả
tệ hơn không có cổng: nó tạo cảm giác được bảo vệ. Ở dự án tiền nhiệm, một script cổng
**không tồn tại trên đĩa** khiến ba mục cổng không chạy suốt thời gian dài trong khi tài
liệu vẫn ghi bình thường — xem
[`../../../audit/2026-08-23-cong-khong-ton-tai.md`](../../../audit/2026-08-23-cong-khong-ton-tai.md).

### 9.4 Bẫy khi thi công detector

Ở dự án tiền nhiệm, detector cho luật tương đương `CoreSource_MustNotContain_BusinessNameStringLiteral`
quét văn bản, nên một chuỗi nội suy hợp lệ vẫn bị bắt và người viết phải tách biến chỉ để làm chuỗi
"sạch" — đuôi vẫy chó.

---

## 10. Đếm bằng lệnh, đừng chép số

Một project trên đĩa mà không nằm trong solution thì không được build, không được test, và không ai biết.
