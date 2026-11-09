---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0129 — Ở Development, CORS và lớp 1 chống giả mạo nhận mọi origin `https://localhost:<cổng>` qua một hàm origin chung; `npm start` bỏ bước kiểm cổng

> **Trạng thái:** Đã chấp nhận (2026-11-09)
>
> Người dùng chốt ngày 2026-11-09: ở môi trường Development, FE chạy ở cổng localhost nào cũng đăng nhập được — phương án *"Dev nhận mọi cổng localhost"*, sau khi được nêu cái giá: trang web khác chạy trên localhost của máy dùng được phiên dev của repo này. Người dùng loại hai phương án: đổi sang một cổng cố định khác; giữ nguyên và thêm origin bằng `user-secrets` mỗi lần. Kiến trúc sư chốt phần còn lại: chỉ `https` và đúng host `localhost` (quyết định 3), chỗ đặt hàm chung, số phận bước kiểm cổng của `npm start` và cặp 2 của luật T14.

## Bối cảnh

Người dùng cần nhường cổng 4200 cho một repo khác. FE của repo này chạy cổng khác thì không đăng nhập được: BE dev chỉ nhận đúng các origin khai trong `Core:Auth:AllowedOrigins`, nên trình duyệt chỉ thấy preflight `OPTIONS` 204 rồi không gửi `POST`. Sáng cùng ngày, sau một lần gặp đúng lỗi đó, một dòng nhật ký đã chốt hướng ngược lại: `npm start` dừng khi 4200 bận, chạy cổng khác thì thêm origin bằng `user-secrets` — dòng *Sinh từ lỗi thật trên máy dev* ngày 2026-11-09 ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), điểm (3). Hướng đó bắt người dev gõ hai lệnh và khởi động lại BE mỗi lần đổi cổng, kèm bẫy chỉ số mảng của `user-secrets`; người dùng không nhận cái giá ấy.

Hiện trạng ngày 2026-11-09, đã mở code ra đối chiếu:

| Chỗ | Nói gì | Neo |
| --- | --- | --- |
| CORS | Policy so origin với danh sách | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`, chuỗi `.WithOrigins(authOptions.Value.AllowedOrigins)` |
| Lớp 1 chống giả mạo | Middleware tự duyệt danh sách, so không phân biệt hoa thường | `src/BE/Core/CoreAndSkill.Core.Web/Security/AntiforgeryValidationMiddleware.cs`, chuỗi `string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase)` |
| Dạng mục danh sách | Kiểm lúc khởi động — mục phải đúng dạng header `Origin` | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreOptionsServiceCollectionExtensions.cs`, chuỗi `private static bool IsWellFormedOrigin(` |
| Danh sách của máy dev | Hai origin: FE và trang Scalar của chính API | `src/BE/CoreAndSkill.Api/appsettings.Development.json`, chuỗi `"AllowedOrigins": ["https://localhost:4200", "https://localhost:7100"]` |
| Token chống giả mạo | Nằm trong **thân** phản hồi của `GET /api/v1/core/antiforgery/token` — CORS quyết trang nào đọc được nó | `docs/contracts/auth.md:37` |
| Bước kiểm cổng của `npm start` | Dừng khi cổng của `ng serve` bận | `src/FE/package.json`, chuỗi `"prestart": "node scripts/kiem-cong-dev.cjs"` |
| Cặp 2 của luật T14 | Cổng đòi danh sách dev chứa origin của `ng serve` | `scripts/dev-config-check.sh`, chuỗi `AllowedOrigins chứa $PIN_FE_ORIGIN` |
| Bản cài | Chạy Production | `docker-compose.yml`, chuỗi `ASPNETCORE_ENVIRONMENT: Production` |
| Host của integration test | Mặc định Production | `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/CoreWebApplicationFactory.cs`, chuỗi `string environment = "Production"` |
| Tiền lệ công tắc theo môi trường trong `Core.Web` | OpenAPI chỉ đăng ký ở Development | `CoreWebServiceCollectionExtensions.cs`, chuỗi `if (environment.IsDevelopment())` — [ADR-0118](0118-openapi-sinh-bang-goi-cua-aspnetcore-xem-thu-bang-scalar-chi-o-development-ide-mo-trang-scalar-bo-cong-http.md) |

Ba sự thật của trình duyệt quyết hình dạng của lời giải:

