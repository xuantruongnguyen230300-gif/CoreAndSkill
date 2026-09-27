---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0098 — Giải mã ở bước dựng `CoreConnectionOptions`; cả nhóm dùng một database dev chung; khoá mang mã `dev` nằm trong git, nên mã hoá chỉ còn là che mắt

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

[`0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md`](0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md) chốt hướng đi: mật khẩu DB dev chung vào git dưới dạng AES-256-GCM, khoá giải mã ở ngoài repo. ADR đó để ngỏ ba câu cho người dùng:

1. Giải mã ở tầng options hay bằng provider cấu hình.
2. Ai giữ khoá, phát khoá qua kênh nào, ai kích hoạt xoay khoá.
3. Một database chung, hay mỗi người một database trên máy chủ chung.

Người dùng trả lời cả ba ngày 2026-09-25. Câu 3 kéo theo luật vận hành database chung, ghi ở [`0099-luat-van-hanh-db-dev-chung.md`](0099-luat-van-hanh-db-dev-chung.md).

**Câu 2 được trả lời bằng cách đổi tiền đề của nó.** Người dùng chọn đặt khoá dev **trong git**, giống dự án tham khảo. Kiến trúc sư giải thích hệ quả trước khi người dùng chốt: ai đọc được repo thì giải mã được mật khẩu DB dev. Mã hoá khi đó không bảo vệ gì khi repo lộ; nó chỉ che mắt — mật khẩu không hiện ra khi lướt tệp hay chia sẻ màn hình, và gitleaks không báo động.

Kiến trúc sư đối chiếu dự án tham khảo ngày 2026-09-25, chỉ đọc: ở `D:\HRE\VNR.HRE\src\backend`, tệp `Config/Common/Security.json` có lịch sử commit trong git và không có thay đổi cục bộ, tức nằm trong git. Tệp đó mang khoá cấu hình `SecretPassword`. Nội dung khoá không được chép ra.

Mô hình người dùng quen là: **dev clone về là code được; môi trường thật là việc của DevOps.** Mô hình của ADR-0097 thêm một bước ngoài git cho mỗi dev mới — nhận khoá qua kênh riêng — và một vai trò người giữ khoá. Người dùng không muốn cả hai.

**Hôm nay chưa có gì để gỡ.** `src/` chưa vào git, nên chưa có bản mã nào trong lịch sử. Chưa dòng mã nào của tính năng mã hoá được thi công.

Chỗ đọc chuỗi kết nối trong mã sản phẩm, đối chiếu ngày 2026-09-25:

| Chỗ | Neo |
| --- | --- |
| Bind options | `src/BE/Core/CoreAndSkill.Core.Web/DependencyInjection/CoreOptionsServiceCollectionExtensions.cs`, chuỗi `services.AddOptions<CoreConnectionOptions>()` |
| Đọc `IConfiguration` trực tiếp | Không có trong mã sản phẩm: `grep -rn 'GetConnectionString' src/BE/Core src/BE/CoreAndSkill.Api --include=*.cs` không ra dòng nào ngoài `obj/`. Dòng duy nhất dưới `src/BE` là của Testcontainers trong `PostgresFixture` |

Dòng thứ hai là điều kiện để cơ chế B đủ: không mã nào của Core đọc chuỗi kết nối qua đường khác options.

## Quyết định

Người dùng chốt ngày 2026-09-25 ba câu trả lời dưới đây. Kiến trúc sư ghi phần đi kèm.

### 1. Cơ chế giải mã: B — ở bước dựng `CoreConnectionOptions`

Bước giải mã chạy lúc dựng đối tượng `CoreConnectionOptions`, không phải ở một provider cấu hình. `IConfiguration` vẫn giữ bản mã; chỉ đối tượng options mang bản rõ. `Program.cs` không đổi, thứ tự trong `AddCore` không đổi. Lỗi giải mã đi đúng đường của mọi lỗi cấu hình khác: `ValidateOnStart`, dừng lúc khởi động, nêu tên khoá cấu hình.

Phép kiểm *dấu mã hoá nằm sai chỗ* cũng chạy ở mốc khởi động, không chạy ở mốc đăng ký dịch vụ. Nhờ vậy lệnh runner không cần database, như `core encrypt-secret`, vẫn chạy được khi cấu hình đang hỏng.

### 2. Một database dev chung cho cả nhóm

