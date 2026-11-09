---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0118 — Tài liệu OpenAPI sinh bằng `Microsoft.AspNetCore.OpenApi`, xem và thử bằng Scalar, chỉ đăng ký ở Development; chạy BE từ IDE mở trang Scalar, bỏ cổng http

> **Trạng thái:** Đã chấp nhận (2026-10-06)
>
> Người dùng chốt ngày 2026-10-06 (đợt rà đồng bộ, câu Q38 và Q39): OpenAPI kèm Scalar, chỉ ở Development, đưa vào lộ trình BE; chạy BE từ IDE thì mở trang OpenAPI, bỏ cổng http. Kiến trúc sư chốt phần còn lại: phiên bản gói, đường dẫn, ngoại lệ tiền tố, cách thi công luật R11 theo đợt. Không đổi trạng thái ADR nào.

## Bối cảnh

Người dùng muốn gỡ lỗi BE bằng một trang tài liệu API thử được ngay trên trình duyệt, kiểu Swagger. Hai luật cho việc này đã viết sẵn — S29 (chỉ bật ở Development, ở Production chỉ sau xác thực hoặc tắt hẳn) và R11 (mọi action khai `[ProducesResponseType]`, kiểu thành công là `ApiEnvelope<T>`) — nhưng không pha nào của lộ trình BE nhận việc dựng OpenAPI, và chú thích trong code hẹn một *"pha có Swagger"* không tồn tại.

Hiện trạng ngày 2026-10-06, đã mở code ra đối chiếu:

| Chỗ | Nói gì | Neo |
| --- | --- | --- |
| Gói OpenAPI | Không tệp `.cs`, `.csproj`, `.props` nào dưới `src/BE` nhắc `OpenApi`, `Swashbuckle` hay `ProducesResponseType` | Lệnh `grep -rln -E 'OpenApi\|Swashbuckle\|ProducesResponseType' --include=*.cs --include=*.csproj --include=*.props src/BE` không ra dòng nào |
| Chỗ chờ | Một chú thích trong `AddCoreWeb` | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`, chuỗi `vào ở pha có Swagger` |
| Phiên bản nền | SDK .NET 10, các gói `Microsoft.AspNetCore.*` ghim ở `10.0.12` | `src/BE/global.json`, chuỗi `"version": "10.0.400"` · `src/BE/Directory.Packages.props`, chuỗi `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore" Version="10.0.12"` |
| Kiểu trả về của action | Mọi action trả `IActionResult` qua một lối `Handle*`, nên bộ sinh tài liệu không suy được kiểu phản hồi — thiếu `[ProducesResponseType]` thì tài liệu **không có** schema phản hồi, chứ không ra schema sai. Hai lối trả tệp thành công bằng chính tệp, không envelope | `src/BE/Core/CoreAndSkill.Core.Web/Controllers/ApiControllerBase.cs`, chuỗi `protected IActionResult HandleResult<T>(Result<T> result)` và chuỗi `Thành công: thân là chính tệp, KHÔNG phải envelope` |
| Tiền tố đường dẫn | Mọi endpoint nằm dưới `/api/v<N>/<khu>/…`, trừ đúng hai endpoint kiểm tra sức khoẻ, *"không có ca thứ ba"* | `docs/quy-uoc/be-api-controller.md:662` |
| Host mỏng | `AddCore` nhận `IHostEnvironment`, có nhắc Swagger là chỗ dùng nó; `Program.cs` dưới 50 dòng, không logic | `docs/quy-uoc/be-architecture.md:359` |
| Chạy từ IDE | Profile mở trình duyệt ở gốc API — gốc không có endpoint nên ra 404 thân rỗng, người dùng tưởng BE hỏng; profile còn một cổng http, mà cookie phiên mang `SecurePolicy = Always` nên đăng nhập qua cổng đó không giữ được phiên | `src/BE/CoreAndSkill.Api/Properties/launchSettings.json`, chuỗi `"launchBrowser": true` và chuỗi `https://localhost:7100;http://localhost:49543` · `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`, chuỗi `options.Cookie.SecurePolicy = CookieSecurePolicy.Always;` |
| Lớp 1 chống giả mạo | Request ghi mang `Origin` ngoài allowlist bị từ chối; allowlist máy dev chỉ có origin của FE | `src/BE/Core/CoreAndSkill.Core.Web/Security/AntiforgeryValidationMiddleware.cs`, chuỗi `lớp 1: Origin ngoài allowlist` · `src/BE/CoreAndSkill.Api/appsettings.Development.json`, chuỗi `"AllowedOrigins": ["https://localhost:4200"]` |

