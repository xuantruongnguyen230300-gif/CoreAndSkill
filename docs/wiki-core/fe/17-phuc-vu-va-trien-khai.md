---
kind: luat
scope: core
verified: 2026-11-05
---

# 17. Phục vụ và triển khai Frontend

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG** (đối chiếu cả tệp 2026-11-05, chỉ bằng đọc tệp — chưa chạy bảng kiểm §9). Phần máy dev và cơ chế cấu hình lúc build đã có ở `src/`; phần của môi trường thật — tầng phục vụ, cache header, header bảo mật — đã có cấu hình ở `Caddyfile` gốc repo nhưng **chưa ai kiểm cú pháp, chưa ai chạy** `docker compose up`. Hạng mục có thật và hạng mục chưa: bảng ở §10, neo ở bảng ngay dưới.
>
> Cái gì được commit và cái gì là artifact build: [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md). Bảo mật liên quan: [`14-security.md`](14-security.md).

## Có thật hôm nay → sẽ thành

Đối chiếu lại cả bảng 2026-11-05 (lần đầu 2026-10-06), chỉ bằng đọc tệp, không chạy ứng dụng.

| Mục | Có thật hôm nay (neo bằng chuỗi) | Sẽ thành |
| --- | --- | --- |
| FE và API ở hai origin trên máy dev (§4.1) | API: `src/BE/CoreAndSkill.Api/Properties/launchSettings.json`, chuỗi `"applicationUrl": "https://localhost:7100`; FE gọi API qua `src/FE/src/environments/environment.development.ts`, chuỗi `apiBaseUrl: 'https://localhost:7100/api/v1'` | — |
| HTTPS ở máy dev (§4.2) | `src/FE/angular.json` khối `serve`, chuỗi `"ssl": true`, `"sslCert": ".cert/localhost.pem"`, `"sslKey": ".cert/localhost.key"`; thư mục chứng chỉ của FE bị `.gitignore` gốc loại khỏi git — lệnh kiểm 5 ở [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §10 | — |
| HTTPS và tầng phục vụ ở bản cài Compose | TLS kết thúc ở Caddy ([ADR-0120](../../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md)): `docker-compose.yml` service `caddy`, chuỗi `image: caddy:2.10.2-alpine`; tệp tĩnh FE gắn chỉ-đọc, chuỗi `${FE_WWW_DIR:-./fe-www}:/srv/fe:ro`; biến `FE_WWW_DIR` ở `.env.example`, chuỗi `FE_WWW_DIR=./fe-www`. `Caddyfile` gốc repo, khối `{$FE_DOMAIN}`, chuỗi `root * /srv/fe` | Chạy thật một lần — **chưa ai chạy** `docker compose up`, chưa ai kiểm cú pháp `Caddyfile` |
| CORS allowlist | Máy dev: `src/BE/CoreAndSkill.Api/appsettings.Development.json`, chuỗi `"AllowedOrigins": ["https://localhost:4200", "https://localhost:7100"]` — origin thứ hai là của chính API, cho trang Scalar ([nhật ký quyết định](../../adr/nhat-ky-quyet-dinh.md) 2026-10-06); ở Development BE nhận thêm mọi `https://localhost:<cổng>` — vì sao hai mục vẫn giữ: [`../../quy-uoc/chay-may-dev.md`](../../quy-uoc/chay-may-dev.md) §5. BE quyết origin bằng một hàm chung cho CORS và lớp 1 chống giả mạo ([ADR-0129](../../adr/0129-development-nhan-moi-cong-https-localhost-mot-ham-origin-chung-cho-cors-va-lop-1-go-kiem-cong-prestart.md); đối chiếu 2026-11-09): `src/BE/Core/CoreAndSkill.Core.Web/Security/OriginAllowlist.cs`, chuỗi `isDevelopment && IsDevelopmentLocalhostOrigin(origin)`; CORS gọi nó ở `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreWebServiceCollectionExtensions.cs`, chuỗi `.SetIsOriginAllowed(originAllowlist.IsAllowed)`. Tệp gốc để trống: `src/BE/CoreAndSkill.Api/appsettings.json`, chuỗi `"AllowedOrigins": []`; bản cài Compose dựng origin FE từ `FE_DOMAIN`, không biến riêng (đối chiếu 2026-11-09) — `docker-compose.yml`, chuỗi `Core__Auth__AllowedOrigins__0: "https://${FE_DOMAIN:?`; luật và lý do: [`../be/18-trien-khai-va-van-hanh.md`](../be/18-trien-khai-va-van-hanh.md) §3, mục *Bản cài Compose* | — |
| Cấu hình nhúng lúc build, tệp môi trường của bản cài (§5.2) | `src/FE/angular.json` cấu hình `production`, chuỗi `"with": "src/environments/environment.ban-cai.ts"`; tệp đó bị loại khỏi git — `src/FE/.gitignore`, chuỗi `/src/environments/environment.ban-cai.ts`; mẫu tạo tệp: `src/FE/src/environments/environment.ban-cai.mau` | — |
| SPA fallback, cache header, header bảo mật, giữ tệp bản trước (§2, §3, §6) | `Caddyfile` gốc repo, khối `{$FE_DOMAIN}` — chỉ đọc tệp, chưa chạy. Fallback (§2): matcher `@spa` gồm `not file` và `not path_regexp \.[A-Za-z0-9]+$`, rồi chuỗi `rewrite @spa /index.html`. Cache (§3): mặc định chuỗi `header Cache-Control "no-store"`; tệp băm chuỗi `header @hashed Cache-Control "public, max-age=31536000, immutable"`; tệp dịch chuỗi `header @i18n Cache-Control "no-cache"`; font không băm ở `/fonts/` chuỗi `header @fonts Cache-Control "public, max-age=86400"`. Header (§6): chuỗi `Strict-Transport-Security`, `Content-Security-Policy-Report-Only` (CSP chỉ báo cáo — [`14-security.md`](14-security.md) §4.2), `X-Content-Type-Options "nosniff"`, `X-Frame-Options "DENY"`, `Referrer-Policy "strict-origin-when-cross-origin"`. Giữ tệp bản trước (§3.3): chỉ là chú thích ở `Caddyfile` và `.env.example` — chép đè, không xoá; chưa có script triển khai | Chạy thật rồi đi hết §9; CSP sang chế độ chặn theo [`14-security.md`](14-security.md) §4.2 |
| Bắt lỗi tải phần mã của tuyến, mời tải lại trang (§3.3 lớp 2) | Đối chiếu 2026-10-08, đọc tệp: `src/FE/src/app/core/navigation/loi-tai-phan-ma.ts`, chuỗi `export class LoiTaiPhanMaService {`; `src/FE/src/app/app.config.ts`, chuỗi `inject(LoiTaiPhanMaService);` — neo từng luật ở [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §8.1 | — |
| Quét bí mật trên thư mục build trước khi đưa lên (§8 bước 2b) | Chỉ có thủ tục tay ở §8; CI không quét thư mục build ([ADR-0120](../../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md) quyết định 4). Chưa bản cài nào được dựng, nên bước này chưa chạy lần nào | Chạy ở mỗi lần dựng bản cài — bằng tay cho tới khi có script dựng bản cài |
| Hiển thị phiên bản build trong app (§7) | Mã commit là hằng số của bản dựng: `src/FE/src/app/core/config/build-info.ts`, chuỗi `export const MA_COMMIT`; CI ghi đè tệp đó ngay trước `ng build` — `.github/workflows/docs-gate.yml`, chuỗi `4b0. nhúng mã commit vào bản dựng`; khung ứng dụng truyền xuống `Footer` — `src/FE/src/app/platform/shell/shell.component.ts`, chuỗi `protected readonly maCommit = maCommitNgan();`. Không có mã thì ẩn ([`../../Design/Components/Footer.md`](../../Design/Components/Footer.md) §Phiên bản hiển thị) | Bản cài: §8 bước 2 nhúng mã bằng tay, đúng thao tác của CI — chưa bản cài nào dựng theo, chưa có script |

---

## 1. Bản build FE là gì

Kết quả build là một thư mục **tệp tĩnh**: một tài liệu HTML gốc, các tệp JavaScript và CSS có mã băm trong tên, và tài nguyên (font, ảnh, tệp dịch).

Hai hệ quả quan trọng:

| Hệ quả | Nghĩa là |
| --- | --- |
| Không cần runtime để phục vụ | Bất kỳ máy chủ tệp tĩnh nào cũng phục vụ được. Không cần Node ở môi trường chạy |
| **Không có tầng nào để giấu bí mật** | Mọi thứ trong thư mục đó công khai — [`14-security.md`](14-security.md) §5 |

---

## 2. SPA fallback — điều kiện tối thiểu để routing hoạt động

### 2.1 Vấn đề

Ứng dụng một trang định tuyến ở phía trình duyệt. Máy chủ chỉ có một tài liệu HTML gốc; nó **không** có tệp nào tương ứng với một đường dẫn của ứng dụng.

Điều đó không gây vấn đề khi người dùng đi lại trong app (router xử lý, không có request nào tới máy chủ). Nó gây vấn đề ở đúng ba tình huống:

1. Tải lại trang khi đang ở một màn bất kỳ
2. Mở một đường link được gửi cho mình
3. Mở một dấu trang đã lưu

Cả ba đều gửi một request thật tới máy chủ cho một đường dẫn không tồn tại → 404.

### 2.2 Luật

> **Mọi đường dẫn không khớp tệp thật đều trả về tài liệu HTML gốc, với mã trạng thái 200.**

### 2.3 Hai bẫy

| Bẫy | Hậu quả |
| --- | --- |
| Fallback bắt cả tệp tĩnh không tồn tại | Một tệp thiếu trả về HTML với mã 200, trình duyệt cố dùng nó làm ảnh hoặc script, và lỗi thật bị che |
| Trả fallback với mã 404 thay vì 200 | Một số công cụ và bộ nhớ đệm xử lý khác đi; và bản thân việc này là nói dối về trạng thái |

Thứ tự đúng của quy tắc phục vụ: (1) tệp tĩnh tồn tại → trả tệp; (2) còn lại → trả HTML gốc.

Bản cài Compose (matcher `@spa` ở `Caddyfile`, neo ở bảng đầu tệp) tách tệp tĩnh khỏi đường dẫn app bằng **đuôi tệp**: đường dẫn không khớp tệp thật mà có đuôi rơi xuống `file_server` → 404, nên bẫy thứ nhất không xảy ra. Cái giá: đường dẫn app có đoạn cuối mang dấu chấm (ví dụ `/tep/bao-cao.xlsx` làm tham số route) tải lại cũng ra 404.

> Máy chủ FE **không** có tiền tố API nào để chuyển tiếp — API ở origin riêng (§4.1). Bẫy *"fallback bắt luôn tiền tố API"* chỉ tồn tại ở mô hình cùng nguồn, tức phương án đã loại ở [`../../adr/0015-fe-va-api-khac-nguon.md`](../../adr/0015-fe-va-api-khac-nguon.md).

---

## 3. Cache header — hai loại tệp, hai chính sách ngược nhau

Đây là chỗ **một cấu hình sai gây ra loại lỗi tệ nhất**: người dùng chạy phiên bản cũ mà không biết, và tải lại trang cũng không sửa được.

| Loại tệp | Chính sách | Vì sao an toàn / cần thiết |
| --- | --- | --- |
| Tệp có **mã băm trong tên** (JS, CSS, font) | Cache rất lâu, đánh dấu bất biến | Nội dung đổi → tên đổi → URL khác. Không bao giờ có chuyện tệp cũ mang tên mới |
| **Tài liệu HTML gốc** | **Không cache**, luôn hỏi lại máy chủ | Nó là tệp chứa danh sách tên các tệp băm. Cache nó là ghim toàn bộ phiên bản cũ |
| Tệp dịch, tệp cấu hình chạy | Cache ngắn hoặc kiểm lại mỗi lần | Chúng đổi mà **không** đổi tên |

Bản cài Compose (`Caddyfile`, neo ở bảng đầu tệp): mặc định `no-store` phủ HTML gốc — kể cả khi nó là kết quả của fallback — và mọi tệp không thuộc nhóm nào; font tự chứa ở `/fonts/` không băm tên nên theo nhóm ba, cache một ngày. Mẫu nhận tệp băm (`@hashed`) chưa đối chiếu với tên tệp của một bản build production; lệch mẫu thì tệp băm rơi về `no-store` — tải chậm hơn chứ không chạy bản cũ.

### 3.1 Vì sao HTML gốc là điểm mấu chốt

Chuỗi phụ thuộc là: HTML gốc → tên tệp băm → nội dung. Trình duyệt chỉ biết phiên bản mới tồn tại khi nó lấy được HTML gốc mới.

Cache HTML gốc, dù chỉ vài giờ, tạo ra tình huống: đã triển khai bản mới, một phần người dùng vẫn chạy bản cũ, và **tải lại trang không giúp gì** vì trình duyệt lấy HTML từ bộ nhớ đệm. Cách sửa duy nhất từ phía người dùng là xoá dữ liệu duyệt web — điều không thể hướng dẫn hàng loạt.

### 3.2 Tệp dịch là ngoại lệ hay bị quên

Tệp dịch được tải lúc chạy và tên **không** có mã băm. Cache dài thì sửa một câu chữ không có tác dụng cho người đã truy cập. Cache header cho chúng phải theo nhóm thứ ba, không theo nhóm thứ nhất.

### 3.3 Triển khai giữa chừng

Người dùng đang mở app cũ, một bản mới được triển khai, các tệp băm cũ có thể không còn trên máy chủ. Khi họ điều hướng tới một nhánh lazy chưa tải, request tệp đó trả 404 và **màn hình không mở được**.

Hai lớp giảm nhẹ, đã chốt:

**1. Giữ lại tệp của bản trước một thời gian sau khi triển khai.** Nó là việc của tầng phục vụ, không cần dòng code FE nào.

Bản cài Compose: triển khai chép đè vào thư mục `FE_WWW_DIR`, không xoá trước, nên tệp băm của bản trước còn nằm lại (chú thích khối `{$FE_DOMAIN}` ở `Caddyfile`). Dọn tệp cũ chưa có thủ tục — thư mục lớn dần qua mỗi lần triển khai.

**2. Bắt lỗi tải phần mã của tuyến và mời tải lại trang — chốt 2026-10-08.** Chưa màn nào hiện trong lần tải trang → trang lỗi toàn trang, `EmptyState` biến thể `error`, `headingLevel` 1, nút "Tải lại trang". Đã có màn → giữ màn, bật toast `danger` không tự tắt kèm nút "Tải lại trang". Hình thức và câu chữ: [`../../Design/Screens/02-trang-loi.md`](../../Design/Screens/02-trang-loi.md), mục *Không tải được phần ứng dụng* và mục *Điều hướng hỏng khi đã có màn đang hiện*; cách bắt: [`../../quy-uoc/fe-routing-guard.md`](../../quy-uoc/fe-routing-guard.md) §8.1 — ✅ CÓ THẬT (đối chiếu 2026-10-08), neo ở khối cuối mục đó và ở bảng đầu tệp này. Lớp này cần lớp 1: tệp cũ còn trên máy chủ thì phần lớn lần điều hướng không hỏng tới mức phải báo.

---

## 4. Chạy sau reverse proxy

### 4.1 FE và API ở HAI nguồn khác nhau

> **FE và API phục vụ ở hai origin khác nhau** — dev là hai cổng, prod là hai subdomain **của cùng một tên miền gốc**. Reverse proxy phục vụ tệp tĩnh của FE; API có origin riêng.
>
> 📖 Chuỗi ràng buộc kéo theo — CORS allowlist, cookie `SameSite=Lax`, antiforgery hai lớp: đọc [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md) §7. Đó là file chủ; mục này **không** chép lại.

Bốn cái giá phải trả, ghi ra để không ai tưởng chúng là chi tiết vặt:

| Cái giá | Nghĩa là |
| --- | --- |
| Phải cấu hình CORS đúng ở **mọi** môi trường | Sai một chỗ là hỏng đăng nhập, và triệu chứng không nói ra nguyên nhân |
| FE và API phải cùng tên miền gốc **và cùng scheme** | Để cookie phiên còn là cùng site. FE `http` mà API `https` là khác site — cookie không được gửi ⇒ **HTTPS ở mọi môi trường, kể cả máy lập trình viên** |
| Phải có cơ chế biết đường dẫn API | Xem §5 — quyết định ở đó phụ thuộc trực tiếp vào mục này |
| CSP phải khai thêm nguồn cho phép | Một dòng nữa phải đúng, và nó chỉ sai lúc chạy thật |

**Đổi lại:** không cần dựng reverse proxy ở mọi môi trường, và FE triển khai độc lập với BE.

### 4.2 Môi trường phát triển phải giống

Ở máy dev, máy chủ dev và API ở hai cổng khác nhau — **đúng hình dạng của môi trường thật** (§4.1). Vì vậy dev **bật CORS và chạy HTTPS**, không dùng proxy của máy chủ dev để giấu ranh giới đi.

Vì sao: mọi bẫy của cookie giữa hai origin — lệch scheme thành khác site, allowlist thiếu một origin, antiforgery không qua được — chỉ lộ ra khi dev chạy đúng cấu hình của thật. Dùng proxy dev để né chúng nghĩa là dời toàn bộ nhóm lỗi đó sang lần triển khai đầu tiên.

**Dựng HTTPS ở máy dev — một lần mỗi máy.** Máy chủ dev của FE dùng chính chứng chỉ dev của .NET, xuất ra dạng PEM:

```bash
# 1. Tạo và tin cậy chứng chỉ dev trên máy
dotnet dev-certs https --trust

# 2. Xuất cert và khoá riêng dạng PEM, khoá không đặt mật khẩu, vào src/FE/.cert/ — chạy từ gốc repo.
#    Ra hai tệp: localhost.pem (cert) và localhost.key (khoá riêng).
mkdir -p src/FE/.cert
dotnet dev-certs https --export-path "src/FE/.cert/localhost.pem" --format Pem --no-password
```

Khối `serve` của `src/FE/angular.json` đọc đúng hai tệp đó — đường dẫn tính từ `src/FE/` (neo ở bảng đầu tệp).

🛑 **Không commit cert hay khoá riêng.** Một khoá riêng đã vào commit thì nằm trong lịch sử git — commit xoá sau đó không gỡ được nó. Dòng ignore và lệnh kiểm: hàng chứng chỉ HTTPS ở [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) §6.3.

### 4.3 Ứng dụng không nằm ở gốc miền

Nếu app phục vụ dưới một đường dẫn con, phải khai đường dẫn cơ sở lúc build — nếu không mọi tài nguyên sẽ được yêu cầu từ gốc miền và không tìm thấy.

Đây là một quyết định phải chốt **trước** khi build, không sửa được sau khi đã có artifact. Mặc định của repo này: **phục vụ ở gốc miền**; nếu một dự án cần khác, đó là một biến thể phải khai tường minh.

### 4.4 Nén

Nén tệp văn bản ở tầng proxy. Nén trước lúc build và để proxy phục vụ tệp đã nén là tốt hơn nữa (nén một lần thay vì nén mỗi request). Ảnh và font đã nén sẵn thì không nén lại — tốn CPU mà không giảm được gì.

---

## 5. Cấu hình: lúc build hay lúc chạy

### 5.1 Hai mô hình

| | Nhúng lúc build | Đọc lúc chạy |
| --- | --- | --- |
| Cách làm | Giá trị nội suy vào bundle | Một tệp cấu hình được tải trước khi app khởi động |
| Số artifact | **Một artifact cho mỗi môi trường** | **Một artifact cho mọi môi trường** |
| Đổi giá trị | Build lại và triển khai lại | Sửa một tệp, tải lại trang |
| Rủi ro | Build cho môi trường A đem triển khai lên B là sai âm thầm | Thêm một request chặn lúc khởi động |

### 5.2 Quyết định cho v1

> **Đường dẫn API là giá trị BẮT BUỘC phải cấu hình.** Vì FE và API khác nguồn (§4), FE không suy ra được đường dẫn API từ nguồn của chính nó.

Đây là hệ quả trực tiếp của §4, không phải một lựa chọn riêng: chọn khác nguồn là chọn luôn việc phải có chỗ khai đường dẫn API.

Những gì còn lại (chế độ build, phiên bản hiển thị) nhúng lúc build là đủ.

> **Chốt cho v1: nhúng lúc build.** `apiBaseUrl` nằm ở `environments/*.ts` ([`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md)), mỗi môi trường một artifact. Hình dạng giá trị: [`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §2.1.
>
> Cái giá phải canh: build cho môi trường A đem triển khai lên B là sai **âm thầm** — FE gọi nhầm API. Artifact đặt tên theo môi trường, và bước kiểm sau triển khai xác nhận trang vừa lên gọi đúng API.

> **Mã đơn vị nghiệp vụ cũng nhúng lúc build** — khoá `tenantCode` ([ADR-0116](../../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md)). Mỗi công ty một bản cài, nên người dùng không gõ mã; trang đăng nhập gửi mã này.
>
> Build `production` lấy cả `apiBaseUrl` lẫn `tenantCode` từ **tệp môi trường của bản cài** — tệp nằm ngoài git, người triển khai tạo cho từng bản cài. Thiếu tệp thì build hỏng (luật F54), nên ca *"quên thay placeholder"* không ra được artifact; ca *"build cho A đem lên B"* ở trên thì vẫn còn. Tên tệp, vai của từng tệp môi trường: [`../../quy-uoc/fe-architecture.md`](../../quy-uoc/fe-architecture.md) §1.1.
>
> Cái giá riêng của `tenantCode`: nó phải bằng mã đơn vị nghiệp vụ trong database mà không máy nào kiểm. Lệch thì **mọi** người dùng trượt đăng nhập với câu chung, sửa phải build lại — bước kiểm ở §8 bắt ca này.

**Vì sao ghi quyết định này ra dù nó là "không làm gì":** đây chính xác là loại quyết định mà sáu tháng sau có người sẽ hỏi *"sao không có config runtime?"* và tự thêm vào. Ghi ra để câu trả lời có sẵn, kèm điều kiện mở lại.

### 5.3 Điều kiện mở lại

Cần cấu hình runtime khi một bản cài **đang chạy** phải đổi một giá trị mà không được build lại — đường dẫn API của chính bản cài đó đổi, hay một cờ bật/tắt tính năng theo môi trường.

> Điều kiện *"FE và API ở hai nguồn khác nhau"* **không còn là điều kiện** — nó luôn đúng (§4.1). Thứ còn phải chọn là **nhúng lúc build hay đọc lúc chạy**, không phải *có cần cấu hình hay không*.
>
> *"Muốn một artifact dùng cho nhiều môi trường"* cũng **không** là điều kiện mở lại: hướng đó bị khoá `K48` (§10). Core là khung; mỗi khách hàng là một dự án phát triển tiếp từ Core và build riêng, mỗi bản cài một artifact. Muốn đi hướng đó thì viết ADR trích `K48` ([`../README.md`](../README.md) §9.1 luật 3), không mở lại qua mục này.

Khi làm, hai ràng buộc: tệp cấu hình **không cache** (§3), và app phải xử lý được trường hợp tải cấu hình thất bại — bằng một màn lỗi có nghĩa, không phải màn trắng.

---

## 6. Header bảo mật đặt ở tầng phục vụ

Bốn header đặt ở proxy, không đặt trong ứng dụng:

| Header | Việc |
| --- | --- |
| Chính sách nội dung (CSP) | Giới hạn nguồn được phép chạy mã — [`14-security.md`](14-security.md) §4 |
| Chặn đoán kiểu nội dung | Không để trình duyệt tự suy diễn kiểu tệp |
| Kiểm soát nhúng vào khung | Chống bị nhúng vào trang khác để lừa thao tác |
| Kiểm soát thông tin nguồn gửi kèm | Không rò rỉ đường dẫn nội bộ khi người dùng đi ra ngoài |

**Vì sao ở proxy chứ không trong ứng dụng:** ứng dụng tĩnh không có chỗ nào để đặt header. Thẻ trong tài liệu HTML chỉ hỗ trợ một phần và không thay được header thật.

Hệ quả tổ chức phải nói rõ: **cấu hình proxy là một phần của sản phẩm**, không phải việc riêng của người vận hành. Nó phải nằm trong kho mã, được review, và đi cùng phiên bản.

HSTS cho cả hai origin, và header nào của API do proxy hay do Core đặt: [`../be/09-security-beyond-auth.md`](../be/09-security-beyond-auth.md) §6. Proxy của bản cài Compose là Caddy: [`../be/18-trien-khai-va-van-hanh.md`](../be/18-trien-khai-va-van-hanh.md) §3.

Bản cài Compose: khối `{$FE_DOMAIN}` ở `Caddyfile` đặt đủ bốn header cộng HSTS (neo ở bảng đầu tệp). CSP đang ở chế độ **chỉ báo cáo**, nên chỉ thị chống nhúng khung của nó không chặn gì — nhúng khung bị chặn thật bằng header `X-Frame-Options`. Việc còn lại trước khi CSP chặn thật: [`14-security.md`](14-security.md) §4.2.

---

## 7. Kiểm tra sức khoẻ và chẩn đoán

| Việc | Ghi chú |
| --- | --- |
| Một endpoint kiểm tra sức khoẻ tĩnh | Để hệ thống giám sát biết tầng tệp tĩnh còn sống |
| Hiển thị phiên bản build ở đâu đó trong app | Khi người dùng báo lỗi, câu đầu tiên là "bạn đang chạy bản nào" |
| Không bật bản đồ mã nguồn công khai ở môi trường thật | Nó phơi toàn bộ mã nguồn có cấu trúc. Sinh ra và giữ riêng cho việc chẩn đoán thì được |

Dòng thứ hai rẻ và có ích bất ngờ: nó phân biệt được *"lỗi đã sửa nhưng người dùng chưa nhận bản mới"* (vấn đề cache, §3) với *"lỗi chưa sửa"* — hai tình huống trông giống hệt nhau từ phía người báo lỗi.

---

## 8. Quy trình triển khai

Thứ tự đúng, và lý do của thứ tự:

```
1. Chạy cổng FE  ────► đỏ thì dừng, chưa build gì cả
2. Build         ────► nhúng mã commit trước (lệnh dưới); cần tệp môi trường của bản cài (§5.2), thiếu thì build hỏng. Ra artifact có mã băm; xong thì hoàn nguyên build-info.ts
2b. Quét bí mật  ────► gitleaks dir trên thư mục build, cấu hình của repo; thư mục không có tệp .js là đỏ; đỏ thì dừng
3. Đưa tệp tĩnh lên trước
4. Chuyển proxy sang bản mới
5. Kiểm nhanh: mở app, tải lại ở một màn sâu, đăng nhập
```

**Bước 2 nhúng mã commit đúng như CI** — bước `4b0. nhúng mã commit vào bản dựng` của `.github/workflows/docs-gate.yml` ghi đè `src/FE/src/app/core/config/build-info.ts` ngay trước `ng build`. Không làm thì bản cài ẩn phiên bản và §7 không đạt. Tệp được git theo dõi với giá trị `null`, nên build xong phải hoàn nguyên. Chạy từ gốc repo, trên cây làm việc sạch để mã trỏ đúng mã nguồn đã dựng:

```bash
printf "export const MA_COMMIT: string | null = '%s';\n" "$(git rev-parse HEAD)" > src/FE/src/app/core/config/build-info.ts
# … ng build bằng tệp môi trường của bản cài …
git checkout -- src/FE/src/app/core/config/build-info.ts
```

**Bước 2b quét đúng bản sẽ lên máy chủ**: bước 2 nội suy giá trị của bản cài vào bundle, và bản build trên CI dùng tệp bản cài giả nên không thay được bước này ([`../../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md`](../../adr/0120-https-ket-thuc-o-caddy-trong-compose-header-chia-doi-postgres-quan-tri-cum-rieng-quet-bundle-fe-khi-dung-ban-cai.md)). Phiên bản gitleaks: đúng bản ghim ở job `gitleaks` của CI. Chốt *"có tệp `.js`"* chặn ca quét nhầm thư mục rỗng mà vẫn xanh (luật T6). 🚧 Chưa có script dựng bản cài — người dựng chạy bước này bằng tay; luật S6 ở [`../../RULES-BE.md`](../../RULES-BE.md) khai phần bundle FE là chưa có cổng.

Bước 3 trước bước 4 là để tránh khoảng thời gian mà HTML gốc mới đã phục vụ nhưng tệp băm mới chưa có.

Bản cài Compose không có bước đổi proxy: Caddy phục vụ thẳng thư mục `FE_WWW_DIR` (neo ở bảng đầu tệp). Bước 3–4 là chép đè thư mục build vào đó, không xoá trước (§3.3); để giữ thứ tự trên, `index.html` chép **sau cùng**. Quay lui là chép lại artifact của bản trước theo cùng thứ tự. 🚧 Chưa có script — người triển khai làm tay, và chưa ai chạy thử.

**Hai thứ phải kiểm ở bước 5, vì chúng chỉ hỏng ở môi trường thật:** tải lại trang ở một màn sâu (kiểm SPA fallback, §2) và đăng nhập (kiểm cookie và XSRF sau proxy, §4). Đăng nhập ở bước 5 dùng trang hai ô, không gõ mã đơn vị: trượt với mật khẩu đúng nghĩa là `tenantCode` của bản cài lệch mã trong database (§5.2).

**Quay lui phải làm được.** Giữ artifact của bản trước để quay lui là đổi proxy về, không phải build lại từ mã nguồn.

---

## 9. Kiểm chứng

- [ ] Tải lại trang ở một màn sâu → hiển thị đúng màn đó, không phải 404
- [ ] Gọi một endpoint API không tồn tại → nhận 404 JSON, **không** phải HTML gốc
- [ ] Một tệp tĩnh không tồn tại → 404, không phải HTML gốc với mã 200
- [ ] Header cache: tệp băm cache lâu; **HTML gốc không cache**; tệp dịch cache ngắn
- [ ] Triển khai một bản mới → người dùng đang mở app nhận bản mới sau khi tải lại
- [ ] Đăng nhập chạy được sau proxy; cookie được gửi kèm request
- [ ] Bốn header bảo mật có mặt trong phản hồi
- [ ] Không có bản đồ mã nguồn công khai ở môi trường thật
- [ ] Phiên bản build hiển thị được trong app
- [ ] Quay lui về bản trước làm được mà không cần build lại

---

## 10. §Áp dụng — Core này có gì, cố ý thiếu gì

> Đối chiếu với `src/` ngày 2026-11-05, chỉ bằng đọc tệp: hàng ghi ✅ CÓ THẬT có neo ở cột *Ghi chú* hoặc ở bảng *có thật hôm nay → sẽ thành* đầu tệp; hàng ghi 🚧 có hàng ở bảng đó, cột *Sẽ thành* nói việc còn lại. Ý nghĩa ký hiệu: [`../README.md`](../README.md) §9.

| Hạng mục | Trạng thái | Ghi chú |
| --- | --- | --- |
| SPA fallback theo đúng thứ tự hai quy tắc | 🚧 đang thi công | §2.3 — cấu hình có ở `Caddyfile`, chưa chạy: bảng đầu tệp, hàng *SPA fallback, cache header…* |
| Cache header cho ba nhóm tệp | 🚧 đang thi công | §3 — tài liệu HTML gốc **không** cache. Cùng hàng bảng đầu tệp như trên |
| FE và API ở hai origin, CORS allowlist khai đúng ở mọi môi trường | ✅ CÓ THẬT 2026-11-05 | §4.1 — ràng buộc kéo theo ở file chủ BE. Neo: bảng đầu tệp, hàng *FE và API ở hai origin* và hàng *CORS allowlist* |
| HTTPS ở mọi môi trường, kể cả máy lập trình viên | 🚧 đang thi công | §4.1 — lệch scheme là khác site. Máy dev có thật; bản cài Compose có cấu hình, chưa chạy: bảng đầu tệp, hàng *HTTPS ở máy dev* và *HTTPS và tầng phục vụ ở bản cài Compose* |
| Đã chọn và khai cơ chế cấu hình đường dẫn API | ✅ CÓ THẬT 2026-11-05 — nhúng lúc build | §5.2. Neo: bảng đầu tệp, hàng *Cấu hình nhúng lúc build* |
| Cấu hình proxy nằm trong kho mã, được review | ✅ CÓ THẬT 2026-11-05 | §6 — bản cài Compose: `Caddyfile` gốc repo (chuỗi `root * /srv/fe`), chốt 2026-10-06. Vế *được review* là quy trình, đọc tệp không kiểm được |
| Quét bí mật trên thư mục build trước khi đưa lên | 🚧 đang thi công — chốt 2026-10-06 | §8 bước 2b; chưa có script, chạy tay. Bảng đầu tệp, hàng *Quét bí mật…* |
| Hiển thị phiên bản build trong app | ✅ CÓ THẬT 2026-11-05 ở bản dựng của CI | §7 — phân biệt "chưa nhận bản mới" với "lỗi chưa sửa". Neo và giới hạn: bảng đầu tệp, hàng *Hiển thị phiên bản build trong app* — bản cài nhúng mã bằng tay ở §8 bước 2, chưa bản cài nào dựng theo |
| Giữ tệp của bản trước sau khi triển khai | 🚧 đang thi công | §3.3 lớp 1 — chỉ là chú thích và thủ tục tay: bảng đầu tệp, hàng *SPA fallback, cache header…* |
| **Bắt lỗi tải phần mã của tuyến, mời tải lại trang** | ✅ CÓ THẬT 2026-11-05 — chốt 2026-10-08 | §3.3 lớp 2. Neo: `src/FE/src/app/core/navigation/loi-tai-phan-ma.ts` (chuỗi `export class LoiTaiPhanMaService {`), bảng đầu tệp; [`../../Design/Screens/02-trang-loi.md`](../../Design/Screens/02-trang-loi.md) mục *Không tải được phần ứng dụng* |
| Tầng cấu hình lúc chạy | ❌ chưa | Điều kiện: tình huống ở §5.3 |
| Endpoint kiểm tra sức khoẻ tĩnh | ❌ chưa | Điều kiện: có hệ thống giám sát thật |
| Phục vụ dưới một đường dẫn con | ❌ chưa | Điều kiện: dự án cụ thể cần — phải khai tường minh **trước** khi build (§4.3) |
| Một bản build dùng chung cho mọi môi trường | ❌ loại, không hoãn `K48` | §5.2 — Core là khung; mỗi khách hàng phát triển tiếp và build riêng, mỗi bản cài một artifact ([`../../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md`](../../adr/0116-ma-don-vi-nghiep-vu-lay-tu-cau-hinh-ban-cai-form-hai-o-van-hanh-qua-lien-ket-rieng.md)) |
| Bản đồ mã nguồn công khai ở môi trường thật | ❌ loại, không hoãn `K49` | §7 |

> Cách đọc ba ký hiệu của bảng trên — và khi nào *"FE thiếu X"* là finding: [`../README.md`](../README.md) §9.

---

## 11. Đọc tiếp

| Câu hỏi | File |
| --- | --- |
| Cái gì được commit, cái gì là artifact | [`../../quy-uoc/repo-artifact.md`](../../quy-uoc/repo-artifact.md) |
| CSP và header bảo mật | [`14-security.md`](14-security.md) §4 |
| Cookie và CORS — FE và API khác nguồn ở mọi môi trường | [`07-auth-identity.md`](07-auth-identity.md) §8 |
| Cache tệp dịch | [`08-i18n.md`](08-i18n.md) §2 |
| Ngân sách bundle | [`13-performance.md`](13-performance.md) §4 |
| Cổng phải chạy trước khi build | [`trien-khai/05-gate.md`](trien-khai/05-gate.md) |
