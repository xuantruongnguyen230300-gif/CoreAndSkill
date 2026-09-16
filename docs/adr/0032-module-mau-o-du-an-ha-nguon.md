---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0032 — Module mẫu là module nghiệp vụ thật đầu tiên của dự án hạ nguồn đầu tiên, không nằm trong repo Core

> **Trạng thái:** Đã chấp nhận (2026-09-14)

## Bối cảnh

Nhóm skill sinh code bị hoãn vì **cần một bản gốc đã chạy được để sao chép** ([`../../.claude/README.md`](../../.claude/README.md) §6, dòng 236–242 lúc ghi; [`0012-giai-doan-1-chi-docs.md`](0012-giai-doan-1-chi-docs.md), mục *Tiêu cực*). Cả hai chỗ đều viết *"sau khi module mẫu đầu tiên chạy được"* — nhưng không chỗ nào nói module đó **nằm ở đâu**, **ai dựng**, và **thế nào là xong**.

Hai lộ trình không trả lời thay được:

| Tài liệu (lúc ghi ADR) | Nói gì |
| --- | --- |
| [`../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/be/trien-khai/00-lo-trinh-tong-the.md) §1 | Năm pha B0–B4, cả năm chỉ phủ Core |
| [`../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md`](../wiki-core/fe/trien-khai/00-lo-trinh-tong-the.md) §4 (dòng 86) | *"`modules/` chưa có thư mục nào ở cuối F3, và đó là đúng"* |

Không pha nào sinh ra một module. Trong khi đó, hợp đồng lắp ghép một module ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §7) và cơ chế lưu dữ liệu module ([`0025-luu-du-lieu-module-mot-transaction.md`](0025-luu-du-lieu-module-mot-transaction.md)) là mô tả mà chưa ai lắp thử.

Ràng buộc có thật: Core phân phối bằng clone, và dự án không sửa tệp thuộc Core ([`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md), luật 1); một thứ chỉ lên Core khi từ hai module trở lên cần nó ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2).

## Quyết định

> **Module mẫu là module nghiệp vụ thật đầu tiên của dự án hạ nguồn đầu tiên. Repo Core không chứa module mẫu nào.**

1. Hai lộ trình thêm một mục **"Sau B4 / sau F3 — module mẫu ở dự án hạ nguồn đầu tiên"**, kèm định nghĩa hoàn thành riêng. Nội dung định nghĩa nằm ở hai file lộ trình, không ở đây.
2. **Skill scaffold viết ở dự án đó**, đối chiếu với module đang chạy thật, rồi **đưa về repo Core** theo đường *nâng thay đổi lên Core rồi kéo về* của ADR-0016. Phần mang nghiệp vụ của dự án đó được gỡ trước khi đưa về.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Module mẫu nằm trong repo Core

**Được:** skill viết được ngay trong repo Core; hợp đồng lắp ghép được kiểm trong Core từ sớm; các cổng về module (F2, F4, R2) có đầu vào thật. Đây là phương án một người tỉnh táo sẽ chọn, và nó mạnh ở đúng chỗ phương án đã chọn yếu.

**Vì sao loại — ba lý do, mỗi cái đủ đứng một mình:**

1. **Clone mang module mẫu vào mọi dự án.** Dự án xoá nó là xoá tệp của Core — xung đột ở mỗi lần kéo bản vá (ADR-0016 luật 1 và 3). Không xoá thì mỗi dự án mang một module rác, và Core mất đúng tính chất *"mang đi được"* mà nó tồn tại vì.
2. **Một module không có nghiệp vụ không có nhu cầu thật.** Không luật nghiệp vụ, không quan hệ sang module khác, không quyền nào người dùng thật cần. Skill sao chép từ nó sẽ sao chép đúng phần dễ và để trống đúng phần khó.
3. **Core chứa một module là Core biết một module** — dạng vi phạm R1 dễ trượt nhất, vì nó không cần ProjectReference nào.

### Phương án B — Module mẫu ở một repo phụ riêng, bám tag của Core

**Được:** không vào bản clone; kiểm được hợp đồng lắp ghép ngoài dự án thật.

**Vì sao loại:** thêm một repo phải đồng bộ tag Core và giữ cho chạy được — chi phí vận hành cho một thứ không ai dùng thật. Và nó vẫn là module không nghiệp vụ, nên mang nguyên lý do 2 của phương án A.

