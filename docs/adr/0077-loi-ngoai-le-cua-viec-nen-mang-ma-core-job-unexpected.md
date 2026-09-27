---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0077 — Việc nền hỏng vì ngoại lệ ghi mã `CORE.JOB.UNEXPECTED`; `ErrorType.Unexpected` không rời `Core.Web`

> **Trạng thái:** Đã chấp nhận (2026-09-23)
>
> **Bổ sung [`0034-errortype-unexpected.md`](0034-errortype-unexpected.md).** Quyết định 2 của ADR-0034 — *chỉ `IExceptionHandler` phát `Unexpected`, `CommonErrors` là ngoại lệ có tên vì nó khai chứ không trả* — giữ nguyên **từng chữ**. ADR này không thêm ngoại lệ nào vào đó; nó gỡ chỗ duy nhất đang vi phạm nó.

## Bối cảnh

Lượt soát Core ngày 2026-09-23 tìm ra một lời gọi ngược luật **R9** trong `Core.Application`.

`src/BE/Core/CoreAndSkill.Core.Application/Jobs/JobRunner.cs` có, trong nhánh `catch (Exception ex) when (ex is not OperationCanceledException)` của `ExecuteAsync`, câu `return JobOutcome.Failure(CommonErrors.Unexpected);`. Đó là một tham chiếu tới `ErrorType.Unexpected` đứng ngoài `Core.Web`, và nó **trả** giá trị đó chứ không khai nó.

Luật R9 ở [`../RULES.md`](../RULES.md) §3 và quyết định 2 của [ADR-0034](0034-errortype-unexpected.md) nói cùng một điều: ngoài hạ tầng xác thực và `IExceptionHandler` ở `Core.Web`, không kiểu nào được tham chiếu `ErrorType.Unauthorized` hay `ErrorType.Unexpected`; ngoại lệ có tên là lớp `CommonErrors` ở `Core.Application`, và lý do nó được miễn trừ được viết ra rõ ràng — nó **khai** giá trị, không trả.

Bốn phép đo lấy cùng ngày, trên mã nguồn hôm nay:

- **`type` không đi ra dây trên đường việc nền.** `src/BE/Core/CoreAndSkill.Core.Application/Jobs/JobJson.cs` có đúng một hàm, `public static string ErrorOf(Error error)`, và nó tuần tự hoá ba trường `code`, `message`, `messageParams`. `Error.Type` bị bỏ. Vậy thứ thật sự đi qua `core.job.error` rồi ra `GET /api/v1/core/jobs/{id}` trong `data.error` của một phản hồi **200** là chuỗi mã `CORE.SYSTEM.UNEXPECTED`, không phải giá trị `Unexpected` của `type`. Báo cáo soát ban đầu nói `type` ra dây; phép đo này bác điều đó, và việc bác nó **không** làm vi phạm R9 nhẹ đi — nó chỉ đổi thứ bị lộ.
- **`JobOutcome` không đi qua bộ ánh xạ HTTP.** `src/BE/Core/CoreAndSkill.Core.Application/Jobs/IJobExecutor.cs` khai `public sealed record JobOutcome(string? ResultJson, Guid? ResultFileId, Error? Error)`; `JobRunner` đưa `Error` vào `tracked.Fail(now, JobJson.ErrorOf(outcome.Error!))`. Không nhánh nào chạm `ResultToHttpMapper`. Mã 500 mà `ErrorType.Unexpected` tồn tại để sinh ra **không bao giờ** sinh ra trên đường này.
- **Khu việc nền đã có khuôn cho loại mã này.** `src/BE/Core/CoreAndSkill.Core.Application/Jobs/JobErrors.cs` mang chú thích `Hai mã dưới chỉ đi vào cột` — `error` của `core.job` — rồi khai `CORE.JOB.NO_EXECUTOR` và `CORE.JOB.INTERRUPTED`, cả hai `ErrorType.BusinessRule`. Tức catalog này **đã** chứa mã chỉ-vào-bản-ghi-việc, mang một `ErrorType` không bao giờ được dùng.
- **Chưa cổng nào bắt được.** Cột *Trạng thái* của R9 là `📐`: đặc tả cổng đã chốt, chưa file nào mang tên đó. Lệnh tự rà ở đầu [`../RULES.md`](../RULES.md) không đọc hiểu mã nguồn nên không thấy vi phạm này.

