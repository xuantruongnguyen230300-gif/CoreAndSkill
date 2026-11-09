---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0115 — Bỏ ô mã đơn vị: FE suy đơn vị từ tên miền con và vẫn gửi `tenantCode`; phiên chỉ dùng được từ origin của chính đơn vị mình

> **Trạng thái:** Đã loại bỏ (2026-10-06)
>
> Đề xuất ngày 2026-10-05, không được chấp nhận. Lý do: người dùng cho biết mô hình triển khai là **mỗi công ty một bản cài**, nên không cần tên miền con hay wildcard. Hướng được chọn thay: [ADR-0116](0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md). Nội dung dưới đây giữ nguyên làm bản ghi — phần phân tích tên miền con, cookie, CORS và máy dev là điểm xuất phát nếu sau này một bản cài phục vụ nhiều đơn vị.
>
> **Nếu được chấp nhận:** lật hàng *"Suy tenant từ tên miền / subdomain"* ở `docs/wiki-core/be/17-multi-tenant.md:355` (tệp luật, không phải ADR — sửa thẳng); **sửa một phần** [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) §6 (cách tài khoản vận hành chỉ ra đơn vị hệ thống); **bổ sung** [ADR-0015](0015-fe-va-api-khac-nguon.md) (FE thành nhiều origin, một origin mỗi đơn vị). [ADR-0013](0013-multi-tenant.md), [ADR-0017](0017-khu-quan-tri-he-thong.md), [ADR-0029](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) và hướng khoá `K01` (không màn chọn đơn vị, không đổi đơn vị giữa phiên) giữ nguyên.

## Bối cảnh

### Hiện trạng ngày 2026-10-05

| Chỗ | Nói gì | Neo |
| --- | --- | --- |
| Luồng đăng nhập | Form ba ô: mã đơn vị, tên đăng nhập, mật khẩu | `docs/luong/D1-dang-nhap.md:17` |
| Hợp đồng đăng nhập | `tenantCode` bắt buộc trong thân `POST /api/v1/core/auth/login`; chỉ là điều kiện tra cứu trước khi xác thực | `docs/contracts/auth.md:154` |
| Chống dò đơn vị | Sai mã đơn vị, đơn vị ngưng, sai tên, sai mật khẩu: một mã lỗi — để người ngoài không biết cơ quan nào đang dùng hệ thống | `docs/contracts/auth.md:290` |
| Phương án tên miền | Bị loại với hai lý do: thêm cơ quan thành thao tác hạ tầng (trái `docs/adr/0013-multi-tenant.md:85`), và `Host` là dữ liệu client gửi nên cần allowlist phía server | `docs/wiki-core/be/17-multi-tenant.md:355` · điều kiện mở lại đã ghi sẵn ở `docs/wiki-core/be/17-multi-tenant.md:438` |
| Ngoại lệ M2 | Bước đăng nhập là chỗ duy nhất đơn vị đến từ dữ liệu người dùng nhập, và chỉ là điều kiện tra cứu | `docs/wiki-core/be/17-multi-tenant.md:172` |
| Mô hình origin | FE và API hai origin; prod là hai subdomain cùng tên miền gốc, cookie `SameSite=Lax` | `docs/adr/0015-fe-va-api-khac-nguon.md:26` · `docs/wiki-core/fe/17-phuc-vu-va-trien-khai.md:93` |
| Điều kiện cookie | Cùng tên miền gốc và cùng scheme; lệch là ADR mới | `docs/quy-uoc/be-api-controller.md:448` |
| Tài khoản vận hành | Gõ mã đơn vị hệ thống vào ô mã đơn vị | `docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md:94` |

Code, neo bằng chuỗi tìm được:

- CORS lấy danh sách origin chính xác: `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`, chuỗi `.WithOrigins(authOptions.Value.AllowedOrigins)`. Lớp 1 của antiforgery so khớp chính xác từng mục: `src/BE/Core/CoreAndSkill.Core.Web/Security/AntiforgeryValidationMiddleware.cs`, chuỗi `string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase)`.
- Cookie phiên không đặt `Domain`, nên chỉ thuộc host của API; `SameSite` là Lax — cùng tệp DI, chuỗi `options.Cookie.SameSite = SameSiteMode.Lax;`. Phép kiểm phiên ở mọi request đã tra đơn vị của phiên — chuỗi `var tenant = await tenantLookup.FindByIdAsync(tenantId, ct);`; bản ghi trả về mang `Code` (`src/BE/Core/CoreAndSkill.Core.Application/Tenants/ITenantLookup.cs`, chuỗi `TenantSummary(Guid Id, string Code`). Phiên hỏng thì đăng xuất luôn — chuỗi `await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);`.
- Khuôn mã đơn vị cho phép `_` và `-` cuối: `src/BE/Core/CoreAndSkill.Core.Application/Tenants/CreateTenantCommandValidator.cs`, chuỗi `[GeneratedRegex(@"^[A-Z0-9][A-Z0-9_-]{0,49}$")]`; FE lặp lại ở `src/FE/src/app/platform/he-thong/don-vi/state/tao-don-vi.store.ts`, chuỗi `const MA_DON_VI_PATTERN`. Đường bootstrap **không** kiểm khuôn: `src/BE/Core/CoreAndSkill.Core.Application/Configuration/CoreBootstrapOptions.cs` chỉ có `[Required(AllowEmptyStrings = false)]` trên `SystemTenantCode`, và `src/BE/Core/CoreAndSkill.Core.Infrastructure/Tenants/TenantProvisioningService.cs` chỉ chuẩn hoá (`input.Code.Trim().ToUpperInvariant()`).
- Máy dev: API ở `src/BE/CoreAndSkill.Api/Properties/launchSettings.json`, chuỗi `"applicationUrl": "https://localhost:7100;`; FE trỏ tới đó ở `src/FE/src/environments/environment.development.ts`, chuỗi `apiBaseUrl: 'https://localhost:7100/api/v1'`; allowlist dev ở `src/BE/CoreAndSkill.Api/appsettings.Development.json`, chuỗi `"AllowedOrigins": ["https://localhost:4200"]`. Bản cài: `docker-compose.yml`, chuỗi `Core__Auth__AllowedOrigins__0: ${CORE_AUTH_ALLOWED_ORIGIN}`.
- Chứng chỉ dev mà máy chủ dev của FE dùng (thư mục chứng chỉ của FE bị gitignore — mỗi máy tự xuất từ chứng chỉ dev .NET; máy này SDK 10.0.400) mang SAN `localhost`, `*.dev.localhost`, `*.dev.internal`, `host.docker.internal`, `host.containers.internal` — đọc 2026-10-05 bằng `openssl x509 -noout -ext subjectAltName`. **Không** có `dev.localhost` trần.
- FE báo phiên kết thúc cho tab khác qua `localStorage` — chỉ chạy trong cùng một origin: `src/FE/src/app/core/auth/tab-session-broadcast.service.ts`, chuỗi `localStorage.setItem(KHOA_BAO_TAB_PHIEN_KET_THUC, String(Date.now()));`.
- Chưa có bản cài thật: `src/FE/src/environments/environment.ts` còn placeholder `apiBaseUrl: 'https://api.core-and-skill.invalid/api/v1'`. Theo tài liệu, không môi trường nào giữ dữ liệu đơn vị thật — người dùng xác nhận điều này trước khi chấp nhận (quyết định con 1).

