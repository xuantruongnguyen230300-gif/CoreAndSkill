---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0100 — Host `CoreAndSkill.Api` thuộc vùng dự án và ra khỏi khối `core-paths`; ba vùng sở hữu tệp thay cho *"tệp thuộc `Core/`"*

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

Luật 1 của [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) nói: *"Dự án KHÔNG sửa tệp thuộc `Core/`"*. ADR đó không định nghĩa `Core/` là gì. Có hai cách đọc, và tài liệu đang dùng cả hai:

1. Thư mục `src/BE/Core/`.
2. Khối `core-paths` ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §10. Luật F38 ở [`../RULES.md`](../RULES.md) §7 đọc theo cách này: `index.html` thuộc khối `core-paths` nên dự án hạ nguồn không sửa nó.

Khối `core-paths` gồm cả host `src/BE/CoreAndSkill.Api/`. Theo cách đọc thứ hai, dự án hạ nguồn không được sửa host. Nhưng dự án hạ nguồn **bắt buộc** sửa host:

- Mỗi module thêm một dòng đăng ký vào `Program.cs` ([`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3).
- `appsettings.json` của host mang giá trị riêng của sản phẩm.
- Dockerfile của host chép từng tệp `.csproj`, nên mỗi module mới thêm một dòng.

Kiến trúc sư đối chiếu host ngày 2026-09-25:

| Tệp trong `src/BE/CoreAndSkill.Api/` | Giá trị mang tên sản phẩm |
| --- | --- |
| `CoreAndSkill.Api.csproj` | Chuỗi `<UserSecretsId>` — một GUID cố định |
| `appsettings.json` | Chuỗi `"CookieName": "coreandskill.session"` và `"AntiforgeryCookieName": "coreandskill.antiforgery"` |
| `Dockerfile` | Chuỗi `/var/lib/coreandskill/files` — thư mục kho tệp; tên image trong chú thích lệnh build |
| `Program.cs` | Không. Tệp có 20 dòng |
| `appsettings.Development.json`, `Properties/launchSettings.json` | Không |

Hai hệ quả khi host nằm trong khối:

- Trong một dự án hạ nguồn, **mỗi lần đăng ký module là một lần "sửa Core"**, trái luật 1 của ADR-0016.
- Hook `Stop` chặn lượt cho tới khi có một lượt `core-reviewer` mới hơn. Lượt đó phải soát một dòng đăng ký module theo luật của Core.

Khối `core-paths` là **đầu vào của hook** ([`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8). Bộ test hook dựng một khối giả riêng, không đọc khối thật. Cổng tài liệu §26 đọc khối thật để kiểm nó còn đọc được.

Một báo cáo đánh giá độc lập ngày 2026-09-25 nêu vấn đề này là P1. Người dùng chốt hướng sửa cùng ngày.

## Quyết định

Người dùng chốt ngày 2026-09-25: **host thuộc vùng dự án, ra khỏi khối `core-paths`; sở hữu tệp chia ba vùng.** Kiến trúc sư ghi phần đi kèm.

1. **Gỡ `src/BE/CoreAndSkill.Api/` khỏi khối `core-paths`.** Host thuộc vùng dự án, kể cả trong repo Core. Ở repo Core, host đóng vai **mẫu** composition root, mà dự án hạ nguồn nhận về rồi sở hữu.

2. **Ba vùng sở hữu tệp.** Định nghĩa gốc ở [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.5.
   - **Vùng Core** — dự án không sửa.
   - **Vùng dự án** — Core không phụ thuộc vào nội dung của nó: host, `CORE_VERSION`, `src/BE/Modules/`, `database/scripts/modules/`.
   - **Vùng dùng chung có thủ tục** — `src/BE/CoreAndSkill.slnx`, `src/BE/Directory.Packages.props`. Dự án chỉ **thêm** dòng của mình, không sửa và không xoá dòng của Core.

   Luật 1 của ADR-0016 từ nay đọc là: *dự án không sửa tệp thuộc vùng Core*.

3. **Hai câu hỏi, hai nguồn, không suy qua lại.**
   - Khối `core-paths` trả lời: *đổi tệp này có cần `core-reviewer` không*.
   - Vùng sở hữu trả lời: *dự án hạ nguồn có được sửa tệp này không*.

   Mọi đường dẫn trong khối thuộc vùng Core; điều ngược lại không đúng. Ví dụ `src/BE/Tests/CoreAndSkill.Core.UnitTests/` thuộc vùng Core mà không nằm trong khối.

   F38 đọc lại theo đó: `index.html` thuộc vùng Core **vì bảng vùng nói vậy**, không vì nó nằm trong khối. Kết luận của F38 không đổi, chỉ đổi căn cứ. Cách đọc *"thuộc `core-paths` nên dự án không sửa"* ở [`0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md`](0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md) và ở F38 hết hiệu lực.

4. **Phía FE chưa phân vùng xong.** Phần FE trong khối `core-paths` thuộc vùng Core. Phần còn lại của `src/FE/` — composition root `app.config.ts`, tệp token toàn cục — vẫn là câu hỏi để ngỏ ở phương án C của ADR-0059. ADR này không trả lời nó.

5. **Thay đổi Core mà host phải làm theo thì CHANGELOG bắt buộc có dòng *"Dự án phải làm gì"*, kể cả khi thay đổi đó không phá vỡ.** Ví dụ: một dòng mới ở `Program.cs`, một khoá mới trong `appsettings.json`, một dòng `COPY` mới trong Dockerfile. Host thuộc dự án, nên lần kéo bản Core mới **không** mang thay đổi đó tới dự án theo cách nào khác. Đây là phần mở rộng luật 4 của ADR-0016.

6. **Bù cho việc host ra khỏi khối: hai cổng ArchTest.** ArchTests vẫn nằm trong khối, nên nới hai cổng này vẫn đòi `core-reviewer`.
   - **A7 thành test thật.**
     - Tệp trong host chỉ thuộc tập cho phép: `Program.cs`, `appsettings*.json`, tệp `.csproj`, `Properties/launchSettings.json`, `Dockerfile`.
     - Không kiểu nào trong assembly host là controller, middleware, filter, handler, `DbContext` hay migration. Tầng kiểu vẫn cần, vì một lớp khai ngay trong `Program.cs` vẫn lọt tầng tệp.
   - **`Program.cs` dưới 50 dòng** thành test. Ngưỡng giữ nguyên chữ *"dưới 50 dòng"* của [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §3.

7. **Giá trị mang tên Core trong vùng dự án phải được thay khi dựng dự án từ bản clone.** Danh sách và thao tác ở [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.6.
   - **`UserSecretsId` — dự án sinh GUID mới lúc clone; không gỡ khỏi bản Core.**
     - Vì sao phải đổi: hai repo cùng GUID trên một máy dùng chung một kho `user-secrets`. Chuỗi kết nối và mật khẩu bootstrap của dự án này chạy sang dự án kia mà không báo gì.
     - Vì sao không gỡ khỏi bản Core: framework chỉ nạp `user-secrets` khi assembly mang `UserSecretsId`. Gỡ đi thì mọi dev của repo Core mất `user-secrets`. Muốn có lại, mỗi dev phải tự sinh một GUID vào một tệp đang được theo dõi, và mỗi máy một giá trị.
   - **Tên hai cookie** `coreandskill.session`, `coreandskill.antiforgery`. Trình duyệt không cách ly cookie theo cổng: chạy repo Core và một dự án cùng lúc trên `localhost` thì hai app ghi đè phiên của nhau. Hai sản phẩm dưới cùng một miền cha cũng vậy.
   - **Đường kho tệp** `/var/lib/coreandskill/files` trong Dockerfile, cùng giá trị `Core:File:RootPath` của mỗi môi trường. Hai sản phẩm trên cùng máy chủ mà chung đường thì bảo trì kho tệp của sản phẩm này xoá tệp *"không có bản ghi"* của sản phẩm kia.

8. **Các ArchTest đang quét host vẫn quét host.** Chúng biết host bằng tên project, không bằng khối `core-paths`. Câu *"host thuộc Core theo khối `core-paths`"* ở mô tả B7 hết đúng và được sửa.

9. **Luật.** Hai luật của ADR-0016 lần đầu có mã. Không luật nào trong số dưới đây có cổng hôm nay, nên cả bốn nằm ở [`../DEBT.md`](../DEBT.md):

   | Mã | Luật |
   | --- | --- |
   | **A17** | Dự án không sửa tệp thuộc vùng Core — luật 1 của ADR-0016 |
   | **A18** | `CORE_VERSION` đúng ba dòng, cập nhật cùng lần kéo — luật 2 |
   | **A19** | Giá trị mang tên Core được thay khi dựng dự án — quyết định 7 |
   | **A20** | Thay đổi ở repo Core chạm host thì cùng PR có mục CHANGELOG — quyết định 5 |

   A7 ở [`../RULES.md`](../RULES.md) §3 mở rộng theo quyết định 6.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ host trong khối, định nghĩa ngoại lệ cho từng tệp dự án được sửa

**Được:** mọi thay đổi host trong repo Core vẫn đòi `core-reviewer`. Không có cửa sổ nào host đứng ngoài tầm soát.

**Mất:**

- Dự án hạ nguồn vẫn bị hook chặn mỗi lần đăng ký module.
- Danh sách ngoại lệ theo tệp phải liệt kê từng dòng được sửa của `Program.cs`. Không định dạng nào của khối biểu diễn được *"dòng này được sửa, dòng kia không"*.

**Vì sao loại:** host là tệp dự án **phải** sửa, không phải tệp dự án **thỉnh thoảng** sửa. Một luật mà mọi dự án vi phạm ở tuần đầu tiên sẽ bị bỏ qua, và kéo theo luật 1 của ADR-0016 bị bỏ qua.

### Phương án B — Chuyển phần host mà dự án sửa vào một project đăng ký module riêng; host còn nguyên trong khối

**Được:** host thật sự bất biến. Dự án chỉ sửa một project khác.

**Mất:**

- Thêm một project cho mỗi dự án.
- `appsettings.json`, `UserSecretsId`, Dockerfile vẫn là giá trị dự án và vẫn nằm ở host. Không giải được phần lớn vấn đề.

**Vì sao loại:** chỉ giải được một trong bốn loại thay đổi dự án phải làm ở host.

### Phương án C — Dùng khối `core-paths` làm luôn định nghĩa vùng Core

**Được:** một danh sách thay vì hai.

**Mất:** hai câu hỏi khác nhau bị buộc chung một câu trả lời.

- Có tệp thuộc Core mà không cần review mỗi lần sửa, như test đơn vị của Core.
- Có tệp cần review mà vùng sở hữu chưa chốt, như phần FE ở mục 4.

Hook đòi đường dẫn chính xác. Bảng vùng cần quy tắc mặc định cho phần còn lại của cây.

**Vì sao loại:** đây chính là nguyên nhân của vấn đề. F38 đã đọc khối như định nghĩa vùng, và cách đọc đó làm host thành vùng Core.

### Phương án D — Gỡ `UserSecretsId` khỏi bản Core, thay cho việc dự án sinh mới

**Được:** không có GUID nào để quên đổi.

**Mất:** repo Core mất `user-secrets` cho tới khi từng dev tự sinh GUID vào một tệp đang được theo dõi. Mỗi máy một giá trị, và diff của tệp `.csproj` lệch theo người.

**Vì sao loại:** lý do ở quyết định 7.

## Hệ quả

### Tích cực

- Dự án hạ nguồn đăng ký module, đổi tên cookie, thêm dòng Dockerfile mà không vi phạm ADR-0016 và không bị hook chặn.
- Luật 1 của ADR-0016 có một định nghĩa đọc được, kèm quy tắc cho phần chưa liệt kê.
- Hai luật của ADR-0016 lần đầu có mã luật và có chỗ trong sổ nợ, nên nhìn thấy được là chưa ép.
- Ràng buộc *host mỏng* chuyển từ *"có người review"* sang *"có test"*, khi A7 được thi công.

### Tiêu cực

- 🛑 **Cửa sổ không canh.** Từ lúc khối được sửa, tức ngay lượt này, tới lúc A7 và test `Program.cs` được thi công, một thay đổi host **trong repo Core** không đòi `core-reviewer` và không test nào bắt. Một middleware lọt vào host trong cửa sổ đó sẽ không bị ai thấy. A7 hôm nay còn `📐`. Việc đầu tiên của `test-engineer` là đóng cửa sổ này.
- **Xung đột lúc kéo bản Core.** Repo Core sửa `Program.cs` của nó, dự án cũng đã sửa `Program.cs` của mình, nên `git merge` theo tag sẽ xung đột ở host. Dự án tự giải, vì host là của dự án. CHANGELOG là thứ duy nhất nói dự án phải giữ dòng nào.
- **Hai danh sách phải giữ nhất quán:** khối `core-paths` và bảng vùng. Luật *"mọi đường dẫn trong khối thuộc vùng Core"* chưa có cổng.
- **Phía FE và các tệp gốc repo còn chưa phân vùng.** Cổng của A17 không dựng được cho tới khi chúng được phân vùng.
- **A19 là việc làm tay một lần**, dễ quên. Quên thì chỉ lộ ra khi một dev chạy cùng lúc hai repo trên một máy.

### Rút lui nếu sai

1. Thêm lại dòng `src/BE/CoreAndSkill.Api/` vào khối, và hàng *host* vào bảng *Nguồn của từng nhóm*.
2. Chạy `bash .claude/hooks/tests/run-tests.sh` và `bash .claude/check-docs.sh`.
3. Bảng vùng sở hữu và bốn dòng nợ giữ nguyên: chúng vẫn đúng khi host quay lại khối. Chỉ dòng *host* trong bảng vùng đổi sang vùng Core, kèm ngoại lệ cho dòng đăng ký module.

Không dữ liệu nào phải xử lý. Dấu `core-touched` chỉ là trạng thái chạy.

### Dấu hiệu quyết định này bắt đầu sai

- Một PR của repo Core thêm vào host một thứ ngoài tập cho phép, mà không cổng nào đỏ — nghĩa là A7 chưa về.
- Một dự án hạ nguồn sửa `Program.cs` quá phần đăng ký module, để né một seam Core còn thiếu. Đó là dấu hiệu seam thiếu, và phải nâng lên Core theo [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.4.
- CHANGELOG có một bản phát hành đổi host mà không có dòng *"Dự án phải làm gì"*.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Mâu thuẫn chứng minh được trên giấy, giữa luật 1 của ADR-0016, cách đọc của F38, và mẫu `Program.cs` ở `be-architecture.md` §3. **Chưa đo trên dự án hạ nguồn thật** — chưa có dự án nào |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án C, dùng khối làm định nghĩa vùng, đơn giản hơn: một danh sách. Nó trộn hai câu hỏi, và chính sự trộn đó sinh ra vấn đề |
| 3 | Chi phí vận hành thêm | Giữ hai danh sách nhất quán. Mục CHANGELOG cho mọi thay đổi chạm host. Một bước thay giá trị lúc dựng mỗi dự án |
| 4 | Ai bảo trì | Bảng vùng: `architect`. Cổng A7 và test `Program.cs`: `test-engineer`. CHANGELOG: người phát hành Core |
| 5 | Rút lui thế nào | Ba bước ở mục *Rút lui nếu sai* |
| 6 | Có buộc Core biết nghiệp vụ không | Ngược lại: nó rút một tệp mang giá trị riêng của sản phẩm ra khỏi phạm vi Core |

## Thi công và kiểm

| # | Ai | Việc |
| --- | --- | --- |
| 1 | `test-engineer` | A7 thành ArchTest thật, hai tầng như quyết định 6. Kèm canary `Detector_*` (T1): một tệp lạ trong host đỏ; một lớp middleware khai trong `Program.cs` đỏ. Meta-test T6: tập tệp host đọc được khác rỗng |
| 2 | `test-engineer` | Test `Program.cs` dưới 50 dòng, kèm canary |
| 3 | `backend-expert` | Không có việc mã ở repo Core. Khi dựng dự án hạ nguồn đầu tiên: thay ba nhóm giá trị theo [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.6 |
| 4 | Phiên chính | Khuôn mục trong `CHANGELOG.md` ở gốc repo: dòng *"Dự án phải làm gì"* áp cho mọi thay đổi host phải làm theo, không riêng thay đổi phá vỡ. Tệp nằm ngoài phạm vi ghi của `architect` |

## Liên quan

- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) — được ADR này sửa một phần: phạm vi của luật 1, phần mở rộng của luật 4
- [`0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md`](0059-cong-fe-canary-va-tai-san-fe-cua-core-vao-core-paths.md) — được ADR này bổ sung: cách đọc *"thuộc `core-paths`"*
- [`../quy-uoc/repo-artifact.md`](../quy-uoc/repo-artifact.md) §15.5, §15.6 — bảng vùng, giá trị phải thay
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §10 — khối `core-paths`
- [`0102-module-o-src-be-modules-test-canh-module.md`](0102-module-o-src-be-modules-test-canh-module.md) — chỗ đặt `src/BE/Modules/`
