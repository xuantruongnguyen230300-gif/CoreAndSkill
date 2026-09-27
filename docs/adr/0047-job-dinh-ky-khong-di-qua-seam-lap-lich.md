---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0047 — Job **chạy một lần theo yêu cầu** đi qua seam `IBackgroundJobScheduler`; job **chạy định kỳ** là một `BackgroundService` đứng riêng

> **Trạng thái:** Đã chấp nhận (2026-09-20)

## Bối cảnh

Ngày 2026-09-15 đã chốt: cơ chế job nền của Core là `BackgroundService` của .NET, không thư viện
lập lịch — ghi ở [`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §7.
Quyết định đó nói về **thư viện**, và nó vẫn đúng nguyên vẹn.

Thứ chưa ai quyết lúc đó là **hình dạng**: một job đi qua seam `IBackgroundJobScheduler`
([`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §10.2) hay không. Câu hỏi chưa
đặt ra vì lúc chốt chưa có job nào. Bốn file `kind: luat` vì thế đã viết một câu ngắn gọn hơn thực
tế — rằng job đầu tiên của B3 là *"job đầu tiên **sau seam** `IBackgroundJobScheduler`"*.

Khi B3 thi công thì mâu thuẫn lộ ra, và nó lộ ra theo chiều **code đúng, tài liệu sai**:

- Seam đó, theo chính chữ ký đã chốt ở §10.2, chỉ có hai hình dạng: đẩy một việc chạy **ngay**, và
  đẩy một việc chạy **sau một khoảng trễ**. Không có hình dạng *lặp lại theo chu kỳ*.
- Job đầu tiên chạy thật ở B3 — dọn dữ liệu quá hạn — là một **vòng lặp định kỳ**. Nó nằm ở
  `src/BE/Core/CoreAndSkill.Core.Infrastructure/Jobs/StaleDataCleanupHostedService.cs`, chạy trên
  `PeriodicTimer`, và **không** đi qua seam.
- Hiện thực của seam thì vẫn có thật, ở
  `src/BE/Core/CoreAndSkill.Core.Infrastructure/Jobs/BackgroundJobScheduler.cs` cùng phía tiêu thụ
  `BackgroundJobQueueHostedService`. Nó chưa có lời gọi thật nào — mẫu dùng đầu tiên thuộc Nhóm B,
  chưa dựng.

Lý lẽ của việc tách hai hình dạng hiện **chỉ nằm trong một chú thích code**. Đó là chỗ sai để giữ
nó: [`0010-comment-toi-thieu.md`](0010-comment-toi-thieu.md) đã chốt comment trong code ở mức tối
thiểu, còn bốn file `kind: luat` thì vẫn nói ngược lại. Hệ quả đo được: mục nghiệm thu B3 số 1
không tick được theo đúng câu chữ của nó, và người khép pha bị đẩy vào chỗ phải chọn giữa đánh dấu
xong một câu sai, hoặc treo mục vô thời hạn.

## Quyết định

`architect` chốt hai nhánh, và chỉ hai:

1. Một việc **chạy một lần theo yêu cầu** — ngay hoặc sau một khoảng trễ — đi qua seam
   `IBackgroundJobScheduler`. Handler không tự sinh luồng.
2. Một việc **lặp lại theo chu kỳ** là một `BackgroundService` đứng riêng, đăng ký thẳng ở
   composition root, **không** đi qua seam.

Seam `IBackgroundJobScheduler` **không** nhận thêm hình dạng định kỳ ở v1.