### Phương án C — Viết skill theo tài liệu, không cần module mẫu

**Vì sao loại:** [`0012-giai-doan-1-chi-docs.md`](0012-giai-doan-1-chi-docs.md) đã loại — skill sinh ra code không build được, và không có đáp án đúng nào để đối chiếu khi đi sửa.

### Phương án D — Dùng một tính năng Core (người dùng, vai trò) làm bản gốc

**Vì sao loại:** tính năng Core không có đúng những phần khiến một module là module: schema riêng, `DbContext` và chuỗi migration riêng, điểm đăng ký `AddXModule()`, nguồn danh mục khoá quyền riêng ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §7). Skill sao chép từ đó thiếu đúng các phần ấy.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Repo Core không có module nào chạy được, kể cả sau B4 và F3** | Các cổng về module trong repo Core không có đầu vào thật — vùng ranh giới module ở F0 đã ghi *"danh sách module rỗng ⇒ vùng này chưa có gì để ép"* ([`../wiki-core/fe/trien-khai/01-f0-nen-mong.md`](../wiki-core/fe/trien-khai/01-f0-nen-mong.md) §3.1). Chúng chỉ được chứng minh ở repo hạ nguồn |
| **Hợp đồng lắp ghép và ADR-0025 được kiểm lần đầu ở dự án hạ nguồn** | Seam Core sai thì phát hiện muộn, và sửa phải đi lên repo Core rồi kéo về — vòng sửa dài hơn hẳn sửa tại chỗ |
| **Kiểm điều phối nhiều `DbContext` trong repo Core phải dựng context riêng trong project test** | Một thứ phải viết và giữ chỉ để thay cho module chưa tồn tại |
| **Dự án hạ nguồn đầu tiên gánh thêm việc** | Đạt định nghĩa hoàn thành của module mẫu, viết skill, gỡ nghiệp vụ rồi đưa về Core. Các dự án sau hưởng; lịch của dự án đầu bị Core kéo |
| **Skill đưa về Core có nguy cơ mang từ vựng nghiệp vụ** | Cùng khuôn rò mà [`0019-ba-component-nang-thuoc-core.md`](0019-ba-component-nang-thuoc-core.md) ghi cho component. Lúc chốt, không cổng nào quét `.claude/skills/` tìm từ vựng nghiệp vụ |
| **Thời điểm có skill scaffold nằm ngoài lịch của repo Core** | Nó phụ thuộc lúc có dự án hạ nguồn, không phụ thuộc tiến độ B0–B4, F0–F3 |

### Tích cực

- Bản clone của Core sạch — không module nào đi theo.
- Skill sao chép từ một module có nghiệp vụ thật, đã chạy thật.
- Seam lắp ghép của Core được chứng minh bằng nhu cầu thật — đúng tinh thần ngưỡng hai module.

### Điều kiện lật quyết định

1. **B4 và F3 đóng mà chưa có dự án hạ nguồn nào khởi động.** Core không còn đường nào kiểm hợp đồng lắp ghép; xét lại phương án A hoặc B.
2. **Dự án hạ nguồn đầu tiên phải sửa tệp thuộc Core để lắp được module đầu tiên.** Seam Core sai ở mức mà một module thử trong Core lẽ ra đã bắt được sớm.
3. **Skill đưa về Core bị phát hiện mang từ vựng nghiệp vụ.** Quy trình gỡ không đủ; cần một cổng, hoặc một phương án khác.

## Liên quan

- [`0012-giai-doan-1-chi-docs.md`](0012-giai-doan-1-chi-docs.md) — vì sao skill sinh code cần bản gốc
- [`0016-phan-phoi-core-bang-clone.md`](0016-phan-phoi-core-bang-clone.md) — clone, tag, và luật không sửa `Core/`
- [`0025-luu-du-lieu-module-mot-transaction.md`](0025-luu-du-lieu-module-mot-transaction.md) — seam lưu dữ liệu module chờ được lắp thử
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.2, §7 — ngưỡng hai module, hợp đồng lắp ghép
- [`../../.claude/README.md`](../../.claude/README.md) §6 — skill cố ý hoãn