- **Cookie không phân biệt cổng.** Trang ở `https://localhost:<n>` cùng site với API ở `https://localhost:7100`, nên cookie phiên `SameSite=Lax` đi theo request của nó. Hôm nay thứ duy nhất ngăn một trang như thế là CORS (không đọc được phản hồi, nên không lấy được token) và lớp 1 (không ghi được).
- **Site gồm scheme và host** ([ADR-0015](0015-fe-va-api-khac-nguon.md), bảng *Tiêu cực*): FE `http` mà API `https` là khác site, cookie phiên không đi. Theo cùng lý đó, trang ở `127.0.0.1` hay `[::1]` khác site với API ở `localhost` — trình duyệt bỏ cookie `Lax` mà phản hồi đăng nhập đặt từ một request khác site.
- **CORS và lớp 1 phải ra cùng một phán quyết** — luật S30: lớp 1 dùng cùng allowlist với CORS. Hôm nay hai bên đọc cùng danh sách nhưng là hai đoạn mã: CORS qua `WithOrigins`, lớp 1 qua vòng lặp riêng. Theo mã nguồn ASP.NET Core — chưa đo trong repo — `WithOrigins` chuẩn hoá mục về chữ thường rồi so phân biệt hoa thường, còn lớp 1 so không phân biệt: hai bên đã có thể lệch nhau ở ca hoa thường.

