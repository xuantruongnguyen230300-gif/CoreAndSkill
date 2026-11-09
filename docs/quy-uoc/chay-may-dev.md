---
kind: luat
scope: core
verified: 2026-10-06
---

# Chạy máy dev — từ clone tới đăng nhập (DB + BE + FE)

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Đối chiếu 2026-10-06, chỉ bằng đọc tệp: mọi lệnh, giá trị, đường dẫn dưới đây neo vào tệp nguồn bằng chuỗi tìm được.
>
> | Có thật hôm nay | Sẽ thành |
> | --- | --- |
> | Lệnh, khoá cấu hình, đường dẫn ở §1–§4 khớp tệp nguồn (neo tại chỗ) | Một lần chạy trọn §2 trên một máy Windows sạch bằng Git Bash — **chưa ai chạy**; phần database chung điều kiện với [`../database/script-runbook.md`](../database/script-runbook.md) §6 |
> | Cổng của cụm ở §5 có thật: `scripts/dev-config-check.sh` và canary `scripts/tests/dev-config-check.test.sh`, chạy tay xanh 2026-10-06 | Job CI `dev-config` trong `.github/workflows/docs-gate.yml` chạy lần đầu — **chưa chạy**: thay đổi chưa push |
> | BE tự chuyển console sang UTF-8: `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreConsoleEncoding.cs`, chuỗi `UseUtf8OnWindowsConsole` — chưa qua review Core | Qua review; chạy thử trong PowerShell — **chưa ai chạy** |
> | **§2 bước 10–11, §4 và §5 khớp mã sau [ADR-0129](../adr/0129-development-nhan-moi-cong-https-localhost-mot-ham-origin-chung-cho-cors-va-lop-1-go-kiem-cong-prestart.md)** — đối chiếu 2026-11-09 bằng đọc tệp và test: `npm start` không còn bước kiểm cổng — `src/FE/package.json`, chuỗi `"start": "ng serve"`, không còn khoá `prestart`, thư mục `src/FE/scripts/` không còn; BE dev nhận mọi `https://localhost:<cổng>` — `src/BE/Core/CoreAndSkill.Core.Web/Security/OriginAllowlist.cs`, chuỗi `isDevelopment && IsDevelopmentLocalhostOrigin(origin)`, test `CorsAndLayerOne_GiveTheSameVerdict_ForTheSameOrigin` xanh; cổng T14 không đọc `AllowedOrigins` — `grep -c AllowedOrigins scripts/dev-config-check.sh` ra 0, cổng và canary chạy tay xanh | Một lần chạy FE ở cổng khác 4200 trên trình duyệt thật và đăng nhập được — **chưa ai chạy** |
> | Hai script Git Bash bọc các bước của §2: `scripts/dev/chuan-bi-may-dev.sh` (không chạm database — chế độ `--kiem-tra` chạy xanh trên một máy dev 2026-10-06) và `scripts/dev/ap-db-dev.sh` (người dùng chạy — đầu tệp chuỗi `NGƯỜI DÙNG CHẠY`; chỉ qua `bash -n`) | `ap-db-dev.sh` chạy trọn một lần trên database cục bộ — **chưa ai chạy** |
>
> **Tệp này làm chủ hai thứ:** thứ tự các bước từ clone tới đăng nhập (§2), và cụm cổng/origin phải khớp (§5). Bước nào có tệp chủ thì chỉ trỏ sang đó, không chép lệnh.

---

## 1. Điều kiện cần

Phiên bản đọc từ tệp nguồn bằng lệnh — tệp này không chép số.