### Cái gì đã đổi so với lúc loại phương án tên miền

1. **Ưu tiên của người dùng.** Người dùng chọn bỏ thao tác gõ mã đơn vị và muốn mỗi đơn vị một địa chỉ riêng. Đây là ưu tiên sản phẩm, **không phải vấn đề đã đo** — chưa có người dùng thật nào.
2. **Lý do "thêm cơ quan thành thao tác hạ tầng" trả lời được** bằng wildcard DNS cộng wildcard cert: thao tác hạ tầng xảy ra một lần cho cả vùng, không lặp theo đơn vị. ADR này biến điều đó thành **điều kiện tiên quyết** (quyết định con 6) — không làm được thì ADR này không nên được chấp nhận.
3. **Lý do "`Host` là dữ liệu client gửi" trả lời được** bằng một bất biến: `Host` không bao giờ được đọc; `Origin` không bao giờ là **nguồn** của đơn vị, chỉ là **lý do để từ chối**. Một giá trị chỉ dùng để từ chối thì giả mạo nó không mở thêm được gì.

## Quyết định

Người dùng chọn (2026-10-05) bỏ ô mã đơn vị trên form đăng nhập và suy đơn vị từ tên miền con. Kiến trúc sư đề xuất hiện thực hướng đó bằng sáu điểm dưới đây; điểm nào gắn với một quyết định con *cần người dùng chốt* thì đang mang phương án kiến trúc sư đề xuất cho câu đó.

1. **Mỗi đơn vị một địa chỉ `https://<nhãn>.<vùng đơn vị>`, nhãn là mã đơn vị viết thường.** Form đăng nhập còn hai ô. FE đọc `location.hostname`, bóc đúng một nhãn trước hậu tố vùng, viết hoa, rồi gửi làm `tenantCode` — thân `POST /api/v1/core/auth/login` **không đổi**.
2. **`Origin` chỉ dùng để từ chối, không bao giờ để chọn đơn vị.** Hai chỗ áp: (a) request mang `Origin` thuộc vùng có nhãn X mà phiên thuộc đơn vị Y ≠ X ⇒ phiên **coi như không có** ở request đó — không đăng xuất, không gia hạn, không xoá cookie; (b) đăng nhập từ origin nhãn X với `tenantCode` khác X ⇒ trượt bằng `CORE.AUTH.INVALID_CREDENTIALS`, đi qua sàn thời gian chung ([ADR-0058](0058-dang-nhap-truot-cho-du-mot-san-thoi-gian.md)). Request không mang `Origin` (công cụ, test) không bị ràng buộc — như hôm nay.
3. **CORS và lớp 1 antiforgery dùng chung một mẫu origin khai ở cấu hình**, dạng `https://*.<vùng>[:cổng]`: scheme và cổng khớp đúng, host kết thúc bằng `.` + vùng, phần trước đúng **một** nhãn khớp khuôn nhãn ở điểm 4. Không còn danh sách origin chính xác. Origin của API không bao giờ khớp mẫu.
4. **Mã đơn vị thu hẹp về khuôn dùng được làm nhãn DNS:** chữ HOA `A`–`Z`, chữ số, gạch ngang đơn nằm giữa hai ký tự chữ-số; không `_`, không `-` đầu hay cuối, không `--`; tối đa 50 ký tự. Lệnh bootstrap kiểm cùng khuôn.
5. **Vùng đơn vị chỉ chứa FE của các đơn vị**; API nằm ngoài vùng nhưng cùng tên miền đăng ký, để cookie `SameSite=Lax` của API vẫn là cùng site với mọi FE đơn vị. Vùng không được là một hậu tố công khai.
6. **Điều kiện tiên quyết:** vùng có wildcard DNS và wildcard cert. Thêm đơn vị vẫn là một dòng dữ liệu.

Ví dụ minh hoạ (tên miền giả): vùng `dv.ten-mien.example`, FE đơn vị `https://so-gd.dv.ten-mien.example`, API `https://api.ten-mien.example`. Tên vùng thật do từng bản cài khai qua cấu hình; Core không cố định nó.

## Quyết định con

Mỗi câu: phương án, cái giá, đề xuất. Nhãn **[architect]** = đề xuất của kiến trúc sư, người dùng lật được; **[người dùng chốt]** = phụ thuộc ưu tiên hay hạ tầng của người dùng, kiến trúc sư không chốt thay.

### QC1 — Khuôn tên miền và ánh xạ mã ↔ nhãn DNS · [người dùng chốt]

Khuôn mã hôm nay (`docs/contracts/tenants.md:99`) lệch luật nhãn DNS ở ba chỗ: `_` không hợp lệ trong tên máy, `-` đầu/cuối không hợp lệ, `--` ở vị trí 3–4 là tiền tố tên miền quốc tế (`xn--`) mà trình duyệt hiển thị thành chữ Unicode. Chữ hoa/thường không phải vấn đề: DNS không phân biệt, trình duyệt luôn gửi chữ thường.

| Phương án | Được | Mất |
| --- | --- | --- |
| **1A — nhãn = mã viết thường; thu hẹp khuôn mã** (điểm 4) | Một định danh duy nhất; không cột mới, không migration — `core.tenant.code` không có ràng buộc `CHECK` nào, khuôn chỉ nằm ở validator | Mã có `_` hay `-` cuối không tạo được nữa; mã đã có mà sai khuôn thì không có địa chỉ |
| 1B — cột `subdomain` riêng | Giữ khuôn mã cũ; đổi địa chỉ không đổi mã | Hai định danh cho một đơn vị; cột, unique, migration, trường mới ở form tạo đơn vị và ở hợp đồng; người vận hành phải hiểu hai thứ |
| 1C — ánh xạ biến đổi (`_` → `-`) | Giữ khuôn cũ, không cột mới | `A_B` và `A-B` cùng một địa chỉ — va chạm. Loại |

**Đề xuất: 1A.** Chưa có dữ liệu thật, nên thu hẹp lúc này gần như miễn phí; sau khi có dữ liệu thì không. Trước khi chấp nhận, người dùng chạy trên mọi database đang có (agent không chạm database): `SELECT code FROM core.tenant WHERE code !~ '^[A-Z0-9]+(-[A-Z0-9]+)*$';` — kết quả phải rỗng. Tên vùng (`dv.` hay tên khác) do người dùng chọn cho từng bản cài.

### QC2 — Đơn vị hệ thống đăng nhập ở đâu · [người dùng chốt]

