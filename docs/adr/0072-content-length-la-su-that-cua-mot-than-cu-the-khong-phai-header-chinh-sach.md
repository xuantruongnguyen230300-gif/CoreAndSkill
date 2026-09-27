---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0072 — `Content-Length` là **sự thật của một thân cụ thể**, không phải header chính sách: nhánh tải tệp đặt nó, nhánh xuất cố ý không

> **Trạng thái:** Đã chấp nhận (2026-09-23) · Bổ sung [`0068-nhanh-tai-tep-di-qua-handlefile.md`](0068-nhanh-tai-tep-di-qua-handlefile.md)

## Bối cảnh

[ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md) quyết định 3 cấm nhánh tải tệp dựng một lớp kết quả thứ hai đặt lại ba header bắt buộc. `backend-expert` thi công đúng: `FileDownloadResponse.For` gói `FileDownload` thành `ExportFile` rồi trả `ExportFileResult`, không lớp kết quả nào mới.

Trong lúc thi công nó đo được một thứ ADR-0068 không nhắc tới. Kiến trúc sư kiểm lại ngày 2026-09-23:

| Đo gì | Kết quả |
| --- | --- |
| Hành vi trước ADR-0068 | Action gọi `File(stream, contentType, fileName)`; kết quả của framework đặt `Content-Length` khi luồng `CanSeek` |
| Hành vi khi đi qua `ExportFileResult` nguyên bản | Không đặt `ContentLength`. Kestrel không biết độ dài thân nên chuyển sang mã hoá phân đoạn |
| Người dùng mất gì | Trình duyệt không biết tổng dung lượng nên không vẽ được thanh tiến trình. Không lỗi, không cảnh báo, không test nào đỏ |
| Cách `backend-expert` xử | Thêm tham số **tuỳ chọn** `long? contentLength = null` cho `ExportFileResult`; `FileDownloadResponse` truyền độ dài đọc **trước** khi luồng bị tiêu thụ, và chỉ khi luồng `CanSeek`. Nhánh xuất không truyền gì, hành vi giữ nguyên |
| Cổng khoá lại | Khẳng định trong `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Web/FileDownloadResponseTests.cs` — `response.Content.Headers.ContentLength` |

Câu hỏi đưa lên kiến trúc sư: giữ và ghi nhận, hay gỡ vì vượt phạm vi ADR-0068.

Câu trả lời phụ thuộc một phân biệt mà ADR-0068 chưa từng phải phát biểu, vì lúc đó chỉ có ba header cùng loại: **ba header của quyết định 3 là header CHÍNH SÁCH, `Content-Length` thì không.**

`Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Cache-Control: no-store` mang một quyết định của hệ thống, giống hệt nhau ở mọi phản hồi tệp, và giá trị của chúng không phụ thuộc thân. Đó là vì sao chúng phải có đúng một nguồn: hai bản sẽ lệch, và bản quên `no-store` không làm gì đỏ.

`Content-Length` không mang quyết định nào. Nó là một **phép đo của đúng thân đang gửi**, và nó *phải* khác nhau giữa hai phản hồi. Một "nguồn duy nhất" cho nó là vô nghĩa — cái duy nhất được phép dùng chung là **quy tắc** *"biết thì khai, không biết thì đừng khai"*.

## Quyết định

Kiến trúc sư chốt:

1. **Giữ tham số `contentLength`.** Nó không vi phạm quyết định 3 của [ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md): không có lớp kết quả thứ hai, và ba header chính sách vẫn đặt ở đúng một chỗ.
2. **Ranh giới phát biểu ra:** `ExportFileResult` đặt **ba** header chính sách cho mọi nhánh, và đặt `Content-Length` **chỉ khi nơi gọi khai một giá trị**. Thêm một header chính sách thứ tư vẫn cần một ADR; thêm một phép đo của thân thì không.
3. **Nhánh XUẤT cố ý KHÔNG khai `Content-Length`, và điều đó được ghi lại như một lựa chọn, không phải một thiếu sót.** `ExportFile` mang một delegate ghi dần theo lô và tệp xuất không được vật chất hoá; biết trước độ dài đòi phải đệm toàn bộ vào bộ nhớ hoặc tệp tạm, tức lật đúng quyết định luồng của [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md). Phản hồi xuất là phân đoạn, và người dùng không có thanh tiến trình khi xuất.
4. **Không bao giờ khai một `Content-Length` suy đoán.** Luồng không tua lại được thì không khai. Một con số sai ở header này làm hỏng chính giao thức — client cắt thân sớm hoặc treo chờ phần không bao giờ tới — nên thiếu header luôn là lựa chọn an toàn hơn đoán.
5. **Ca khẳng định trong test là một phần của quyết định.** Gỡ nó đi thì việc mất header quay lại thành một hồi quy im lặng, đúng như nó đã từng.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Gỡ tham số, trả về đúng phạm vi ADR-0068

**Được:** `ExportFileResult` giữ một chữ ký duy nhất, không tham số tuỳ chọn nào. Lượt thi công khớp từng chữ với ADR đã duyệt, và kỷ luật *"không làm thêm thứ ADR không yêu cầu"* được giữ nguyên vẹn — đây là một kỷ luật thật, không phải hình thức.

**Mất:** nó khôi phục một hồi quy đã đo được. ADR-0068 quyết định 5 đổi `Download` sang `HandleFile` nhằm gom ba header; nó **không** chọn việc bỏ `Content-Length`, nó chỉ không nhìn thấy. Gỡ tham số là biến một hệ quả ngoài ý muốn thành một quyết định — mà không ai thật sự quyết.

