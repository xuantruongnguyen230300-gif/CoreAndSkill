---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 18. Triển khai và vận hành backend

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Code của chủ đề này đã có một phần dưới `src/BE`, nhưng **chưa mục nào trong tệp được đối chiếu** với nó — tệp vẫn trong tầm chấm review (2026-09-24) — trừ §3, mục *Bản cài Compose*, đối chiếu riêng 2026-10-06 với bảng ở chính mục đó.
>
> **Máy chủ chạy Linux (chốt 2026-09-10); cách chạy, nguồn bí mật và cơ chế job nền chốt ở §7 (2026-09-15).** File này khai **nguyên tắc trung lập nền tảng** ở §1–§6 — đúng dù chạy trên máy chủ riêng, container hay dịch vụ lưu trữ; phần cụ thể chỉ nằm ở §7.
>
> Phía FE: [`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md).

---

## 1. Một artifact cho mọi môi trường

Build **một lần**, triển khai cùng một artifact lên mọi môi trường. Khác biệt giữa các môi trường chỉ nằm ở **cấu hình đọc lúc chạy**, không nằm trong bản build.

Build riêng cho từng môi trường nghĩa là thứ đã thử ở môi trường thử **không phải** thứ chạy ở môi trường thật.

## 2. Bí mật và cấu hình theo môi trường

| Luật | Vì sao |
| --- | --- |
| Bí mật **dạng rõ** **không** nằm trong repo, kể cả tệp cấu hình theo môi trường — ngoại lệ có tên duy nhất là giá trị dev công khai, chỉ mở database cục bộ của máy dev, không dùng cho môi trường máy chủ nào (luật S28) | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §6.1 |
| Ở môi trường thật, bí mật đến từ **nguồn ngoài cây làm việc** do nền tảng cấp — biến môi trường hoặc kho bí mật | Đổi bí mật không cần build lại, và artifact rò ra ngoài không mang bí mật theo |
| `appsettings.Development.json` **không** đi vào artifact publish — luật S23 vế (e) ([`RULES-BE.md`](../../RULES-BE.md)) | Tệp đó mang giá trị dev công khai, gồm hai mật khẩu bootstrap. Máy chủ lỡ chạy ở môi trường `Development` sẽ nạp chúng, và lệnh bootstrap chạy ở đó tạo tài khoản mang mật khẩu ai cũng đọc được trên GitHub ([`../../adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md`](../../adr/0110-db-cuc-bo-moi-dev-gia-tri-dev-cong-khai-trong-git-hoan-db-chung.md)) |
| Thứ tự ưu tiên của framework: tệp mặc định → tệp theo môi trường → **biến môi trường thắng** | Người vận hành ghi đè được mọi giá trị mà không sửa tệp đã đóng gói |
| Thiếu một giá trị bắt buộc thì app **không khởi động** — ở **mọi** môi trường, kể cả máy dev | [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §4 — hỏng lúc triển khai, không hỏng lúc người dùng chạm tới |

Danh sách giá trị bắt buộc **không** chép vào đây — nó là tập các lớp cấu hình có kiểm lúc khởi động, đọc bằng lệnh khi có `src/`:

```bash
grep -rn 'ValidateOnStart' src/BE --include='*.cs'
```

## 3. Proxy đứng trước API

Có proxy thì khai danh sách proxy tin cậy; không có thì để trống. Vị trí bước này và hai cách hỏng: [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §3.1, ràng buộc 4.

### Khoá cấu hình proxy tin cậy — định nghĩa gốc

| Khoá | Giá trị | Ví dụ |
| --- | --- | --- |
| `Core:Network:KnownProxies` | Mảng địa chỉ IP, mỗi proxy một mục | `["10.0.0.5"]` |
| `Core:Network:KnownNetworks` | Mảng dải mạng dạng CIDR | `["10.0.0.0/24"]` |

- **Hai khoá cùng rỗng hoặc vắng = không tin nguồn nào.** Header `X-Forwarded-For` / `X-Forwarded-Proto` bị bỏ qua hẳn. Đây là giá trị đúng khi app nhận kết nối trực tiếp.
- **Có ít nhất một mục** thì header chuyển tiếp chỉ được áp khi kết nối TCP đến từ đúng proxy hoặc dải đã khai. Danh sách loopback mặc định của framework **không** cộng thêm vào.
- **Mục sai dạng thì app không khởi động**, thông báo nêu tên khoá — cùng luật fail-fast ở [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §4.
- Qua biến môi trường, chỉ số mảng đi sau hai dấu gạch dưới: `Core__Network__KnownProxies__0=10.0.0.5`.

> ⚠️ **Rỗng phải có nghĩa là tắt, và điều đó không tự có.** Middleware chuyển tiếp của framework chỉ đối chiếu nguồn khi ít nhất một trong hai danh sách có mục. Xoá danh sách mặc định mà vẫn bật cờ chuyển tiếp thì nó **tin mọi nguồn** — ai cũng tự đặt được IP để chọn phân vùng rate limit ([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §6.3). Vì thế cờ chuyển tiếp chỉ bật khi đã khai ít nhất một mục.

### Bản cài Compose — Caddy đứng trước API

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (người dùng chốt 2026-10-06, [`../../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md`](../../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md)).
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | ✅ Đối chiếu 2026-10-06, `docker-compose.yml`: service `caddy` chuỗi `image: caddy:2.10.2-alpine`, publish `80`, `443`; service `api` không có khoá `ports:`, nằm trong hai mạng `edge`, `internal`; `db` chỉ trong `internal`; mạng `edge` chuỗi `subnet: 172.28.0.0/24`, khớp chuỗi `Core__Network__KnownNetworks__0: 172.28.0.0/24` ở `api`. `Caddyfile` gốc repo: khối `{$API_DOMAIN}` chuỗi `reverse_proxy api:8080` | — |
> | ✅ Đối chiếu 2026-10-06: `Caddyfile` gốc repo có khối origin FE `{$FE_DOMAIN}` — chuỗi `root * /srv/fe`, `rewrite @spa /index.html`; thư mục gắn ở `docker-compose.yml`, chuỗi `${FE_WWW_DIR:-./fe-www}:/srv/fe:ro`. **Chưa ai kiểm cú pháp** `Caddyfile` | Một lần Caddy đọc tệp không lỗi — `caddy validate` hay `docker compose up` |
> | Chưa ai chạy `docker compose up` — cấp chứng chỉ, chuyển tiếp, proxy tin cậy trên máy thật **chưa đo** | Một lần dựng bản cài thật theo tệp này và [`../../database/script-runbook.md`](../../database/script-runbook.md) §3.3, mục *Bản cài Compose* |

| Việc | Ở đâu |
| --- | --- |
| Kết thúc HTTPS, chuyển hướng HTTP sang HTTPS, tự cấp và gia hạn chứng chỉ | Caddy — service trong `docker-compose.yml`, cấu hình ở `Caddyfile` gốc repo |
| Phục vụ tệp tĩnh FE ở origin FE | Caddy, theo [`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md) §2, §3, §6 |
| Chuyển tiếp origin API tới `api` | Caddy, qua HTTP trong mạng compose `edge` |
| Proxy tin cậy | `Core:Network:KnownNetworks` mang dải cố định của mạng `edge` — chỉ Caddy và `api` nằm trong đó, `db` thì không |
| Header bảo mật | Chia đôi giữa Caddy và middleware Core — bảng ở [`09-security-beyond-auth.md`](09-security-beyond-auth.md) §6 |
| Origin FE trong allowlist của BE — CORS và lớp 1 antiforgery | Suy từ `FE_DOMAIN`, không khai riêng — đoạn ngay dưới |