| Phương án | Được | Mất |
| --- | --- | --- |
| **2A — như mọi đơn vị: `https://<mã hệ thống viết thường>.<vùng>`** | Không nhánh riêng; mã vẫn đặt qua cấu hình như [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) §6; ràng buộc ở điểm 2 áp cho cả phiên vận hành | Ai đoán được mã hệ thống thì thấy form đăng nhập vận hành — ngang hôm nay, khi người đó gõ mã vào ô |
| 2B — một host riêng ngoài vùng (ví dụ `quantri.<tên miền>`) ánh xạ cố định về đơn vị hệ thống | Có thể chặn theo IP ở tầng phục vụ FE | Chặn ở host FE **không** che được API dùng chung — muốn thật sự chặn phải thêm luật IP phía API cho đăng nhập vận hành, là một tính năng mới. Bộ khớp origin có thêm một loại ánh xạ thứ hai |
| 2C — tên miền gốc của vùng trỏ về đơn vị hệ thống | Dễ nhớ | Trang mặc định của hệ thống thành form đăng nhập vận hành. Loại |

**Đề xuất: 2A.** Lợi ích của 2B chỉ có thật khi kèm một tính năng chặn phía API mà chưa ai yêu cầu.

### QC3 — Ai suy đơn vị · [architect]

| Phương án | An toàn so với hôm nay | Chi phí |
| --- | --- | --- |
| **3a — FE đọc `location.hostname`, vẫn gửi `tenantCode`; BE kiểm khớp với nhãn của `Origin` (điểm 2b)** | Ngang hôm nay: `tenantCode` vẫn là điều kiện tra cứu do client gửi, không cấp quyền gì. Kiểm khớp thêm một lớp: trình duyệt ở origin X không đăng nhập được vào đơn vị khác X | Hợp đồng, ngoại lệ M2, khoá giới hạn tần suất theo cặp mã + tên, và mọi test đăng nhập giữ nguyên. Thêm: một hàm suy mã ở FE, một phép so ở đăng nhập |
| 3b — BE suy từ `Origin`, bỏ `tenantCode` khỏi thân | Không hơn 3a: `Origin` do trình duyệt đặt nhưng công cụ ngoài trình duyệt đặt tuỳ ý — đúng như `tenantCode` | Đổi hợp đồng; viết lại ngoại lệ đăng nhập của M2 và khoá giới hạn tần suất; client không phải trình duyệt (test, ứng dụng khác sau này) phải giả một header bảo mật để chọn đơn vị; mọi test đăng nhập đổi |
| 3c — BE suy từ `Host` | — | Với API dùng chung, `Host` là host của API, không mang đơn vị; chỉ có nghĩa với phương án 4B. Đọc `Host` chính là điều §11.1 lo |

**Đề xuất: 3a.** Lời *"BE gần như không đổi"* của 3a **không đúng trọn**: BE vẫn phải đổi CORS và lớp 1 antiforgery cho N origin (QC5) bất kể chọn gì, và đổi phép kiểm phiên (QC4). 3a chỉ giữ nguyên **hợp đồng đăng nhập** — và đó là thứ làm đường lùi rẻ.

### QC4 — Một tên miền API dùng chung hay theo đơn vị; cookie phiên · [người dùng chốt]

Với một API dùng chung, cookie phiên thuộc host của API, nên trình duyệt gửi **cùng một** cookie dù trang đang mở là của đơn vị nào. Không có biện pháp gì thì người đã đăng nhập đơn vị A mở trang đơn vị B sẽ thấy B dựng giao diện bằng phiên của A; tệ hơn, tab A cũ sau khi người dùng đăng nhập B sẽ gửi request bằng phiên B — dữ liệu ghi nhầm đơn vị.

| Phương án | Được | Mất |
| --- | --- | --- |
| **4A — API chung, một cookie, ràng buộc phiên–origin phía server (điểm 2a)** | Phiên A vô hình ở trang B; tab A cũ sau khi đăng nhập B nhận 401 và về màn đăng nhập — hỏng về phía an toàn. Không DNS, cert hay cấu hình FE nào thêm. Phép kiểm dùng bản ghi đơn vị mà phép kiểm phiên đã tra sẵn — không thêm truy vấn | **Một phiên mỗi hồ sơ trình duyệt**: đăng nhập B xoá ngầm khả năng dùng phiên A (không chia sẻ, nhưng mất). Người làm cho hai đơn vị phải dùng hai hồ sơ trình duyệt |
| 4B — API theo đơn vị (`<nhãn>.<vùng API>`), cookie tách tự nhiên | Nhiều phiên song song; trình duyệt tự cách ly cookie | Vùng wildcard thứ hai cùng cert; `apiBaseUrl` của FE thành mẫu suy lúc chạy (đụng chốt *nhúng lúc build* ở `docs/wiki-core/fe/17-phuc-vu-va-trien-khai.md` §5.2); CSP `connect-src` thành wildcard; API phải đọc `Host` và vẫn phải kiểm `Origin` ↔ `Host` ↔ claim — nhiều phép kiểm hơn, không ít hơn |
| 4C — API chung, tên cookie theo mã đơn vị, BE chọn cookie theo `Origin` | Nhiều phiên song song, một host API | Tự viết bộ chọn cookie nằm đúng trên đường nhạy cảm nhất; cookie chống giả mạo cũng phải tách; số cookie trên host API tăng theo số đơn vị đã ghé; chọn nhầm cookie chính là lỗi xuyên đơn vị đang muốn chặn |
| 4D — cùng origin mỗi đơn vị qua reverse proxy (`<nhãn>.<vùng>/api`) | Cookie tách tự nhiên, không CORS | Lật [ADR-0015](0015-fe-va-api-khac-nguon.md): proxy ở mọi môi trường kể cả máy dev — đúng cái giá ADR đó đã từ chối |

**Đề xuất: 4A.** Nó khớp tiền đề một tài khoản một đơn vị và không đổi đơn vị giữa phiên của ADR-0013 (`docs/adr/0013-multi-tenant.md:23`). Hai chi tiết thi công bắt buộc: phiên lệch origin **không** gọi đăng xuất (nếu gọi, chỉ cần mở trang B là mất phiên A), và **không** gia hạn cookie. Điều 4A đóng thêm, ngoài phạm vi câu hỏi: hôm nay với một origin, tab cũ dùng phiên mới của đơn vị khác cũng xảy ra (đăng xuất rồi đăng nhập đơn vị khác ở tab kia) và BE không chặn được; với 4A thì chặn được.

### QC5 — Allowlist cho CORS và kiểm `Origin` · [architect]