Cả nhóm dùng **một** database trên DB server chung. Chuỗi kết nối trong `appsettings.Development.json` trỏ vào database đó. Người không dùng nó thì đè `ConnectionStrings:Core` bằng `user-secrets`. Luật vận hành ở ADR-0099.

### 3. Khoá mang mã `dev` nằm trong git

1. **Mã khoá `dev` là mã dành riêng** cho khoá nằm trong git. Khoá nằm ở `Core:ConfigEncryption:Keys:dev` trong **chính** `appsettings.Development.json`, cùng tệp với bản mã.
   - Kiến trúc sư chọn chỗ này: nó không thêm nguồn cấu hình nào, và tệp đó đã là tệp duy nhất mang bản mã.
   - Tệp riêng như `Security.json` của dự án tham khảo bị loại ở phương án C dưới đây.
2. **Khoá của mọi mã khác không bao giờ vào repo.** Mọi giá trị mã hoá trong repo mang mã `dev`, và giải được bằng khoá `dev` trong repo.
3. **Môi trường thật do DevOps cấp.** Mật khẩu và khoá của môi trường thật đến qua biến môi trường. Môi trường thật **được** dùng cơ chế mã hoá này với khoá riêng của DevOps, mang mã khác `dev`, nhưng **không bắt buộc**. Nếu bản mã và khoá cùng nằm trong biến môi trường của một máy thì cái được cũng chỉ là che mắt.
4. **Điều kiện bắt buộc của DB dev.** Cả bốn điều kiện dưới đây phải đúng, vì mật khẩu DB dev nay nằm trong tay mọi người đọc được repo:
   - Chỉ với tới được từ mạng nội bộ hoặc VPN.
   - Không mang dữ liệu thật — dữ liệu cá nhân thật, hồ sơ nghiệp vụ thật.
   - Tiến trình ứng dụng dùng `coreandskill_app`, quyền hạn chế theo [`../database/script-runbook.md`](../database/script-runbook.md) §3.6. Không bao giờ dùng `coreandskill_owner`.
   - Mật khẩu `coreandskill_app` của DB dev không trùng mật khẩu nào của môi trường khác.
5. **`appsettings.Development.json` không đi vào gói publish.** Khoá `dev` và bản mã không được theo artifact lên máy chủ. Project host khai loại trừ tệp đó khỏi publish; một ArchTest canh khai báo đó (vế (e) của S23).
6. **Mã hoá nay chỉ là che mắt — ghi thẳng ra.** Nó không lộ mật khẩu khi lướt tệp, và không làm gitleaks báo động. Nó không bảo vệ gì khi repo lộ.
7. **Câu 2 của ADR-0097 không còn áp dụng ở dạng cũ.**
   - Không có người giữ khoá và không có kênh phát khoá: khoá đi theo bản clone.
   - **Xoay khoá `dev` vô tác dụng**, vì lịch sử git giữ cả khoá cũ lẫn bản mã cũ. Không có quy trình xoay khoá `dev`.
   - Khi có người rời nhóm, nhóm gỡ quyền đọc repo của người đó. **Nếu người đó còn với tới được DB dev** — còn trong mạng nội bộ hay còn VPN — thì đổi mật khẩu `coreandskill_app` của DB dev, mã hoá lại bằng khoá `dev`, rồi commit. Bản clone cũ trên máy người đó vẫn giải ra mật khẩu cũ, nên chỉ đổi mật khẩu mới thu hồi được.

### 4. Luật

- **S6** viết lại ([`../RULES.md`](../RULES.md) §6). Cấm secret dạng rõ. Khoá giải mã duy nhất được vào repo là khoá mã `dev`. Khoá mọi mã khác và bản mã bằng khoá khác `dev` không vào repo.
- **S23** ở [`../DEBT.md`](../DEBT.md): vế (c) viết lại thành chỗ khai khoá `dev`; thêm vế (d) — mã khoá của mọi bản mã và việc giải được bằng khoá trong repo; thêm vế (e) — loại trừ khỏi publish. Mỗi vế một canary. Cổng vẫn phải xanh **trước** commit giá trị mã hoá đầu tiên (quyết định 9 của ADR-0097 còn nguyên).
- **S25** viết lại: không môi trường máy chủ nào dùng khoá `dev`, và mật khẩu DB dev không dùng lại ở đâu.
- **T13** mới: không test nào kết nối được tới DB dev chung. Xem mục *Tiêu cực*.

### 5. Phần nào của ADR-0097 còn hiệu lực

