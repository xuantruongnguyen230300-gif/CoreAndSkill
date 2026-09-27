---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0044 — `database/scripts/core/` vào khối `core-paths`; lỗ còn lại ở nhánh Edit/Write của `on-edit.sh` ghi thành nợ thay vì im lặng

> **Trạng thái:** Đã chấp nhận (2026-09-20)

## Bối cảnh

Khối `core-paths` ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §10 là nguồn duy nhất trả lời câu hỏi *"thay đổi này có chạm Core không"*. Hook `PostToolUse` đọc khối đó rồi ghi dấu; hook `Stop` đọc dấu rồi đòi một lượt `core-reviewer`.

Đợt review BE ngày 2026-09-20 nêu một đường dẫn nằm ngoài khối. Ba phép đo chạy lại trên repo này cùng ngày:

| Đo gì | Kết quả |
| --- | --- |
| ArchTest nào đọc thư mục script | `src/BE/Tests/CoreAndSkill.ArchTests/PermissionScriptParityTests.cs:175-177` dựng đường dẫn bằng bốn bậc `..` từ thư mục tệp test, tức `database/scripts/core` tính từ gốc repo |
| Khối `core-paths` có đường dẫn đó không | Không. Khối liệt kê ba nhóm: `src/BE/`, ba tầng FE kèm hai tệp ESLint, và bốn mục `docs/` |
| `database/` có bị `.gitignore` loại không | Không — `git check-ignore` thoát mã 1. Nó chỉ chưa vào git, cùng tình trạng với `src/` |

Nghĩa là: sửa `database/scripts/core/0002__core__seed-permission-catalog.sql` một mình là đổi **tập khoá quyền của Core** — thứ mà §4.1 của tài liệu chủ xếp vào Core không cần bàn — nhưng không dấu nào được ghi, nên không lượt nào bị đòi review.

**Phép đo thứ tư mới là phép đo quyết định cách sửa.** Lượt review khuyến cáo kiểm xem hook có dùng được một đường dẫn ngoài `src/` không. Kiểm rồi: hook có **hai** nhánh, và chúng hỏng khác nhau.

| Nhánh của `on-edit.sh` | Xử lý được `database/scripts/core/` không |
| --- | --- |
| Lệnh shell | **Được.** Nhánh này dựng tập thư mục quét từ chính khối `core-paths`, lấy mọi mục không bắt đầu bằng `src/` và có thật trên đĩa. Thêm đường dẫn vào khối là nhánh này quét ngay |
| Edit / Write / NotebookEdit | **Không.** Nhánh này lọc qua một phép thử khu vực trước, và khu vực chỉ gồm `docs/`, `.claude/`, `spec/`, `src/`. Một tệp dưới `database/` bị loại **trước khi** khối `core-paths` được đọc tới |

Phép thử khu vực đó chạy độc lập cho thấy đúng điều trên: `database/scripts/core/0002__core__seed-permission-catalog.sql` bị loại, trong khi `src/BE/Core/x.cs` và `docs/RULES.md` đi qua.