| Phương án | Được | Mất |
| --- | --- | --- |
| **5A — một mẫu có cấu trúc (điểm 3), khớp chặt, cộng ràng buộc điểm 2** | Không truy vấn database ở CORS; không lộ đơn vị nào tồn tại; thêm đơn vị không đụng cấu hình | Mọi nhãn dưới vùng được tin, kể cả nhãn không có đơn vị. Ràng buộc điểm 2 giới hạn thiệt hại: một origin lạ dưới vùng chỉ dùng được phiên của đơn vị **trùng nhãn** với nó |
| 5B — allowlist lấy từ bảng đơn vị (nhãn phải là một đơn vị đang hoạt động) | Origin không có đơn vị bị chặn sớm | CORS trả lời khác nhau cho mã có và không có ⇒ **công cụ dò đơn vị**, ngược chủ đích ở `docs/contracts/auth.md:290`. Truy vấn hoặc cache trên mọi preflight. Lợi ích thêm gần như không có, vì mọi nhãn đều do cùng một tầng phục vụ FE trả về |
| 5C — danh sách chính xác, thêm một dòng mỗi đơn vị | Hẹp nhất | Thêm đơn vị = sửa cấu hình + khởi động lại API — đúng lý do §11.1 từng loại |
| 5D — biểu thức chính quy tuỳ ý trong cấu hình | Linh hoạt | Bẫy kinh điển: quên neo hay quên thoát dấu chấm thì `so-gd.dv.ten-mien.example.ke-tan-cong.example` lọt. Loại |
| 5E — giữ danh sách chính xác **bên cạnh** mẫu | Thêm được origin lẻ cho công cụ | Origin trong danh sách chính xác không mang nhãn ⇒ ràng buộc điểm 2 không áp ⇒ phiên mọi đơn vị dùng được từ đó. Loại, trừ khi QC10 chọn 10B |

**Đề xuất: 5A.** Kiến trúc sư **không** nhận 5B và 5D. Luật *"không phản hồi lại `Origin` client gửi"* (`docs/quy-uoc/be-api-controller.md:500`) phải viết lại thành *"chỉ phản hồi origin đã qua phép khớp mẫu"* — CORS theo mẫu luôn phản hồi lại origin, và câu cũ đọc theo chữ sẽ cấm chính phương án này. Bộ khớp có danh sách ca thù địch bắt buộc trong test: hậu tố bị nối thêm, thiếu dấu chấm trước vùng, hai nhãn, `http`, cổng lệch, nhãn bắt đầu hay kết thúc bằng `-`, nhãn `xn--…`, chính tên vùng trần, `Origin: null`.

### QC6 — Thêm đơn vị còn là một dòng dữ liệu không · [người dùng chốt]

| Phương án | Được | Mất |
| --- | --- | --- |
| **6A — wildcard DNS `*.<vùng>` + wildcard cert `*.<vùng>`** | Thêm đơn vị = một dòng dữ liệu. Cert wildcard không ghi tên từng đơn vị vào nhật ký Certificate Transparency — danh sách đơn vị không lộ ra công khai | Gia hạn cert mãi mãi. Cấp cert wildcard qua ACME đòi xác thực DNS-01 ⇒ khoá API của nhà cung cấp DNS nằm trên máy chủ — một bí mật mới phải giữ; hoặc mua cert năm. Wildcard chỉ phủ **một** tầng nhãn — khớp luật một nhãn ở điểm 3; tên vùng trần cần SAN riêng nếu QC7 dùng nó |
| 6B — wildcard DNS + cert cấp theo từng host lúc chạy | Không cần cert wildcard | Mỗi đơn vị một cert ⇒ **mã mọi đơn vị lên nhật ký Certificate Transparency công khai** — đúng danh sách mà việc gộp mã lỗi đăng nhập đang che. Kẻ lạ kích hoạt cấp cert cho nhãn ngẫu nhiên. Loại |
| 6C — bản ghi DNS + cert từng đơn vị, làm tay | Không phụ thuộc hạ tầng wildcard | Thêm đơn vị thành một yêu cầu hạ tầng — chính lý do §11.1 loại phương án tên miền |

**Đề xuất: 6A — và là điều kiện để chấp nhận ADR.** Chưa biết: tên miền thật của bản cài là gì, người giữ DNS có cho bản ghi wildcard dưới một vùng con không, và nơi cấp cert có cấp wildcard không (với tên miền `.gov.vn` thì phụ thuộc đơn vị quản lý tên miền). Người dùng xác nhận ba điều đó. Không đạt thì quay lại phương án B ở mục dưới, không chuyển sang 6C.

### QC7 — Mở tên miền gốc của vùng, không có nhãn · [người dùng chốt]

| Phương án | Được | Mất |
| --- | --- | --- |
| **7A — trang một ô "mã đơn vị", bấm thì chuyển tới `https://<mã>.<vùng>`; không gọi API, không tra mã** | Người quên địa chỉ vẫn tới được; không lộ đơn vị nào tồn tại (mã sai cũng chuyển, rồi trượt ở form đăng nhập bằng câu chung) | Một màn nữa trong bản build FE; FE phải biết không khởi tạo phần gọi API ở host này (origin trần không khớp mẫu nên mọi lời gọi API bị CORS chặn, kể cả báo lỗi về server); cert phải phủ tên vùng trần. Ô mã đơn vị vẫn tồn tại — ở một trang phụ, không ở form đăng nhập |
| 7B — trang tĩnh "dùng địa chỉ đơn vị của bạn" | Rẻ nhất trong các phương án có trang | Ngõ cụt cho người quên địa chỉ |
| 7C — chuyển tới đơn vị hệ thống | — | Như 2C. Loại |
| 7D — không có bản ghi DNS cho tên trần | Không làm gì | Trình duyệt báo lỗi phân giải tên — không ai hiểu |

**Đề xuất: 7A.**

### QC8 — Máy dev · [architect, kèm thử nghiệm bắt buộc trước khi chấp nhận]

Phương án dễ nghĩ tới nhất — FE ở `https://demo.dev.localhost:4200`, API giữ `https://localhost:7100` — **theo suy luận là hỏng đăng nhập**. *Same site* so tên miền đăng ký, tính bằng Public Suffix List với quy tắc mặc định coi nhãn cuối không có trong danh sách là hậu tố công khai: `demo.dev.localhost` có tên miền đăng ký `dev.localhost`, còn `localhost` không có tên miền đăng ký. Hai bên khác site ⇒ cookie `SameSite=Lax` của API không đi theo `fetch` từ FE ⇒ đăng nhập trả 200 rồi mọi request sau đó 401. Kiến trúc sư suy ra từ định nghĩa, **chưa thử** trên trình duyệt.

| Phương án | Được | Mất |
| --- | --- | --- |
| **8A — FE `https://<mã>.dev.localhost:4200`, API `https://api.dev.localhost:7100`; mẫu origin dev `https://*.dev.localhost:4200`** | Chrome và Edge tự phân giải `*.localhost` về máy; chứng chỉ dev .NET đang dùng phủ cả hai host bằng SAN `*.dev.localhost`; FE và API cùng tên miền đăng ký `dev.localhost`; không công cụ thêm | Ở dev, API nằm **trong** vùng đơn vị — origin của API vẫn không khớp mẫu vì khác cổng, nhưng dev không mô phỏng được bố cục *API ngoài vùng* của bản thật. Máy có chứng chỉ dev cũ thiếu SAN `*.dev.localhost` phải tạo lại chứng chỉ và xuất lại cho FE. Công cụ ngoài trình duyệt trên Windows (curl) không tự phân giải `*.localhost` |
| 8B — tên miền dev tuỳ chọn trong tệp hosts + chứng chỉ tự tạo | Bố cục y bản thật | Mỗi máy sửa tệp hosts cho từng đơn vị (Windows không có wildcard trong tệp hosts) và cài công cụ tạo chứng chỉ — ma sát cho mọi người mới |
| 8C — dev giữ form ba ô ở `localhost:4200` | Không đổi gì ở dev | Dev khác thật đúng ở chỗ hay hỏng nhất — trái chủ đích của [ADR-0015](0015-fe-va-api-khac-nguon.md). Loại |