Ràng buộc có thật: ngày ArchTest R9 được viết **đúng** đặc tả đang khai ở cột *Ép bằng gì* (quét `Core.Application`, miễn trừ đúng lớp `CommonErrors`), dòng trong `JobRunner` làm cổng đỏ ngay lần chạy đầu. Lối sửa rẻ nhất **lúc đó** là thêm `JobRunner` vào danh sách miễn trừ — một quyết định kiến trúc bị ép ra dưới áp lực của một cổng đang chặn việc khác.

`docs/contracts/jobs.md` §1 hôm nay không liệt kê mã nào cho trường `error` — không `NO_EXECUTOR`, không `INTERRUPTED`, không mã của nhánh ngoại lệ. Bảng *Lỗi* của card chỉ liệt kê lỗi **HTTP** của chính endpoint.

## Quyết định

`architect` chốt ba điều:

1. **Nhánh ngoại lệ của `JobRunner` ghi một mã của khu việc nền, không ghi `CommonErrors.Unexpected`.** `JobErrors` nhận mục thứ tư: mã `CORE.JOB.UNEXPECTED`, `ErrorType.BusinessRule`, đứng cùng khối và cùng chú thích với `NO_EXECUTOR` và `INTERRUPTED`. Sau thay đổi này, `Core.Application` chỉ còn **một** chỗ nhắc `ErrorType.Unexpected`: dòng khai trong `CommonErrors` — đúng ngoại lệ mà R9 đã có tên.
2. **R9 giữ đúng một ngoại lệ có tên.** Không thêm `JobRunner`, không thêm bất kỳ kiểu nào khác. Cột *Ép bằng gì* của R9 được bổ sung một câu trỏ về ADR này, để người viết ArchTest biết ca `JobRunner` đã được xử **bằng cách đổi mã**, không bằng cách nới miễn trừ.
3. **`docs/contracts/jobs.md` §1 liệt kê cả ba mã đi vào `core.job.error`** — `CORE.JOB.NO_EXECUTOR`, `CORE.JOB.INTERRUPTED`, `CORE.JOB.UNEXPECTED` — trong một bảng **tách khỏi** bảng lỗi HTTP của endpoint, kèm một câu nói rõ chúng không bao giờ là `code` của một envelope thất bại.

Việc thi công thuộc `backend-expert`; phạm vi ở mục *Hệ quả*.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Ghi ngoại lệ có tên **thứ hai** vào R9 và ADR-0034

Đây là phương án reviewer đặt lên bàn, và là phương án ít chạm mã nhất: không mã mới, không khoá dịch mới, không hàng hợp đồng mới. Lý do đưa ra: `JobOutcome` không đi qua `ResultToHttpMapper` nên `Unexpected` ở đây không ánh xạ sai cái gì.

**Được:** rẻ nhất hôm nay. Lý do nêu ra là **đúng** — phép đo ở §Bối cảnh xác nhận `JobOutcome` không chạm bộ ánh xạ.

**Vì sao loại:** ba lý do, không lý do nào là chuyện thẩm mỹ.

Thứ nhất, nó đổi **hình dạng** của R9, không chỉ đổi độ dài danh sách. Ngoại lệ đang có là một **catalog**: một lớp khai hằng buộc phải nêu kiểu của chính mục nó khai, nên miễn trừ nó là hệ quả bắt buộc của việc có catalog, và tiêu chí đó phát biểu được bằng một câu ai cũng kiểm được. `JobRunner` là một nơi **trả** giá trị. Gộp hai loại vào một danh sách thì R9 thôi là *"chỉ `Core.Web` trả"* và thành *"`Core.Web` cộng những cái tên trong danh sách"* — và một luật có hình dạng đó không có tiêu chí nào để từ chối cái tên thứ ba.