| Phần của ADR-0097 | Sau ADR này |
| --- | --- |
| Quyết định 1, 2, 4, 5, 6, 7, 9, 10 — chỗ được mã hoá, dạng `enc:v1:`, không giải được thì dừng, bản không dấu dùng nguyên, chỗ đặt, lệnh `core encrypt-secret`, cổng trước giá trị đầu tiên, tính năng ở `Core.Web` | **Còn nguyên** |
| Tiêu đề và câu *"khoá giải mã không bao giờ vào repo"* | Chỉ còn đúng cho khoá mang mã khác `dev` |
| Quyết định 3 — khoá ngoài repo, *"nhiều khoá sống cùng lúc; đó là cơ chế xoay khoá"* | Khoá `dev` nằm trong repo và không xoay. Nhiều mã khoá vẫn sống cùng lúc được — cho môi trường của DevOps |
| Quyết định 8 — S6 cấm khoá giải mã trong repo; S23 vế (c) cấm mọi `appsettings*.json` khai nhóm khoá `Core:ConfigEncryption`; S25 | Thay bằng mục 4 ở trên |
| Câu hỏi 1 và 3 | Trả lời ở mục 1 và 2 |
| Câu hỏi 2 | Không còn áp dụng — mục 3.7 |
| *Tích cực: "Mật khẩu không bao giờ nằm dạng rõ trong git"* | Vẫn đúng về chữ, nhưng không còn giá trị an ninh |
| *Tiêu cực: "Một khoá chung… tập người biết mật khẩu là tập người từng giữ khoá"* | Tập đó nay là tập người **từng đọc được repo** |
| *Phương án A: "Không phương án nào bỏ được bước đi ngoài git"* | Hết đúng: dev không còn bước nào đi ngoài git |

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ khoá ngoài repo, như ADR-0097

**Được:** mã hoá bảo vệ thật khi repo lộ. Người rời nhóm mà không giữ khoá thì không đọc được mật khẩu.

**Mất:**

- Mỗi dev mới cần một bước ngoài git để nhận khoá.
- Cần một người giữ khoá, một kênh phát, một quy trình xoay khoá năm bước mỗi lần có người rời nhóm.

**Vì sao loại:** người dùng chọn mô hình *"clone là code"*, sau khi nghe rõ cái mất.

### Phương án B — Bỏ mã hoá, để mật khẩu dạng rõ trong `appsettings.Development.json`

**Được:**

- Đây là phương án **đơn giản nhất**. Core không cần dòng mã mật mã nào: không bộ mã hoá, không lệnh runner, không nhóm khoá.
- Về an ninh, nó **ngang** với phương án đã chọn. Khoá nằm cạnh bản mã thì ai đọc được bản mã cũng đọc được khoá.

**Mất:**

- Rule gitleaks cho `appsettings*.json` phải có ngoại lệ cho **một mật khẩu dạng rõ** ở một tệp. Luật *"không mật khẩu rõ trong tệp cấu hình"* mất tính phổ quát. Cổng không còn phân biệt được *giá trị được phép* với *bản rõ dán nhầm* bằng hình dạng, chỉ còn phân biệt được bằng đường dẫn.
- Mật khẩu hiện ra mỗi lần có người mở tệp trên màn hình chung.
- Mất đường sẵn có cho môi trường thật, nếu DevOps muốn dùng.

**Vì sao loại:** người dùng chọn giữ mã hoá. Lý do kiến trúc đi kèm là gạch đầu dòng thứ nhất của mục *Mất*: với dạng mã hoá, **mọi** chuỗi mật khẩu dạng rõ trong mọi `appsettings*.json` đều đỏ, không có ngoại lệ theo đường dẫn.

### Phương án C — Khoá trong một tệp riêng nằm trong git, như `Security.json` của dự án tham khảo

**Được:** giống dự án tham khảo từng chi tiết.

**Mất:**

