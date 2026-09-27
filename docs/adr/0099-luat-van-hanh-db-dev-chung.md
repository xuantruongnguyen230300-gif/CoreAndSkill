---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0099 — Database dev chung: script chỉ áp sau khi hợp nhất, do người vận hành DB dev áp tay; việc nền của nhiều máy trên cùng dữ liệu ghi thành nợ có điều kiện; công cụ SQL dùng tài khoản Postgres cá nhân

> **Trạng thái:** Đã chấp nhận (2026-09-25) · Bổ sung bởi [ADR-0104](0104-tai-khoan-ung-dung-cua-dev-tren-db-chung-tao-qua-man-quan-tri.md) (2026-09-25)

## Bối cảnh

Người dùng chốt ngày 2026-09-25 rằng cả nhóm dùng **một** database dev trên DB server chung ([`0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md`](0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md), quyết định 2). Câu hỏi 3 của [`0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md`](0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md) đã nêu ba việc nằm ngoài phạm vi của nó. Người dùng đồng ý với đề xuất viết một ADR riêng cho ba việc đó, và ADR-0097 đặt điều kiện: chưa có ADR này thì chưa commit giá trị mã hoá nào.

Tài liệu hôm nay mô tả **mỗi máy một database**:

- [`../database/script-runbook.md`](../database/script-runbook.md) §6 dựng Postgres trên máy dev.
- §5.1 của cùng tệp chỉ **cảnh báo** khi database đi trước code, và **từ chối khởi động** khi database đi sau code.

Kiến trúc sư đọc mã ngày 2026-09-25 để xem chuyện gì xảy ra khi nhiều tiến trình dev chạy trên cùng dữ liệu. [`0014-mot-instance-key-ring-postgres.md`](0014-mot-instance-key-ring-postgres.md) chốt *một instance* cho môi trường chạy thật. Trên DB dev chung, **mỗi máy dev là một instance**, và mã Core đã tự ghi rằng nó chỉ đúng với một instance:

| Thành phần | Neo | Chuyện xảy ra trên DB chung |
| --- | --- | --- |
| Khôi phục việc dở dang lúc khởi động | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Jobs/JobRecoveryHostedService.cs`, chú thích `KHÁC sẽ bị đánh dấu oan` | Mỗi lần **một** máy khởi động, mọi việc `running` của **mọi** máy khác bị đánh dấu hỏng, và người khởi tạo nhận thông báo hỏng. Việc `queued` mà dòng outbox đã `done` — tức đã nằm trong hàng đợi bộ nhớ của máy khác — cũng bị đánh dấu hỏng |
| Bộ phát outbox | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Outbox/OutboxDispatcher.cs`, chuỗi `FOR UPDATE SKIP LOCKED` | Không phát trùng. Nhưng sự kiện do máy A ghi có thể được máy B phát, bằng mã trên **nhánh của B** |
| Việc nền theo yêu cầu | Hàng đợi trong bộ nhớ tiến trình, đẩy vào qua bộ phát outbox | Việc do người dùng ở máy A yêu cầu có thể chạy trên máy B. Tệp kết quả nằm trên đĩa của B |
| Bảo trì kho tệp | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Files/FileMaintenanceHostedService.cs`, chú thích `bản ghi KHÔNG có tệp ⇒ BÁO CÁO` | `Core:File:RootPath` là thư mục của từng máy, còn bản ghi tệp nằm trong DB chung. Mỗi máy báo mọi tệp của máy khác là *"bản ghi không có tệp"*. Tệp máy A tải lên thì máy B không mở được |

Chỗ không hỏng: bộ khoá bảo vệ dữ liệu nằm trong PostgreSQL (ADR-0014 quyết định 2), nên cookie phiên của máy nào cũng giải được ở máy khác.

## Quyết định

Người dùng chốt: một database chung, và ba việc dưới đây phải có luật. Kiến trúc sư chốt nội dung từng luật.

### 1. Script schema chỉ áp lên DB dev chung sau khi PR mang nó đã hợp nhất

1. **Chỉ script đã nằm trên nhánh chính** mới được áp lên DB dev chung. Nhánh chưa hợp nhất mà mang script mới thì thử trên **database cục bộ**, đè chuỗi bằng `user-secrets`.
2. **Người áp là người vận hành DB dev chung.** Đó là người giữ mật khẩu `coreandskill_owner` của DB dev, cộng một người dự phòng. Tên người do nhóm chốt, không ghi vào `docs/` của Core, vì `docs/` đi theo Core sang dự án khác.
3. **Người hợp nhất một PR mang script** báo cho người vận hành **ngay khi hợp nhất**. Người vận hành áp theo [`../database/script-runbook.md`](../database/script-runbook.md) §3.4: từng tệp một, đọc kết quả từng tệp, rồi chạy lại khối cấp quyền.
4. **Không phải CI.** Runner của GitHub Actions không với tới DB dev: điều kiện của ADR-0098 là DB chỉ nằm trong mạng nội bộ. Muốn CI áp script thì cần một runner tự dựng trong mạng nội bộ **và** mật khẩu `coreandskill_owner` trong kho bí mật của CI. Cả hai là hạ tầng mới.
5. **Bước thu hẹp** — xoá cột, xoá bảng, đổi tên — được báo trước cho cả nhóm, vì nó làm hỏng mọi nhánh chưa kéo code mới về ([`../database/migration-policy.md`](../database/migration-policy.md) §5).

### 2. Việc nền của nhiều máy trên cùng dữ liệu — ghi nợ, chưa sửa

Kiến trúc sư **ghi nợ B20** ([`../DEBT.md`](../DEBT.md)) thay vì sửa ngay. Bốn hiện tượng ở bảng *Bối cảnh* là cái giá được chấp nhận ở giai đoạn này, vì:

- DB dev không mang dữ liệu thật (ADR-0098, quyết định 3.4). Việc bị đánh dấu hỏng oan thì chạy lại được.
- Sửa đúng cách cần một công tắc cấu hình mới trong Core. Đó là một thay đổi chạm Core, cho một nhu cầu chỉ có ở môi trường dev. Chưa ai đo nó gây mất bao nhiêu thời gian.

**Lối tránh không cần mã:** ai đang gỡ lỗi việc nền, outbox hay tệp đính kèm thì chạy trên database cục bộ, như luật 1.1.

**Điều kiện kích hoạt**, đạt một trong hai thì phải có ADR mới:

- (a) Có người báo mất thời gian vì một việc bị máy khác đánh dấu hỏng, hoặc chạy trên máy khác.
- (b) Module đầu tiên đưa vào một việc nền mà người dùng phải chờ kết quả trong lúc phát triển.

Hướng sửa khi kích hoạt: một khoá cấu hình tắt ba dịch vụ nền — bộ phát outbox, khôi phục việc dở dang, bảo trì kho tệp — trên máy dev. Chỉ một máy chạy chúng, hoặc không máy nào. Hướng này **chưa được chốt**. Khoá cấu hình vào Core cần ADR riêng.

### 3. Người mở DB dev chung bằng công cụ SQL dùng tài khoản Postgres cá nhân

1. Mỗi người cần mở DB bằng công cụ SQL có **một tài khoản Postgres riêng**, do người vận hành DB dev tạo.
2. **Quyền không rộng hơn `coreandskill_app`** ([`../database/script-runbook.md`](../database/script-runbook.md) §3.6): đọc và ghi dữ liệu, **không** DDL. Đổi schema từ công cụ SQL là đi vòng qua bảng lịch sử script, và DB lệch khỏi repo mà không bảng nào ghi lại.
3. Người rời nhóm thì xoá tài khoản của người đó.
4. **Ghi thẳng giới hạn:** mật khẩu `coreandskill_app` giải được từ git (ADR-0098). Luật *"không dùng tài khoản ứng dụng trong công cụ SQL"* là một quy ước, không phải hàng rào. Cái nó mua được là **truy vết** — thao tác từ công cụ SQL mang tên người — và **thu hồi riêng lẻ** với những ai không đọc được repo.
5. Không có lệnh giải mã. Quyết định 7 của ADR-0097 còn nguyên.

### 4. Điều kiện commit giá trị mã hoá đầu tiên

ADR này thoả điều kiện *"có ADR vận hành DB chung"* của ADR-0097. Điều kiện còn lại là cổng S23 phải xanh **trước**, cùng T13 của ADR-0098.

### 5. Luật

| Mã | Luật | Chỗ |
| --- | --- | --- |
| **E21** | Script chỉ áp lên DB dev chung sau khi hợp nhất, do người vận hành DB dev | [`../DEBT.md`](../DEBT.md) — không ép được bằng máy |
| **B20** | Trên DB dev chung, việc nền của một máy không xử lý hay đánh dấu việc của máy khác | [`../DEBT.md`](../DEBT.md) — nợ có điều kiện kích hoạt |
| **S26** | Công cụ SQL trên DB dev chung dùng tài khoản Postgres cá nhân, quyền không rộng hơn `coreandskill_app` | [`../DEBT.md`](../DEBT.md) — không ép được bằng máy |

## Phương án đã cân nhắc và vì sao loại

### Phương án A — CI áp script lên DB dev chung sau khi hợp nhất

**Được:** không phụ thuộc người nhớ. Cửa sổ giữa lúc hợp nhất và lúc áp gần bằng không.

**Mất:** cần runner tự dựng trong mạng nội bộ, và mật khẩu `coreandskill_owner` trong kho bí mật CI. Đó là hai thứ phải cài, phải giữ, và phải khôi phục khi hỏng.

**Vì sao loại:** chi phí vận hành trả mãi mãi, cho một cửa sổ mà một người đọc thông báo là đóng được. Lật khi nhóm đã có runner tự dựng vì lý do khác.

### Phương án B — Ai cũng áp được script lên DB chung, kể cả từ nhánh

**Được:** không chờ ai; dev thử migration trên đúng DB mọi người dùng.

**Mất:** script từ một nhánh chưa hợp nhất làm DB đi trước code của mọi người khác. [`../database/script-runbook.md`](../database/script-runbook.md) §5.1 chỉ cảnh báo ca đó, không chặn. Một bước thu hẹp từ nhánh của một người làm hỏng mọi người khác **lúc chạy**, không phải lúc khởi động.

**Vì sao loại:** hỏng lan sang người không liên quan, và hỏng lúc chạy.

### Phương án C — Sửa ngay việc nền chéo máy bằng công tắc cấu hình

**Được:** hết bốn hiện tượng ở *Bối cảnh*.

**Mất:** thêm một khoá cấu hình vào Core cho một nhu cầu chỉ có ở dev, khi chưa đo thiệt hại. Mỗi khoá cấu hình của Core đi theo sang mọi dự án.

**Vì sao loại lúc này:** chưa có số đo. Ghi nợ kèm điều kiện kích hoạt, và kèm lối tránh không cần mã.

### Phương án D — Công cụ SQL dùng chung tài khoản `coreandskill_app`

**Được:** không phải tạo tài khoản nào. Mật khẩu vốn đã giải được từ git.

**Mất:** mọi thao tác từ công cụ SQL mang cùng một tên với tiến trình ứng dụng. Nhật ký của Postgres không phân biệt được người với ứng dụng.

**Vì sao loại:** mất truy vết. Và người dùng đã chốt tài khoản cá nhân.

## Hệ quả

### Tích cực

- DB dev chung không bao giờ đi trước nhánh chính. Mọi nhánh đã kéo code mới về đều khởi động được.
- Chỉ hai người giữ mật khẩu `coreandskill_owner` của DB dev.
- Thao tác bằng công cụ SQL truy được về người.
- Rủi ro chéo máy của việc nền được gọi tên, có lối tránh, có điều kiện kích hoạt — thay vì lộ ra dưới dạng *"việc của tôi tự nhiên hỏng"*.

### Tiêu cực

- **Cửa sổ sau khi hợp nhất.** Từ lúc hợp nhất một PR mang script tới lúc người vận hành áp xong, mọi người kéo nhánh chính về đều **không khởi động được** với DB chung: [`../database/script-runbook.md`](../database/script-runbook.md) §5.1 từ chối khởi động khi DB thiếu migration. Cửa sổ đó phụ thuộc một người.
- **DB chung không thay được DB cục bộ.** Nhánh mang script mới, và việc gỡ lỗi việc nền, vẫn cần database cục bộ. Mỗi dev vẫn phải dựng được một Postgres, dù ít dùng hơn.
- **Bước thu hẹp làm hỏng nhánh chưa kéo code mới**, dù đã báo trước.
- **Bốn hiện tượng chéo máy vẫn còn**, cho tới khi nợ B20 được trả.
- **Tệp đính kèm theo máy.** Tệp máy A tải lên thì máy B không mở được. Điều này nằm trong B20.
- **Luật tài khoản cá nhân không phải hàng rào.** Mật khẩu ứng dụng giải được từ git.

### Rút lui nếu sai

1. Quay về mỗi máy một database: mỗi dev đè `ConnectionStrings:Core` bằng `user-secrets`, theo [`../database/script-runbook.md`](../database/script-runbook.md) §6.
2. Gỡ bản mã khỏi `appsettings.Development.json`, rồi đổi mật khẩu `coreandskill_app` của DB chung, theo mục *Rút lui* của ADR-0098.
3. Viết ADR mới đánh dấu E21, B20, S26 hết hiệu lực.

Không dữ liệu nào của ứng dụng phụ thuộc quyết định này. DB chung chỉ mang dữ liệu dev.

### Dấu hiệu quyết định này bắt đầu sai

- Cửa sổ sau khi hợp nhất thường kéo dài quá một buổi làm việc.
- Một bảng lịch sử script của DB chung mang tên một script không có trên nhánh chính — lệnh kiểm (2) ở [`../database/script-runbook.md`](../database/script-runbook.md) §3.5 ra dòng.
- Điều kiện (a) hay (b) của nợ B20 đạt mà chưa ai viết ADR.
- Có người xin quyền DDL cho tài khoản cá nhân.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Ba rủi ro suy ra từ mã và từ tài liệu đã có, neo ở bảng *Bối cảnh*. **Chưa đo** thiệt hại thật, vì DB chung chưa được dùng ngày nào |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Đơn giản nhất là không luật nào: ai cũng áp script, ai cũng dùng tài khoản ứng dụng. Nó thiếu ở chỗ hỏng lan sang người không liên quan (phương án B) và không truy vết được (phương án D) |
| 3 | Chi phí vận hành thêm | Một người vận hành và một người dự phòng. Áp script sau mỗi lần hợp nhất có script. Tạo và xoá tài khoản cá nhân. Báo trước mỗi bước thu hẹp. Trả mãi mãi, chừng nào còn DB chung |
| 4 | Ai bảo trì | Người vận hành DB dev chung của nhóm. Không có mã nào để bảo trì cho tới khi nợ B20 kích hoạt |
| 5 | Rút lui thế nào | Ba bước ở mục *Rút lui nếu sai* |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Không dòng mã Core nào đổi |

## Liên quan

- [`0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md`](0097-mat-khau-db-dev-chung-vao-git-ma-hoa-aes-gcm-khoa-ngoai-repo.md) — câu hỏi 3, được ADR này bổ sung
- [`0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md`](0098-giai-ma-o-buoc-dung-options-mot-db-dev-chung-khoa-dev-trong-git.md) — quyết định một database chung, bốn điều kiện của DB dev
- [`0014-mot-instance-key-ring-postgres.md`](0014-mot-instance-key-ring-postgres.md) — giả định một instance; ADR này bổ sung ngoại lệ của môi trường dev chung
- [`../database/script-runbook.md`](../database/script-runbook.md) §3.4, §3.6, §5.1, §6
- [`../DEBT.md`](../DEBT.md) — E21, B20, S26