Thứ hai, lý do *"không đi qua `ResultToHttpMapper`"* đúng cho `JobRunner` **hôm nay**, nhưng nó không phải một tính chất của kiểu, nó là một tính chất của đường đi. Không gì giữ cho `JobRunner` mãi nằm ngoài một đường có ánh xạ HTTP, và không cổng nào kiểm được điều đó. Miễn trừ thì mãi mãi; lý do miễn trừ thì không.

Thứ ba, nó **không rẻ hơn** ở phía tài liệu. Quyết định 2 của ADR-0034 là nội dung của một ADR đã chấp nhận, nên luật **D45** cấm sửa nó; ghi một ngoại lệ thứ hai vẫn phải đi bằng một ADR mới. Phương án A tốn đúng một ADR như phương án đã chọn, và đổi lại một luật yếu hơn.

### Phương án B — Không làm gì, để ArchTest R9 xử lúc được viết

**Được:** không tốn gì hôm nay.

**Vì sao loại:** nó đẩy quyết định sang đúng thời điểm tệ nhất để ra quyết định. Lúc đó cổng đang đỏ, người gặp nó đang làm một việc khác, và lối ra rẻ nhất trong tay họ là nới miễn trừ — tức phương án A, chọn dưới áp lực, không có ADR. Hôm nay đây còn là một quyết định kiến trúc; để lâu nó thành một cổng phải làm cho xanh.

### Phương án C — Bỏ `ErrorType` khỏi đường `core.job.error`

`Job.Fail` nhận mã và tham số thay vì nhận một `Error`; `JobJson.ErrorOf` đổi chữ ký theo. Đây là phương án chạm gốc: trường `Type` trên đường này vốn đã chết, và cách sạch nhất để một trường chết không nói dối là không có nó.

**Được:** xoá hẳn khả năng một `ErrorType` sai nghĩa nằm trên mã chỉ-vào-bản-ghi-việc — xoá luôn hệ quả tiêu cực thứ nhất của phương án đã chọn.

**Vì sao loại:** `JobErrors` là catalog **trộn**: `CORE.JOB.NOT_FOUND` được `GetJobQueryHandler` trả qua `Result` và **có** ra HTTP 404. Tách đường ghi khỏi `Error` vì thế kéo theo tách catalog, và `Error` là từ vựng dùng chung của cả BE — đổi nó cho một đường ghi là cái giá lớn hơn hẳn vấn đề đang giải. Phương án này **được giữ lại làm đường lùi**, không bị bác về nguyên tắc; điều kiện xét lại ở mục *Hệ quả*.

### Phương án D — Mã mới nhưng giữ `ErrorType.Unexpected`

Khai `CORE.JOB.UNEXPECTED` với `ErrorType.Unexpected`.

**Được:** kiểu đọc lên đúng nghĩa của thứ vừa xảy ra.

**Vì sao loại:** nó không giải gì cả. Vi phạm R9 là một **tham chiếu tới giá trị enum** trong `Core.Application`; đổi mã mà giữ kiểu để nguyên tham chiếu đó, ArchTest vẫn đỏ đúng chỗ cũ.

## Hệ quả

### Tích cực

- ArchTest R9, viết đúng đặc tả đang khai ở cột *Ép bằng gì*, xanh ngay lần chạy đầu — không ai phải chọn giữa "nới miễn trừ" và "chặn việc của người khác".
- `CORE.SYSTEM.UNEXPECTED` từ nay có **một** nghĩa duy nhất khi đọc log và khi người dùng báo mã: một request đã ra 500 qua `IExceptionHandler`. Trước thay đổi này cùng chuỗi đó phủ hai sự kiện khác hẳn nhau — một request hỏng, và một việc nền hỏng nhiều phút sau khi request tạo nó đã trả 202.
- Ba đường hỏng của việc nền mang cùng tiền tố `CORE.JOB.`, khớp bề mặt mà chúng xuất hiện (`core.job.error`).