**Đề xuất: 8A.** Cấu hình kéo theo: `src/BE/CoreAndSkill.Api/appsettings.Development.json` khai mẫu origin dev; `src/FE/src/environments/environment.development.ts` đổi `apiBaseUrl` sang host `api.dev.localhost` và thêm hậu tố vùng `dev.localhost`; `applicationUrl` ở `launchSettings.json` giữ `localhost` (Kestrel nghe trên loopback, trình duyệt tới bằng tên) nếu thử nghiệm xác nhận; `src/FE/angular.json` chỉ thêm `allowedHosts` cho `.dev.localhost` nếu máy chủ dev từ chối host lạ. Hai đơn vị dev mở ở `hethong.dev.localhost:4200` và `demo.dev.localhost:4200`.

**Thử nghiệm bắt buộc trước khi chấp nhận** (không sửa code, chỉ sửa cấu hình cục bộ rồi trả lại): với form ba ô hôm nay, thêm tạm `https://demo.dev.localhost:4200` vào allowlist dev, trỏ FE tới `https://api.dev.localhost:7100`, đăng nhập rồi gọi `me` ⇒ phải 200. Lặp lại với FE trỏ tới `https://localhost:7100` ⇒ dự đoán 401. Cùng lần đó kiểm máy chủ dev của Angular nhận host `demo.dev.localhost` và trình duyệt kết nối được cả khi máy chủ dev chỉ nghe IPv4 hoặc IPv6.

### QC9 — Tác động lên tài khoản vận hành, khôi phục xuyên đơn vị, test · [architect]

- **Đăng nhập vận hành:** mở `https://<mã hệ thống>.<vùng>` (QC2); thân đăng nhập mang mã hệ thống; `isSystemOperator` trong phản hồi không đổi. Ràng buộc điểm 2 thêm một lợi ích: một lỗi XSS ở trang của một đơn vị, chạy trong trình duyệt đang giữ phiên vận hành, **không** dùng được phiên đó — nhãn lệch.
- **Khôi phục quản trị đơn vị ([ADR-0029](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md)):** quyết định không đổi. Người vận hành báo cho quản trị đơn vị **địa chỉ** `https://<mã>.<vùng>` cùng mật khẩu tạm — màn danh sách đơn vị có thể hiện địa chỉ, FE tự dựng từ mã và hậu tố vùng, không cần BE. Cái giá đã ghi ở ADR-0029 (người vận hành biết mật khẩu tạm) không đổi; đăng nhập thử bằng mật khẩu đó trên cùng trình duyệt sẽ thay phiên vận hành (QC4).
- **Khôi phục tài khoản vận hành** (`core reset-operator-password`): không đổi. Bước kiểm sau cài ở runbook đổi từ "gõ mã đơn vị hệ thống" sang "mở địa chỉ đơn vị hệ thống".
- **Quên mật khẩu tự phục vụ** (ngoài v1): khi mở lại, thân `forgot-password` vẫn mang `tenantCode`, FE suy cùng cách. Không việc gì lúc này.
- **Test BE:** test gửi `tenantCode` trong thân đăng nhập giữ nguyên. Test đặt `Origin` hay `AllowedOrigins` phải đổi sang origin khớp mẫu — tra bằng `grep -rlE "AllowedOrigins|Headers.Origin|\"Origin\"" src/BE/Tests`. Test mới: bộ khớp với các ca thù địch ở QC5; phiên đơn vị A gọi `me` từ origin đơn vị B ⇒ 401 và **không** có `Set-Cookie` xoá phiên; đăng nhập lệch nhãn ⇒ `CORE.AUTH.INVALID_CREDENTIALS` không sớm hơn sàn; preflight từ origin ngoài mẫu ⇒ không có header cho phép; khuôn mã mới ở validator và ở lệnh bootstrap.
- **Test FE:** spec của trang đăng nhập và `AuthField` (biến thể `tenantCode`); spec mới cho hàm suy mã từ hostname (tên vùng trần, hai nhãn, sai hậu tố, chữ hoa) và cho phép so `tenantCode` của phiên với hostname. Tra bằng `grep -rlE "tenantCode" src/FE/src`.
- **Lỗi kiểm dữ liệu của `TenantCode`** (ví dụ hostname dài bất thường) không còn ô nào để gắn — FE hiện ở mức form.

### QC10 — Core chỉ có chế độ tên miền con, hay giữ cả chế độ ô mã đơn vị · [người dùng chốt]

| Phương án | Được | Mất |
| --- | --- | --- |
| **10A — chỉ tên miền con** | Một đường đăng nhập, một bộ test, ràng buộc phiên áp cho mọi origin được phép | Mọi dự án lấy Core ([ADR-0016](0016-phan-phoi-core-bang-clone.md)) phải có vùng wildcard DNS + cert. Dự án nào không có thì phải đổi Core — ADR mới |
| 10B — hai chế độ, chọn bằng cấu hình | Dự án không có wildcard vẫn dùng Core nguyên | Hai đường đăng nhập; đường không dự án nào dùng hằng ngày sẽ mục mà không ai thấy; ma trận test gấp đôi; chế độ ô mã đơn vị cần danh sách origin chính xác — không mang nhãn — nên ràng buộc phiên không áp được ở chế độ đó. Là một tham số cấu hình phục vụ dự án **chưa tồn tại** |

**Đề xuất: 10A.** Theo ngưỡng chống phình Core: không dựng chế độ thứ hai cho một nhu cầu dự đoán. Khi một dự án hạ nguồn thật sự cần, lúc đó viết ADR với nhu cầu thật của nó. Cái giá về tính mang đi được ghi ở mục *Hệ quả*.

### QC11 — Trang đăng nhập có hiện tên đơn vị không · [người dùng chốt]

Đây là lý do thường gặp nhất để làm tên miền con (thương hiệu từng đơn vị), nên nó sẽ được hỏi.

| Phương án | Được | Mất |
| --- | --- | --- |
| **11A — chỉ hiện mã, đọc từ hostname** | Người dùng biết mình đang ở đơn vị nào; không gọi API, không lộ gì | Không có tên đầy đủ, không logo trước khi đăng nhập |
| 11B — endpoint ẩn danh trả tên, logo theo mã | Trang đăng nhập mang thương hiệu đơn vị | Endpoint trả lời khác nhau cho mã có và không có ⇒ **công cụ dò đơn vị**; thêm một mục vào allowlist `[AllowAnonymous]` (luật S4); ngược lý do gộp mã lỗi ở `docs/contracts/auth.md:290` |
| 11C — không hiện gì | Đơn giản nhất | Người dùng không thấy mình đang ở đơn vị nào cho tới khi đăng nhập |