**Core hay module.** Cơ chế CORS và lớp 1 đã nằm trong `Core.Web` — [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2, danh sách thành phần ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §2.1. Ngưỡng hai module ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2 đếm module cần một năng lực; ở đây không module nào tiêu thụ gì — việc đổi là hành vi của chính một cơ chế Core, có nghĩa với máy dev của mọi sản phẩm (phép thử §4.1), không mang khái niệm nghiệp vụ nào. Đặt ở host thì host phải dựng lại policy CORS của Core, còn lớp 1 là kiểu `internal sealed` của `Core.Web`: hai nơi giữ một quy tắc — đúng thứ S30 cấm.

## Quyết định

1. **Một quy tắc origin, một hàm.** `Core.Web/Security/` giữ một kiểu `internal sealed` mang hàm quyết một origin có được nhận không. CORS gọi hàm đó qua `CorsPolicyBuilder.SetIsOriginAllowed`; lớp 1 của `AntiforgeryValidationMiddleware` gọi đúng hàm đó. Không chỗ nào khác đọc `Core:Auth:AllowedOrigins` để quyết một origin. Phép kiểm dạng origin lúc khởi động dời vào cùng kiểu, để *đúng dạng header `Origin`* có một định nghĩa.
2. **Hàm nhận một origin khi:** (a) ở mọi môi trường — origin khớp một mục `AllowedOrigins`, không phân biệt hoa thường; hoặc (b) **chỉ khi `IHostEnvironment.IsDevelopment()`** — origin đúng dạng header `Origin`, scheme `https`, host đúng chữ `localhost`, cổng bất kỳ. Ngoài Development chỉ còn (a): hành vi như hôm nay. `[Required, MinLength(1)]` và phép kiểm dạng mục giữ nguyên ở mọi môi trường, kể cả Development.
3. **Nhánh (b) hẹp hơn đề bài người dùng nhận:** không `http://`, không `127.0.0.1`, `[::1]`, không tên khác trỏ về máy (`loopback`, `*.localhost`) — lý do ở phương án D. Hàm so `Host` với chữ `localhost`, **không** dùng `Uri.IsLoopback`: thuộc tính đó trả `true` cho cả `loopback`, dải `127.0.0.0/8` và `[::1]`.
4. **`npm start` bỏ bước kiểm cổng.** Gỡ script `prestart` khỏi `src/FE/package.json`, xoá `src/FE/scripts/kiem-cong-dev.cjs`. Thư mục `src/FE/scripts/` hết tệp nên rời cây thư mục ở [`../quy-uoc/fe-architecture.md`](../quy-uoc/fe-architecture.md) §1.1. Cổng 4200 bận thì `ng serve` hỏi có dùng cổng khác không — chọn có là đăng nhập được; hoặc `npm start -- --port <n>`. Cổng mặc định của `angular.json` giữ 4200.
5. **Cặp 2 của T14 đổi nghĩa.** `scripts/dev-config-check.sh` giữ hai phép kiểm phía FE — khối `serve` bật `ssl`, cổng mặc định khớp hằng ghim 4200, cấu hình `development` không ghi đè hai giá trị đó — và gỡ phép kiểm `AllowedOrigins` chứa origin FE: phía BE không còn gì để khớp. Hai mục dev của `AllowedOrigins` ở `appsettings.Development.json` giữ nguyên.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Đổi FE sang một cổng cố định khác

**Được:** không dòng code BE nào; một giá trị đổi ở hai tệp, cổng T14 canh.
**Mất:** chỉ dời chỗ va chạm — repo khác hay ứng dụng khác có thể giữ đúng cổng mới.
**Vì sao loại:** người dùng loại ngày 2026-11-09.

### Phương án B — Giữ nguyên, thêm origin bằng `user-secrets` mỗi lần cần cổng khác

**Được:** BE không đổi, Production và Development cùng một nhánh mã; cái giá chỉ rơi vào đúng người cần cổng khác, và chỉ cổng đó mở. Hướng dẫn đã viết sáng cùng ngày.
**Mất:** mỗi lần đổi cổng hai lệnh cộng khởi động lại BE; `user-secrets` gộp mảng theo chỉ số, trùng chỉ số là thay mục của tệp chứ không cộng; cổng mà CLI tự đề nghị khi 4200 bận đổi theo từng lần.
**Vì sao loại:** người dùng loại ngày 2026-11-09.

### Phương án C — Khai sẵn một dải cổng trong `appsettings.Development.json`

**Được:** không dòng code BE, không nhánh theo môi trường; diện mở chỉ là các cổng khai sẵn.
**Mất:** khi 4200 bận, CLI đề nghị một cổng nó tự chọn, nằm ngoài dải — đúng đường đã sinh ra lỗi thật sáng cùng ngày; người dev phải nhớ gõ `--port` với một cổng trong dải; cổng T14 phải canh cả dải.
**Vì sao loại:** không phủ đường dễ gặp nhất — trả lời *có* khi CLI hỏi đổi cổng.

### Phương án D — Đúng đề bài: mọi host loopback (`localhost`, `127.0.0.1`, `[::1]`), scheme `http` hoặc `https`

**Được:** phủ mọi cách gõ địa chỉ máy mình; khớp nguyên văn lời giao việc.
**Mất:** thêm diện mở mà không thêm ca đăng nhập nào. `http://localhost:<n>` khác scheme với API nên khác site, cookie phiên không đi — mà máy chủ cục bộ chạy `http` là loại hay gặp nhất trên một máy dev (công cụ dev, container). `127.0.0.1`, `[::1]` khác site với API ở `localhost`: CORS và lớp 1 cho qua, đăng nhập trả thành công, rồi request kế tiếp ra 401 vì cookie bị bỏ — lỗi không nói ra nguyên nhân, đúng loại ADR-0015 cảnh báo.
**Vì sao loại:** không ca nào trong phần nới thêm đăng nhập được; nới chúng chỉ cộng cái giá. Cần thì nới là một vế trong hàm, kèm ca test.

### Phương án E — Cờ cấu hình bật hành vi, ví dụ một khoá dưới `Core:Auth` đặt `true` ở `appsettings.Development.json`

**Được:** hành vi hiện ra ngay trong tệp cấu hình; dự án hạ nguồn tắt được mà không sửa Core.
**Mất:** một khoá nữa, một tổ hợp nữa phải test. Cờ đứng một mình thì một biến môi trường đặt nhầm ở bản cài là mở CORS; cờ kèm `IsDevelopment()` thì cờ chỉ còn tác dụng tắt.
**Vì sao loại:** chưa dự án hạ nguồn nào tồn tại, nên nhu cầu tắt là dự đoán. Thêm cờ khi dự án đầu tiên cần là một khoá và một test — đảo được, không mất gì vì chờ.

### Phương án F — Mẫu ký tự đại diện trong `AllowedOrigins`, ví dụ `https://localhost:*`

**Được:** hành vi nằm trong tệp cấu hình, không nhánh theo môi trường trong mã.
**Mất:** allowlist có ngôn ngữ mẫu ở **mọi** môi trường — bản cài cũng viết được mẫu; phép kiểm dạng mục phải học cú pháp mẫu. ASP.NET Core chỉ có ký tự đại diện cho tên miền con (`SetIsOriginAllowedToAllowWildcardSubdomains`), không cho cổng, nên vẫn phải tự viết hàm.
**Vì sao loại:** mở cho Production một cú pháp mà chỉ Development cần.

### Phương án G — Proxy của `ng serve`, FE và API cùng origin ở máy dev

**Vì sao loại:** đã loại ở [ADR-0015](0015-fe-va-api-khac-nguon.md) phương án B — giấu nhóm lỗi chéo nguồn khỏi máy dev.

### Phương án H — Mỗi bên tự viết nhánh Development: CORS một hàm, lớp 1 một hàm cùng logic

**Được:** chữ ký middleware không đổi.
**Mất:** hai bản của một quy tắc bảo mật; lệch nhau là CORS cho đọc mà lớp 1 chặn ghi, hay ngược lại. Hai đoạn mã hôm nay đã có thể lệch ở ca hoa thường.
**Vì sao loại:** S30 đòi một allowlist cho hai lớp; một hàm là cách duy nhất để điều đó đúng bằng cấu trúc chứ không bằng sự cẩn thận.

### Phương án I — Giữ bước kiểm cổng, đổi nghĩa thành tự chọn cổng trống cho `ng serve`

**Được:** `npm start` không bao giờ dừng vì cổng bận, không câu hỏi nào.
**Mất:** lặp việc CLI đã làm; một công cụ Node phải bảo trì trên ba hệ điều hành.
**Vì sao loại:** sau quyết định 2, cổng lạ không còn hỏng gì — không còn lỗi nào để bước kiểm chặn.

### Phương án J — Gỡ trọn cặp 2 của T14, kể cả phép kiểm `ssl` và cổng ghim

**Được:** cổng ngắn hơn.
**Mất:** `ssl` tắt ở `angular.json` thì origin thành `http`, BE dev không nhận (quyết định 3) — mất phép bắt tĩnh duy nhất của ca đó; cổng mặc định đổi thì các bước ở [`../quy-uoc/chay-may-dev.md`](../quy-uoc/chay-may-dev.md) §2 ghi 4200 sai.
**Vì sao loại:** hai phép kiểm đó có sẵn, đã có canary, không tốn thêm gì.

## Hệ quả

### Tích cực

- Ở Development, FE chạy cổng nào trên `https://localhost` cũng đăng nhập được, không bước cấu hình nào.
- CORS và lớp 1 không còn cách nào lệch nhau: cùng một hàm, kể cả ở ca hoa thường.
- Gỡ một công cụ máy dev và một hướng dẫn `user-secrets` có bẫy chỉ số.
- Ngoài Development không gì đổi: bản cài chạy Production, host test mặc định chạy Production nên các test S30 đang có tiếp tục canh nhánh (a).

### Tiêu cực

- **Ở Development, mọi trang `https://localhost:<cổng>` trên máy dev dùng trọn phiên dev.** Cookie phiên đi theo request của trang đó; CORS cho nó đọc thân của `GET /api/v1/core/antiforgery/token`, nên nó có token; lớp 1 cho `Origin` của nó qua. Cả ba lớp đang ngăn một trang lạ — CORS, lớp 1, lớp 2 — đều không ngăn trang trên localhost của máy dev. Người dùng nhận cái giá này. Phần giảm nhẹ: dữ liệu là dữ liệu dev, mật khẩu ban đầu của tài khoản dev công khai trong git ([ADR-0110](0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md)), và trang `http` cục bộ không qua (quyết định 3).
- **Máy dev không còn chạy nhánh so khớp danh sách.** Một lỗi ở nhánh (a) chỉ lộ ở test và ở bản cài; nhánh (b) thì chỉ chạy ở máy dev, không ở bản cài. Hai môi trường chạy hai nhánh khác nhau của cùng một hàm.
- **Hai mục dev của `AllowedOrigins` thừa ở Development** nhưng vẫn phải có vì `MinLength(1)`; người đọc `appsettings.Development.json` có thể tưởng chỉ hai cổng đó được nhận.
- **Mọi dự án hạ nguồn nhận luôn hành vi này** trên máy dev của họ; muốn tắt thì phải sửa Core cho tới khi có cờ ở phương án E.
- **`IsDevelopment()` thành công tắc bảo mật thứ hai** sau OpenAPI. Một máy chủ chạy nhầm Development thì CORS của nó nhận mọi trang `https://localhost` trên máy người dùng; cookie của máy chủ khác site với `localhost` nên không đi, thứ lộ ra là phản hồi của endpoint ẩn danh. Development trên máy chủ vốn đã là lỗi nặng hơn — OpenAPI, `user-secrets`.
- **Mất thông báo riêng của bước kiểm cổng**, kể cả ca hệ điều hành không cho nghe cổng (`EACCES`, dải cổng Windows giữ riêng); `ng serve` tự báo lỗi theo cách của nó.

### Rút lui nếu sai

1. Gỡ nhánh (b) khỏi hàm chung — một khối điều kiện. Quyết định 1 đứng được một mình, giữ nguyên.
2. Đảo các ca Development của test: `https://localhost:55697` thành bị từ chối ở cả CORS lẫn lớp 1.
3. Máy dev cần cổng khác thì quay về phương án B: hướng dẫn ở [`../quy-uoc/chay-may-dev.md`](../quy-uoc/chay-may-dev.md) §5 dựng lại từ lịch sử git; phép kiểm `AllowedOrigins` của cặp 2 và hai ca canary của nó lấy lại từ lịch sử `scripts/dev-config-check.sh`, `scripts/tests/dev-config-check.test.sh`; bước kiểm cổng lấy lại `src/FE/scripts/kiem-cong-dev.cjs` và dòng `prestart` nếu muốn.

Không schema, không dữ liệu, không cookie, không hợp đồng API công khai nào đổi; mã lỗi `CORE.AUTH.ORIGIN_REJECTED` giữ nguyên. Rút lui vừa một lượt thi công.

### Dấu hiệu quyết định này bắt đầu sai

- Một dự án hạ nguồn hay một người dev cần tắt hành vi ở máy dev → phương án E.
- Có người cần `http://localhost` hay `127.0.0.1` thật sự đăng nhập được → xét nới, kèm test chứng minh cookie đi.
- Một bản cài nào đó chạy Development.
- `grep -rn 'AllowedOrigins' src/BE/Core --include=*.cs` ra một chỗ thứ hai ngoài hàm chung và phần khai báo, ràng buộc của `CoreAuthOptions` mà quyết một origin.
- Một sự cố trên máy dev đi qua một trang localhost.

## ADR cũ — phần nào còn hiệu lực

- [ADR-0015](0015-fe-va-api-khac-nguon.md) giữ nguyên vẹn: hai origin ở mọi môi trường, CORS bật và HTTPS ở máy dev, không proxy. ADR này thêm vào phần máy dev của nó: tập origin được nhận ở Development rộng hơn ở bản cài. Dòng trạng thái của ADR-0015 thêm đoạn *Bổ sung bởi ADR-0129*.
- [ADR-0118](0118-openapi-sinh-bang-goi-cua-aspnetcore-xem-thu-bang-scalar-chi-o-development-ide-mo-trang-scalar-bo-cong-http.md) không đổi; mục `https://localhost:7100` của danh sách dev vẫn đúng, nay thừa ở Development.
- Dòng *Sinh từ lỗi thật trên máy dev* ngày 2026-11-09 ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md): (1), (2), (5) giữ hiệu lực. (3) hết áp dụng. (4) không còn tệp nào để áp; dòng nhật ký giữ lý do đặt chỗ cho lần có công cụ máy dev FE đầu tiên.