| Công cụ | Nguồn phiên bản | Lệnh kiểm | Đạt khi |
| --- | --- | --- | --- |
| .NET SDK | `src/BE/global.json` — CI đọc đúng tệp này; nâng SDK: [`repo-artifact.md`](repo-artifact.md) §3.2 | `cd src/BE && dotnet --version` | In ra phiên bản. Chạy trong `src/BE` thì `dotnet` đọc `global.json`; máy thiếu bản khớp thì lệnh báo lỗi thay vì in số |
| Node, npm | `src/FE/.nvmrc` — CI đọc đúng tệp này; khoá `engines` của `src/FE/package.json` là miền chấp nhận | `node -v` | Bằng `.nvmrc`; khác thì ít nhất phải nằm trong `engines` |
| PostgreSQL | `docker-compose.yml`, dòng `image: postgres:` | `psql --version` | Cùng bản lớn. Cài sẵn hay chạy container: bảng *Chỗ* ở runbook §6. Windows không thấy `psql`: [`../wiki-core/be/ly-do/script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §6, mục *Windows — `psql` không có trong PATH* |
| Git Bash | — | `bash --version` | Khối lệnh của tệp này và của runbook viết cho bash; PowerShell, cmd không chạy nguyên văn |
| Windows Terminal — nên có | — | — | Console có phông đủ chữ tiếng Việt — hàng cuối §4 |

`.env` và `.env.example` ở gốc repo chỉ dành cho `docker-compose.yml`, tức bản cài thật (`.env.example`, chuỗi `Mẫu cho docker-compose.yml`) — máy dev không cần. Máy dev cũng không chạy `docker-compose.yml`: bảng *Chỗ* ở runbook §6.

---

## 2. Các bước — đúng thứ tự

Chạy từ gốc repo, trong Git Bash; lệnh `dotnet` chạy trong `src/BE` (dạng `(cd src/BE && …)` giữ cửa sổ ở gốc repo), vì SDK ghim ở `src/BE/global.json` chỉ có hiệu lực dưới `src/BE`. Bước 2–6 là năm bước ở runbook §6; bước 8 trở đi chưa tệp nào khác viết.

| # | Việc | Làm theo | Xong khi |
| --- | --- | --- | --- |
| 1 | Postgres chạy trên máy; `psql` gọi được, hoặc dùng DBeaver, pgAdmin | Bảng *Chỗ* ở runbook §6 | `psql --version` in ra phiên bản |
| 2 | Tạo hai role và database `coreandskill_dev` — tệp `database/dev/tao-db-local.sql`, chạy bằng `postgres`, nối vào database `postgres` | Runbook §6 bước 1 | Không lỗi |
| 3 | Áp script schema bằng `coreandskill_owner`, nối vào `coreandskill_dev`; câu kiểm *đang ở đâu, là ai* chạy trước tiên. Chưa có thư mục module nào dưới `database/scripts/modules/` thì bỏ vòng module | Runbook §6 bước 2, dẫn sang §3.3 bước 2 | Câu kiểm ra `coreandskill_dev` và `coreandskill_owner`; không vòng nào in `DUNG` |
| 4 | Cấp quyền cho `coreandskill_app` | Runbook §6 bước 3, dẫn sang §3.6 | Câu nghiệm thu của §3.6 ra 0 dòng |
| 5 | Tạo thư mục kho tệp — đường dẫn ở `Core:File:RootPath` của `src/BE/CoreAndSkill.Api/appsettings.Development.json` | Runbook §6 bước 4 | Thư mục có |
| 6 | Bootstrap, **một lần**: `(cd src/BE && dotnet run --project CoreAndSkill.Api -- core bootstrap)` — tạo hai đơn vị, hai tài khoản rồi thoát. BE đang chạy trong IDE khoá thư mục `bin` nên lệnh không build được: thêm `--no-build`, hoặc trong Visual Studio chọn hồ sơ `CoreAndSkill.Api — core bootstrap` (`src/BE/CoreAndSkill.Api/Properties/launchSettings.json`, chuỗi `"commandLineArgs": "core bootstrap"`) rồi F5. Script SQL không tạo tài khoản nào — chưa bootstrap thì `core.app_user` rỗng, đăng nhập báo sai thông tin | Runbook §3.3 bước 5 | Thoát mã 0 |
| 7 | Chứng chỉ HTTPS dev — một lần mỗi máy, làm lúc nào cũng được trước bước 10 | [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §4.2 | Thư mục chứng chỉ của FE — thư mục `.gitignore` loại, khối `serve` của `angular.json` đọc — có hai tệp PEM |
| 8 | Chạy BE: `(cd src/BE && dotnet run --project CoreAndSkill.Api)` — giữ cửa sổ này mở | §5 cặp 1 | `https://localhost:7100/scalar` mở ra trang tài liệu API |
| 9 | `cd src/FE && npm ci` — lần đầu, và mỗi khi `package-lock.json` đổi | — | Không lỗi |
| 10 | `npm start` trong `src/FE`, ở cửa sổ thứ hai. Cổng 4200 bận thì CLI hỏi có dùng cổng khác không — chọn có được | §5 cặp 2 | Log báo phục vụ ở `https://localhost:<cổng>` — mặc định 4200 |
| 11 | Mở đúng địa chỉ log của bước 10 in ra — chưa đăng nhập thì về `/dang-nhap` — rồi đăng nhập | §3 | Vào màn đổi mật khẩu bắt buộc |

**Hai script bọc các bước trên.** `bash scripts/dev/chuan-bi-may-dev.sh --kiem-tra` chỉ đọc và in điều kiện cần (§1), bước 5, 7, 9 và cụm §5; bỏ `--kiem-tra` thì nó làm phần còn thiếu của bước 5, 7, 9 — không chạm database. `bash scripts/dev/ap-db-dev.sh` làm bước 2–3 rồi so checksum, **người dùng chạy**, chỉ cho Postgres trên chính máy; bước 4 vẫn dán khối của runbook §3.6 — script in lệnh, không chép khối đó.

**Chạy script bằng DBeaver thay `psql`.** Hai kết nối, đặt tên nói rõ database và tài khoản: một tới database `postgres` bằng `postgres` — **chỉ** cho bước 2; một tới database `coreandskill_dev` bằng `coreandskill_owner` — cho bước 3, 4. Áp script schema trên kết nối tới database `postgres` là lỗi hay gặp nhất (§4). Mỗi script chạy cả tệp bằng *Execute script*. Database dev đã có, chỉ thiếu script mang khối tự chặn (Core từ `0008`): chạy được bằng `postgres` trên một kết nối tới `coreandskill_dev`; database trống, hay còn thiếu script `0001`–`0007`, thì vẫn `coreandskill_owner` — runbook §3.1, *Tài khoản chạy script*. Chi tiết: mục *Công cụ đồ hoạ (DBeaver, pgAdmin)* ở [`../wiki-core/be/ly-do/script-runbook.md`](../wiki-core/be/ly-do/script-runbook.md) §6.

**Chạy script bằng `psql` trong Git Bash.** Đặt `export PGCLIENTENCODING=UTF8` trước bước 2: script là UTF-8 có chú thích tiếng Việt, mà `psql` chạy trong console Windows có thể lấy bảng mã theo console — tài liệu `psql`, biến `PGCLIENTENCODING`.

**Bước 8, 10 chạy thế nào.** `dotnet run` dùng hồ sơ trong `src/BE/CoreAndSkill.Api/Properties/launchSettings.json`: môi trường `Development` (chuỗi `"ASPNETCORE_ENVIRONMENT": "Development"`), cổng ở `applicationUrl`. `npm start` là `ng serve` (`src/FE/package.json`, chuỗi `"start": "ng serve"`), cấu hình mặc định `development` (`src/FE/angular.json`, khối `serve`, chuỗi `"defaultConfiguration": "development"`). Trang đăng nhập: `src/FE/src/app/app.config.ts`, chuỗi `dangNhap: '/dang-nhap'`.

Bước 8 phải sau bước 3 và 5: BE từ chối khởi động khi database thiếu migration hay thiếu kho tệp (§4).

---

## 3. Ai đăng nhập vào đâu

Giá trị không chép vào đây — đọc bằng lệnh:

```bash
grep -nE '"(SystemTenantCode|FirstTenantCode|OperatorUserName|OperatorPassword|AdminUserName|AdminPassword)"' \
  src/BE/CoreAndSkill.Api/appsettings.Development.json
```

| Danh tính | Tầng | Giá trị ở | Dùng ở đâu | Đã đổi mật khẩu rồi quên |
| --- | --- | --- | --- | --- |
| `postgres` | Database — quản trị cụm | Đặt lúc cài Postgres; container của runbook §6: tham số `POSTGRES_PASSWORD` của lệnh `docker run` | Bước 2, nối vào database `postgres` | Theo cách của bản Postgres đã cài — ngoài phạm vi repo |
| `coreandskill_owner` | Database — chủ schema | `database/dev/tao-db-local.sql` | Bước 3, 4, nối vào `coreandskill_dev` | Chạy lại `tao-db-local.sql`: tệp đặt lại mật khẩu cho role đã có |
| `coreandskill_app` | Database — ứng dụng | Khoá `ConnectionStrings:Core` của `appsettings.Development.json` | BE tự nối, kể cả lệnh bootstrap — không ai gõ | Như hàng trên |
| Quản trị đơn vị — tên ở `Core:Bootstrap:AdminUserName` | Ứng dụng — đơn vị nghiệp vụ `Core:Bootstrap:FirstTenantCode` | Mật khẩu ban đầu: `Core:Bootstrap:AdminPassword` | Form hai ô ở `/dang-nhap`, **không** gõ mã đơn vị: FE gửi `tenantCode` của `src/FE/src/environments/environment.development.ts` | Tài khoản vận hành khôi phục từ khu quản trị hệ thống — luồng [`V4`](../luong/V4-khoi-phuc-quan-tri-don-vi.md), endpoint ở [`../contracts/tenants.md`](../contracts/tenants.md) §4. Dựng lại database là đường cuối |
| Vận hành hệ thống — tên ở `Core:Bootstrap:OperatorUserName` | Ứng dụng — đơn vị hệ thống `Core:Bootstrap:SystemTenantCode` | Mật khẩu ban đầu: `Core:Bootstrap:OperatorPassword` | Liên kết *Quản trị hệ thống* ở trang đăng nhập — tức `/dang-nhap?cheDo=van-hanh` — rồi gõ mã đơn vị hệ thống | Lệnh `core reset-operator-password`: runbook §8. Máy dev đặt mật khẩu mới bằng `user-secrets` trước — bỏ bước đó thì lệnh đặt lại đúng mật khẩu dev công khai |

Neo: `src/FE/src/app/platform/xac-thuc/pages/dang-nhap/dang-nhap.page.ts`, chuỗi `GIA_TRI_CHE_DO_VAN_HANH = 'van-hanh'`; chữ của liên kết: `src/FE/public/i18n/vi.json`, chuỗi `"lienKetCheDoVanHanh": "Quản trị hệ thống"`. Tên các khoá và vai của chúng: bảng khoá bootstrap ở runbook §3.3 bước 4.

**Lần đăng nhập đầu buộc đổi mật khẩu** — cả hai tài khoản bootstrap mang cờ đó; luồng [`D2`](../luong/D2-doi-mat-khau-lan-dau.md). Đổi xong thì mật khẩu trong `appsettings.Development.json` hết dùng được cho tài khoản đó, và chạy lại bootstrap **không** đặt lại: lệnh bỏ qua phần đã có (`src/BE/Core/CoreAndSkill.Core.Web/Commands/CoreCommandRunner.cs`, chuỗi `đã có — bỏ qua.`). Luồng đăng nhập: [`D1`](../luong/D1-dang-nhap.md).

---

## 4. Triệu chứng → nguyên nhân

| Triệu chứng | Nguyên nhân | Làm gì |
| --- | --- | --- |
| `https://localhost:7100` trả 404 | API không phục vụ trang nào ở gốc. Trang tài liệu chỉ ở `/scalar`, chỉ ở môi trường Development — `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreApplicationBuilderExtensions.cs`, chuỗi `app.MapScalarApiReference(` | Mở `https://localhost:7100/scalar`; kiểm BE sống ở `/health/live` |
| BE dừng ngay khi khởi động, log `KHOI DONG BI TU CHOI — database thieu migration` | Chưa áp đủ script schema | Bước 3; database đang có dữ liệu: runbook §3.4 |
| BE dừng, log `KHOI DONG BI TU CHOI — khong ket noi duoc database` | Postgres chưa chạy, database chưa tạo, hay chưa có role `coreandskill_app` | Bước 1, 2 |
| BE dừng, lỗi `Core:File:RootPath trỏ tới thư mục không tồn tại` | Thiếu thư mục kho tệp | Bước 5 |
| Trình duyệt báo lỗi CORS khi đăng nhập — tab Network chỉ thấy `OPTIONS` trả 204, `POST` không đi —, hay lệnh ghi trả 403 `CORE.AUTH.ORIGIN_REJECTED` | Origin của trang FE không được BE nhận. Ở Development BE nhận mọi `https://localhost:<cổng>` cùng các mục của `Core:Auth:AllowedOrigins` — quy tắc ở [`be-api-controller.md`](be-api-controller.md) §7.3. Thường gặp: trang mở bằng `http://`, hay bằng `127.0.0.1`, tên máy thay vì `localhost`; hoặc BE không chạy ở môi trường Development | Mở đúng địa chỉ log của `ng serve` in ra; BE chạy bằng hồ sơ của `launchSettings.json` (bước 8) |
| FE báo không kết nối được máy chủ; tab Network thấy request tới cổng 7100 hỏng | BE chưa chạy; BE nghe cổng khác (`--urls` hay `ASPNETCORE_URLS` đè `launchSettings.json`); hoặc trình duyệt chưa tin chứng chỉ dev | Mở thẳng `https://localhost:7100/scalar` trên cùng trình duyệt; bước 7, 8; §5 |
| Áp script báo `khong dong duoc vai … (chu schema core) - DUNG` | Kết nối bằng tài khoản không đóng được vai chủ schema `core` — thường `coreandskill_app`. Script mang khối tự chặn dừng trước khi tạo gì | Đổi sang kết nối `coreandskill_owner`, hay `postgres` khi mọi script còn thiếu mang khối — runbook §3.1, *Tài khoản chạy script* |
| Script đầu tiên dừng ở câu đầu (`START TRANSACTION`) với lỗi cú pháp, có thể kèm ký tự lạ trước chữ đầu | BOM UTF-8 ở đầu tệp script | Cảnh báo BOM ở runbook §3.3 bước 2 |
| Áp script báo `relation "RoleNameIndex" already exists`, hay một đối tượng khác *already exists* | Database đang nối đã có schema từ lần chạy trước. Hay gặp nhất: kết nối DBeaver trỏ database `postgres`, không phải `coreandskill_dev` | Chạy câu kiểm *đang ở đâu, là ai*. Dọn database `postgres`: đoạn *Lỡ chạy nhầm vào database `postgres`* ở runbook §6. `coreandskill_dev` áp dở: dựng lại từ đầu, cùng mục đó |
| Đăng nhập trả *Sai thông tin đăng nhập* (`CORE.AUTH.INVALID_CREDENTIALS`) dù gõ đúng giá trị ở §3 | (a) chưa chạy bootstrap — database chưa có tài khoản; (b) tài khoản đã qua lần đổi mật khẩu bắt buộc; (c) tài khoản vận hành đăng nhập ở form hai ô, hay gõ sai mã đơn vị hệ thống; (d) quản trị đơn vị: `tenantCode` của FE lệch `Core:Bootstrap:FirstTenantCode` | (a) bước 6, rồi `SELECT code, is_system FROM core.tenant` ra hai dòng; (b) cột cuối §3; (c) §3; (d) `bash scripts/dev-config-check.sh` |
| Chữ tiếng Việt thành ký tự lạ hay `?` ở console | Console Windows dùng bảng mã OEM. BE tự chuyển console sang UTF-8 khi chạy trên Windows và đầu ra không bị chuyển hướng | Chạy trong Windows Terminal, hoặc `chcp 65001` trước lệnh — Git Bash gõ `chcp.com 65001`. Đầu ra qua ống, vào tệp, hay vẫn ra ô vuông: hàng *Chữ tiếng Việt ở console Windows* ở runbook §6 |

---

## 5. Cụm cổng và origin của máy dev — định nghĩa gốc

Hai tệp cho cặp 1, một tệp cho origin FE. Đổi một chỗ của cặp 1 thì đổi chỗ kia trong **cùng một thay đổi**.

| Mục | Tệp, chuỗi neo | Phải khớp | Lệch thì |
| --- | --- | --- | --- |
| 1. Cổng API | `src/BE/CoreAndSkill.Api/Properties/launchSettings.json`, chuỗi `"applicationUrl": "https://localhost:7100"` | `src/FE/src/environments/environment.development.ts`, chuỗi `apiBaseUrl: 'https://localhost:7100/api/v1'` | FE gọi vào cổng không ai nghe — lỗi kết nối |
| 2. Origin FE | `src/FE/angular.json`, khối `serve`: chuỗi `"ssl": true`, không khai `port` nên máy chủ dev dùng cổng mặc định 4200 | Hằng ghim của cổng T14. **Không** phải khớp `Core:Auth:AllowedOrigins`: ở Development BE nhận mọi `https://localhost:<cổng>` — [`be-api-controller.md`](be-api-controller.md) §7.3 | `ssl` tắt thì origin thành `http`, BE không nhận — CORS chặn, lệnh ghi trả 403 `CORE.AUTH.ORIGIN_REJECTED` |

- `ng serve` dùng `environment.development.ts`: khối `development` của `angular.json` thay `environment.ts` bằng tệp đó. `environment.ts` chỉ là placeholder cho `ng test`, `ng lint` — đổi cổng ở đó không có tác dụng ([`fe-architecture.md`](fe-architecture.md) §1.1, bảng *Các tệp môi trường*).
- `AllowedOrigins` của `appsettings.Development.json` vẫn mang hai mục, `https://localhost:4200` và origin của chính API cho trang Scalar (dòng 2026-10-06 về allowlist origin máy dev ở [`../adr/nhat-ky-quyet-dinh.md`](../adr/nhat-ky-quyet-dinh.md)). Ở Development cả hai thừa, nhưng khoá bắt buộc ít nhất một mục ở mọi môi trường ([`be-architecture.md`](be-architecture.md) §4.1) — đừng gỡ cho gọn.
- Mã đơn vị dev — `tenantCode` của FE với `Core:Bootstrap:FirstTenantCode` — **không** thuộc cụm này. Chủ của nó: đoạn *Cùng giá trị ở FE*, runbook §3.3 bước 4. Cổng dưới canh luôn cặp đó.

**Đổi cổng có chủ ý:** cổng API — đổi cả hai chỗ của cặp 1 và hằng `PIN_API_PORT` ở đầu `scripts/dev-config-check.sh`; cổng mặc định của FE — đổi `angular.json` và hằng `PIN_FE_PORT`, BE không phải đổi gì. Cùng một thay đổi.

**Kiểm:** `bash scripts/dev-config-check.sh` — thoát 0 là khớp. Luật **T14** ở [`../RULES-CHUNG.md`](../RULES-CHUNG.md). Cổng chỉ đọc tệp, nên **không** thấy cổng mà `ng serve` thật sự nghe lúc chạy — không cần thấy: BE dev nhận cổng nào cũng được.

**Chạy cổng khác** — khi cổng 4200 phải để cho việc khác. Một trong hai, BE không phải đổi gì, cổng T14 vẫn xanh:

- `npm start` khi 4200 đang bận, rồi trả lời có khi CLI hỏi dùng cổng khác — CLI tự chọn cổng.
- `npm start -- --port <n>` trong `src/FE` — chọn cổng.

Cái giá của việc BE dev nhận mọi cổng: trang khác chạy trên `https://localhost` của máy dùng được phiên dev — [ADR-0129](../adr/0129-development-nhan-moi-cong-https-localhost-mot-ham-origin-chung-cho-cors-va-lop-1-go-kiem-cong-prestart.md), mục *Tiêu cực*.