- Cần mã nạp thêm một nguồn cấu hình — đúng phương án G mà ADR-0097 đã loại.
- Tệp đó phải được loại khỏi publish **riêng**, và mẫu loại trừ tệp theo môi trường ở [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §10 phải đục thêm một lỗ.

**Vì sao loại:** không được gì về an ninh so với việc đặt khoá cạnh bản mã, mà mất thêm một nguồn cấu hình và một lỗ trong mẫu loại trừ.

### Phương án D — Provider cấu hình, câu hỏi 1 phương án A của ADR-0097

**Vì sao loại:** người dùng chọn B. So sánh đầy đủ nằm ở bảng câu hỏi 1 của ADR-0097. Với khoá nằm trong git, điểm mạnh của B càng rõ: bản rõ chỉ nằm trong đối tượng options, không nằm trong cây cấu hình mà mọi lần in chẩn đoán đều in ra.

### Phương án E — Mỗi người một database trên máy chủ chung, câu hỏi 3 của ADR-0097

**Vì sao loại:** người dùng chọn một database chung. Mỗi người một database thì tên database khác nhau theo người, ai cũng phải đè cả chuỗi, và giá trị trong git không ai dùng.

## Hệ quả

### Tích cực

- Clone về, chạy, là kết nối được DB dev chung. Dev mới không có bước ngoài git nào.
- Không có vai trò người giữ khoá, không có quy trình xoay khoá.
- Cổng S23 đơn giản và phổ quát: mọi mật khẩu dạng rõ trong mọi `appsettings*.json` đều đỏ.
- Cơ chế sẵn cho môi trường thật, nếu DevOps muốn, mà không phải viết thêm dòng mã nào.
- Cơ chế B không đổi `Program.cs` của dự án nào, và không đổi thứ tự trong `AddCore`.

### Tiêu cực

- **Mã hoá không bảo vệ gì khi repo lộ.** Repo lộ tức mật khẩu DB dev lộ. An ninh của DB dev dựa hoàn toàn vào bốn điều kiện ở quyết định 3.4. Chưa điều kiện nào có cổng máy, và điều kiện *"chỉ trong mạng nội bộ"* là cấu hình hạ tầng, nằm ngoài repo.
- **Người rời nhóm giữ mật khẩu cũ mãi mãi**, qua bản clone trên máy họ. Chỉ đổi mật khẩu database mới thu hồi được. Khác với ADR-0097, ở đây không có gì để *không phát* cho người đó.
- **Core mang mã mật mã mà giá trị an ninh ở máy dev gần bằng không.** Mã đó vẫn phải được bảo trì như mã an ninh: nonce không được lặp, người review phải hiểu AEAD. Giá trị an ninh thật chỉ có ở môi trường dùng khoá ngoài repo, mà môi trường đó hôm nay chưa tồn tại.
- **Dấu `enc:v1:` dễ bị đọc nhầm là "an toàn".** Có thể có người mang bản mã của môi trường khác vào repo, hoặc chép khoá `dev` lên máy chủ "cho tiện". S23 vế (d) bắt ca thứ nhất. Ca thứ hai là S25 và không có cổng máy.
- **gitleaks có thể báo khoá `dev`**, vì đó là 32 byte ngẫu nhiên. Nếu nó báo, cần một allowlist hẹp theo đúng đường dẫn và đúng khoá cấu hình. Mỗi allowlist là một chỗ có thể bị nới.
- **CI và mọi test nay có khoá**, vì khoá nằm trong repo. ADR-0097 dựa vào điều ngược lại: *"CI không có khoá, test quên đè chuỗi thì đỏ ồn ào"*. Nay một test dựng host ở môi trường Development mà quên đè `ConnectionStrings:Core` sẽ giải mã thành công, rồi:
  - trên CI: thử kết nối một máy trong mạng nội bộ mà runner không với tới — đỏ chậm, thông điệp nói về kết nối chứ không nói về test;
  - **trên máy dev: kết nối thật vào DB dev chung, và ghi dữ liệu test vào đó.** Đây là rủi ro lớn nhất của quyết định này. Luật **T13** và việc thi công của `test-engineer` ở dưới tồn tại vì nó.
- **Bước E10 của CI** (`dotnet ef migrations has-pending-model-changes`) dựng host ở môi trường Development, nên nay giải mã được chuỗi. Theo mã, bước này so model với snapshot, không mở kết nối. Kiến trúc sư **chưa chạy thử**.

### Rút lui nếu sai

1. Gỡ khoá `dev` và bản mã khỏi `appsettings.Development.json`. Quay về khoá ngoài repo của ADR-0097, hoặc mỗi dev đặt chuỗi bằng `user-secrets`.
2. **Đổi mật khẩu `coreandskill_app` của DB dev.** Bước này bắt buộc: khoá và bản mã cũ còn trong lịch sử git.
3. Viết ADR mới đưa S6, S23 vế (c), (d) về bản ADR-0097.

Tổng cộng là một PR và một lần đổi mật khẩu. Không dữ liệu nào trong database phụ thuộc chỗ đặt khoá.

### Dấu hiệu quyết định này bắt đầu sai

- DB dev được mở ra ngoài mạng nội bộ hay VPN, hoặc có người đề nghị nạp dữ liệu thật vào DB dev "cho sát".
- Có người đề nghị dùng khoá `dev` cho máy chủ test hay staging.
- Repo được chia sẻ ra ngoài nhóm: đối tác, nhà thầu phụ, bản sao công khai.
- Allowlist gitleaks cho khoá `dev` bị nới rộng hơn đúng một đường dẫn và một khoá cấu hình.
- Một test tích hợp để lại dữ liệu trong DB dev chung.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Chi phí vận hành của ADR-0097: một bước ngoài git cho mỗi dev mới, và một vai trò người giữ khoá. **Chưa đo** số người, tần suất vào nhóm và rời nhóm |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án B — bản rõ trong git — đơn giản hơn và an toàn **ngang bằng**. Nó thiếu tính phổ quát của cổng: phải có ngoại lệ theo đường dẫn cho một mật khẩu dạng rõ |
| 3 | Chi phí vận hành thêm | Giảm so với ADR-0097: không người giữ khoá, không xoay khoá. Còn lại: đổi mật khẩu DB dev khi người rời nhóm còn với tới DB; giữ bốn điều kiện của DB dev; bảo trì mã mật mã và allowlist gitleaks |
| 4 | Ai bảo trì | Mã thuộc Core: `backend-expert` thi công, `core-reviewer` soát, `test-engineer` giữ cổng S23 và T13. Bốn điều kiện của DB dev: người vận hành DB dev chung của nhóm (ADR-0099) — tên người không ghi vào `docs/` của Core |
| 5 | Rút lui thế nào | Ba bước ở mục *Rút lui nếu sai*. Đổi mật khẩu là bước bắt buộc |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Mã khoá `dev` là tên của một môi trường, không phải từ vựng nghiệp vụ |

## Thi công và kiểm

Thứ tự trong bảng là thứ tự làm.

| # | Ai | Việc |
| --- | --- | --- |
| 1 | `test-engineer` | **T13 trước tiên:** bảo đảm không test nào nạp được chuỗi của DB dev chung. Hai hướng, người thi công chọn và ghi lý do: dựng host test ở môi trường khác `Development`, để `appsettings.Development.json` không được nạp; hoặc một meta-test khẳng định mọi host dựng trong test đều đè `ConnectionStrings:Core`. Kèm canary: một host test không đè chuỗi phải làm cổng đỏ |
| 2 | `test-engineer` | Cổng S23 đủ năm vế, lớp ArchTest và lớp gitleaks, mỗi vế một canary ([`../DEBT.md`](../DEBT.md), hàng S23). Vế (d) giải mã **mọi** giá trị `enc:` trong repo bằng khoá `dev` trong repo, qua bộ giải mã của Core; hỏng là đỏ. Đo xem rule mặc định của gitleaks có báo khoá `dev` không; nếu có, allowlist hẹp theo đường dẫn và khoá cấu hình. Không sửa `.gitleaks.toml` khi chưa chạy thử trên toàn lịch sử (D35) |
| 3 | `backend-expert` | Bộ mã hoá, nhóm khoá, lệnh `core encrypt-secret` — như bảng *Thi công* của ADR-0097, không đổi |
| 4 | `backend-expert` | Gắn bước giải mã theo cơ chế B: ở bước dựng `CoreConnectionOptions`, lỗi đi qua `ValidateOnStart`. Phép kiểm dấu sai chỗ chạy ở mốc khởi động. `Program.cs` không đổi |
| 5 | `backend-expert` | Project host loại `appsettings.Development.json` khỏi publish |
| 6 | `test-engineer` | Test khởi động và test lệnh runner, như bảng *Thi công* của ADR-0097. Ca *"thiếu khoá"* nay là ca đè nhóm khoá thành rỗng |
| 7 | Người vận hành DB dev | Sinh khoá `dev`, mã hoá mật khẩu `coreandskill_app`, commit — **chỉ sau khi** mục 1 và 2 xanh |
| 8 | `core-reviewer` | Soát mục 3–5. Mã nằm dưới `src/BE/Core/` |

## Liên quan

- [`0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md`](0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md) — quyết định gốc, được ADR này sửa một phần
- [`0099-luat-van-hanh-db-dev-chung.md`](0099-luat-van-hanh-db-dev-chung.md) — luật vận hành database chung
- [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §6.1, §6.4 — luật S6 và bảng vận hành của dạng mã hoá
- [`../DEBT.md`](../DEBT.md) — S23, S24, S25, T13