### Tiêu cực

- **`CORE.JOB.UNEXPECTED` mang `ErrorType.BusinessRule`, và `BusinessRule` ánh xạ 422.** Giá trị đó vô hại hôm nay chỉ vì hai điều kiện cùng đúng: `JobJson.ErrorOf` không ghi `type`, và không handler nào trả mã này. **Không cổng nào canh điều kiện thứ hai.** Ngày có người trả `JobErrors.Unexpected` từ một handler, client nhận 422 cho một lỗi hệ thống. ADR-0034 đã loại đúng cách ghép này cho `CORE.SYSTEM.UNEXPECTED` với lý do *"bảng ánh xạ và `type` trên dây sẽ nói dối"* — lý do đó vẫn đúng, chỉ là nó chưa cắn vì đường đi hôm nay không đi qua bảng ánh xạ. ADR này thêm mã thứ ba vào một chỗ hở đã có sẵn cho `NO_EXECUTOR` và `INTERRUPTED`; **nó không đóng chỗ hở đó.**
- **Thêm một mã là thêm một khoá dịch phải nhớ:** `loi.CORE.JOB.UNEXPECTED` trong mọi tệp ngôn ngữ. Cổng F23 canh *các tệp ngôn ngữ khớp nhau*, không canh *mọi mã BE đều có khoá* — quên đều tay ở mọi ngôn ngữ thì F23 vẫn xanh và màn việc nền hiện nguyên chuỗi khoá.
- **Người vận hành phải nhớ hai chuỗi thay vì một.** Một sự cố hạ tầng nay để lại `CORE.SYSTEM.UNEXPECTED` hoặc `CORE.JOB.UNEXPECTED` tuỳ đường nó đi. Đây là mặt trái không gỡ được của chính lợi ích ở trên: phân biệt được hai sự kiện thì phải có hai tên.
- **Chi phí thi công không bằng không.** Ngoài `JobRunner` và `JobErrors`, ít nhất bốn tệp test đang neo vào mã cũ: `src/BE/Tests/CoreAndSkill.Core.UnitTests/Jobs/JobRunnerTests.cs` có `job.ErrorJson!.ShouldContain(CommonErrors.Unexpected.Code)`; `src/BE/Tests/CoreAndSkill.Core.UnitTests/Jobs/JobHandlersTests.cs` dựng chuỗi `CORE.SYSTEM.UNEXPECTED` làm `ErrorJson` mẫu; `src/BE/Tests/CoreAndSkill.Core.UnitTests/Common/B4SupportTests.cs` gộp `CommonErrors.Unexpected.Code` vào tập mã B4. Chú thích đầu `JobRunner.cs` cũng nêu tên mã cũ.

### Điều kiện xét lại phương án C

Mở lại phương án C khi **một** trong hai điều xảy ra: có mã thứ tư chỉ đi vào `core.job.error`, hoặc một mã `CORE.JOB.*` thuộc nhóm chỉ-vào-bản-ghi-việc bị trả nhầm từ một handler và ra HTTP. Điều thứ hai là dấu hiệu quyết định này bắt đầu sai.

## Liên quan

- [`0034-errortype-unexpected.md`](0034-errortype-unexpected.md) — quyết định 2, giữ nguyên; và bảng phương án đã loại ở đó, nơi cách ghép `CORE.SYSTEM.UNEXPECTED` + `BusinessRule` bị bác.
- [`0027-errortype-unauthorized.md`](0027-errortype-unauthorized.md) — khuôn *"chỉ hạ tầng phát"* mà R9 ép, và lý do R9 quét mã nguồn chứ không quét assembly.
- [`../RULES.md`](../RULES.md) §3 — luật R9 và cột *Ép bằng gì*.
- [`../contracts/jobs.md`](../contracts/jobs.md) §1 — nơi ba mã `core.job.error` được liệt kê.
