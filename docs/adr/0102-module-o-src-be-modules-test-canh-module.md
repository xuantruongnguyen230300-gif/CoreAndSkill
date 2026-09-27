---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0102 — Module nằm ở `src/BE/Modules/<X>/`, test của module nằm cạnh nó; `src/BE/Tests/` chỉ của Core; ArchTests của Core quét cả module

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

Tài liệu hôm nay có quy ước cho project Core, host và test. Chưa có quy ước cho project module:

- Project Core ở `src/BE/Core/`, host ở `src/BE/CoreAndSkill.Api/`, test ở `src/BE/Tests/` ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §2).
- Không chỗ nào nói project của module nằm ở đâu, hay test của module nằm ở đâu.

[`0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md`](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) chia tệp thành ba vùng sở hữu, và cần biết đường dẫn của module để xếp nó vào vùng dự án.

ArchTests của Core hôm nay chỉ nhìn Core và host. Kiến trúc sư đối chiếu ngày 2026-09-25:

- Luật A3 (`Core.*` không tham chiếu `Modules.*`) nhận diện assembly module bằng **tiền tố tên**. Một dự án hạ nguồn đặt tên module khác tiền tố `CoreAndSkill.Modules.` thì lọt khỏi tầm quét. Mô tả hiện có của A3 ở [`../RULES.md`](../RULES.md) §3 ghi rằng tập vi phạm hôm nay rỗng vì chưa có module nào.
- Các luật quét cú pháp dựng gốc quét từ thư mục Core và host.

Nếu ArchTests không quét module, luật của Core không áp được lên module. Nếu module tự mang ArchTests của mình, module sửa được chính cổng canh nó.

Một báo cáo đánh giá độc lập ngày 2026-09-25 nêu vấn đề này là P3. Cùng báo cáo nêu P4 — `PostgresFixture` chỉ áp `database/scripts/core/` — và P4 ghi thành nợ ở mục *Tiêu cực*.

## Quyết định

Người dùng chốt ngày 2026-09-25 chỗ đặt module và test. Kiến trúc sư ghi phần đi kèm.