## Việc thi công

**`backend-expert`** — phạm vi `src/BE/Core/`, `src/BE/Tests/`, chạm Core:

1. Kiểu mới ở `src/BE/Core/CoreAndSkill.Core.Web/Security/`, tên đề xuất `OriginAllowlist`, `internal sealed`, đăng ký singleton trong `AddCoreWeb`. Hàm công khai trong assembly: nhận một chuỗi origin, trả `bool` theo quyết định 2–3. Nên tách lõi thành hàm tĩnh thuần nhận danh sách và cờ Development, để test không cần host. Rỗng hay `null` trả `false`.
2. `IsWellFormedOrigin` dời từ `CoreOptionsServiceCollectionExtensions` vào kiểu mới thành `internal static`; phép `.Validate(` của `CoreAuthOptions` gọi bản dời, thông điệp giữ nguyên. Nhánh (b) dùng đúng hàm đó rồi thêm hai điều kiện: `Uri.Scheme` là `https`, `Uri.Host` bằng `localhost` không phân biệt hoa thường.
3. CORS: trong khối `services.AddOptions<CorsOptions>()`, phụ thuộc đổi từ `IOptions<CoreAuthOptions>` sang kiểu mới; policy dùng `SetIsOriginAllowed(<hàm của kiểu mới>)` và **gỡ** `WithOrigins` — `SetIsOriginAllowed` thay hẳn phép so mặc định đọc danh sách của `WithOrigins`, giữ cả hai là để lại một danh sách không ai đọc. Bốn lời gọi còn lại của policy giữ nguyên. `AllowCredentials` đi cùng `SetIsOriginAllowed` được; chỉ `AllowAnyOrigin` mới bị cấm cùng nó.
4. `AntiforgeryValidationMiddleware`: tham số `IOptions<CoreAuthOptions>` thay bằng kiểu mới; bước 3 gọi hàm của nó; gỡ `IsAllowedOrigin`.
5. Test — đặt dưới `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Security/` (`Core.Web` chỉ mở `internal` cho project này):
   - Hàm thuần, không host. Nhận ở Development: `https://localhost:55697`, `https://localhost`, `https://LOCALHOST:55697`. Từ chối ở Development: `https://evil.example`, `https://localhost.evil.example`, `https://localhost.evil.example:55697`, `https://localhost@evil.example`, `http://localhost:55697`, `https://127.0.0.1:55697`, `https://[::1]:55697`, `https://loopback:55697`, `https://localhost:55697/`, `null` (chuỗi), chuỗi rỗng. Không Development: `https://localhost:55697` bị từ chối khi không có trong danh sách, được nhận khi có.
   - Qua host — `CoreWebApplicationFactory`, chế độ không database, mỗi ca một host vì bộ đếm rate limit sống theo host. Mỗi ca khẳng định **cả hai phía cho cùng một origin**: `GET /api/v1/core/antiforgery/token` kèm `Origin` có hay không có `Access-Control-Allow-Origin` bằng đúng origin đó; `POST /api/v1/core/auth/login` không token kèm `Origin` ra 403 `CORE.AUTH.CSRF_REJECTED` khi lớp 1 nhận — lớp 2 vẫn chặn — hay 403 `CORE.AUTH.ORIGIN_REJECTED` khi lớp 1 từ chối. Các ca: Development nhận `https://localhost:55697`; Development từ chối `https://evil.example` và `https://localhost.evil.example`; Production từ chối `https://localhost:55697`; Development vẫn nhận một mục không phải localhost khai trong danh sách.
   - Host Development nạp `appsettings.Development.json` và kho `user-secrets` của máy chạy test: chọn cổng không ai khai — `55697` — và không dựa vào giá trị nào của kho đó.
   - Các test S30 đang có, `Cors_*` ở `SecurityPipelineTests.cs` và hai test dạng origin ở `ConfigurationFailFastTests.cs` phải xanh nguyên văn.

