---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0103 — Giữa hai module: ĐỌC đồng bộ qua interface trong `.Contracts` của module cung cấp; GHI chỉ qua sự kiện outbox

> **Trạng thái:** Đã chấp nhận (2026-09-25)

## Bối cảnh

[`0101-hop-dong-giua-module-o-contracts-cua-module-phat.md`](0101-hop-dong-giua-module-o-contracts-cua-module-phat.md) quyết định 3 cho phép một module gọi đồng bộ sang module khác qua một interface khai trong `Modules.<X>.Contracts`, và gọi đó là *"đường phụ, không phải đường chính"*. Nó không nói đường phụ được làm gì. Kiến trúc sư nêu khoảng trống đó thành câu hỏi cho người dùng.

Khoảng trống có hậu quả cụ thể vì [`0025-luu-du-lieu-module-mot-transaction.md`](0025-luu-du-lieu-module-mot-transaction.md): `IUnitOfWork` điều phối **mọi** `DbContext` trên một kết nối và một giao dịch. Một handler của module A gọi một interface mà module B hiện thực để **ghi** thì lần ghi đó chạy chung giao dịch với A. Nó chạy được, commit trọn vẹn, test xanh — nên không gì ngăn nó trở thành thói quen.

Cái mất đi thì không lộ ra ngay:

- Lỗi bất biến của B làm quay lui thay đổi của A.
- Hai module dính nhau ở mức giao dịch.
- Tách một module ra thành dịch vụ riêng hết còn là việc cơ học. Lập luận *"ranh giới đã sẵn"* của [`0001-modular-monolith.md`](0001-modular-monolith.md) mất chỗ dựa, vì một lần ghi phải nguyên tử xuyên hai tiến trình thì không tách được.

## Quyết định

Người dùng chốt ngày 2026-09-25: **chỉ ĐỌC đồng bộ qua `.Contracts`; GHI thì qua sự kiện outbox.** Kiến trúc sư ghi phần đi kèm.

1. **Đọc.** Module A đọc dữ liệu của module B qua một interface truy vấn khai trong `Modules.<B>.Contracts`. B hiện thực nó ở `Modules.<B>.Infrastructure`, và `AddBModule()` đăng ký. Interface đó **chỉ đọc**:
   - không đổi trạng thái nào;
   - không ghi database;
   - không ghi sự kiện outbox;
   - không gọi hệ ngoài có tác dụng.
2. **Ghi.** Module A không bao giờ đổi dữ liệu của B một cách đồng bộ. Cách làm là:
   - A ghi một sự kiện vào outbox, cùng giao dịch với thay đổi của A. Kiểu sự kiện khai ở `Modules.<A>.Contracts`.
   - B nhận qua seam `IOutboxEventHandler` và ghi trong giao dịch của lượt phát outbox, tách khỏi giao dịch của A.
3. **Câu *"đường phụ"* của ADR-0101 quyết định 3 được thay bằng hai mục trên.** Phần còn lại của ADR-0101 còn nguyên.
4. **Cơ chế của ADR-0025 không đổi.** Một handler vẫn ghi dữ liệu của chính module mình cùng dòng outbox và dòng nhật ký của Core trong một giao dịch. Cái bị cấm chỉ là ghi sang **module khác**.
5. **Luật.**
   - R2 ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §3 thêm vế *"ghi xuyên module chỉ qua sự kiện"*.
   - Luật mới **A21** ở [`../DEBT.md`](../DEBT.md): interface trong `Modules.<X>.Contracts` chỉ đọc. Chưa có cổng.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Cho phép ghi đồng bộ qua interface, chung giao dịch nhờ ADR-0025

**Được:** nhất quán tức thời. Đơn giản nhất để viết: một lời gọi, một commit, không có trạng thái trung gian.

**Mất:** ba hậu quả ở *Bối cảnh*. Thêm nữa, không cổng nào phân biệt được lời gọi "đọc" với lời gọi "ghi" nếu cả hai cùng được phép.

**Vì sao loại:** người dùng chốt cấm.

### Phương án B — Cấm cả đọc đồng bộ; mỗi module giữ bản sao dữ liệu nó cần, cập nhật qua sự kiện

**Được:** hai module tách hoàn toàn lúc chạy.

**Mất:** mỗi module phải dựng và giữ một mô hình đọc riêng cho dữ liệu của module khác, kèm độ trễ. Với một modular monolith, chung một database, chi phí đó không mua được gì đo được.

**Vì sao loại:** quá nặng cho mô hình một tiến trình.

### Phương án C — Giữ chữ *"đường phụ"* như ADR-0101

