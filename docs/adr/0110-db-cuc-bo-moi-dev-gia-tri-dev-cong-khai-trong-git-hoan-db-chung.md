---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0110 — Mỗi dev dùng PostgreSQL cục bộ; `appsettings.Development.json` mang sẵn giá trị dev công khai; DB dev chung tạm hoãn, không bỏ

> **Trạng thái:** Đã chấp nhận (2026-09-28)

## Bối cảnh

**Hướng DB dev chung đã chốt nhưng chưa thi công dòng nào.** Chuỗi [ADR-0097](0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md), [ADR-0098](0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md), [ADR-0099](0099-luat-van-hanh-db-dev-chung.md), [ADR-0104](0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md), [ADR-0105](0105-thu-khu-quan-tri-he-thong-tren-db-cuc-bo.md) chốt ngày 2026-09-25 một database dev dùng chung: chuỗi kết nối vào git, mật khẩu mã hoá bằng khoá `dev` cũng nằm trong git, người vận hành DB dev áp script và tạo tài khoản. Kiến trúc sư đối chiếu ngày 2026-09-28:

| Thứ | Hôm nay | Lệnh hoặc neo |
| --- | --- | --- |
| Bộ mã hoá, nhóm khoá `Core:ConfigEncryption`, lệnh `core encrypt-secret` | Không có | `grep -rln 'ConfigEncryption' src/BE --include=*.cs` không ra tệp nào |
| Loại `appsettings.Development.json` khỏi gói publish (S23 vế (e) của ADR-0098) | Không có | `grep -rn 'appsettings.Development' src/BE --include=*.csproj --include=*.props --include=*.targets` không ra dòng nào |
| Chuỗi kết nối trong `appsettings.Development.json` đã commit | Không có | `git show HEAD:src/BE/CoreAndSkill.Api/appsettings.Development.json` không chứa chuỗi `ConnectionStrings` |

**Máy dev, theo bản đã commit.** Mỗi dev tự đặt bằng `user-secrets` chuỗi kết nối, `Core:File:RootPath` và hai mật khẩu của lệnh bootstrap ([`0022-seed-dev-khong-co-duong-code-rieng.md`](0022-seed-dev-khong-co-duong-code-rieng.md), [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md)). `appsettings.Development.json` chỉ mang giá trị không bí mật.

**Người dùng nêu, 2026-09-28:** nhóm quen để cấu hình kết nối trong repo; dev mới clone về chỉ cần tạo database cục bộ là kết nối được. **Repo đang công khai trên GitHub** — mọi giá trị trong git là giá trị ai cũng đọc được.

**Đã kiểm trước khi chốt, 2026-09-28:**

- gitleaks 8.30.1, đúng bản CI ghim, quét `src/BE/CoreAndSkill.Api/appsettings.Development.json` và `database/dev/tao-db-local.sql` ở cây làm việc: không phát hiện gì.
- Test ở môi trường Development không đọc chuỗi kết nối hay thư mục kho tệp của tệp này: `CoreWebApplicationFactory` luôn đè `ConnectionStrings:Core` và `Core:File:RootPath` bằng cấu hình trong bộ nhớ — `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/CoreWebApplicationFactory.cs`, chuỗi `merged.TryAdd("Core:File:RootPath"`.
- Không mã nào của BE từ chối chạy bằng siêu quản trị, và script trong `database/scripts/core/` không nhắc tới role nào. Quyền của tài khoản ứng dụng chỉ đến từ khối cấp quyền ở [`../database/script-runbook.md`](../database/script-runbook.md) §3.6. BE chạy bằng `postgres` vẫn lên, và khi đó mọi quyền bị thu ở khối đó không chặn gì.
- Mọi tài khoản mới, gồm hai tài khoản do bootstrap tạo, mang `must_change_password` — `src/BE/Core/CoreAndSkill.Core.Infrastructure/Identity/AppUser.cs`, chuỗi `MustChangePassword = true,`.

## Quyết định

Người dùng chốt ngày 2026-09-28, từng câu:

1. **Mỗi dev dùng một PostgreSQL cục bộ; clone về là chạy, không phải sửa cấu hình.** `src/BE/CoreAndSkill.Api/appsettings.Development.json` mang đủ cấu hình máy dev:
   - `ConnectionStrings:Core` trỏ database `coreandskill_dev` trên `localhost:5432`, tài khoản `coreandskill_app`, mật khẩu trùng tên tài khoản;
   - `Core:File:RootPath` trỏ `C:/CoreAndSkillData/files` — thư mục vẫn tạo tay, Core không tự tạo;
   - `Core:Bootstrap:OperatorPassword` và `Core:Bootstrap:AdminPassword` — mật khẩu ban đầu của hai tài khoản, cả hai mang `must_change_password`.

   Các giá trị này **công khai có chủ ý**: chúng chỉ mở được một database cục bộ dựng từ script công khai. Máy dev không cần `user-secrets` cho các khoá này. `user-secrets` vẫn nạp sau tệp và đè được, cho ai muốn giá trị riêng.
2. **Tài khoản database của máy dev do tệp `database/dev/tao-db-local.sql` tạo.** Tệp tạo hai role `coreandskill_owner`, `coreandskill_app` mang mật khẩu công khai trùng tên role, rồi tạo database `coreandskill_dev` — chủ là `coreandskill_owner`, mã UTF8, từ `template0`. Chạy bằng `postgres` khi đang nối vào database `postgres`. BE chạy bằng `coreandskill_app`, không bằng siêu quản trị.
3. **DB dev dùng chung tạm hoãn, không bỏ.** ADR-0097, 0098, 0099, 0104, 0105 giữ làm hướng cho lúc nhóm cần DB chung. Mọi mục nợ gắn với DB chung và với cơ chế mã hoá của nó chờ tới lúc đó.
4. **Triển khai không đổi.** Test, staging, production nhận bí mật qua biến môi trường ([`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §2, §7). Người dùng xem lại khi triển khai.

Kiến trúc sư ghi phần đi kèm:

5. **Luật S6 có đúng một ngoại lệ bản rõ: giá trị dev công khai.** Định nghĩa nằm ở [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1. Ranh giới được viết sao cho máy đọc được: mật khẩu rõ trong chuỗi kết nối chỉ hợp lệ khi máy đích là máy cục bộ **và** mật khẩu khớp mật khẩu mà `database/dev/tao-db-local.sql` đặt; mật khẩu bootstrap chỉ nằm ở `appsettings.Development.json`. Giá trị nào mở được thứ gì ngoài máy của chính dev thì không còn là giá trị dev công khai, và S6 cấm nó. Chiều ngược lại — giá trị công khai không được tái dùng trên máy chủ nào, và lệnh bootstrap chạy vào database trên máy khác thì phải đè hai mật khẩu — là luật **S28** mới, không ép được bằng máy, nằm ở [`../DEBT.md`](../DEBT.md).
6. **Luật S23 viết lại** ([`../DEBT.md`](../DEBT.md)):
   - vế (b) chừa giá trị dev công khai, theo đúng ranh giới ở mục 5;
   - thêm vế (f): khoá mật khẩu của `Core:Bootstrap` chỉ mang giá trị ở `appsettings.Development.json`;
   - vế (a), (c), (d) — chỗ đặt và khoá của giá trị mã hoá — hoãn cùng DB chung;
   - vế (e) — loại tệp khỏi gói publish — **giữ**, vì đó là luật triển khai và mục 4 giữ triển khai nguyên. Lý do của nó nay là tệp mang mật khẩu bootstrap công khai: máy chủ lỡ chạy ở môi trường `Development` sẽ nạp chúng.
7. **Dòng trạng thái của các ADR cũ.** Mỗi ADR trong bảng dưới đổi dòng trạng thái thành *Sửa một phần bởi ADR-0110*. [`README.md`](README.md) §5.1 không có quan hệ "tạm hoãn", nên dòng trạng thái dùng quan hệ gần nhất; bảng dưới là chỗ đọc đúng nghĩa.

   | ADR | Hết hiệu lực | Hoãn cùng DB chung | Còn hiệu lực hôm nay |
   | --- | --- | --- | --- |
   | 0022 | Mật khẩu hai tài khoản bootstrap của máy dev ở `user-secrets`, *"không nằm trong repo"*; điều kiện lật 2 của nó | — | Không đường code nào chứa sẵn thông tin tài khoản: mật khẩu vẫn đi qua hệ cấu hình, lệnh bootstrap không đổi. Tệp `.sql` không tạo tài khoản. Luật S9 |
   | 0023 | Câu *"trên máy dev, giá trị bí mật nằm ở `user-secrets`"* | — | Mọi phần còn lại |
   | 0097 | Quyết định 8 — bản S6, S23 của nó | Quyết định 1–7, 9, 10 — thiết kế mã hoá và chỗ đặt | — |
   | 0098 | Mục 4 — bản S6 và S23 của nó, trừ vế (e) | Quyết định 1, 2, 3; luật T13, S25 | S23 vế (e) — loại tệp khỏi gói publish |
   | 0099, 0104 | — | Toàn bộ | — |
   | 0105 | Cụm *"mật khẩu từ `user-secrets` của máy mình"* — nay lấy từ `appsettings.Development.json` | Mục 3 — không dùng chung tài khoản vận hành trên DB chung | Mục 1, 2 — thử khu hệ thống trên database cục bộ; không thêm lệnh hay endpoint tạo tài khoản vận hành |

8. **Quay lại DB chung** khi nhóm cần — người dùng quyết thời điểm. Việc đầu tiên lúc đó là một ADR mới lật phần hoãn, rồi đọc lại năm ADR trên với mã lúc đó: mọi neo trong chúng ghi ngày 2026-09-25. Quyết định 9 của ADR-0097 — cổng S23 xanh **trước** giá trị mã hoá đầu tiên — còn nguyên.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ `user-secrets` cho mọi giá trị bí mật của máy dev

Đây là bản đã commit.

**Được:** không mật khẩu nào trong git. S6 không có ngoại lệ bản rõ, và cổng S23 giữ được luật phổ quát *"mọi bản rõ trong mọi `appsettings*.json` đều đỏ"*. ADR-0022 nguyên vẹn.

**Mất:** mỗi dev mới đặt tay chuỗi kết nối, thư mục kho tệp và hai mật khẩu; mỗi máy một bộ tên, nên tài liệu không viết được lệnh cụ thể.

**Vì sao loại:** người dùng chọn mô hình *"clone về là chạy"*. Chưa ai đo bước `user-secrets` có thật là rào cản — điều kiện lật 2 của ADR-0022 đòi số đo đó. Ghi ở câu hỏi 1.

### Phương án B — Dùng thẳng tài khoản `postgres`

**Được:** không cần tệp SQL nào cho dev. Nhiều bản cài Postgres và container đã có sẵn tài khoản đó.

**Mất:**

- Mọi dev phải đặt mật khẩu `postgres` giống nhau.
- BE chạy bằng siêu quản trị, nên khối cấp quyền ở [`../database/script-runbook.md`](../database/script-runbook.md) §3.6 không bao giờ được thử ở máy dev. Nhật ký kiểm toán bất biến, danh mục quyền chỉ đọc, bảng lịch sử script chỉ đọc đều không chặn gì. Lỗi về quyền lộ lần đầu ở bản cài thật.

**Vì sao loại:** người dùng chốt, lý do là gạch thứ hai. Đã kiểm: không mã nào của BE tự từ chối siêu quản trị, nên không lớp nào khác bắt được lỗi đó.

### Phương án C — Bỏ hẳn DB chung

**Được:** năm ADR và các mục nợ của DB chung ra khỏi tầm đọc; không còn luật ngủ.

**Mất:** thiết kế đã cân nhắc kỹ phải làm lại từ đầu khi nhóm cần DB chung.

**Vì sao loại:** người dùng chốt giữ làm hướng tương lai.

### Phương án D — Mã hoá giá trị dev bằng khoá `dev`, theo cơ chế của ADR-0098

Kiến trúc sư xét phương án này khi viết ADR, vì ADR-0098 loại bản rõ đúng ở chỗ này.

**Được:** cổng S23 giữ tính phổ quát — bản rõ nào trong `appsettings*.json` cũng đỏ.

**Mất:** phải thi công bộ mã hoá, nhóm khoá và lệnh runner — mã mật mã trong Core — để che một giá trị cố ý công khai. ADR-0098 đã ghi: khoá nằm cạnh bản mã thì mã hoá chỉ là che mắt.

**Vì sao không chọn:** cái mua được chỉ là một hình dạng để cổng nhận ra. Ranh giới ở quyết định 5 — máy đích cục bộ, mật khẩu khớp tệp SQL — cho cổng một hình dạng khác, không cần dòng mã nào.

### Phương án E — Tệp cấu hình cục bộ ngoài git, kèm tệp mẫu trong git

Kiểu một tệp cấu hình riêng mỗi máy, bị `.gitignore` loại, cộng một tệp mẫu để chép.

**Được:** không mật khẩu nào trong git; mỗi máy đổi được mà không đè.

**Mất:**

- Clone xong phải chép tệp mẫu — một bước, trái đúng mục tiêu.
- Cần mã nạp thêm một nguồn cấu hình, và mẫu loại trừ tệp theo môi trường phải đục thêm một lỗ — cùng lý do ADR-0097 loại phương án G.

**Vì sao loại:** hai cái mất trên.

### Phương án F — Container Postgres dựng sẵn role và database

**Được:** một lệnh là có database đúng tên, đúng role.

**Mất:** đòi Docker trên mọi máy dev; máy dùng Postgres cài sẵn vẫn cần đường khác.

**Vì sao loại:** tệp SQL chạy được trên cả Postgres cài sẵn lẫn container.

## Hệ quả

### Tích cực

- Clone về, dựng database theo [`../database/script-runbook.md`](../database/script-runbook.md) §6, là BE kết nối được; không `user-secrets` nào.
- BE ở máy dev chạy bằng `coreandskill_app`, đúng quyền của bản cài thật. Lỗi về quyền lộ ở máy dev.
- Mọi máy dev cùng một bộ tên. Tài liệu viết được lệnh cụ thể.
- Thiết kế DB chung giữ nguyên; quay lại không phải cân nhắc lại từ đầu.
- Không dòng mã nào của Core đổi, không đường dẫn nào trong khối `core-paths` bị chạm.

### Tiêu cực

- **S6 có ngoại lệ bản rõ, và hôm nay không cổng nào canh ranh giới của nó.** ADR-0098 loại bản rõ đúng vì lý do này. gitleaks mặc định đã quét tệp và không báo gì. Suy ra, chưa đo: một mật khẩu thật người chọn tay, cùng hình dạng, dán vào cùng tệp cũng sẽ lọt (nợ S12). Sửa chuỗi kết nối trong `appsettings.Development.json` nay là việc quen tay, nên xác suất dán nhầm tăng. Tới khi S23 vế (b) có cổng, lớp bắt duy nhất là người review.
- **`must_change_password` không che mật khẩu bootstrap công khai.** Cờ đó làm mật khẩu ban đầu hết dùng *sau khi* chủ tài khoản đổi; ai đăng nhập trước thì người đó đặt mật khẩu mới. Giá trị này an toàn chỉ khi API chỉ với tới được từ chính máy dev.
  - Bẫy cụ thể: dev đè `ConnectionStrings:Core` bằng `user-secrets` để trỏ sang database trên máy khác, rồi chạy `core bootstrap`. Hai mật khẩu vẫn rơi về giá trị công khai.
  - Đây là dạng mới của ca tương quan theo tên môi trường mà [`0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md`](0020-seed-dev-ba-dieu-kien-va-dau-nhan-dang.md) không đóng được, và ADR-0022 đã tránh bằng cách không để mật khẩu trong repo. Không cổng nào chặn; luật S28 chỉ là quy ước.
- **Tệp hôm nay vẫn đi vào gói publish** — suy từ mặc định của Web SDK cộng lệnh ở *Bối cảnh*; chưa ai chạy `dotnet publish` để xem. S23 vế (e) chưa có. Máy chủ lỡ chạy ở môi trường `Development` nạp chuỗi `localhost` — vô hại khi biến môi trường đè nó — và hai mật khẩu công khai — nguy hiểm khi biến môi trường không đặt hai khoá đó và người vận hành chạy bootstrap.
- **An toàn của giá trị công khai phụ thuộc cấu hình Postgres của từng máy.** Postgres của một dev nhận kết nối có mật khẩu từ mạng thì ai trong mạng đó cũng vào được `coreandskill_dev` bằng mật khẩu đọc từ GitHub. Không cổng nào kiểm. Thiệt hại được giới hạn bởi giả định *"database dev không mang dữ liệu thật"*, và giả định đó cũng không cổng nào kiểm.
- **ADR-0022 bị sửa ngược với điều kiện nó tự đặt.** Điều kiện lật 2 của nó đòi đo bước `user-secrets` là rào cản thật, và nói câu trả lời không phải nhúng mật khẩu vào repo. Quyết định này không có số đo đó.
- **Hai tệp phải khớp nhau nằm ở hai vùng sở hữu.** Theo [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.5, `database/dev/` thuộc vùng Core — mọi đường dẫn còn lại dưới `database/` — còn host thuộc vùng dự án. Dự án dựng từ bản clone mà giữ nguyên tên thì, trên cùng một máy, dùng chung database `coreandskill_dev` và thư mục kho tệp với repo Core. §15.6 có dòng cho thư mục kho tệp, chưa có dòng cho tên database. Xem *Câu hỏi còn mở*.
- **Thư mục kho tệp là đường dẫn Windows.** Máy dev không chạy Windows phải đè `Core:File:RootPath` bằng `user-secrets`: trên đó đường dẫn `C:/…` không phải đường dẫn tuyệt đối, và tiến trình không lên ([`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §2).
- **Biến thể cục bộ của T13 không luật nào phủ.** Host dựng trong test ở môi trường `Development` mà không đè `ConnectionStrings:Core` sẽ nối vào `coreandskill_dev` của máy đang chạy test, và ghi dữ liệu test vào đó. Các host test đã thấy đều đè — neo ở *Bối cảnh* — nhưng không gì ép test mới làm vậy. T13 viết cho DB chung.
- **Luật ngủ sẽ mục.** Năm ADR và các mục nợ của DB chung nói về một database không tồn tại; neo của chúng sẽ lệch dần khi mã đổi. Người đọc phải qua bảng ở quyết định 7 để biết cái gì đang áp.
- **Giá trị vào lịch sử git vĩnh viễn** khi người dùng commit, trên một repo công khai. Chấp nhận được chỉ vì chúng không mở gì ngoài máy của từng dev. Ai tái dùng chúng ở chỗ khác thì chính chỗ đó lộ.

### Rút lui nếu sai

1. Xoá phần mật khẩu trong `ConnectionStrings:Core` và hai khoá mật khẩu bootstrap khỏi `appsettings.Development.json`. Mỗi dev đặt lại bằng `user-secrets` theo [`../database/script-runbook.md`](../database/script-runbook.md) §3.3 bước 4. `Core:File:RootPath` và phần không mật khẩu của chuỗi giữ được.
2. Dev nào có Postgres nhận kết nối từ mạng thì đổi mật khẩu hai role trên máy mình. Không môi trường nào khác phải đổi mật khẩu, với điều kiện ranh giới ở quyết định 5 đã được giữ.
3. Viết ADR mới đưa S6 và S23 vế (b) về không ngoại lệ bản rõ, bỏ vế (f). `database/dev/tao-db-local.sql` thì hoặc bỏ, hoặc sửa thành chỉ tạo role và database, mật khẩu gõ tương tác như §3.3 bước 1 của runbook.
4. Không viết lại lịch sử git: giá trị đã commit công khai có chủ ý.

Không dữ liệu ứng dụng nào phụ thuộc quyết định này; database cục bộ dựng lại được bất cứ lúc nào.

### Dấu hiệu quyết định này bắt đầu sai

- `ConnectionStrings:Core` trong `appsettings.Development.json` trỏ tới máy khác máy cục bộ, hoặc mật khẩu trong đó lệch mật khẩu mà `tao-db-local.sql` đặt.
- Role `coreandskill_*` mang mật khẩu công khai xuất hiện trên một máy chủ test hay staging "cho tiện".
- Allowlist gitleaks cho tệp này bị nới rộng hơn đúng các giá trị dev công khai.
- Nhật ký kiểm toán của một database không phải cục bộ có dòng do lệnh bootstrap tạo hai tài khoản.
- Nhiều dev đè các khoá này bằng `user-secrets` — giá trị trong git không ai dùng.
- Nhóm bắt đầu chia sẻ dữ liệu demo giữa các máy — tín hiệu DB chung đã tới lúc.

## Câu hỏi còn mở — chờ người dùng

Câu trả lời ghi bằng ADR mới hoặc một dòng ở [`nhat-ky-quyet-dinh.md`](nhat-ky-quyet-dinh.md), không sửa mục này (luật D45).

1. **`database/dev/` thuộc vùng nào.** Theo §15.5 nó thuộc vùng Core, tức dự án không được sửa; nhưng nội dung của nó phải khớp host, vốn thuộc vùng dự án. Hai hướng: chuyển nó sang vùng dự án, hoặc giữ ở vùng Core và thêm tên database vào bảng *"giá trị mang tên Core phải thay"* ở §15.6 cùng một thủ tục cho dự án.
2. **Có mở rộng T13 cho database cục bộ không** — biến thể ở mục *Tiêu cực*.
3. **S23 vế (e) giữ hiệu lực.** Kiến trúc sư đọc vậy từ quyết định 4 *"triển khai không đổi"*; cần người dùng xác nhận.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Ma sát khi dựng máy dev mới — đặt tay chuỗi kết nối, thư mục kho tệp, hai mật khẩu — và thói quen của nhóm. **Chưa đo**: chưa ai ghi lại một lần dựng máy bị chặn ở bước `user-secrets`, điều kiện ADR-0022 đặt để xét lại |
| 2 | Phương án đơn giản hơn, vì sao không đủ | A — giữ `user-secrets` — đơn giản hơn về luật: không ngoại lệ, không tệp SQL dev. Nó thiếu đúng thứ người dùng muốn là clone về là chạy. B — tài khoản `postgres` — đơn giản hơn về bước dựng; nó giấu lỗi về quyền |
| 3 | Chi phí vận hành thêm | Giữ `appsettings.Development.json` khớp `tao-db-local.sql`. Review mọi PR chạm `appsettings.Development.json` cho tới khi S23 vế (b) có cổng. Một allowlist gitleaks theo giá trị khi rule của S12 bật. Đọc lại các mục nợ đang hoãn khi DB chung quay lại. Trả mãi mãi, chừng nào còn giá trị công khai trong git |
| 4 | Ai bảo trì | Người sửa một trong hai tệp sửa tệp kia trong cùng PR. `test-engineer` giữ cổng S23. Hôm nay, người review PR là lớp bắt duy nhất. Không mã Core nào phải bảo trì |
| 5 | Rút lui thế nào | Bốn bước ở mục *Rút lui nếu sai*: một PR, không dữ liệu, không đổi mật khẩu ở môi trường nào ngoài máy dev |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Không dòng mã Core nào đổi; host và `database/dev/` nằm ngoài khối `core-paths`. Tài liệu mang `scope: core` vẫn gọi tên `coreandskill_*` như trước — xem câu hỏi còn mở 1 |

## Thi công và kiểm

| Ai | Việc |
| --- | --- |
| `test-engineer` | Cổng S23 vế (b), (f) — hai lớp, mỗi vế một canary, theo hàng S23 ở [`../DEBT.md`](../DEBT.md). Mật khẩu đối chiếu đọc từ `database/dev/tao-db-local.sql` lúc chạy test, không chép vào test |
| `backend-expert` | S23 vế (e): project host loại `appsettings.Development.json` khỏi gói publish. Host thuộc vùng dự án, ngoài khối `core-paths` |
| `test-engineer` | Canary của vế (e): đọc tệp `.csproj` của host |
| Người dùng | Chạy trọn [`../database/script-runbook.md`](../database/script-runbook.md) §6 trên một máy sạch và báo bước nào hỏng. Chưa ai chạy trọn đường này. Lệnh chạm database là việc của người dùng |

## Liên quan

- [`0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md`](0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md), [`0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md`](0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md), [`0099-luat-van-hanh-db-dev-chung.md`](0099-luat-van-hanh-db-dev-chung.md), [`0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md`](0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md), [`0105-thu-khu-quan-tri-he-thong-tren-db-cuc-bo.md`](0105-thu-khu-quan-tri-he-thong-tren-db-cuc-bo.md) — DB chung, hoãn
- [`0022-seed-dev-khong-co-duong-code-rieng.md`](0022-seed-dev-khong-co-duong-code-rieng.md), [`0023-dich-vu-tao-don-vi-dung-chung.md`](0023-dich-vu-tao-don-vi-dung-chung.md) — nguồn mật khẩu bootstrap của máy dev
- [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1 — luật S6 và giá trị dev công khai
- [`../database/script-runbook.md`](../database/script-runbook.md) §3.3, §3.6, §6 — đường dựng database cục bộ
- [`../DEBT.md`](../DEBT.md) — S12, S23–S28, T13, E21, B20
