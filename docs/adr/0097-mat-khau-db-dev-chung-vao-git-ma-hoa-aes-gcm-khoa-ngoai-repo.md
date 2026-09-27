---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0097 — Chuỗi kết nối tới DB dev dùng chung vào git, phần mật khẩu mã hoá AES-256-GCM; khoá giải mã không bao giờ vào repo; giá trị mã hoá không giải được thì không khởi động

> **Trạng thái:** Đã chấp nhận (2026-09-25) · Sửa một phần bởi [ADR-0098](0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md) (2026-09-25) · Bổ sung bởi [ADR-0099](0099-luat-van-hanh-db-dev-chung.md) (2026-09-25)

## Bối cảnh

**Hôm nay.** Mỗi lập trình viên chạy một database cục bộ. Chuỗi kết nối nằm trong `user-secrets`, ngoài cây làm việc, đúng luật S6 ([`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1). Trong repo, `src/BE/CoreAndSkill.Api/appsettings.json` khai chuỗi rỗng (chuỗi `"Core": ""`), còn `appsettings.Development.json` không khai chuỗi kết nối nào. Mọi chỗ dùng chuỗi đọc **một** nguồn là `IOptions<CoreConnectionOptions>`:

| Chỗ | Neo |
| --- | --- |
| Bind và kiểm lúc khởi động | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreOptionsServiceCollectionExtensions.cs`, chuỗi `services.AddOptions<CoreConnectionOptions>()` |
| Kết nối dùng chung của mọi `DbContext` | `src/BE/Core/CoreAndSkill.Core.Infrastructure/DependencyInjection/CoreInfrastructureServiceCollectionExtensions.cs`, chuỗi `new NpgsqlConnection(sp.GetRequiredService<IOptions<CoreConnectionOptions>>().Value.Core)` |
| Kiểm sức khoẻ database | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Diagnostics/DatabaseHealthCheck.cs`, chuỗi `IOptions<CoreConnectionOptions> options` |

Kiến trúc sư đối chiếu ba neo trên ngày 2026-09-25.

**Sắp tới.** Cả nhóm làm chung một repo và **bắt buộc dùng một máy chủ database chung**. Người dùng muốn cấu hình kết nối nằm trong git: dev khác clone về là code tiếp được, mà đẩy git không lộ mật khẩu.

**Máy chủ triển khai không đổi.** Test, staging và production nhận bí mật qua biến môi trường ([`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §2, §7). ADR này không chạm phần đó.

**Dự án tham khảo.** Người dùng chỉ tới backend của một dự án nội bộ khác. Kiến trúc sư đã đọc các tệp liên quan (chỉ đọc, không chép bí mật nào). Dự án đó làm thế này:

1. Một provider cấu hình (tệp `DecryptingConfigurationExtensions.cs`) giải mã **phần `Password`** của mọi `ConnectionStrings:*` lúc khởi động. Host, database và tài khoản vẫn đọc được trong tệp.
2. Khoá giải mã và một giá trị salt thuộc tầng bí mật, tức đến từ biến môi trường (tệp `SiteConfigKeys.cs`).
3. Thuật toán (tệp `ConnectionStringDecryptor.cs`) là Rijndael + PBKDF1, IV suy ra từ chính khoá, không có xác thực (AEAD). Chính tệp đó tự ghi ba điểm yếu.

Ngoài thuật toán, dự án đó còn bốn hành vi mà ADR này **cố ý không giữ**:

- Thiếu khoá thì provider lặng lẽ bỏ qua, nên chuỗi mã hoá được dùng nguyên làm mật khẩu.
- Giá trị không phải base64 bị coi là bản rõ.
- Giải mã hỏng thì ghi ra stderr rồi dùng nguyên giá trị.
- Provider dựng lại mọi nguồn cấu hình trong một builder tạm để đọc khoá.

Cả bốn đều trái [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4: thiếu hoặc hỏng cấu hình thì phải dừng lúc khởi động. Ở dự án đó, hỏng lộ ra muộn hơn, thành lỗi xác thực database ở một chỗ khác, không chỉ về nguyên nhân.

**Nợ S12 là rủi ro tăng lên cùng quyết định này.** Rule mặc định của gitleaks có thể bỏ sót mật khẩu người chọn tay nằm cạnh khoá `password` ([`../DEBT.md`](../DEBT.md), hàng S12). Hôm nay rủi ro đó thấp, vì không ai sửa chuỗi kết nối trong một tệp nằm trong repo. Sau quyết định này, chuỗi kết nối trong `appsettings.Development.json` thành thứ dev đụng tới thường xuyên. Chỉ cần một lần dán nhầm bản rõ rồi commit là mật khẩu vào lịch sử git vĩnh viễn.

**`src/` chưa vào git.** Lịch sử git hôm nay chưa có chuỗi kết nối nào. Đây là lúc rẻ nhất để dựng cổng, vì cổng dựng trước lần commit đầu tiên thì không có lịch sử nào phải viết lại.

## Quyết định

Người dùng chốt ngày 2026-09-25: **chuỗi kết nối tới DB dev dùng chung nằm trong git; mật khẩu trong chuỗi được mã hoá bằng AES-GCM; khoá giải mã đến từ nguồn ngoài repo; Core tự giải mã lúc khởi động — làm giống dự án tham khảo, trừ thuật toán và trừ cách xử lý khi hỏng: hỏng thì từ chối khởi động, không lặng lẽ dùng chuỗi rác.**

Kiến trúc sư ghi phần đi kèm dưới đây. Hai câu hỏi có đánh đổi thật và một câu thuộc quyền tổ chức vẫn **chờ người dùng**, ở mục *Câu hỏi còn mở*.

1. **Chỉ mã hoá phần mật khẩu của `ConnectionStrings:Core`.** Host, database và tài khoản vẫn là chữ rõ, giống dự án tham khảo. `ConnectionStrings:Core` là chuỗi kết nối duy nhất của Core, vì mọi `DbContext` đi chung một kết nối ([`0025-luu-du-lieu-module-mot-transaction.md`](0025-luu-du-lieu-module-mot-transaction.md)). Giá trị mang dấu mã hoá ở **bất kỳ chỗ nào khác** làm tiến trình không khởi động, thay vì bị dùng như chữ. Muốn mã hoá một khoá thứ hai thì phải có ADR mới.
2. **Dạng giá trị là `enc:v1:<mã khoá>:<dữ liệu>`.** `v1` là AES-256-GCM: nonce 12 byte ngẫu nhiên cho mỗi lần mã hoá, tag 16 byte. Chuỗi đầu `enc:v1:<mã khoá>:` là dữ liệu liên kết (AAD), nên đổi phiên bản hay mã khoá trên bản mã đều làm giải mã hỏng. Dữ liệu viết bằng base64url không đệm, nên không có `+`, `/`, `=` và đứng được trong chuỗi kết nối mà không cần trích dẫn. Bảng vận hành đầy đủ sống ở [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.4; ADR này không chép lại.
3. **Khoá là 32 byte ngẫu nhiên, không KDF, không salt.** Khoá nằm ở khoá cấu hình `Core:ConfigEncryption:Keys:<mã khoá>`, tức biến môi trường `Core__ConfigEncryption__Keys__<mã khoá>`. Máy dev đặt được bằng `user-secrets`. Mã khoá có trong chính bản mã, nên nhiều khoá sống cùng lúc được; đó là cơ chế xoay khoá. Nhóm khoá trống là hợp lệ: dự án không dùng mã hoá thì Core không đòi gì.
4. **Không giải được thì không khởi động.** Có năm ca: thiếu khoá mang đúng mã, sai khoá, dữ liệu bị sửa, sai định dạng, phiên bản lạ. Thêm hai ca nữa: dấu mã hoá nằm sai chỗ, và khoá sai dạng. Cả bảy ca đều làm tiến trình dừng. Thông điệp nêu **tên khoá cấu hình** và **mã khoá**, rồi chỉ tới [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.4. Thông điệp **không bao giờ** in bản rõ, bản mã hay khoá.
5. **Giá trị không có dấu thì dùng nguyên, như hôm nay.** Phép kiểm xét giá trị **hiệu lực**, tức giá trị sau khi mọi nguồn cấu hình đã gộp. Một chuỗi rõ đặt ở `user-secrets` hay biến môi trường sẽ đè bản mã trong tệp, và bản mã bị đè thì không bao giờ được giải. Nhờ vậy máy chưa có khoá vẫn chạy được với database cục bộ.
6. **Chỗ đặt trong git là `appsettings.Development.json`.** Không dùng tệp riêng. `appsettings.json` không bao giờ mang giá trị mã hoá, vì tệp đó đi theo artifact lên máy chủ. Tài khoản trong chuỗi là `coreandskill_app`, không bao giờ là `coreandskill_owner` ([`../database/script-runbook.md`](../database/script-runbook.md) §3.6).
7. **Công cụ tạo giá trị là lệnh runner `core encrypt-secret --key <mã khoá>`.** Bản rõ đọc từ **đầu vào chuẩn**. Lệnh không nhận bản rõ qua tham số, vì tham số nằm lại trong lịch sử shell. Nó cũng không nhận qua hệ cấu hình, vì muốn vào hệ cấu hình thì bản rõ phải nằm trong tệp hoặc biến môi trường. Lệnh in đúng một dòng dạng mã hoá và không mở kết nối database. **Không có lệnh giải mã.**
8. **Luật S6 được viết lại, kèm ba luật mới.**
   - S6 mới cấm secret **dạng rõ** và cấm khoá giải mã trong repo. Ngoại lệ duy nhất là dạng mã hoá ở §6.4.
   - **S23** là luật tĩnh: giá trị mã hoá chỉ nằm đúng chỗ ở quyết định 1 và 6; chuỗi kết nối trong `appsettings*.json` không mang mật khẩu rõ; không tệp nào của repo mang khoá.
   - **S24** là luật lúc chạy: quyết định 4 và 5.
   - **S25** không ép được bằng máy: khoá của nhóm dev không dùng cho môi trường máy chủ nào, và giá trị mã hoá trong git chỉ mở cửa vào môi trường dev chung.
   - Cả ba luật mới chưa có cổng, nên nằm ở [`../DEBT.md`](../DEBT.md).
9. 🛑 **Cổng của S23 phải xanh trước.** Cả hai lớp, kèm canary, phải xanh **trước** commit đầu tiên đưa một giá trị mã hoá vào git. Lịch sử git là vĩnh viễn: mật khẩu rõ lọt vào một lần là phải đổi mật khẩu database, và gỡ khỏi nhánh không gỡ được khỏi lịch sử.
10. **Tính năng thuộc Core và sống ở `Core.Web`.** Không có seam mới ở `Core.Application`, không project mới, không package mới: AES-GCM có sẵn trong thư viện chuẩn của .NET. Dự án hạ nguồn không dùng tính năng này thì không thấy khác biệt nào so với hôm nay. Lý do đặt ở `Core.Web`:
    - `Core.Web` là nơi đã bind options (`AddCoreOptions`).
    - Runner lệnh vận hành cũng sống ở đó.
    - Hai chỗ đó là hai nơi duy nhất cần bộ mã hoá.

    Bộ mã hoá để `internal`. Chưa module nào cần nó; khi một module cần thật thì mới dời, và lúc đó biết cả hai phía cần gì.

## Câu hỏi còn mở — chờ người dùng chốt

Câu trả lời sẽ ghi bằng **một ADR bổ sung ADR này**, không sửa mục này ([`README.md`](README.md) §5.1, luật D45). Phần thi công phụ thuộc câu 1 chưa được bắt đầu trước khi câu 1 có câu trả lời. Phần phụ thuộc là chỗ gắn bước giải mã, phép kiểm dấu sai chỗ, và nếu chọn A thì thêm dòng ở `Program.cs`. Phần còn lại thi công được ngay: bộ mã hoá, nhóm khoá, lệnh runner, cổng S23.

### 1. Cơ chế giải mã: tầng options hay provider cấu hình

Chỉ dẫn "làm giống dự án tham khảo" đọc sát chữ nghĩa là dùng **provider cấu hình**. Kiến trúc sư khuyến nghị **B**, tức giải mã ở tầng options. Hành vi nhìn từ ngoài của hai phương án giống nhau: tự giải mã lúc khởi động, khoá từ nguồn ngoài repo, hỏng thì dừng. Chỗ khác nằm ở bảng dưới.

| | **A — Provider cấu hình, như dự án tham khảo** | **B — Giải mã ở bước dựng `CoreConnectionOptions` (khuyến nghị)** |
| --- | --- | --- |
| Ai thấy bản rõ | Mọi chỗ đọc `IConfiguration`, kể cả thư viện ngoài | Chỉ đối tượng options. `IConfiguration` vẫn giữ bản mã |
| Bản rõ nằm ở đâu trong bộ nhớ | Trong cây cấu hình, suốt đời tiến trình. Mọi lần in cây cấu hình để chẩn đoán đều in cả mật khẩu | Chỉ trong đối tượng options |
| `Program.cs` và `AddCore` | Provider phải được thêm vào builder cấu hình **trước** khi `AddCore` bind. Hoặc mỗi `Program.cs` hạ nguồn thêm một dòng, hoặc `AddCore` tự sửa `IConfiguration` nó nhận. Mẫu host ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3 đổi | Không đổi. Thứ tự trong `AddCore` ở §5.1 không đổi; không có nguồn cấu hình nào phải xếp thứ tự |
| Hỏng lộ ra ở đâu | Lúc dựng cấu hình, trước cả `builder.Build()` | Lúc dựng options, qua đúng đường `ValidateOnStart` mà mọi lỗi cấu hình khác đang đi |
| Lệnh runner khi cấu hình có giá trị không giải được | Không chạy được lệnh nào, kể cả `core encrypt-secret`, tức chính lệnh dùng để sửa. Lối vòng: đè chuỗi bằng biến môi trường | Chạy được. Lệnh không đụng tới options của database |
| Nạp lại cấu hình khi tệp đổi | Provider giải mã giữ bản cũ, và vì nó xếp sau cùng nên bản cũ **thắng** bản mới. Dự án tham khảo còn dựng lại mọi nguồn một lần nữa | Options tự dựng lại theo đường của framework |
| Bí mật thứ hai sau này | Tự có, không phải khai gì | Mỗi options muốn giải mã phải khai một dòng. Thư viện đọc thẳng `IConfiguration` sẽ thấy bản mã |

### 2. Người giữ khoá, kênh phát khoá, người kích hoạt xoay khoá

Đây là việc của tổ chức, không phải của Core. Kiến trúc sư khuyến nghị:

- Một người giữ khoá có tên, cộng một người dự phòng.
- Phát khoá qua trình quản lý mật khẩu của nhóm, không qua chat hay email.
- Danh sách việc khi có người rời nhóm có một dòng *"xoay khoá DB dev và đổi mật khẩu `coreandskill_app`"*.

Tên người không ghi vào `docs/` của Core, vì `docs/` đi theo Core sang dự án khác.

### 3. Một database chung, hay mỗi người một database trên máy chủ chung

Một giá trị duy nhất trong git chỉ có nghĩa khi cả nhóm dùng **một** database. Nếu mỗi người một database thì tên database khác nhau theo người, và ai cũng phải đè cả chuỗi bằng `user-secrets`. Khi đó giá trị trong git gần như không ai dùng.

Một database chung kéo theo ba việc nằm ngoài phạm vi ADR này:

- **Migration từ nhánh chưa hợp nhất.** Áp script lên database chung thì mọi người khác mang theo nó. [`../database/script-runbook.md`](../database/script-runbook.md) §5.1 chỉ cảnh báo khi database đi trước code, nên một bước "thu hẹp" từ nhánh của một người sẽ làm hỏng mọi người khác lúc chạy.
- **Việc nền của nhiều tiến trình dev chạy trên cùng dữ liệu.** Mỗi máy dev là một instance. Điều đó trái giả định một instance của [`0014-mot-instance-key-ring-postgres.md`](0014-mot-instance-key-ring-postgres.md) cho môi trường dev chung. Code trên nhánh của một người có thể xử lý, hoặc đánh dấu hỏng, bản ghi outbox của mọi người.
- **Người mở database bằng công cụ SQL.** Không có lệnh giải mã (quyết định 7), nên người đó cần tài khoản Postgres **cá nhân**. Họ không lấy mật khẩu `coreandskill_app` qua kênh riêng, vì làm vậy thì mất đúng lợi ích "đổi mật khẩu là một commit".

Kiến trúc sư khuyến nghị: dùng **một database chung**, và viết **một ADR riêng** cho luật vận hành database chung, gồm ba việc trên. Chưa có ADR đó thì chưa commit giá trị mã hoá nào.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Không mã hoá: chuỗi không mật khẩu trong git, mật khẩu qua biến môi trường hoặc tệp pgpass

**Được:**

- Core không thêm dòng mã nào, vì Npgsql tự đọc tệp pgpass.
- Không có khoá chung.
- Người rời nhóm chỉ cần đổi mật khẩu database là xong.
- Đây là phương án **đơn giản nhất**.

**Mất:**

- Mỗi lần đổi mật khẩu database, từng dev tự sửa máy mình.
- Mỗi bí mật mới thêm một bước cài đặt trên mỗi máy.

**Vì sao không chọn:** người dùng cân nhắc và không chọn ngày 2026-09-25. Khác biệt khách quan là thứ phải đi ngoài git. Phương án này đưa **mật khẩu database** ra ngoài git; phương án đã chọn đưa **khoá giải mã** ra ngoài git, và khoá đổi ít hơn. **Không phương án nào bỏ được bước đi ngoài git**: dev mới vẫn phải nhận khoá qua một kênh riêng.

### Phương án B — Chép thuật toán của dự án tham khảo

**Được:** giống hệt thứ nhóm đã quen. **Mất:** ba điểm yếu chính tệp đó tự ghi: bản mã tất định, KDF yếu, không phát hiện được dữ liệu bị sửa. **Vì sao loại:** người dùng chốt AES-GCM.

### Phương án C — Mã hoá cả chuỗi kết nối

**Được:**

- Không phải đọc cú pháp chuỗi kết nối.
- Giấu được cả tên máy chủ.

**Mất:**

- Diff không đọc được.
- Đổi tên database cũng phải có khoá.
- Review không thấy chuỗi đang trỏ tới máy chủ nào.
- Cổng tĩnh không phân biệt được "mật khẩu trống" với "mật khẩu đã mã hoá".

**Vì sao loại:** dự án tham khảo chỉ mã hoá mật khẩu, và người dùng chỉ dẫn làm giống. Nếu cần giấu cả tên máy chủ thì lật bằng ADR mới.

### Phương án D — Giải mã mọi giá trị cấu hình mang dấu, ở bất kỳ khoá nào

**Được:** bí mật thứ hai không cần ADR. **Mất:** phạm vi rộng hơn nhu cầu có thật, vốn chỉ là một chuỗi. Đây đúng là khuôn *"sau này chắc cần"* mà ngưỡng Core chặn ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2). Mỗi khoá mới được mã hoá vào git còn mở rộng tập bí mật mà **mọi** người giữ khoá đọc được. Đó là một quyết định an ninh, không được xảy ra ngầm qua một lần sửa cấu hình. **Vì sao loại:** hai lý do trên.

### Phương án E — Mật khẩu người chọn cộng KDF, thay cho khoá ngẫu nhiên

**Được:** khoá gõ tay được. **Mất:**

- Có thêm salt và số vòng phải quản; dự án tham khảo đã phải thêm một bí mật thứ hai là salt.
- Khởi động chậm thêm vì số vòng KDF.
- Mật khẩu người chọn yếu hơn 32 byte ngẫu nhiên.

**Vì sao loại:** khoá không bao giờ cần gõ tay, người dùng dán nó từ kênh phát.

### Phương án F — Công cụ mã hoá tệp bên ngoài (SOPS với age, git-crypt)

**Được:**

- Dùng công cụ chuẩn của ngành, không có mã mật mã trong Core.
- **Khoá theo từng người.** Người rời nhóm thì gỡ đúng người đó, không phải phát lại khoá cho cả nhóm.

**Mất:**

- Phải cài thêm một công cụ trên mọi máy dev và trên CI.
- git-crypt biến tệp thành nhị phân trong diff.
- Cần một bước giải mã trước khi chạy, hoặc một bộ nạp riêng.

**Vì sao loại:** người dùng chọn giải mã trong ứng dụng. Cái giá là mất khoá theo từng người, ghi ở mục *Tiêu cực*.

### Phương án G — Tệp cấu hình riêng cho DB chung

Ví dụ một tệp như `appsettings.SharedDev.json`. **Được:** tách hẳn cấu hình DB chung khỏi cấu hình dev còn lại. **Mất:**

- Cần mã nạp thêm một nguồn cấu hình.
- Mẫu loại trừ tệp theo môi trường chỉ chừa đúng `appsettings.Development.json` ([`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §10, lệnh 2). Tệp mới phải đục thêm một lỗ trong mẫu đó.

**Vì sao loại:** `user-secrets` đè được `appsettings.Development.json` theo thứ tự nguồn của framework, nên dev không dùng DB chung vẫn có đường ra mà không cần tệp riêng.

### Phương án H — Thiếu khoá thì bỏ qua lặng lẽ, như dự án tham khảo

**Được:** máy chưa có khoá vẫn khởi động. **Mất:** tiến trình chạy với chuỗi rác. Triệu chứng là lỗi xác thực database xuất hiện ở chỗ khác và không nói gì về nguyên nhân. **Vì sao loại:** trái [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §4. Máy chưa có khoá đã có đường ra đúng là đè chuỗi, theo quyết định 5.

## Hệ quả

### Tích cực

- Máy mới clone về, đặt đúng một khoá, là chạy được với DB chung. Đổi mật khẩu database chỉ cần một commit.
- Mật khẩu không bao giờ nằm dạng rõ trong git. Host và database vẫn đọc được khi review.
- Thiếu khoá hay sai khoá thì dừng ngay lúc khởi động và nêu tên khoá. Không có ca nào chạy tiếp với chuỗi rác.
- Phần `appsettings*.json` của nợ S12 được đóng **trước** khi rủi ro đó tăng lên.
- Dự án hạ nguồn không dùng tính năng này thì không đổi gì.

### Tiêu cực

- **Một khoá chung.** Ai có khoá cũng đọc được mọi giá trị mã hoá, cả hôm nay lẫn trong **toàn bộ lịch sử git**. Tập người biết mật khẩu DB dev là tập người **từng** giữ khoá, không phải tập người đang giữ.
- **Lộ khoá thì phải đổi mật khẩu database *và* xoay khoá.** Chỉ xoay khoá mà không đổi mật khẩu là vô tác dụng: bản mã cũ trong lịch sử cộng khoá cũ vẫn ra mật khẩu đang dùng.
- **Mỗi lần có người rời nhóm, quy trình phải chạy lại đủ năm bước:** đổi mật khẩu database, sinh khoá mới, mã hoá lại, commit, phát khoá mới cho mọi người còn lại. Phương án A chỉ cần bước đầu và bước phát. Chi phí này trả **mỗi lần**, không phải một lần.
- **Mất khoá theo từng người** mà phương án F có.
- **Bước đi ngoài git vẫn còn**, chỉ đổi từ mật khẩu sang khoá.
- **Core mang thêm mã mật mã phải bảo trì.** Một lỗi ở đó, như nonce lặp lại hay dùng sai API, là lỗi an ninh. Người review mã đó phải hiểu AEAD.
- **S6 có ngoại lệ đầu tiên.** Rule gitleaks tuỳ chỉnh phải có người bảo trì, và mỗi lần báo sai sẽ tạo áp lực nới nó.
- **Không có lệnh giải mã.** Ai cần mở DB chung bằng công cụ SQL không lấy được mật khẩu từ git. Việc đó thuộc câu hỏi 3.
- **CI không có khoá.** Integration test dựng host ở môi trường Development, và hôm nay đều đè `ConnectionStrings:Core` (`src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/CoreWebApplicationFactory.cs`, chuỗi `UnreachableConnectionString`). Một test mới quên đè sẽ đỏ trên CI vì thiếu khoá. Đỏ ồn ào như vậy thì chấp nhận được.
  - Bước E10 dựng host `CoreAndSkill.Api` qua `dotnet ef … has-pending-model-changes`. Theo tài liệu EF, công cụ này mặc định môi trường Development. Bước đó **chưa chạy lần nào**, và kiến trúc sư **chưa chạy thử**. Suy từ mã, nó cần một chuỗi kết nối rõ, không mật khẩu, đè qua biến môi trường của bước. Có thể nó đang cần điều đó ngay hôm nay, vì `ConnectionStrings:Core` rỗng không qua được `[Required]`.
- **DB chung có rủi ro riêng.** Xem câu hỏi 3.

### Rút lui nếu sai

1. Đặt phần mật khẩu trong `appsettings.Development.json` về rỗng. Mỗi dev đặt chuỗi đầy đủ qua `user-secrets` như hôm nay.
2. **Đổi mật khẩu `coreandskill_app` của DB chung.** Bước này bắt buộc: mọi người từng giữ khoá vẫn đọc được bản mã trong lịch sử.
3. Gỡ bước giải mã, lệnh `core encrypt-secret`, nhóm khoá `Core:ConfigEncryption` và test của chúng.
4. Viết ADR mới đưa S6 về không ngoại lệ. Nửa *"không mật khẩu rõ trong `appsettings*.json`"* của S23 giữ lại, vì nó vẫn đúng.

Tổng cộng là một PR cộng một lần đổi mật khẩu. Không dữ liệu nào trong database phụ thuộc định dạng này.

### Dấu hiệu quyết định này bắt đầu sai

- Có đề xuất đặt khoá lên CI hoặc lên máy chủ "cho tiện". Khi đó tập người giữ khoá không còn là nhóm dev.
- Có yêu cầu mã hoá một khoá thứ hai. Nếu yêu cầu kiểu đó lặp lại, xem lại phương án F.
- Có người rời nhóm mà khoá không được xoay.
- Phần lớn dev đè chuỗi bằng `user-secrets`. Khi đó giá trị trong git không ai dùng, và nên gỡ.
- Rule gitleaks tuỳ chỉnh bị nới hoặc bị tắt vì báo sai.
- Giá trị mã hoá xuất hiện ngoài `appsettings.Development.json`.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Một thay đổi quy trình người dùng đã khai: repo chung, DB server chung bắt buộc. **Chưa đo.** Chưa có số liệu về tần suất đổi mật khẩu database, số người trong nhóm hay tốc độ thay người. Lợi thế so với phương án A phụ thuộc đúng hai con số đó |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án A đơn giản hơn: không thêm mã nào. Người dùng không chọn nó. Chỗ nó thiếu so với phương án đã chọn nằm ở mục *Phương án A* |
| 3 | Chi phí vận hành thêm | Giữ và phát khoá. Mỗi lần có người rời nhóm: đổi mật khẩu, xoay khoá, mã hoá lại, phát lại. Bảo trì rule gitleaks tuỳ chỉnh và mã mật mã. Trả mãi mãi |
| 4 | Ai bảo trì | Mã thuộc Core, và người giữ Core chịu. `backend-expert` thi công; `core-reviewer` soát vì mã nằm trong `core-paths`; `test-engineer` giữ cổng. **Người giữ khoá: chưa có** — câu hỏi 2 |
| 5 | Rút lui thế nào | Bốn bước ở mục *Rút lui nếu sai*. Bước đổi mật khẩu là bắt buộc, không bỏ được |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Đây là cơ chế cấu hình của composition root, không có từ vựng nghiệp vụ, không biết module nào. Tên nhóm khoá chung chung; giá trị khoá theo nhóm, nằm ngoài repo |

## Thi công và kiểm

| Ai | Việc | Khi nào |
| --- | --- | --- |
| `backend-expert` | Bộ mã hoá `v1`, `internal`, ở `Core.Web`: nonce lấy từ bộ sinh số ngẫu nhiên mật mã; tag khai tường minh 16 byte; lỗi trả về theo **loại** (thiếu khoá · sai dạng · phiên bản lạ · xác thực hỏng), không theo chuỗi | Ngay |
| `backend-expert` | Nhóm khoá `Core:ConfigEncryption:Keys`: mã khoá đúng khuôn, mỗi khoá đúng 32 byte, `ValidateOnStart` (luật A8), nhóm trống hợp lệ | Ngay |
| `backend-expert` | Lệnh `core encrypt-secret --key <mã khoá>` trong runner. Hàng lệnh đã có ở [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3 | Ngay |
| `backend-expert` | Gắn bước giải mã cho phần mật khẩu của `ConnectionStrings:Core`, và phép kiểm dấu mã hoá sai chỗ. Cả hai xét giá trị hiệu lực, chạy ở mốc khởi động, không chạy ở mốc đăng ký, để lệnh runner không bị chặn | **Sau khi chốt câu hỏi 1** |
| `test-engineer` | Test bộ mã hoá: mã hoá rồi giải mã ra đúng bản rõ; hai lần mã hoá cùng bản rõ cho hai bản mã khác nhau; sửa nonce, bản mã, tag hay chuỗi đầu thì hỏng; mã khoá lạ ra lỗi thiếu khoá; sai khoá cùng mã; dữ liệu sai dạng; khoá sai độ dài | Ngay |
| `test-engineer` | Test khởi động, host không database: giải mã đúng; thiếu khoá; sai khoá; chuỗi bị sửa; không dấu thì dùng nguyên; bản mã bị nguồn sau đè thì khởi động được mà không cần khoá; dấu ở khoá khác thì dừng; dấu viết hoa khác vẫn bị nhận ra. Thông điệp nêu khoá cấu hình và mã khoá, và **không** chứa bản rõ, bản mã hay khoá | Sau khi chốt câu hỏi 1 |
| `test-engineer` | Test lệnh runner: đầu vào chuẩn đi một vòng mã hoá rồi giải mã về đúng; thiếu khoá thì thoát mã khác 0 và không in gì ra đầu ra chuẩn; không mở kết nối database | Ngay |
| `test-engineer` | Cổng S23, lớp 1: ArchTest đọc mọi `appsettings*.json` dưới `src/BE`, loại `bin/` và `obj/`. Chuỗi kết nối đọc bằng bộ đọc nhận mọi tên đồng nghĩa của khoá mật khẩu. Kèm meta-test T6 khẳng định tập tệp khác rỗng, và canary `Detector_*` cho từng dạng vi phạm | Ngay — **trước** commit giá trị mã hoá đầu tiên |
| `test-engineer` | Cổng S23, lớp 2: rule tuỳ chỉnh trong `.gitleaks.toml`, nhắm `appsettings*.json`, bắt mật khẩu bất kể entropy, miễn trừ **đúng** dạng §6.4. Theo luật D35, chạy thử trên toàn bộ lịch sử và cây làm việc trước khi hợp nhất. Đo xem rule mặc định có báo bản mã không. Nếu có, thêm allowlist hẹp theo cả đường dẫn lẫn đúng dạng, và ghi vào [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1 | Ngay — **trước** commit giá trị mã hoá đầu tiên |
| `test-engineer` | Bước E10 trên CI: xác nhận nó dựng host được khi không có khoá. Nếu không, cấp chuỗi rõ không mật khẩu qua biến môi trường của bước | Lần đầu job backend chạy |
| `core-reviewer` | Soát phần thi công. Mã nằm dưới `src/BE/Core/` | Sau mỗi lượt chạm Core |

Mẫu cấu hình gitleaks **cố ý không** viết sẵn ở đây. Một mẫu chưa ai chạy, nằm trong tài liệu, sẽ được chép nguyên văn ([`../audit/2026-09-21-mau-ma-trong-luat-khong-ai-chay.md`](../audit/2026-09-21-mau-ma-trong-luat-khong-ai-chay.md)). Yêu cầu của rule nằm ở hàng S23 trong [`../DEBT.md`](../DEBT.md).

## Liên quan

- [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1, §6.4 — luật S6 mới và bảng vận hành của dạng mã hoá
- [`../DEBT.md`](../DEBT.md) — S12, S23, S24, S25
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3, §4 — lệnh runner, cấu hình fail-fast
- [`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §2 — máy chủ triển khai, không đổi
- [`0014-mot-instance-key-ring-postgres.md`](0014-mot-instance-key-ring-postgres.md) — giả định một instance, câu hỏi 3
- [`0025-luu-du-lieu-module-mot-transaction.md`](0025-luu-du-lieu-module-mot-transaction.md) — một kết nối cho mọi `DbContext`