Vậy khai đường dẫn vào khối là **cần nhưng chưa đủ**. Nửa còn lại nằm ở `.claude/hooks/on-edit.sh` — ngoài phạm vi ghi của `architect`, và theo [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §8 thì sửa tệp đó buộc phải chạy tay bộ test hook.

## Quyết định

1. **`database/scripts/core/` vào khối `core-paths`**, thành một nhóm riêng, kèm một hàng trong bảng *Nguồn của từng nhóm* trỏ về [`../database/script-runbook.md`](../database/script-runbook.md) §1–§2.
2. **Lỗ ở nhánh Edit/Write ghi thẳng vào §10 của tài liệu chủ, ngay dưới khối**, nêu rõ nhánh nào canh được và nhánh nào chưa. Không dán nhãn "đã canh" cho một đường mà phép đo nói là chưa.
3. **Việc sửa phép thử khu vực trong `on-edit.sh` bàn giao ra ngoài lượt này**, kèm điều kiện nghiệm thu: một payload Edit nhắm vào một tệp dưới `database/scripts/core/` phải sinh dấu chạm Core.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Không thêm, ghi ở §10 vì sao cố ý không thêm

Đây là cánh cửa thứ hai mà lượt review để ngỏ.

**Được:** không đụng gì, không tạo khoảng cách giữa điều khai và điều hook làm được.

**Vì sao loại:** để viết được đoạn *"cố ý không thêm"* thì phải lập luận rằng danh mục khoá quyền của Core không thuộc Core. Lập luận đó mâu thuẫn với §4.1 của chính tài liệu chủ (phân quyền là Core), với việc Core sở hữu migration schema `core`, và với sự tồn tại của một ArchTest Core nhắm thẳng vào thư mục đó. Loại vì tiền đề sai, không vì chi phí.

### Phương án B — Hoãn khai đường dẫn cho tới khi `on-edit.sh` được sửa

**Được:** khối `core-paths` không bao giờ khai một thứ mà hook chưa dùng trọn.

**Vì sao loại:** hai lý do đo được. Thứ nhất, nó vứt luôn phần **đang chạy được**: nhánh lệnh shell dùng được đường dẫn này ngay hôm nay, và sửa tệp bằng lệnh shell là đường có thật. Thứ hai, và nặng hơn: hoãn biến một lỗ **nhìn thấy được** thành một lỗ **không ai nhớ**. Khối `core-paths` là định nghĩa *cái gì thuộc Core*, không phải bản kê *cái gì hook bắt được*; treo định nghĩa theo năng lực hiện thời của một tệp script là đảo ngược quan hệ giữa hai thứ đó.

### Phương án C — Thêm đường dẫn và sửa luôn `on-edit.sh` trong cùng lượt

**Được:** đóng trọn lỗ, không để lại nợ nào.

**Vì sao loại:** `.claude/hooks/` nằm ngoài phạm vi ghi của vai trò ra quyết định này — chính sự tách giữa người quyết và người thi công là lý do vai trò tồn tại. Thêm nữa, một thay đổi ở phép thử khu vực mở rộng tầm ghi dấu ra một thư mục mới và phải chạy qua bộ test hook trước khi tin được; gộp nó vào một lượt viết tài liệu là cách một thay đổi hành vi đi qua mà không ai chạy cổng của nó.

## Hệ quả

### Tích cực

- Sửa script Core **bằng lệnh shell** từ nay ghi dấu chạm Core, và lượt đó bị đòi `core-reviewer`.
- Câu trả lời cho *"cái gì thuộc Core"* hết lệch với thứ một ArchTest Core đang canh.
- Lỗ nhánh Edit/Write trở thành một dòng có người đọc được, kèm điều kiện nghiệm thu, thay vì một suy luận phải đọc ba tệp mới thấy.

### Tiêu cực

- **Lỗ vẫn mở, và nó nằm ở đúng đường hay dùng nhất.** Sửa một tệp `.sql` bằng công cụ Edit vẫn không sinh dấu nào cho tới khi `on-edit.sh` được sửa. Từ hôm nay tới lúc đó, khối `core-paths` khai rộng hơn thứ hook thi hành được — khoảng cách này là cái giá thật của việc tách quyết định khỏi thi công, và nó chỉ chấp nhận được vì đã viết ra.
- Nhánh lệnh shell có thêm một gốc để `find` đi qua mỗi lần một lệnh shell chạy. Không đáng kể hôm nay, nhưng nó tăng theo số mục không thuộc `src/` trong khối.
- `database/` chưa vào git. Khi `src/` được commit mà `database/` thì không, job cổng BE vẫn bật theo điều kiện `src/BE` và ArchTest sẽ đỏ với thông điệp nói *script đã dời chỗ* — đỏ đúng, chẩn đoán sai. ADR này **không** sửa điều đó; xem mục Liên quan.

## Liên quan

- Điều kiện `detect` của [`../../.github/workflows/docs-gate.yml`](../../.github/workflows/docs-gate.yml) chỉ nhìn `src/BE` và `src/FE`. Câu hỏi *cổng BE có nên đòi `database/` có mặt không* là một quyết định riêng, chưa chốt ở đây.
- [`0008-core-so-huu-migration.md`](0008-core-so-huu-migration.md) — Core sở hữu migration schema `core`, tiền đề cho việc script Core thuộc Core.
- [`0030-dieu-kien-chuyen-giai-doan-2.md`](0030-dieu-kien-chuyen-giai-doan-2.md) và [`0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md`](0035-hoan-dieu-kien-3-den-pr-cham-core-dau-tien.md) — lớp chặn `core-reviewer` chưa từng chạy bằng payload thật; lỗ nêu ở đây là một lý do nữa để phép thử đó đừng hoãn thêm.
