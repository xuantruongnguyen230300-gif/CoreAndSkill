---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0008 — Core sở hữu migration của schema `core`

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm, **toàn bộ** migration nằm trong project host, kể cả migration của các bảng thuộc Core. Đây không phải tình cờ: họ có test kiến trúc **ép giữ nguyên** trạng thái đó — một test bắt buộc host sở hữu migration và ảnh chụp model, một test khác cấm project Core chứa file nguồn migration.

Lý do của họ hợp lý: migration phụ thuộc provider cụ thể, mà Core thì không nên biết chạy trên hệ quản trị dữ liệu nào.

Nhưng hệ quả cũng thật: **dự án mới không thừa kế được migration cho bảng của Core** — phải tự sinh lại từ đầu. Họ đã bù bằng cách để Core vẫn ship một tệp SQL nền, và có test giữ tệp đó tồn tại. Cách bù này giữ được hình dạng bảng ban đầu, nhưng không giữ được **lịch sử thay đổi**: khi Core lên phiên bản mới và bảng của nó đổi, dự án đã dựng từ phiên bản cũ không có đường nào để đi lên ngoài việc so tay hai tệp SQL.

Với một dự án đơn lẻ, chi phí đó chấp nhận được. Với một **bộ khung nhiều dự án dùng chung** — đúng mục tiêu của CoreAndSkill — nó là chi phí nhân với số dự án, lặp lại ở mọi lần nâng cấp Core.

## Quyết định

**Đảo ngược quyết định của dự án tiền nhiệm.**

- `Core.Infrastructure` sở hữu migration của schema `core`.
- Mỗi module sở hữu migration của schema riêng nó.
- Host **không** chứa migration nào. Luật A7 ở [`../RULES.md`](../RULES.md) vốn đã cấm host chứa logic; điều này chỉ làm nó nhất quán.
- Luật E6 thay cho luật cũ: mỗi migration nằm trong project sở hữu schema của nó.
- Cấm khoá ngoại vật lý xuyên schema (luật E5) — nếu không, migration của một schema sẽ phụ thuộc thứ tự áp của schema kia, và quyền sở hữu tách bạch trở thành vô nghĩa.

Chi tiết ở [`../database/migration-policy.md`](../database/migration-policy.md).

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ nguyên: host sở hữu tất cả migration

**Được:** Core hoàn toàn không biết provider. Chỉ có một chuỗi migration duy nhất nên thứ tự áp rõ ràng, và không có khả năng hai chuỗi va nhau.

**Vì sao loại:** dự án thứ hai không thừa kế được gì. Nó phải sinh lại migration cho bảng Core từ model — và bản sinh lại đó chỉ đúng cho **thời điểm** dựng dự án. Từ đó về sau, hai dự án có hai lịch sử migration khác nhau cho cùng một tập bảng, và mọi nâng cấp Core phải được dịch bằng tay sang từng lịch sử. Đây đúng là dạng nhân bản mà [`0002-core-5-project.md`](0002-core-5-project.md) đang loại bỏ ở tầng code, chỉ khác là nó nằm ở tầng dữ liệu.

### Phương án B — Core chỉ ship tệp SQL nền, không ship migration

Đây là cách bù mà dự án tiền nhiệm đã dùng.

**Được:** Core vẫn không biết provider ở mức mã nguồn C#; dự án mới có một điểm khởi đầu.

**Vì sao loại:** giải quyết được ngày đầu, không giải quyết được ngày thứ hai. Tệp SQL nền cho biết bảng **ban đầu** trông thế nào; nó không nói cách đi từ phiên bản Core cũ lên phiên bản mới. Mà nâng cấp Core mới chính là việc bộ khung này tồn tại để làm cho dễ.

### Phương án C — Core sinh migration lúc chạy trong từng dự án

**Được:** giữ Core không ràng provider, đồng thời dự án nào cũng có chuỗi migration đầy đủ của mình.

**Vì sao loại:** bản sinh ra phụ thuộc phiên bản công cụ và thứ tự cấu hình model của từng dự án, nên hai dự án dựng từ cùng một phiên bản Core vẫn có thể ra hai kết quả khác nhau. Không tái lập được là không kiểm chứng được, và một thứ không kiểm chứng được thì không đặt vào đường áp schema production.

## Hệ quả

### Tích cực

- **Dự án mới thừa kế nguyên lịch sử schema của Core.** Nâng cấp Core là áp thêm migration mới của Core, không phải so tay hai tệp SQL.
- **Quyền sở hữu khớp với ranh giới module:** ai sở hữu bảng thì sở hữu luôn migration của bảng đó — cùng nguyên tắc đã áp cho code.
- Kiểm được bằng máy qua luật E6, thay cho hai test cũ vốn ép điều ngược lại.

### Tiêu cực — cái giá thật

- **`Core.Infrastructure` nay biết provider là PostgreSQL.** Đây là một phụ thuộc thật, chấp nhận có ý thức vì PostgreSQL đã chốt cho mọi dự án dựng trên bộ khung này. Nếu một ngày cần hệ quản trị dữ liệu khác, phần migration của Core phải viết lại — và đó là công việc nặng, không phải đổi cấu hình.
- **Nhiều chuỗi migration cùng tồn tại**, mỗi schema một chuỗi. Thứ tự áp giữa các chuỗi trở thành thứ phải khai rõ trong runbook, và người vận hành phải theo đúng thứ tự đó. Đây là chi phí trực tiếp của việc tách quyền sở hữu.
- **Cấm khoá ngoại xuyên schema là ràng buộc kéo theo, không phải lựa chọn.** Toàn vẹn tham chiếu giữa dữ liệu Core và dữ liệu module từ nay do ứng dụng giữ, không do cơ sở dữ liệu giữ — nghĩa là mất một hàng rào an toàn thật, đổi lấy tính độc lập khi triển khai.
- **Một migration của Core viết cẩu thả sẽ lan ra mọi dự án.** Trước đây một migration hỏng chỉ ảnh hưởng một dự án; nay nó là tài sản dùng chung, nên phải soát kỹ hơn.

## Liên quan

- [`../database/migration-policy.md`](../database/migration-policy.md) — ai sở hữu gì, quy ước đặt tên
- [`../database/script-runbook.md`](../database/script-runbook.md) — thứ tự áp giữa các chuỗi
- [`../RULES.md`](../RULES.md) — luật E5, E6, E8
- [`0009-ap-schema-chay-tay.md`](0009-ap-schema-chay-tay.md) — cách đưa migration vào DB thật