**Vì sao loại:** người review không có gì để đối chiếu. Một luật không nói được ranh giới thì bị hiểu theo cách rẻ nhất.

## Hệ quả

### Tích cực

- Mỗi lời gọi xuyên module có một câu hỏi kiểm được: *nó có đổi trạng thái không?*
- Tách một module ra dịch vụ riêng vẫn là việc cơ học. Đọc thành lời gọi HTTP; ghi vốn đã là sự kiện.
- Lỗi của B không quay lui thay đổi của A.

### Tiêu cực

- **Mọi lần ghi xuyên module là nhất quán cuối cùng.** Trạng thái của B cập nhật sau A, cách một lượt phát outbox. Nghiệp vụ nào cần *"cả hai module cùng ghi hoặc không ai ghi"* thì không làm được. Khi đó phải gộp hai module, hoặc xem lại ranh giới theo tiêu chí tách module ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4.3.
- **B từ chối một sự kiện thì A đã commit rồi.** Muốn hoàn tác thì A cần một sự kiện bù. Đó là thêm một luồng phải thiết kế, phải test.
- **Đọc đồng bộ vẫn là phụ thuộc lúc chạy.** B chậm thì A chậm.
- **Cổng máy chỉ bắt được hình dạng quen.** Một interface "đọc" gọi sang một dịch vụ có ghi gián tiếp sẽ lọt qua ArchTest; lớp bắt được còn lại là review.

### Rút lui nếu sai

Một ca thật cần ghi đồng bộ xuyên module thì viết ADR mới, nêu đúng ca đó và điều kiện của nó. Không dữ liệu nào phụ thuộc quyết định này. Hôm nay chưa có module nào.

### Dấu hiệu quyết định này bắt đầu sai

- Nhiều cặp module phải thêm sự kiện bù để hoàn tác lẫn nhau.
- Có người thêm vào `.Contracts` một interface mà phương thức trả `Task` không mang dữ liệu — hình dạng của một lệnh, không phải một truy vấn.
- Hai module thường xuyên phải đọc đồng bộ lẫn nhau theo cả hai chiều — dấu hiệu chúng là một module.

## Sáu câu hỏi trước khi chấp nhận

| # | Câu hỏi | Trả lời |
| --- | --- | --- |
| 1 | Vấn đề thật, đã đo chưa | Khoảng trống trong ADR-0101, cộng một khả năng kỹ thuật có thật do ADR-0025 mở ra. **Chưa có ca thật**: chưa module nào tồn tại |
| 2 | Phương án đơn giản hơn, vì sao không đủ | Phương án A đơn giản hơn để viết. Nó làm hai module dính nhau ở mức giao dịch và bỏ đường tách module |
| 3 | Chi phí vận hành thêm | Không hạ tầng mới. Mỗi lần ghi xuyên module cần một kiểu sự kiện và một bên nhận |
| 4 | Ai bảo trì | Module cung cấp giữ interface đọc của mình. Cổng A21: `test-engineer`, trong ArchTests của Core |
| 5 | Rút lui thế nào | ADR mới cho từng ca cần ghi đồng bộ; không dữ liệu phải chuyển |
| 6 | Có buộc Core biết nghiệp vụ không | Không. Luật nói về hình dạng lời gọi, không nói về module nào |

## Thi công và kiểm

| Ai | Việc | Khi nào |
| --- | --- | --- |
| `test-engineer` | ArchTest A21: lớp hiện thực một interface lấy từ `*.Contracts` không gọi `SaveChanges*`, không gọi `Add`/`Update`/`Remove` trên `DbSet` hay `DbContext`, không gọi `ExecuteUpdate*`/`ExecuteDelete*`, không dùng `IUnitOfWork`, không ghi outbox. Canary trên module giả của ADR-0102 | Cùng lượt mở rộng ArchTests ra module ([`0102-module-o-src-be-modules-test-canh-module.md`](0102-module-o-src-be-modules-test-canh-module.md)) |
| `backend-expert` | Không có việc mã hôm nay | — |

## Liên quan

- [`0101-hop-dong-giua-module-o-contracts-cua-module-phat.md`](0101-hop-dong-giua-module-o-contracts-cua-module-phat.md) — được ADR này bổ sung
- [`0025-luu-du-lieu-module-mot-transaction.md`](0025-luu-du-lieu-module-mot-transaction.md) — một giao dịch cho mọi `DbContext`
- [`../wiki-core/be/05-cross-module-consistency.md`](../wiki-core/be/05-cross-module-consistency.md) §4 — integration event qua outbox