**Đề xuất: 11A.** Phiên chính lưu ý: màn đăng nhập đang được `design-expert` sửa — chưa nên vẽ tên hay logo đơn vị lên đó trước khi câu này được chốt.

## Phương án đã cân nhắc và vì sao loại

Ngoài các phương án trong từng quyết định con, bốn phương án cho **cả hướng**:

### Phương án A — Giữ ô mã đơn vị như hôm nay

**Được:** không đổi gì; không hạ tầng mới. **Mất:** người dùng gõ mã mỗi lần đăng nhập, đơn vị không có địa chỉ riêng. **Vì sao loại:** người dùng đã chọn bỏ.

### Phương án B — Giữ ô, FE nhớ mã lần trước trong trình duyệt

**Được:** phần lớn người dùng chỉ gõ mã một lần trên mỗi máy; chỉ sửa FE; không DNS, không cert, không đổi BE; lùi lại trong một giờ. Đây là **phương án đơn giản nhất** giải được phần lớn phiền toái. **Mất:** máy dùng chung và trình duyệt mới vẫn phải gõ; đơn vị không có địa chỉ riêng; ô vẫn còn trên form. **Vì sao loại:** người dùng muốn bỏ hẳn ô và muốn địa chỉ riêng. Nếu QC6 không đạt, đây là phương án quay về.

### Phương án C — Địa chỉ theo đường dẫn: `https://<app>/<mã>/…`

**Được:** địa chỉ riêng cho mỗi đơn vị; không wildcard DNS hay cert; CORS giữ một origin. **Mất:** mọi đơn vị chung **một** origin, nên phía server không có tín hiệu nào do trình duyệt đặt để ràng buộc phiên với đơn vị — phiên A dùng được ngay trong trang của B, chặn chỉ còn ở FE. Lưu trữ cục bộ và mật khẩu trình duyệt lưu cũng chung. **Vì sao loại:** mất đúng tính chất mà tên miền con cho miễn phí — mỗi đơn vị một origin do trình duyệt ép.

### Phương án D — Mỗi đơn vị một tên miền tuỳ ý (ví dụ tên miền riêng của từng sở)

**Được:** thương hiệu riêng trọn vẹn. **Mất:** mỗi tên miền một site khác với API ⇒ cookie thành bên thứ ba, `SameSite=None`, trình duyệt có thể chặn — lật điều kiện của [ADR-0015](0015-fe-va-api-khac-nguon.md); mỗi đơn vị một bản ghi DNS và một cert ⇒ thêm đơn vị là thao tác hạ tầng. **Vì sao loại:** vỡ cả hai điều mà hướng này cần giữ.

## Hệ quả

### Tích cực

- Người dùng không gõ mã đơn vị; mỗi đơn vị một địa chỉ đánh dấu, in ra được.
- Trình quản lý mật khẩu của trình duyệt lưu theo origin ⇒ tách theo đơn vị.
- Ràng buộc phiên–origin đóng một lỗ có từ hôm nay: tab cũ gửi request bằng phiên mới của đơn vị khác. Lỗi XSS ở trang một đơn vị không dùng được phiên của đơn vị khác hay phiên vận hành.
- Hợp đồng đăng nhập không đổi ⇒ test đăng nhập, giới hạn tần suất theo cặp mã + tên, ngoại lệ M2 giữ nguyên; đường lùi rẻ.
- Danh sách đơn vị vẫn không lộ: wildcard DNS, wildcard cert, CORS theo mẫu, trang đăng nhập không gọi API đều không trả lời được câu *"mã X có tồn tại không"*.

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Hạ tầng bắt buộc mới, trả mãi** | Wildcard DNS, wildcard cert gia hạn định kỳ, tầng phục vụ FE nhận mọi nhãn dưới vùng. Cấp qua ACME thì khoá API DNS nằm trên máy chủ — một bí mật mới; lộ khoá đó hay khoá riêng của cert wildcard là giả mạo được **mọi** đơn vị cùng lúc |
| **Một phiên mỗi hồ sơ trình duyệt** (4A) | Đăng nhập đơn vị B làm tab đơn vị A về màn đăng nhập. An toàn, nhưng người làm cho hai đơn vị sẽ thấy phiền; báo phiên kết thúc qua `localStorage` không đi xuyên đơn vị, nên tab A chỉ biết ở request kế tiếp |
| **Vùng đơn vị phải giữ sạch, và không cổng nào canh** | Ai thêm một bản ghi DNS khác dưới vùng (trang trạng thái, dịch vụ thuê ngoài) là đưa một origin lạ vào allowlist. Ràng buộc phiên giới hạn thiệt hại về đơn vị trùng nhãn, không xoá nó: origin đó vẫn gọi được endpoint ẩn danh và vẫn đăng nhập-hộ được vào đơn vị trùng nhãn nếu có |
| **Core áp yêu cầu hạ tầng lên mọi dự án hạ nguồn** (10A) | Dự án không có quyền wildcard trên tên miền của mình không dùng được form đăng nhập của Core mà không đổi Core |
| **Thêm nhánh trên đường nhạy cảm nhất** | Bộ khớp origin sai một ký tự là vượt CORS cho cả hệ; phép kiểm phiên có thêm một nhánh từ chối không đăng xuất. Chỉ test ca thù địch và review canh |
| **Mã đơn vị thành công khai và gần như không đổi được** | Mã nằm trong thanh địa chỉ, lịch sử trình duyệt, và trong origin gửi kèm request tới bên thứ ba nếu FE nạp tài nguyên ngoài. Đổi mã là đổi địa chỉ — gãy dấu trang, văn bản đã phát |
| **Khuôn mã hẹp lại** | Không còn `_`, `-` đầu/cuối, `--`. Mã đã có sai khuôn (nếu có) không có địa chỉ |
| **Dev đổi thói quen** | Mở `<mã>.dev.localhost:4200` thay `localhost:4200`; chứng chỉ dev phụ thuộc phiên bản SDK đã tạo nó; trình duyệt không phải Chromium và công cụ dòng lệnh phải thử riêng |
| **Tuỳ chọn lưu trên trình duyệt tách theo đơn vị** | Theme hay trạng thái thanh bên chọn ở đơn vị A không theo sang đơn vị B |
| **Request không mang `Origin` không bị ràng buộc** | Như hôm nay: ai có cookie và dùng công cụ ngoài trình duyệt vẫn dùng được phiên. Ràng buộc là hàng rào cho trình duyệt, không phải cho kẻ đã lấy được cookie |

## Sáu câu hỏi bắt buộc