Câu khai gốc của hai nhánh này nằm ở hàng `IBackgroundJobScheduler` trong
[`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1. Mọi file khác trỏ tới đó.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — thêm một hình dạng định kỳ vào `IBackgroundJobScheduler`

**Được:** một seam duy nhất cho mọi thứ chạy nền; tài liệu cũ không phải sửa chữ nào; giống hình
dạng API mà Hangfire và Quartz.NET quen dùng, nên người mới không thấy lạ.

**Mất:** seam hiện tại nhận một biểu thức gọi phương thức và trả về mã việc để hỏi trạng thái —
đúng khuôn *"trả mã việc rồi hỏi"* ở [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md)
§10.3. Một việc định kỳ không có mã việc để trả, không có ai hỏi trạng thái, và không có người gọi
ở tầng Application. Nhét nó vào cùng interface là bắt mọi hiện thực tương lai phải trả lời hai câu
hỏi không liên quan nhau.

**Vì sao loại:** nó mở rộng một seam để chứa một thứ không dùng seam đó. Cái giá trả ngay: mọi
hiện thực thay thế — kể cả bản Hangfire nếu sau này đổi — phải hiện thực cả nhánh định kỳ dù phần
còn lại của hệ không gọi tới. Cái giá trả sau: seam mất tính chất *"một câu hỏi, một câu trả lời"*,
và đó là tính chất khiến nó thay thế được.

### Phương án B — sửa code cho khớp câu chữ: đẩy job dọn dữ liệu qua seam

**Được:** tài liệu không phải sửa dòng nào; mục nghiệm thu B3 số 1 tick được ngay.

**Mất:** hiện thực sẽ là một `BackgroundService` gọi `ScheduleAsync` rồi job đó tự lên lịch lần
sau cho chính mình. Một vòng lặp viết thành đệ quy qua hàng đợi, trên một hàng đợi trong bộ nhớ
tiến trình nên mất lịch khi khởi động lại.

**Vì sao loại:** đây đúng là khuôn mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §4 cấm —
đóng một việc bằng cách làm thực tế khớp với mô tả, thay vì ngược lại. Và nó đổi một cơ chế đang
đúng lấy một cơ chế mong manh hơn, chỉ để một câu văn khỏi phải sửa.

### Phương án C — sửa bốn file luật, không viết ADR

**Được:** rẻ nhất; [`README.md`](README.md) §2 cũng nói quy ước thì ghi ở `quy-uoc/`, không cần ADR.

**Mất:** ranh giới *"cái gì được vào seam của `Core.Application`"* là ranh giới kiến trúc, không
phải quy ước đặt tên. Và phương án A là thứ người tỉnh táo sẽ đề xuất lại — nó giống API của mọi
thư viện job nền phổ biến, nên người đọc code sẽ tưởng việc tách hai nhánh là sót, rồi "sửa lại cho
đúng".

**Vì sao loại:** trúng hai dấu hiệu ở [`README.md`](README.md) §2 — *chạm ranh giới* và *trái với
thói quen ngành*. Phép thử nhanh ở mục đó cũng cho câu trả lời có: sáu tháng nữa sẽ có người hỏi
*"vì sao job dọn dữ liệu không đi qua seam?"*.

## Hệ quả

### Tích cực

- Mục nghiệm thu B3 số 1 tick được theo đúng câu chữ của nó, mà không phải sửa một dòng code nào.
- Seam giữ nguyên chữ ký đã chốt ở §10.2 — không có hiện thực nào phải viết thêm nhánh chết.
- Người thi công có một câu hỏi phân nhánh trả lời được trong một giây: *việc này chạy một lần hay
  lặp lại?*
- Lý lẽ rời khỏi chú thích code và vào file luật, đúng chiều của [`0010-comment-toi-thieu.md`](0010-comment-toi-thieu.md).

### Tiêu cực

- **Core có hai cơ chế job nền thay vì một.** Người mới phải học cả hai, và câu hỏi *"đặt job mới ở
  đâu"* nay có hai câu trả lời đúng. Đây là chi phí thật, trả mãi, và không lấy lại được bằng nỗ
  lực — nó là cái giá của việc không ép hai hình dạng vào một seam.
- **Job định kỳ không có seam nào che.** Nó gọi thẳng `BackgroundService` của .NET, nên đổi sang
  một thư viện lập lịch sau này sẽ phải sửa từng job định kỳ, không phải sửa một dòng đăng ký. Với
  một job thì rẻ; với mười job thì không.
- **Ranh giới hai nhánh dựa trên một câu chữ, không dựa trên một cổng.** Không có ArchTest nào bắt
  được một job định kỳ lỡ đi qua seam, hay ngược lại. Luật này vì thế thuộc danh sách chưa có cổng
  ở [`../RULES.md`](../RULES.md) §10 cho tới khi có người viết cổng — xem mục *Liên quan*.
- **Một việc "định kỳ nhưng kích hoạt được bằng tay"** rơi vào giữa hai nhánh và ADR này không trả
  lời. Chưa có ca nào như vậy; khi có, nó cần một quyết định riêng.

## Liên quan

| Cái gì | Ở đâu |
| --- | --- |
| Quyết định 2026-09-15 mà ADR này **bổ sung** — `BackgroundService`, không thư viện | [`../wiki-core/be/18-trien-khai-va-van-hanh.md`](../wiki-core/be/18-trien-khai-va-van-hanh.md) §7 |
| Câu khai gốc của hai nhánh | [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.1 |
| Chữ ký seam và khuôn *"trả mã việc rồi hỏi"* | [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §10.2, §10.3 |
| Điều kiện xem lại (cần lịch cron do người dùng cấu hình) | [`../wiki-core/be/01-core-components.md`](../wiki-core/be/01-core-components.md) §5.2 |
| Pha thi công và nghiệm thu | [`../wiki-core/be/trien-khai/04-b3-van-hanh.md`](../wiki-core/be/trien-khai/04-b3-van-hanh.md) §1, §5 |

**Luật sinh ra từ ADR này chưa có cột *"Ép bằng gì"* trong [`../RULES.md`](../RULES.md).** Tệp đó
đang do `backend-expert` cầm ở lượt này, nên `architect` **không** ghi vào — đây là việc còn nợ,
không phải việc đã làm. Cổng khả dĩ: một ArchTest kiểm mọi lớp kế thừa `BackgroundService` trong
`Core.Infrastructure` không phụ thuộc `IBackgroundJobScheduler`, trừ đúng phía tiêu thụ hàng đợi.