1. **Mỗi module một thư mục**: `src/BE/Modules/<X>/`, trong đó mỗi project ở `src/BE/Modules/<X>/CoreAndSkill.Modules.<X>.<Tầng>/`. `<Tầng>` là một trong các project ở bảng hợp đồng lắp ghép, [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §7.
2. **Test của module nằm cạnh module**: `src/BE/Modules/<X>/Tests/`. Cả thư mục `src/BE/Modules/` thuộc vùng dự án.
3. **`src/BE/Tests/` chỉ của Core**, và thuộc vùng Core trọn vẹn. Không project test nào của module đặt ở đó.
4. **ArchTests của Core quét cả module.** Đây là yêu cầu cho `test-engineer`:
   - Luật quét cú pháp lấy thêm gốc `src/BE/Modules/`.
   - Assembly của module nạp qua host. Host đăng ký mọi module nên tham chiếu mọi module, và không test nào giữ danh sách module viết tay.
   - A3 xác định tập assembly module theo **thư mục** `src/BE/Modules/`, không theo tiền tố tên.
   - Có một **module giả** làm canary (luật T1): vi phạm cố ý ở module giả phải làm từng detector đỏ.
   - Ở repo Core, `src/BE/Modules/` rỗng là trạng thái hợp lệ và vĩnh viễn — cùng lập luận với F4 ở [`0036-f4-rong-khop-rong-la-hop-le.md`](0036-f4-rong-khop-rong-la-hop-le.md). Chốt T6 của các detector đó phải đi qua canary module giả, không đi qua tập module thật.
5. **ArchTests ở lại vùng Core.** Module không có ArchTests riêng thay thế chúng. Module thêm test của riêng mình ở thư mục test của nó — như bản sao cổng B7 của nợ B9 — nhưng không nới được luật nào của Core.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Test của module đặt ở `src/BE/Tests/CoreAndSkill.Modules.<X>.Tests/`

**Được:** mọi test ở một chỗ, giống thói quen của nhiều solution .NET.

**Mất:** `src/BE/Tests/` hoá thành thư mục trộn hai vùng.

- Dự án thêm tệp vào một thư mục Core, nên mỗi lần kéo bản Core mới dễ xung đột.
- Cổng tương lai của A17 phải liệt kê ngoại lệ theo tên project.

**Vì sao loại:** trộn vùng sở hữu, đúng thứ ADR-0100 vừa tách.

### Phương án B — Project module đặt phẳng: `src/BE/CoreAndSkill.Modules.<X>.<Tầng>/`

**Được:** đường dẫn ngắn; giống cách host đang nằm.

**Mất:**

- Không có *một thư mục = một module*. Gỡ một module là gỡ nhiều thư mục ngang hàng.
- Bảng vùng phải khai vùng dự án theo mẫu tên thay vì theo thư mục.

**Vì sao loại:** ranh giới module phải thấy được trên cây thư mục.

### Phương án C — Mỗi module mang ArchTests riêng

**Được:** module tự chủ; ArchTests của Core không phải biết gì về module.

**Mất:** module nằm trong vùng dự án, nên dự án sửa được ArchTests của chính module mình. Luật của Core áp lên module thành tự nguyện.

**Vì sao loại:** ArchTests nằm ở vùng Core chính là để thứ bị canh không sửa được thứ canh nó.

## Hệ quả

### Tích cực

- Ranh giới module thấy được trên cây thư mục; bảng vùng khai `src/BE/Modules/` bằng một dòng.
- Kéo bản Core mới không đụng thư mục test của module.
- Luật của Core áp lên module mà module không nới được.
- A3 hết phụ thuộc vào cách dự án đặt tên.

### Tiêu cực

- **ArchTests phải biết đường tới module qua host.** Một module có mặt trên đĩa mà host chưa đăng ký thì không được nạp. A6 bắt được project không khai trong solution, nhưng không bắt được project có trong solution mà host không tham chiếu.
- **Module giả là mã test phải bảo trì**, và nó phải đủ giống module thật để canary có nghĩa.
- **Project test của module dùng mã test chung của Core** ([`0070-ma-test-dung-chung-la-tep-nguon-noi-vao-hai-project.md`](0070-ma-test-dung-chung-la-tep-nguon-noi-vao-hai-project.md)) qua đường dẫn tương đối dài, từ vùng dự án trỏ vào vùng Core. Đường đó gãy nếu Core dời `src/BE/Tests/Shared/`.
- **Mỗi module mới thêm dòng `COPY` vào Dockerfile của host** — vùng dự án, không có gì canh.
- **Test tích hợp đỏ ngay khi có module đầu tiên** (P4, nợ **B19** ở [`../DEBT.md`](../DEBT.md)). `PostgresFixture` chỉ áp script dưới `database/scripts/core/`: `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/PostgresFixture.cs`, chuỗi `"database", "scripts", "core"`. Còn `SchemaVerifier` kiểm **mọi** `DbContext` đã đăng ký: `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/SchemaVerifier.cs`, chuỗi `IEnumerable<DbContext> contexts`. Module có `DbContext` thì host test từ chối khởi động. Hướng sửa (a): fixture áp thêm `database/scripts/modules/*/`, theo thứ tự ở [`../database/script-runbook.md`](../database/script-runbook.md) §3.3. Bản sửa phải về **trước** PR mang module đầu tiên.

### Rút lui nếu sai

Dời thư mục là thao tác cơ học: di chuyển thư mục, sửa đường dẫn trong `.slnx` và trong Dockerfile. Hôm nay chưa có module nào, nên chưa có gì để dời. Phần mở rộng ArchTests không phụ thuộc chỗ đặt, chỉ phụ thuộc một hằng số gốc quét.

### Dấu hiệu quyết định này bắt đầu sai

- Một project test của module xuất hiện dưới `src/BE/Tests/`.
- Một module tự thêm project ArchTests và bắt đầu *"miễn trừ"* luật Core ở đó.
- Canary module giả xanh trong khi một vi phạm thật ở module lọt qua.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Khoảng trống của tài liệu: chưa quy ước. Lỗ của A3 đo được bằng cách đọc cách nó nhận diện module. **Chưa có module thật để đo** |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án B đặt phẳng, đơn giản hơn. Không thấy được ranh giới module trên cây |
| 3 | Chi phí vận hành thêm | Một module giả trong mã test. Gốc quét thêm một thư mục |
| 4 | Ai bảo trì | ArchTests và module giả: `test-engineer`, trong vùng Core. Thư mục module và test của nó: dự án |
| 5 | Rút lui thế nào | Dời thư mục; hôm nay không có gì để dời |
| 6 | Có buộc Core biết nghiệp vụ không | Không. ArchTests biết **đường dẫn** `src/BE/Modules/`, không biết tên module nào |

## Thi công và kiểm

| # | Ai | Việc |
| --- | --- | --- |
| 1 | `test-engineer` | Mở rộng ArchTests ra module theo quyết định 4, kèm module giả làm canary cho từng luật được mở rộng. A3 bỏ nhận diện theo tiền tố tên |
| 2 | `test-engineer` | ArchTest A4 ba vế ([`0101-hop-dong-giua-module-o-contracts-cua-module-phat.md`](0101-hop-dong-giua-module-o-contracts-cua-module-phat.md)), dùng chung module giả |
| 3 | `test-engineer` | Nợ B19: `PostgresFixture` áp thêm `database/scripts/modules/*/`. Trước PR mang module đầu tiên |
| 4 | `backend-expert` | Không có việc mã ở repo Core hôm nay |

## Liên quan

- [`0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md`](0100-host-thuoc-vung-du-an-ba-vung-so-huu-tep.md) — ba vùng sở hữu
- [`0101-hop-dong-giua-module-o-contracts-cua-module-phat.md`](0101-hop-dong-giua-module-o-contracts-cua-module-phat.md) — `Modules.<X>.Contracts`
- [`0032-module-mau-o-du-an-ha-nguon.md`](0032-module-mau-o-du-an-ha-nguon.md) — module mẫu đầu tiên ở dự án hạ nguồn
- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §9.1 — layout thư mục test