**`frontend-expert`** — ngoài `core-paths`: gỡ dòng `prestart` ở `src/FE/package.json`, xoá `src/FE/scripts/kiem-cong-dev.cjs` rồi thư mục rỗng. `"start": "ng serve"` và `angular.json` không đổi. Đi sau lượt BE hoặc cùng lượt: FE về trước thì người dev chọn cổng khác vẫn hỏng cho tới khi BE về.

**`test-engineer`** — `scripts/dev-config-check.sh` và canary của nó dưới `scripts/tests/`, chạm Core:

- Cổng: gỡ phép đọc `AllowedOrigins` và phép kiểm nó chứa origin FE; giữ cặp 1, cặp 3, phép kiểm `ssl` bật, cổng mặc định khớp `PIN_FE_PORT`, cấu hình `development` không ghi đè `port`/`ssl`. Chú thích đầu tệp tả lại cặp 2: phía FE ghim, BE dev nhận mọi cổng `https://localhost` — [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.3.
- Canary: gỡ hai ca đỏ về `AllowedOrigins` (bỏ mục 4200; mục còn `http://`); ca *bản chép nguyên vẹn* thôi đòi chuỗi `AllowedOrigins chứa`, đòi dòng OK của `ssl` thay vào. Ca đỏ còn lại của cặp 2 vẫn có ca xanh đối ứng theo T12 — ca nguyên vẹn và ca cấu hình `e2e`.
- Chạy: `bash scripts/dev-config-check.sh` thoát 0, `bash scripts/tests/dev-config-check.test.sh` thoát 0 trên máy dev.

## Liên quan

- Quy tắc và chỗ của nó: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7.2, §7.3; luật S30 ở [`../RULES-BE.md`](../RULES-BE.md); luật T14 ở [`../RULES-CHUNG.md`](../RULES-CHUNG.md)
- Máy dev: [`../quy-uoc/chay-may-dev.md`](../quy-uoc/chay-may-dev.md) §2, §4, §5
- FE và API khác origin, điều kiện cùng site: [ADR-0015](0015-fe-va-api-khac-nguon.md)
- Công tắc Development đầu tiên của `Core.Web`: [ADR-0118](0118-openapi-sinh-bang-goi-cua-aspnetcore-xem-thu-bang-scalar-chi-o-development-ide-mo-trang-scalar-bo-cong-http.md)
- Giá trị dev công khai trong git: [ADR-0110](0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md)