**Vì sao loại:** quy tắc *"đúng phạm vi ADR"* tồn tại để chặn việc **mở rộng thiết kế** giữa lúc thi công, không phải để buộc người thi công im lặng khi họ đo được một tác dụng phụ của chính quyết định đó. Ranh giới đúng là: phát hiện ra thì **báo lên và chờ chấm** — và đó chính là điều đã xảy ra.

### Phương án B — Giữ hành vi bằng cách cho `FileDownloadResponse` dựng một `IActionResult` riêng đặt cả bốn header

**Được:** không đụng chữ ký của `ExportFileResult`, nên nhánh xuất không thấy gì thay đổi.

**Vì sao loại:** đây đúng là phương án C mà ADR-0068 đã loại, quay lại dưới một cái cớ mới. Ba header chính sách thành hai bản, và bản thứ hai nằm trong `Core.Web` nên trông chính đáng hơn — tệ hơn ca ban đầu chứ không nhẹ hơn.

### Phương án C — Đặt `Content-Length` cho **cả** nhánh xuất bằng cách đệm tệp trước khi gửi

**Được:** một quy tắc duy nhất cho mọi phản hồi tệp, không có nhánh nào "đặc biệt". Xuất cũng có thanh tiến trình.

**Mất:** một lượt xuất lớn phải nằm trọn trong bộ nhớ hoặc trong một tệp tạm trước khi byte đầu tiên ra dây. Đổi một chi tiết trải nghiệm lấy một trần dung lượng mới cho toàn hệ, cộng một vòng đời tệp tạm phải dọn.

**Vì sao loại:** cái giá không cân xứng với cái được, và nó lật một quyết định luồng đã có mà không nêu được lý do mới nào ngoài tính đối xứng. Tính đối xứng không phải một yêu cầu.

### Phương án D — Để nhánh xuất khai `Content-Length` khi nó *tình cờ* biết trước

**Vì sao loại:** nó làm hình dạng phản hồi của cùng một endpoint đổi theo dữ liệu — cùng một lệnh xuất, lần này phân đoạn, lần sau không. Người dò lỗi sẽ mất thời gian tìm nguyên nhân trong tầng HTTP trong khi nguyên nhân nằm ở số dòng của bộ lọc. Một hình dạng ổn định đáng giá hơn một tối ưu không đều.

## Hệ quả

### Tích cực

- Nhánh tải tệp giữ đúng hành vi trên dây mà nó có trước ADR-0068; lượt gom header không âm thầm lấy đi thứ gì.
- Ranh giới *header chính sách* ↔ *phép đo của thân* nay có tên. Lần sau có người muốn thêm một header vào `ExportFileResult`, câu hỏi đầu tiên là phân loại nó, và câu trả lời quyết định có cần ADR hay không.
- Việc nhánh xuất chạy phân đoạn thành một lựa chọn đã ghi, kèm cái giá. Trước đó nó là một chỗ trống mà ai cũng có thể "sửa" bằng một lần đệm tệp.
- Hành vi được khoá bằng một khẳng định chứ không bằng một câu dặn.

### Tiêu cực

- **Một tham số tuỳ chọn là một nhánh mà phần lớn nơi gọi không đi.** `ExportFileResult` nay có hai chế độ, và chỉ một trong hai được dùng ở nhánh xuất. Người đọc lớp này phải đọc thêm chú thích mới biết vì sao. Chi phí này trả bằng chú thích, và chú thích thì mục ruỗng được.
- **`ExportFileResult` nay gánh ba thứ mà tên nó không nói gì tới**: nhánh xuất, nhánh tải tệp ([ADR-0068](0068-nhanh-tai-tep-di-qua-handlefile.md) đã khai), và nay là hai chế độ độ dài. Tên vẫn giữ vì nó là hằng số cú pháp của cổng S20 — nhưng khoảng cách giữa tên và trách nhiệm vừa rộng thêm một nấc. Đây là lần thứ hai khoảng cách đó được ghi nhận; lần thứ ba thì phải đặt lại câu hỏi về tên, chứ không ghi nhận tiếp.
- **Không cổng nào canh việc `Content-Length` còn được đặt.** Ca khẳng định trong `FileDownloadResponseTests` là một test hành vi của một endpoint, không phải một luật có mã. Ai viết một lối tệp thứ ba sẽ không có gì nhắc. Chỗ này chưa được nâng thành mã luật vì hôm nay chỉ có hai lối và một trong hai cố ý không đặt — một luật ép "mọi lối tệp phải khai độ dài" sẽ sai ngay với nhánh xuất.
- **Xuất tệp không có thanh tiến trình, và điều đó nay là chính sách.** Người dùng tải một báo cáo lớn không biết còn bao lâu. Đó là cái giá của quyết định 3, trả thật, không trả bằng lời hứa sẽ tối ưu sau.

### Rút lui nếu sai

Một dòng. Bỏ đối số thứ hai ở `FileDownloadResponse.For` thì nhánh tải tệp quay lại phân đoạn; bỏ luôn tham số ở `ExportFileResult` thì về đúng hình dạng ADR-0068 để lại. Ca khẳng định trong `FileDownloadResponseTests` sẽ đỏ trước, nên việc rút lui không xảy ra được trong im lặng.

## Liên quan

- [`0068-nhanh-tai-tep-di-qua-handlefile.md`](0068-nhanh-tai-tep-di-qua-handlefile.md) quyết định 3 và 5 — ba header chính sách một nguồn, `Download` đổi sang `HandleFile`.
- [`0064-nhanh-tra-tep-di-qua-apicontrollerbase.md`](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) quyết định 4 — `ExportFileResult` giữ tên và giữ `internal` vì nó là dấu hiệu của cổng S20.
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §3 — bốn tên `Handle*`, và luật **B16** ép chúng.
- [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) — tệp xuất ghi thẳng ra luồng, không vật chất hoá.