**Origin FE của bản cài suy từ `FE_DOMAIN`, không có biến riêng.** `docker-compose.yml` dựng khoá `Core:Auth:AllowedOrigins:0` của `api` thành `https://` + `FE_DOMAIN` — đúng origin mà Caddy phục vụ FE, cổng 443 mặc định nên không ghi cổng. Một bản cài một origin FE ([ADR-0120](../../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md) QC1), nên không có biến thứ hai tả cùng giá trị đó để người cài gõ lệch: lệch thì trình duyệt chặn đăng nhập, tab Network chỉ thấy `OPTIONS` 204, và không lỗi nào nêu nguyên nhân. Hai lớp chặn giá trị hỏng:

- `FE_DOMAIN` là tên miền trần — không `https://`, không `/` cuối. Thiếu hay rỗng thì `docker compose` dừng trước khi dựng container, kèm câu hướng dẫn.
- Giá trị có `https://` hay `/` cuối vẫn qua được bước trên, nhưng origin dựng ra sai dạng, nên `api` không khởi động và nêu tên khoá — phép kiểm dạng origin ở [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7.3.

Cần origin FE thứ hai là đổi `docker-compose.yml` có chủ ý, không thêm biến vào `.env`. ✅ CÓ THẬT (đối chiếu 2026-11-09, chỉ đọc tệp — chưa ai chạy `docker compose up`): `docker-compose.yml`, chuỗi `Core__Auth__AllowedOrigins__0: "https://${FE_DOMAIN:?`; `.env.example`, chuỗi `FE_DOMAIN chỉ là tên miền`; không tệp nào còn biến origin riêng — `grep -c CORE_AUTH_ALLOWED_ORIGIN docker-compose.yml .env.example` ra 0 ở cả hai.

**Bản cài không có nhánh máy dev của quy tắc origin.** Nhánh nhận mọi `https://localhost:<cổng>` chỉ chạy khi môi trường là Development ([`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7.3, [ADR-0129](../../adr/0129-development-nhan-moi-cong-https-localhost-mot-ham-origin-chung-cho-cors-va-lop-1-go-kiem-cong-prestart.md); ✅ CÓ THẬT, đối chiếu 2026-11-09: `src/BE/Core/CoreAndSkill.Core.Web/Security/OriginAllowlist.cs`, chuỗi `isDevelopment && IsDevelopmentLocalhostOrigin(origin)`, ca Production của `CorsAndLayerOne_GiveTheSameVerdict_ForTheSameOrigin` từ chối `https://localhost:55697`); `api` của bản cài chạy Production nên chỉ origin dựng từ `FE_DOMAIN` được nhận. Đổi môi trường của `api` là bật nhánh đó — cùng lúc với OpenAPI, `user-secrets`. ✅ CÓ THẬT (đối chiếu 2026-11-09, chỉ đọc tệp): `docker-compose.yml`, chuỗi `ASPNETCORE_ENVIRONMENT: Production`.

Bản cài không có tên miền công khai thì Caddy không tự xin được chứng chỉ: nạp chứng chỉ của khách hàng, gia hạn tay — ADR-0120, QC1. Máy dev không có proxy ([`../../adr/0015-fe-va-api-khac-nguon.md`](../../adr/0015-fe-va-api-khac-nguon.md)), nên hai khoá ở trên để trống.

## 4. Thứ tự triển khai một bản mới

1. **Áp script schema trước**, theo checklist ở [`../../database/script-runbook.md`](../../database/script-runbook.md) §7 — app không bao giờ tự áp ([`../../adr/0009-ap-schema-chay-tay.md`](../../adr/0009-ap-schema-chay-tay.md)).
2. Script tuân **mở rộng trước, thu hẹp sau** ([`13-core-data-migration.md`](13-core-data-migration.md) §4) — nên **bản app cũ vẫn chạy được trên schema mới**. Đây là điều kiện để bước 5 tồn tại.
3. Triển khai artifact mới. Readiness **đỏ** khi còn migration chưa áp — [`07-observability.md`](07-observability.md) §8.
4. Kiểm sau triển khai: readiness xanh **và một request ghi thật đi qua**. Health xanh không chứng minh cookie và antiforgery chạy — proxy khai sai vẫn cho health xanh trong khi không request ghi nào qua được.
5. **Quay lui = triển khai lại artifact trước, không đụng DB** — làm được nhờ bước 2.

**Thu hẹp schema** (xoá cột, xoá bảng cũ) là **một lần triển khai riêng**, chỉ làm sau khi bản mới đã ổn định. Gộp vào cùng lần mở rộng thì bước 5 mất: bản app cũ không còn chạy được trên schema đã thu hẹp. Quay lui ở mức schema: [`13-core-data-migration.md`](13-core-data-migration.md) §6.

v1 chạy **một instance** nên mỗi lần triển khai có gián đoạn ngắn — đã chấp nhận ở [`../../adr/0014-mot-instance-key-ring-postgres.md`](../../adr/0014-mot-instance-key-ring-postgres.md).

## 5. Sao lưu

[`10-data-retention.md`](10-data-retention.md) §7 — kèm §7.1 về bảng khoá bảo vệ dữ liệu, thứ do chính Core sinh ra.

## 6. Khi có sự cố — truy một lỗi người dùng báo

| Bước | Làm gì |
| --- | --- |
| 1 | Lấy **mã lần gọi** người dùng thấy trên màn lỗi — [`07-observability.md`](07-observability.md) §3.2 |
| 2 | Tìm log theo mã đó: mọi dòng của request đều mang nó, kể cả dòng do thư viện ghi |
| 3 | Đọc `code` trong envelope. **4xx mang mã nghiệp vụ** thường không phải sự cố — là một luật đã chặn đúng. **5xx** mới là sự cố: log có exception |
| 4 | Lỗi ở việc chạy nền, **khi Outbox đã bật**: bản ghi outbox mang mã lần gọi của request đã sinh ra nó — nối được về nguyên nhân |
| 5 | Sửa dữ liệu ở môi trường thật **chỉ** qua script theo checklist [`../../database/script-runbook.md`](../../database/script-runbook.md) §7 — không bao giờ sửa tay |
| 6 | Xong thì ghi postmortem ở [`../../audit/`](../../audit/) theo khuôn bốn phần |

## 7. §Áp dụng

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| Một artifact cho mọi môi trường | ✅ sẽ có | §1 |
| Bí mật ngoài repo, không khởi động khi thiếu | ✅ sẽ có | §2 |
| Triển khai theo thứ tự schema → app, quay lui không đụng DB | ✅ sẽ có | §4 |
| **Hệ điều hành máy chủ** | ✅ Linux — chốt 2026-09-10 | |
| **Cách chạy bản thật** | ✅ Docker Compose — chốt 2026-09-15 | Một máy chủ Linux, không cloud ở v1 |
| **Nguồn bí mật ở bản thật** | ✅ Biến môi trường do Compose cấp từ tệp `.env` **ngoài repo** — chốt 2026-09-15 | Cùng khoá cấu hình với `appsettings.Development.json` ở máy dev; không Vault ở v1. Tệp `.env` không commit ([`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §6) |
| **Cơ chế job nền** | ✅ `BackgroundService` của .NET, không thư viện — chốt 2026-09-15 | Hai hình dạng, ranh giới khai ở [`../../quy-uoc/be-architecture.md`](../../quy-uoc/be-architecture.md) §1.1: chạy một lần theo yêu cầu thì qua seam `IBackgroundJobScheduler`, lặp theo chu kỳ thì `BackgroundService` riêng — [`../../adr/0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md`](../../adr/0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md). Xem lại (Quartz.NET) chỉ khi cần lịch cron do người dùng cấu hình |
| **HTTPS và proxy ở bản thật** | ✅ Caddy là service trong compose — chốt 2026-10-06 | §3, mục *Bản cài Compose*. Service và cả hai khối origin của `Caddyfile` có trong repo (đối chiếu 2026-10-06); chưa ai kiểm cú pháp `Caddyfile`, chưa chạy thật |
| **Origin FE trong allowlist của BE ở bản thật** | ✅ Suy từ `FE_DOMAIN`, không biến riêng — chốt 2026-11-09 | §3, mục *Bản cài Compose*; có ở `docker-compose.yml` (đối chiếu 2026-11-09), chưa chạy thật |
| **Header bảo mật** | ✅ Chia đôi: Caddy đặt HSTS, CSP, header của FE; middleware Core đặt `nosniff`, `no-store` — chốt 2026-10-06 | [`09-security-beyond-auth.md`](09-security-beyond-auth.md) §6 — middleware Core và cả hai khối origin của `Caddyfile` có thật (đối chiếu 2026-10-06), neo ở đó |
| **Quản trị cụm Postgres ở bản Compose** | ✅ Tài khoản `postgres` riêng; tạo tài khoản, áp script bằng `docker compose exec db psql` — chốt 2026-10-06 | Mô hình quyền giữ nguyên: [`../../database/script-runbook.md`](../../database/script-runbook.md) §3.3 bước 1, §3.6; lệnh của đường Compose: cùng tệp §3.3, mục *Bản cài Compose*. Compose đặt `POSTGRES_USER: postgres` và gắn `./database/scripts:/scripts:ro` (đối chiếu 2026-10-06); chưa chạy thật |
| **Quét bí mật bản build FE** | ✅ Ở bước dựng bản cài thật — chốt 2026-10-06 | [`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md) §8, bước 2b. Chưa có script — chạy tay; luật S6 ở [`../../RULES-BE.md`](../../RULES-BE.md) |
| **Kho tệp (`Core:File:RootPath`)** | ✅ Volume có tên `coreandskill-files` gắn vào thư mục tạo sẵn trong image — chốt 2026-09-21 | Khoá bắt buộc khi bản lưu là bản đĩa cục bộ của Core ([`14-file-storage.md`](14-file-storage.md) §2, [`../../adr/0107-rootpath-chi-kiem-khi-dung-ban-luu-cua-core.md`](../../adr/0107-rootpath-chi-kiem-khi-dung-ban-luu-cua-core.md)), không mặc định, không nằm ở `appsettings.json`; khai ở `docker-compose.yml` (`Core__File__RootPath`) chứ không qua `.env` vì không phải bí mật. Máy dev: giá trị có sẵn trong `appsettings.Development.json`, thư mục tạo tay theo [`../../database/script-runbook.md`](../../database/script-runbook.md) §6 — thư mục phải có sẵn và ghi được, nếu không tiến trình không lên. Muốn đường dẫn khác thì đè bằng `dotnet user-secrets set "Core:File:RootPath" "<đường dẫn tuyệt đối, ngoài thư mục ứng dụng>"`. Sao lưu cùng DB, phục hồi về cùng mốc: [`14-file-storage.md`](14-file-storage.md) §8 |
| **Triển khai không gián đoạn** | ❌ chưa | Cần nhiều instance — điều kiện ở ADR-0014 |