| # | Câu hỏi | Trả lời | Chắc chắn? |
| --- | --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Người dùng phải gõ mã đơn vị mỗi lần đăng nhập; người dùng muốn mỗi đơn vị một địa chỉ. **Chưa đo** — chưa có người dùng thật; đây là ưu tiên sản phẩm | Không — nói thẳng |
| 2 | Phương án đơn giản hơn | Phương án B (nhớ mã trong trình duyệt) và C (đường dẫn). B không bỏ được ô; C mất tín hiệu origin để ràng buộc phiên | Có |
| 3 | Chi phí vận hành thêm | Wildcard DNS, wildcard cert và gia hạn, cấu hình tầng phục vụ FE cho mọi nhãn, giữ vùng sạch, khai mẫu origin ở mỗi môi trường, dev đổi hostname | Có; riêng chi phí cấp cert wildcard cho tên miền thật **chưa biết** (QC6) |
| 4 | Ai bảo trì | Code: phần Core BE và FE, thi công bởi `backend-expert` và `frontend-expert`, soát bởi `core-reviewer`. DNS và cert: người vận hành của từng bản cài. **Chưa biết** ai trong tổ chức người dùng giữ DNS và cert của tên miền thật | Một nửa |
| 5 | Rút lui thế nào | Mục *Rút lui nếu sai* dưới | Có |
| 6 | Có buộc Core biết nghiệp vụ không | Không: mẫu origin và hậu tố vùng là cấu hình triển khai, Core không biết tên đơn vị nào. Nhưng với 10A, Core áp một **ràng buộc triển khai** lên mọi dự án — đó là cái giá về tính mang đi được, không phải nghiệp vụ lọt vào Core | Có |

## Rút lui nếu sai

1. **FE:** đảo các commit bỏ ô và suy mã (việc F2, F3 ở dưới) — form ba ô quay lại. Hợp đồng đăng nhập không đổi trong suốt quyết định này, nên BE không phải sửa để đăng nhập chạy lại.
2. **BE:** nếu vẫn phục vụ trên tên miền con thì giữ nguyên mẫu origin và ràng buộc phiên — chúng vô hại với form ba ô. Nếu quay về một origin FE: đảo commit của việc B2 và B3 (khôi phục danh sách origin chính xác, gỡ ràng buộc), đổi cấu hình từng môi trường.
3. **Dữ liệu:** khuôn mã mới là **tập con** của khuôn cũ — mọi mã tạo trong thời gian quyết định này còn hiệu lực đều hợp lệ với khuôn cũ. Không migration, không dọn dữ liệu.
4. **Hạ tầng:** DNS và cert wildcard để nguyên được; trỏ thêm tên miền gốc về FE.
5. **Phần không lùi được:** địa chỉ đã phát cho các đơn vị (dấu trang, văn bản). Giữ một quy tắc chuyển hướng ở tầng phục vụ: `https://<mã>.<vùng>/…` ⇒ trang đăng nhập một origin kèm mã điền sẵn, để liên kết cũ không gãy.

Ước lượng: một đến hai ngày công cộng một lần triển khai, nếu lùi trước khi có tính năng nào dựa trên origin theo đơn vị.

## Dấu hiệu quyết định này bắt đầu sai

- Có người đề nghị thêm một origin chính xác "cho một trường hợp đặc biệt" vào bên cạnh mẫu — ràng buộc phiên đang bị khoét.
- Có bản ghi DNS không phải FE đơn vị xuất hiện dưới vùng đơn vị.
- Code đọc `Host` hay `Origin` để **chọn** đơn vị, ở bất kỳ đâu ngoài bộ khớp.
- Một endpoint ẩn danh trả thông tin đơn vị theo mã xuất hiện "cho trang đăng nhập đẹp hơn".
- Yêu cầu một người dùng đăng nhập hai đơn vị cùng lúc lặp lại — xét 4B hay 4C bằng ADR mới.
- Một đơn vị đòi tên miền riêng của mình (phương án D) — ADR mới, vì nó lật điều kiện cùng site của ADR-0015.
- Một dự án hạ nguồn không có quyền wildcard trên tên miền của mình — QC10 cần xét lại với nhu cầu thật đó.

## Việc sau khi chấp nhận

### Trước khi chấp nhận — điều kiện

| # | Ai | Việc |
| --- | --- | --- |
| T0a | Người dùng | Chốt QC1, QC2, QC4, QC6, QC7, QC10, QC11. Xác nhận tên miền thật, quyền wildcard DNS dưới một vùng con, khả năng cấp cert wildcard, người giữ DNS và cert. Chạy câu `SELECT` ở QC1 trên mọi database đang có |
| T0b | `frontend-expert` (cần chạy cả BE) | Thử nghiệm ở QC8, khoảng 30 phút, chỉ sửa cấu hình cục bộ rồi trả lại; báo kết quả cookie đi hay không ở hai cấu hình, và máy chủ dev có nhận host lạ không |

### Sau khi chấp nhận — mỗi việc 30–40 phút

