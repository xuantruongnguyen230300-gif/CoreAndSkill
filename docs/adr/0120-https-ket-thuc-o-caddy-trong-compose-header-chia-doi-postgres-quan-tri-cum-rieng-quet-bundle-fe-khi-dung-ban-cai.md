---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0120 — Bản cài Compose kết thúc HTTPS ở Caddy chạy trong compose; header bảo mật chia đôi giữa Caddy và middleware Core; Postgres có tài khoản `postgres` riêng làm quản trị cụm; quét bí mật bản build FE ở bước dựng bản cài

> **Trạng thái:** Đã chấp nhận (2026-10-06)

## Bối cảnh

Đợt rà đồng bộ ngày 2026-10-06 mở bốn finding về bản cài thật: BE-15, BE-16, DB-05, CI-04 kèm FE-11. Người dùng duyệt toàn bộ đề xuất của đợt rà cùng ngày. ADR này ghi bốn câu thuộc bản cài — Q08, Q21, Q24, Q29 của đợt rà.

### Hiện trạng đọc từ tệp, 2026-10-06

| Chỗ | Đọc được |
| --- | --- |
| `docker-compose.yml` | Service `api` publish `"8080:8080"`, HTTP thuần; không có service proxy; không khai khoá `Core__Network__*`. Chú thích đầu tệp ghi tệp **chưa chạy thử** |
| Cookie phiên, cookie antiforgery | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`, chuỗi `options.Cookie.SecurePolicy = CookieSecurePolicy.Always;`. Trình duyệt không nhận cookie `Secure` qua HTTP, nên bản cài dựng theo compose hôm nay không đăng nhập được |
| Proxy tin cậy | Cùng tệp, chuỗi `forwarded.KnownProxies.Count + forwarded.KnownIPNetworks.Count > 0` — hai danh sách rỗng thì bỏ qua `X-Forwarded-*`. Đặt proxy mà không khai thì mọi request mang IP của proxy, và rate limit theo IP gom mọi người vào một phân vùng ([`be-api-controller.md`](../quy-uoc/be-api-controller.md) §6.3) |
| Header bảo mật của API | [`09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) §13 ghi *"✅ sẽ có — middleware, một chỗ"*. Pipeline ở `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreApplicationBuilderExtensions.cs` (chuỗi `app.UseForwardedHeaders();`) không có middleware header nào. Chỉ phản hồi tệp mang `nosniff` và `no-store` — `src/BE/Core/CoreAndSkill.Core.Web/Http/ExportFileResult.cs`, chuỗi `response.Headers.CacheControl = "no-store";` |
| Header của FE | [`fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §6 đã chốt bốn header đặt ở tầng phục vụ tệp tĩnh: ứng dụng tĩnh không có chỗ nào khác để đặt header |
| Tài khoản Postgres | Compose đặt `POSTGRES_USER: ${POSTGRES_OWNER_USER}`. Image chính thức biến tài khoản đó thành superuser của cụm, và không tạo `coreandskill_app`. [`script-runbook.md`](../database/script-runbook.md) §3.3 bước 1 đòi `coreandskill_owner` mang `NOSUPERUSER`, tạo bằng một tài khoản quản trị cụm riêng. Container `db` không mở cổng ra host, nên lệnh `psql -h "$PGHOST"` của runbook không chạy được từ ngoài container |
| Quét bí mật bundle FE | Hàng S6 ở [`RULES-BE.md`](../RULES-BE.md) khai *"bundle FE khi có `src/FE`"*. Job `gitleaks` ở `.github/workflows/docs-gate.yml` chỉ quét lịch sử git; không bước nào quét thư mục build. Bản build trên CI dùng tệp bản cài giả (bước `4b. ng build — bằng tệp bản cài giả của CI`), nên giá trị thật chỉ vào bundle ở lúc dựng bản cài |

### Ràng buộc có sẵn

- FE và API ở hai origin; ở bản thật là hai tên miền con của cùng một tên miền gốc, reverse proxy phục vụ tệp tĩnh của FE — [ADR-0015](0015-fe-va-api-khac-nguon.md), [`fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §4.1.
- HTTPS ở mọi môi trường — cùng tệp §4.1.
- Cấu hình reverse proxy vào repo và được review như code — [`repo-artifact.md`](../quy-uoc/repo-artifact.md) §2, hàng *Cấu hình hạ tầng*.
- Một instance API — [ADR-0014](0014-mot-instance-key-ring-postgres.md). Bản cài là Docker Compose trên một máy chủ Linux — [`18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §7.

## Quyết định

Người dùng chốt ngày 2026-10-06, theo đề xuất của kiến trúc sư:

1. **HTTPS kết thúc ở Caddy, chạy như một service trong `docker-compose.yml`.** Caddy phục vụ tệp tĩnh của FE ở origin FE, chuyển tiếp origin API tới container `api` qua HTTP trong mạng compose, và chuyển hướng HTTP sang HTTPS. Container `api` không publish cổng ra host. `Core:Network:KnownNetworks` khai đúng dải của một mạng compose cố định, chỉ Caddy và `api` nằm trong đó.
2. **Header bảo mật chia đôi.** Caddy đặt nhóm gắn với kênh truyền và với tài liệu trình duyệt hiển thị: `Strict-Transport-Security` ở cả hai origin; bốn header của FE 17 §6 ở origin FE; CSP chặn hết ở origin API. Một middleware của `Core.Web` đặt nhóm gắn với nội dung phản hồi API: `X-Content-Type-Options: nosniff` và `Cache-Control: no-store` trên mọi phản hồi, kể cả phản hồi lỗi. Bảng chia vai: [`09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) §6.
3. **Postgres của bản cài Compose có tài khoản `postgres` riêng làm quản trị cụm.** `coreandskill_owner` và `coreandskill_app` do runbook §3.3 bước 1 tạo, như ở mọi môi trường. Người vận hành chạy bước 1–3 bằng `docker compose exec db psql`; thư mục `database/scripts/` gắn chỉ-đọc vào container `db`. Mô hình quyền ở runbook §3.6 giữ nguyên, không thêm ngoại lệ.
4. **Quét bí mật trên bản build FE chạy ở bước dựng bản cài thật**, ngay sau `ng build` bằng tệp môi trường của bản cài và trước khi đưa tệp lên: `gitleaks dir` trên thư mục build với cấu hình của repo, kèm chốt thư mục build có tệp. Đỏ thì không đưa lên. CI không thêm bước quét thư mục build. Trong lúc bước này chưa có script, ô *Ép bằng gì* của S6 khai phần bundle FE là **chưa có cổng**.

## Quyết định con

### QC1 — Caddy

- Image ghim tag phiên bản cụ thể, không `latest`; nâng theo lịch như mọi phụ thuộc ([`repo-artifact.md`](../quy-uoc/repo-artifact.md) §3.2).
- Cấu hình ở tệp `Caddyfile` ở gốc repo, cạnh `docker-compose.yml`, gắn chỉ-đọc vào container. Tên miền FE, tên miền API, email ACME là biến `{$…}` đọc từ `.env` — repo không mang tên miền của bản cài nào.
- Volume có tên cho thư mục dữ liệu của Caddy: chứng chỉ, khoá tài khoản ACME. Mất volume thì Caddy xin lại chứng chỉ, và Let's Encrypt giới hạn số lần cấp cho một tên miền trong một tuần.
- Bản cài không có tên miền công khai, chạy trong mạng nội bộ: Caddy không tự xin được chứng chỉ Let's Encrypt. Biến thể: nạp chứng chỉ của khách hàng bằng chỉ thị `tls <tệp chứng chỉ> <tệp khoá>`, hai tệp nằm ngoài repo, gắn vào container. Gia hạn khi đó là việc tay của người vận hành.
- Khối phục vụ FE theo [`fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §2 (SPA fallback), §3 (cache ba nhóm tệp), §6 (bốn header). Đưa bản mới lên theo §8 cùng tệp, giữ tệp băm của bản trước (§3.3).
- Máy dev không đổi: không proxy, hai cổng, CORS và HTTPS như thật ([ADR-0015](0015-fe-va-api-khac-nguon.md)).
- Vùng sở hữu của `Caddyfile` và `docker-compose.yml` vẫn là *chưa phân vùng* như mọi tệp gốc repo ([`repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.5). Dự án hạ nguồn đổi tên miền qua `.env`, không cần sửa hai tệp đó.

### QC2 — Mạng compose và proxy tin cậy

- Hai mạng. Mạng `edge` có dải cố định khai bằng `ipam` trong compose, chỉ gồm Caddy và `api`. Mạng nội bộ gồm `api` và `db`. `db` không nằm trong `edge`.
- `Core__Network__KnownNetworks__0` mang dải của `edge`, khai trong compose vì không phải bí mật. Dải đó trùng mạng của máy chủ khách thì đổi ở compose, cả ở `ipam` lẫn ở khoá này.

### QC3 — Middleware header của Core

- Đăng ký ngay sau `UseForwardedHeaders`, trước `TraceIdMiddleware`. Header đặt lúc phản hồi bắt đầu, để phản hồi 401, 403, 429, phản hồi của `UseExceptionHandler` và preflight CORS cũng mang.
- Không ghi đè giá trị endpoint đã đặt. `ExportFileResult` và nhánh tải tệp giữ nguyên ba header của chúng ([ADR-0064](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md), [ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md)).
- Thứ tự pipeline mới ghi vào [`be-architecture.md`](../quy-uoc/be-architecture.md) §3.1 khi code về — khối đó đang mang nhãn đã đối chiếu, nên không sửa trước code.
- Không thêm gói ngoài: middleware tự viết.

### QC4 — Postgres

- `POSTGRES_USER=postgres`, mật khẩu lấy từ biến `POSTGRES_ADMIN_PASSWORD` của `.env`. Không đặt `POSTGRES_DB`: database rỗng do runbook §3.3 bước 1 tạo, chủ là `coreandskill_owner`, mã hoá `UTF8`, mẫu `template0`.
- Healthcheck đổi sang tài khoản `postgres`.
- `docker compose exec` đi qua Unix socket trong container, và image chính thức để `trust` cho kết nối cục bộ — ai chạy được `docker compose exec` trên máy chủ thì vào được mọi tài khoản database. Chấp nhận: người đó vốn đã có quyền root trên máy chủ.
- Câu lệnh cụ thể của đường Compose thuộc runbook §3.3, §6 — không chép vào đây.

### QC5 — Quét bundle FE

- Lệnh và vị trí trong quy trình: [`fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §8, bước 2b.
- Chốt chống xanh rỗng (luật T6): thư mục build không có tệp `.js` nào thì bước quét đỏ.
- Phiên bản gitleaks: đúng phiên bản ghim ở job `gitleaks` của CI.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Kestrel tự mang chứng chỉ, không proxy

**Được:** không thêm container; ít hơn một chỗ cấu hình. **Mất:** FE vẫn cần một máy chủ tệp tĩnh, tức vẫn có một tầng phục vụ, chỉ là tầng đó không lo TLS; gia hạn chứng chỉ thành việc tay theo chu kỳ; bốn header của FE vẫn chỉ đặt được ở tầng phục vụ FE. **Vì sao loại:** không bỏ được tầng phục vụ FE, nên chỉ dời việc TLS sang chỗ khó tự động hơn.

### Phương án B — Hạ tầng ngoài repo: load balancer của khách lo HTTPS, repo chỉ ghi yêu cầu

**Được:** hợp doanh nghiệp đã có hạ tầng sẵn; repo không mang thêm phần mềm nào. **Mất:** cấu hình quyết định hành vi sản phẩm — header, cache, SPA fallback, chuyển tiếp — sống ngoài review. **Vì sao loại:** trái [`repo-artifact.md`](../quy-uoc/repo-artifact.md) §2, hàng *Cấu hình hạ tầng*. Bản cài thật sự đứng sau load balancer của khách vẫn làm được: load balancer chuyển tiếp tới Caddy, và `KnownNetworks` khai thêm dải của nó.

### Phương án C — Nginx thay Caddy

**Được:** phổ biến hơn, nhiều người vận hành đã quen; cấu hình chi tiết hơn. **Mất:** không tự cấp, tự gia hạn chứng chỉ — cần thêm certbot và một lịch chạy, tức thêm một thành phần nữa. **Vì sao loại:** v1 chạy một máy chủ, không có người vận hành chuyên trách; tự gia hạn là thứ đáng giá nhất ở đây. Đổi sang Nginx sau này chỉ chạm compose và tệp cấu hình proxy — câu 5 ở bảng *Sáu câu hỏi*.

### Phương án D — Middleware Core đặt mọi header, kể cả HSTS và CSP

**Được:** một chỗ cho mọi header của API, test được bằng integration test, không phụ thuộc proxy. **Mất:** header của origin FE vẫn phải đặt ở proxy (FE 17 §6), nên HSTS vẫn có một bản ở proxy, giờ thêm bản thứ hai ở Core cho origin API. **Vì sao loại:** không gỡ được nửa ở proxy, chỉ nhân đôi HSTS. Đây là phương án đúng khi bản cài không có proxy — đề xuất gốc của Q21 nêu rõ điều kiện đó.

### Phương án E — Owner kiêm superuser, ghi thành ngoại lệ ở runbook §3.6

**Được:** compose giữ gần như hiện tại; bớt một mật khẩu phải giữ. **Mất:** tài khoản áp script mang quyền superuser, nên một script sai chạm được mọi database của cụm; `NOSUPERUSER` ở runbook §3.3 bước 1 không còn đúng ở đúng môi trường quan trọng nhất. **Vì sao loại:** nới quyền tối thiểu ở bản cài thật để đổi lấy một mật khẩu.

### Phương án F — Quét thư mục build FE ở CI

**Được:** rẻ, chạy mỗi lần push. **Mất:** bản build của CI dùng tệp bản cài giả, nên chỉ phủ mã nguồn và gói npm. **Vì sao loại:** quét đúng chỗ không có giá trị thật. Bước dựng bản cài mới là chỗ giá trị thật được nội suy vào bundle.

## Hệ quả

### Tích cực

- Bản cài Compose đăng nhập được: cookie `Secure` đi qua HTTPS.
- Rate limit theo IP thấy IP thật của người dùng, và chỉ tin header chuyển tiếp từ Caddy.
- Bản cài có tên miền công khai tự cấp, tự gia hạn chứng chỉ.
- Phản hồi API mang `nosniff`, `no-store` ở cả máy dev lẫn test tích hợp, không phụ thuộc proxy, nên kiểm được bằng test.
- Mô hình quyền database giống nhau ở máy dev và bản cài thật.

### Tiêu cực — cái giá thật

- **Thêm một phần mềm phải vận hành.** Caddy phải nâng phiên bản theo lịch; cổng 80 và 443 của máy chủ mở ra ngoài; DNS của hai tên miền con phải trỏ đúng trước lần khởi động đầu, nếu không Caddy xin chứng chỉ thất bại liên tục và có thể chạm giới hạn của Let's Encrypt.
- **Bản cài trong mạng nội bộ mất phần tự động.** Chứng chỉ của khách gia hạn tay; quên thì trình duyệt chặn cả FE lẫn API cùng lúc.
- **Bản cài không theo compose thì HSTS và CSP phụ thuộc người dựng.** Dự án triển khai sau hạ tầng khác — Kubernetes, load balancer của khách mà không có Caddy — phải tự đặt hai header đó; không cổng nào biết chúng vắng.
- **Phản hồi tệp nhận `nosniff`, `no-store` từ hai lớp.** Middleware đặt cho mọi phản hồi, `ExportFileResult` và nhánh tải tệp vẫn tự đặt. Cùng giá trị nên không lệch nghĩa, nhưng sửa một lớp không đổi được hành vi phản hồi tệp.
- **Thêm một mật khẩu phải giữ** — `postgres` của cụm. Ai có `docker compose exec` thì có mọi tài khoản database.
- **Quét bundle FE chưa có cổng thật.** Tới khi bước dựng bản cài thành script, đó là bước tay; người dựng bản cài phải cài gitleaks đúng phiên bản.
- **Dải mạng cố định có thể đụng mạng của máy chủ khách.** Lỗi chỉ lộ lúc `docker compose up`.
- **Chưa chạy thử.** Compose chưa từng lên trên Docker thật; mạng, Caddy, ACME chỉ đúng trên giấy cho tới lần chạy đầu.

## Sáu câu hỏi bắt buộc

| # | Câu hỏi | Trả lời | Chắc chắn? |
| --- | --- | --- | --- |
| 1 | Vấn đề thật | Compose hôm nay không đăng nhập được vì cookie `Secure` đi qua HTTP; không khai proxy tin cậy; phản hồi API thiếu `nosniff`, `no-store`; owner là superuser, trái runbook; S6 khai một cổng không tồn tại | Suy từ code và cấu hình, đối chiếu 2026-10-06 — **chưa quan sát trên bản cài chạy thật**, vì compose chưa từng chạy |
| 2 | Phương án đơn giản hơn | Kestrel tự mang chứng chỉ (A) — vẫn phải có tầng phục vụ FE; owner kiêm superuser (E) — nới quyền ở bản cài thật; middleware đặt hết (D) — vẫn phải đặt header FE ở proxy | Có |
| 3 | Chi phí vận hành thêm | Một container nâng theo lịch, một volume chứng chỉ, cổng 80/443, DNS hai tên miền con, một mật khẩu `postgres`, gitleaks trên máy dựng bản cài; bản cài nội bộ gia hạn chứng chỉ tay | Thời gian vận hành chưa đo |
| 4 | Ai bảo trì | Compose, `Caddyfile`, `.env.example`: `backend-expert` thi công, khối FE của `Caddyfile` đối chiếu FE 17 bởi `frontend-expert`. Middleware header: `backend-expert`, qua `core-reviewer`. Tên miền, chứng chỉ của từng bản cài: người vận hành bản cài đó | Có |
| 5 | Rút lui thế nào | (1) Caddy sai: thay bằng proxy khác chỉ chạm service proxy trong compose và tệp cấu hình của nó — app chỉ biết dải `KnownNetworks`; chứng chỉ không mang sang, proxy mới xin lại. (2) Chia header sai: chuyển HSTS và CSP của origin API vào middleware Core — thêm dòng ở middleware và test, bỏ dòng ở `Caddyfile`, viết ADR mới. (3) `postgres` riêng sai: `ALTER ROLE` trên cụm đang chạy; không dựng lại volume, vì `POSTGRES_USER` chỉ áp khi volume rỗng. (4) Quét bundle sai chỗ: bỏ một bước ở FE 17 §8 | Có, trừ (1): chưa có bản cài thật để đo thời gian đổi proxy |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Middleware là hạ tầng HTTP chung; `Caddyfile` và compose là mẫu bản cài, không mang tên module, tên miền hay tên khách hàng | Có |

## Dấu hiệu quyết định này bắt đầu sai

- Lần `docker compose up` đầu tiên phải sửa tay `Caddyfile` hay khai mạng mới chạy được — mẫu chưa đủ chung.
- Một bản cài thật gặp chứng chỉ hết hạn mà không ai biết trước — cần giám sát gia hạn.
- Dự án hạ nguồn đầu tiên không dùng compose và thiếu HSTS hay CSP — xét chuyển HSTS về middleware Core.
- Cần instance API thứ hai ([ADR-0014](0014-mot-instance-key-ring-postgres.md)) — `KnownNetworks` và vai của Caddy phải xét lại cùng lượt.
- Người dựng bản cài bỏ bước quét bundle hai lần — chuyển bước đó vào script dựng bản cài, hoặc xét lại phương án F.

## Việc sau khi chấp nhận

| # | Việc | Ai |
| --- | --- | --- |
| 1 | `docker-compose.yml`, `Caddyfile`, `.env.example` theo QC1, QC2, QC4; chú thích `Core__File__RootPath` nói khoá bắt buộc khi bản lưu là bản đĩa cục bộ của Core ([ADR-0107](0107-rootpath-chi-kiem-khi-dung-ban-luu-cua-core.md)) | `backend-expert` |
| 2 | Middleware header theo QC3, kèm test | `backend-expert`, rồi `core-reviewer` |
| 3 | Đường Compose ở runbook §3.3, §6 — bước 1–3 qua `docker compose exec` | `architect`, sau lượt đang sửa runbook |
| 4 | Ghi pipeline mới vào `be-architecture.md` §3.1; lật nhãn hàng *Header bảo mật* ở 09 §13 khi code về | `architect` |
| 5 | Chạy thử `docker compose up` lần đầu trên một máy có Docker | Người dùng |

## Liên quan

- [ADR-0015](0015-fe-va-api-khac-nguon.md) — FE và API khác nguồn; proxy không có ở máy dev. ADR này không đổi điều đó.
- [ADR-0014](0014-mot-instance-key-ring-postgres.md) — một instance.
- [ADR-0064](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md), [ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md) — ba header của phản hồi tệp; giữ nguyên.
- [`18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §3, §7 — proxy tin cậy, bảng áp dụng của bản cài.
- [`09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) §6 — bảng chia vai header.
- [`fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §6, §8 — header của FE, quy trình triển khai FE.
- [`script-runbook.md`](../database/script-runbook.md) §3.3, §3.6 — tạo tài khoản và mô hình quyền.
- Hàng S6 ở [`RULES-BE.md`](../RULES-BE.md).