Hai gói tra trên nuget.org ngày 2026-10-06: `Microsoft.AspNetCore.OpenApi` có bản `10.0.12` cùng dòng với các gói `Microsoft.AspNetCore.*` đang ghim, phụ thuộc `Microsoft.OpenApi` `[2.12.0, 3.0.0)`; `Scalar.AspNetCore` bản mới nhất `2.17.13` (phát hành 2026-10-02), có đích `net10.0`, không phụ thuộc gói nào ngoài framework ASP.NET Core, giấy phép MIT. Gói Scalar nhúng sẵn bản dựng giao diện trong assembly (tài nguyên `ScalarStaticAssets.scalar.js`) — không tải mã giao diện từ CDN — nhưng có hai lời gọi ra ngoài bật sẵn: **telemetry** (ghi việc một request có được gửi qua trình thử hay không) và **phông chữ mặc định tải từ CDN của Scalar**.

## Quyết định

Người dùng chọn OpenAPI kèm Scalar, chỉ ở Development. Kiến trúc sư chốt cách làm như sau:

1. **Hai gói, ghim chính xác ở `src/BE/Directory.Packages.props`:** `Microsoft.AspNetCore.OpenApi` `10.0.12` sinh tài liệu — nâng cùng lúc với các gói `Microsoft.AspNetCore.*` khác; `Scalar.AspNetCore` `2.17.13` làm giao diện. Chỉ `Core.Web` tham chiếu hai gói này.
2. **Đăng ký và map trong `Core.Web`, chỉ khi `IsDevelopment()`:** `AddCoreWeb` gọi `AddOpenApi()`; `UseCoreAsync` map tài liệu ở đường mặc định `/openapi/v1.json` và trang Scalar ở `/scalar`, tắt telemetry và phông mặc định của Scalar — trang không tự gọi ra ngoài máy dev. Mọi môi trường khác không đăng ký, không map: hai đường đó ra 404 như đường không tồn tại. Đây là nhánh *tắt hẳn* của S29; không có nhánh *sau xác thực*.
3. **Hai đường công cụ là ngoại lệ có tên thứ hai của tiền tố `/api`:** chỉ tồn tại ở Development, ẩn danh, không envelope, không phiên bản. Ngoài hai đường này không thêm ca nào; câu luật nằm ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §8.1.
4. **Luật R11 thi công theo đợt, cổng ở đợt cuối:**
   - **Lượt 1** — hai gói, điểm 2, cặp test S29 (`OpenApiDocument_IsNotServedAnonymously_InProduction` kiểm cả `/openapi/v1.json` lẫn `/scalar`; `OpenApiDocument_IsServed_InDevelopment`), sửa profile chạy ở điểm 5, gỡ chú thích *"pha có Swagger"*.
   - **Lượt 2, và lượt 3 nếu một lượt không đủ** — khai `[ProducesResponseType]` cho mọi action của Core theo từng nhóm controller, theo khuôn ở [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §8.2. ArchTest `EveryAction_DeclaresSuccessAndErrorResponseTypes` cùng meta-test T6 vào **lượt khai cuối cùng**, khi không còn action nào thiếu.
5. **Profile chạy từ IDE:** chỉ còn `https://localhost:7100`; `launchBrowser: true` kèm `launchUrl: "scalar"`. Gỡ OpenAPI sau này thì đổi về `launchBrowser: false` — không mở gốc API.

ADR này **không** đổi allowlist origin. Thử lệnh ghi từ trang Scalar vì thế bị lớp 1 từ chối — xem *Hệ quả tiêu cực*.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Swashbuckle, giao diện Swagger UI

**Được:** quen thuộc nhất, đúng chữ *"Swagger"* người dùng dùng; nhiều ví dụ trên mạng.
**Mất:** một bộ sinh tài liệu thứ hai ngoài bộ của ASP.NET Core, phát hành theo nhịp riêng; từ .NET 9 khuôn mẫu webapi không còn dùng nó.
**Vì sao loại:** `Microsoft.AspNetCore.OpenApi` là bộ sinh của chính ASP.NET Core, cùng số phiên bản với framework — nâng framework là nâng nó, không có hai nhịp lệch nhau. Người dùng chọn Scalar làm giao diện.

### Phương án B — Bộ sinh của ASP.NET Core, giao diện Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`)

**Được:** giữ bộ sinh của framework, giao diện quen mặt.
**Mất:** vẫn một gói bên thứ ba cho giao diện, ngang Scalar.
**Vì sao loại:** người dùng chọn Scalar ngày 2026-10-06. Hai lựa chọn giao diện tương đương về chi phí; đổi qua lại là một gói và một lời gọi.

### Phương án C — Chỉ tệp `.http`, không thêm gói

**Được:** không phụ thuộc mới; Visual Studio và Rider chạy được.
**Mất:** mọi request phải viết tay; không có trang liệt kê endpoint và kiểu dữ liệu.
**Vì sao loại:** không đáp ứng nhu cầu người dùng nêu — xem và thử API từ một trang có sẵn.

### Phương án D — Đặt OpenAPI ở host thay vì `Core.Web`

**Được:** Core không mang lựa chọn giao diện; mỗi dự án tự chọn.
**Mất:** host phải chứa logic theo môi trường — trái [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3 (host mỏng, luật A7); S29 thành thứ mỗi dự án tự nhớ, và dự án lấy Core sang không có sẵn tài liệu cho chính các endpoint của Core.
**Vì sao loại:** OpenAPI là hạ tầng của đường vào HTTP, cùng loại với CORS và rate limit — qua phép thử [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.1: mọi sản phẩm có API đều cần. Ngưỡng hai module ở §4.2 đếm code nâng từ module lên; ở đây người cần là chính các controller của Core, có thật hôm nay.

### Phương án E — Bật cả ở Production, sau xác thực

**Được:** bên tích hợp ngoài xem được tài liệu của bản cài thật.
**Mất:** thêm bề mặt ở môi trường thật; phải chốt ai được xem — một khoá quyền mới.
**Vì sao loại:** người dùng chọn chỉ Development. Chưa có bên tích hợp ngoài nào.

### Phương án F — Đặt tài liệu dưới `/api` để giữ §8.1 không thêm ngoại lệ

**Được:** câu *"không có ca thứ ba"* giữ nguyên chữ.
**Mất:** tài liệu phủ cả endpoint của module nên không thuộc khu `core`, mà `/api/v1/<khu>` đòi một khu; trang Scalar là HTML, vẫn phải nằm ngoài `/api`.
**Vì sao loại:** không xoá được ngoại lệ — chỉ dời nó từ hai đường sang một đường, và đổi lại một đường tài liệu trái khuôn khu.

### Phương án G — Thi công R11 trọn trong lượt thêm gói, như ô *Ép bằng gì* của R11 hôm nay

**Được:** không có quãng nào luật R11 đã áp mà cổng chưa canh.
**Mất:** một lượt phải khai kiểu cho mọi action của Core cùng lúc với nối gói và viết test S29 — vượt cỡ một lượt 30–40 phút ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §11).
**Vì sao loại:** vì mọi action trả `IActionResult`, action chưa khai chỉ làm tài liệu **thiếu** schema, không làm tài liệu sai schema — quãng giữa hai lượt không sinh thông tin sai cho người đọc.

## Hệ quả

### Tích cực

- Người dùng mở BE từ IDE là thấy ngay trang liệt kê mọi endpoint của Core và của module đã lắp, thay cho trang 404.
- S29 có cổng từ lượt đầu; tài liệu không thể lộ ra ở bản cài thật trừ khi cặp test đỏ.
- Bộ sinh tài liệu đi cùng nhịp nâng ASP.NET Core; không thêm nhịp phát hành thứ hai cho phần sinh.
- Cổng gợi ý D18 (so card `contracts/` với route thật, sinh từ OpenAPI) có được đối tượng thật để so — dựng hay không vẫn theo [ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md).

### Tiêu cực

- **Thử lệnh ghi từ trang Scalar không chạy.** Trang nằm ở origin của API (`https://localhost:7100`), không có trong allowlist máy dev, nên mọi `POST`/`PUT`/`PATCH`/`DELETE` — kể cả đăng nhập — ra 403 `CORE.AUTH.ORIGIN_REJECTED`; qua được lớp 1 thì còn phải dán `X-XSRF-TOKEN` tay. Thử endpoint đọc cần phiên: đăng nhập qua FE trong cùng trình duyệt trước. Muốn thử lệnh ghi trong Scalar thì phải thêm origin của API vào allowlist ở Development — một quyết định riêng về chuỗi chống giả mạo, chưa ai chốt.
- **Core mang theo một lựa chọn giao diện và hai gói ngoài.** Dự án hạ nguồn muốn Swagger UI phải sửa `Core.Web` — một thay đổi Core, đi qua review Core.
- **Scalar là gói bên thứ ba phát hành dày.** Ghim chính xác nghĩa là tụt bản dần; nâng là việc tay, và mỗi lần nâng phải rà lại xem bản mới có thêm lời gọi ra ngoài nào không — bản đang chọn đã bật sẵn hai lời gọi như vậy.
- **Quãng giữa lượt 1 và lượt R11 cuối, tài liệu không có schema phản hồi**, và luật R11 đã áp mà chưa cổng nào canh — chỉ review thấy.
- **§8.1 thêm một ngoại lệ có tên.** Chỉ ở Development, nhưng người đọc luật tiền tố giờ phải nhớ hai ngoại lệ thay vì một.
- **Mất cổng http của profile.** Ai đang gọi BE bằng `http://localhost:49543` — công cụ, script riêng — phải đổi sang https.

### Rút lui nếu sai

Không sinh dữ liệu nào. Gỡ trong một lượt: bỏ hai dòng `PackageVersion` và tham chiếu ở `Core.Web`, bỏ hai lời gọi ở `AddCoreWeb`/`UseCoreAsync`, bỏ cặp test S29, đổi profile về `launchBrowser: false`. Các `[ProducesResponseType]` đã khai giữ lại được — chúng là metadata của MVC, không cần gói nào; ArchTest R11 giữ hay gỡ theo câu đầu của luật R11 (*"Khi `src/BE` có OpenAPI"*). Đổi giao diện sang Swagger UI: thay một gói, một lời gọi map, một `launchUrl`.

### Dấu hiệu quyết định này bắt đầu sai

- Người dùng thường xuyên cần thử lệnh ghi từ trang tài liệu → mở lại câu allowlist origin ở Development.
- Có bên tích hợp ngoài cần đọc tài liệu của bản cài thật → xét lại nhánh *sau xác thực* của S29.
- Dự án hạ nguồn đầu tiên thay giao diện tài liệu → lựa chọn giao diện thuộc dự án, không thuộc Core.
- Scalar đổi giấy phép, ngừng phát hành dòng cho .NET, hoặc thêm lời gọi ra ngoài không tắt được.

## Liên quan

- Câu luật và khuôn khai kiểu: [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §8.1, §8.2
- Luật S29, R11: [`../RULES-BE.md`](../RULES-BE.md)
- Host mỏng: [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3
- Vị trí trong lộ trình: [`../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) §6
- FE và API khác origin: [ADR-0015](0015-fe-va-api-khac-nguon.md)
- Header bảo mật: [ADR-0120](0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md) — CSP của origin API do Caddy đặt ở bản cài, không có ở máy dev; middleware `Core.Web` đặt `nosniff` và `no-store` trên mọi phản hồi, kể cả trang Scalar. Test `OpenApiDocument_IsServed_InDevelopment` chạy qua middleware đó