| # | Agent | Việc | Tệp |
| --- | --- | --- | --- |
| A1 | `architect` | Đổi dòng trạng thái ADR này, [ADR-0015](0015-fe-va-api-khac-nguon.md) (bổ sung), [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) (sửa một phần); mục lục; lật hàng §11.1, sửa §5 (đoạn ngoại lệ đăng nhập — thêm bất biến *`Origin` chỉ để từ chối*), §10 (câu về mã đơn vị hệ thống), §13 | [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) · [`muc-luc.md`](muc-luc.md) · [`../wiki-core/be/02-identity-auth.md`](../wiki-core/be/02-identity-auth.md) §6.2 và bảng *Áp dụng* |
| A2 | `architect` | Card đăng nhập: hàng `Origin` ở §1 (mẫu + ràng buộc), §3 (nguồn của `tenantCode`, ca lệch nhãn, đoạn tài khoản vận hành); card đơn vị: khuôn mã ở §2 | [`../contracts/auth.md`](../contracts/auth.md) · [`../contracts/tenants.md`](../contracts/tenants.md) |
| A3 | `architect` | Quy ước CORS và hai lớp chống giả mạo (§7.1–§7.3, viết lại câu ở `docs/quy-uoc/be-api-controller.md:500`) và lý do đi kèm; thêm luật vào bảng — xem *Luật kéo theo*; dòng sổ chủ quyền cho mẫu origin | [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) · [`../wiki-core/be/ly-do/be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) · [`../RULES-BE.md`](../RULES-BE.md) · [`../RULES-FE.md`](../RULES-FE.md) · [`../OWNERSHIP.md`](../OWNERSHIP.md) |
| A4 | `architect` | Luồng đăng nhập §1 và bước 2; luồng cài đặt bước 6; luồng tạo đơn vị (khuôn mã, địa chỉ); runbook (mã hệ thống theo khuôn mới, các bước kiểm "gõ mã" ⇒ "mở địa chỉ"); phục vụ FE §4.1, §5.2 (khoá hậu tố vùng); phiên FE (phép so mã phiên với hostname); triển khai BE (DNS, cert, tầng phục vụ) | [`../luong/D1-dang-nhap.md`](../luong/D1-dang-nhap.md) · [`../luong/V1-cai-dat-lan-dau.md`](../luong/V1-cai-dat-lan-dau.md) · [`../luong/V2-tao-don-vi-moi.md`](../luong/V2-tao-don-vi-moi.md) · [`../database/script-runbook.md`](../database/script-runbook.md) · [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) · [`../wiki-core/fe/07-auth-identity.md`](../wiki-core/fe/07-auth-identity.md) · [`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) |
| D1 | `design-expert` | Màn đăng nhập hai ô, hiện mã đơn vị theo QC11; `AuthField` bỏ biến thể mã đơn vị nếu không còn nơi dùng; màn tên miền gốc theo QC7; màn danh sách đơn vị: gợi ý khuôn mã mới, cột hay dòng địa chỉ | [`../Design/Screens/01-dang-nhap.md`](../Design/Screens/01-dang-nhap.md) · [`../Design/Components/AuthField.md`](../Design/Components/AuthField.md) · [`../Design/Screens/20-don-vi.md`](../Design/Screens/20-don-vi.md) · một tệp màn mới nếu QC7 chọn 7A hay 7B |
| B1 | `backend-expert` | Khuôn mã mới ở validator tạo đơn vị; lệnh bootstrap kiểm cùng khuôn trước dòng ghi đầu tiên; test | `src/BE/Core/CoreAndSkill.Core.Application/Tenants/CreateTenantCommandValidator.cs` · `src/BE/Core/CoreAndSkill.Core.Application/Configuration/CoreBootstrapOptions.cs` · test đi kèm |
| B2 | `backend-expert` | Tuỳ chọn mẫu origin, kiểm lúc khởi động (sai dạng thì không khởi động); bộ khớp chặt; CORS theo bộ khớp; lớp 1 antiforgery theo bộ khớp; gỡ danh sách chính xác; test ca thù địch | `src/BE/Core/CoreAndSkill.Core.Application/Configuration/CoreAuthOptions.cs` · `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs` · `src/BE/Core/CoreAndSkill.Core.Web/Security/AntiforgeryValidationMiddleware.cs` |
| B3 | `backend-expert` | Ràng buộc phiên–origin trong phép kiểm phiên (từ chối, không đăng xuất, không gia hạn); kiểm khớp ở đăng nhập — `Core.Web` đưa nhãn vào lệnh, handler so dưới sàn thời gian, `Application` không đọc `HttpContext`; integration test | `CoreWebServiceCollectionExtensions.cs` · `src/BE/Core/CoreAndSkill.Core.Application/Auth/LoginCommand.cs` · `src/BE/Core/CoreAndSkill.Core.Application/Auth/LoginCommandHandler.cs` · controller đăng nhập |
| B4 | `backend-expert` | Cấu hình: hai tệp `appsettings`, `docker-compose.yml`, `.env.example`; sửa các test đang đặt `Origin` hay `AllowedOrigins` (lệnh tra ở QC9) | `src/BE/CoreAndSkill.Api/appsettings.json` · `src/BE/CoreAndSkill.Api/appsettings.Development.json` · `docker-compose.yml` · `.env.example` |
| F1 | `frontend-expert` | Khoá hậu tố vùng và `apiBaseUrl` dev mới; dịch vụ suy mã đơn vị từ hostname (trả "không phải host đơn vị" cho tên vùng trần, hai nhãn, sai hậu tố); spec; `allowedHosts` nếu T0b cần | `src/FE/src/environments/environment.ts` · `src/FE/src/environments/environment.development.ts` · `src/FE/angular.json` |
| F2 | `frontend-expert` | Trang đăng nhập bỏ ô, gửi mã suy được; lỗi `TenantCode` hiện ở mức form; khoá dịch; spec | `src/FE/src/app/platform/xac-thuc/pages/dang-nhap/dang-nhap.page.ts` và tệp HTML, spec cùng thư mục · `src/FE/src/app/shared/components/auth-field/auth-field.component.ts` |
| F3 | `frontend-expert` | Khởi động app: host không phải đơn vị ⇒ màn tên miền gốc, không khởi tạo phần gọi API; mã đơn vị của phiên lệch hostname ⇒ không dựng giao diện, về đăng nhập; form tạo đơn vị dùng khuôn mã mới và hiện địa chỉ theo Design | `src/FE/src/app/platform/he-thong/don-vi/state/tao-don-vi.store.ts` · phần khởi động phiên dưới `src/FE/src/app/core/auth/` |

Sau B2–B3 và sau F1–F3, phiên chính gọi `core-reviewer` theo `CLAUDE.md` §8, mỗi lượt một phạm vi. Không cần `test-engineer`: mọi test ở trên là test hành vi đi kèm từng việc, không phải cổng mới.

### Luật kéo theo — thêm vào bảng ở việc A3

| Phạm vi | Luật | Ép bằng gì | Trạng thái lúc thêm |
| --- | --- | --- | --- |
| BE §6 | Từ origin thuộc vùng có nhãn X, phiên của đơn vị khác X coi như không có — không đăng xuất, không gia hạn; đăng nhập lệch nhãn trượt bằng mã chung | Integration test (tên chốt ở việc B3) | `📐` tới khi test có trong `src/BE/Tests/` |
| BE §6 | Allowlist origin là một mẫu có cấu trúc, khớp đúng một nhãn; không danh sách chính xác, không biểu thức tuỳ ý | Unit test bộ khớp với các ca thù địch ở QC5 | `📐` như trên |
| BE §9 | `Host` và `Origin` không bao giờ là nguồn của đơn vị — chỉ để từ chối | Chỉ review canh — ArchTest của M2 không quét đọc header; không dựng cổng khi chưa có vi phạm thật ([ADR-0114](0114-bo-so-no-luat-ve-rules-viec-ton-sang-issues-review-khong-sinh-viec-stop-chan-review-mot-lan-go-nhanh-chua-co-src.md)) | dòng 🕳️ |
| FE §7 | Form đăng nhập không có ô mã đơn vị; mã đơn vị chỉ đến từ hostname | Spec của trang đăng nhập (tên chốt ở việc F2) | `📐` tới khi spec có |

## Liên quan

- [ADR-0013](0013-multi-tenant.md) — mô hình đơn vị; không đổi
- [ADR-0015](0015-fe-va-api-khac-nguon.md) — FE và API khác nguồn; được bổ sung nếu chấp nhận
- [ADR-0016](0016-phan-phoi-core-bang-clone.md) — dự án hạ nguồn lấy Core, lý do QC10 quan trọng
- [ADR-0017](0017-khu-quan-tri-he-thong.md) — tài khoản vận hành; không đổi
- [ADR-0023](0023-dich-vu-tao-don-vi-dung-chung.md) §6 — mã đơn vị hệ thống; bị sửa một phần nếu chấp nhận
- [ADR-0029](0029-dat-lai-mat-khau-ho-va-khoi-phuc-xuyen-don-vi.md) — khôi phục xuyên đơn vị; không đổi
- [ADR-0058](0058-dang-nhap-truot-cho-du-mot-san-thoi-gian.md) — sàn thời gian cho ca đăng nhập trượt
- [`../wiki-core/be/17-multi-tenant.md`](../wiki-core/be/17-multi-tenant.md) §5, §11.1, §13 · [`../contracts/auth.md`](../contracts/auth.md) §1, §3 · [`../contracts/tenants.md`](../contracts/tenants.md) §2 · [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §7
