---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0068 — Nhánh tải tệp đi qua `ApiControllerBase.HandleFile(Result<FileDownload>)`; hai lối tệp tách nhau bằng **kiểu tham số**

> **Trạng thái:** Đã chấp nhận (2026-09-23) · Bổ sung [`0064-nhanh-tra-tep-di-qua-apicontrollerbase.md`](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) · Bổ sung bởi [ADR-0072](0072-content-length-la-su-that-cua-mot-than-cu-the-khong-phai-header-chinh-sach.md) (2026-09-23) — `Content-Length` được phân loại là phép đo của thân chứ không phải header chính sách, nên nhánh tải tệp khai nó mà không phá quyết định 3; mọi quyết định của ADR này giữ nguyên hiệu lực

## Bối cảnh

[ADR-0064](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) gom nhánh **xuất** vào `HandleExport` để ba header của một phản hồi tệp — `Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Cache-Control: no-store` — chỉ được đặt ở đúng một chỗ, và quên chúng là chuyện không xảy ra được. Nó **không** chạm nhánh **tải tệp**, và để lại chỗ đó ở bảng *"Có thật hôm nay"* của [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) với một câu chưa quyết.

Kiến trúc sư đối chiếu ngày 2026-09-23:

| Đo gì | Kết quả |
| --- | --- |
| `FilesController.Download` dựng phản hồi thế nào | `src/BE/Core/CoreAndSkill.Core.Web/Controllers/FilesController.cs`, action `Download` — `return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);` |
| Hai header bảo mật đến từ đâu | Cùng action, hai dòng `Response.Headers.XContentTypeOptions` và `Response.Headers.CacheControl` gõ tay ngay trước lời gọi trên |
| Kiểu dữ liệu nhánh này trả về | `src/BE/Core/CoreAndSkill.Core.Application/Files/FileDto.cs`, `public sealed record FileDownload(Stream Content, string ContentType, string FileName)` — một luồng **đã mở**, khác hẳn `ExportFile` vốn mang một delegate ghi |
| Cổng nào canh cặp header đó | Không cổng nào. Luật là **B16** ở [`../DEBT.md`](../DEBT.md), chưa từng có cổng |
| Cổng S20 có thấy action này không | Không, và **đúng**: `AntiforgeryMarkScanner` chỉ đánh dấu action xuất; `GetFileQueryHandler` không ghi nhật ký kiểm toán nên nhánh phản chiếu cũng không bắt |

Hệ quả: cặp header bảo mật của nhánh tải tệp sống ở **hai** chỗ — `ExportFileResult` cho nhánh xuất, thân action cho nhánh tải. Đó đúng khuôn *"hai bản sẽ lệch nhau"* mà [`../../.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §5 cấm, và bản quên `no-store` không làm gì đỏ. Người dùng chốt gom lại.

## Quyết định

Kiến trúc sư chốt, người dùng đã duyệt:

1. **`ApiControllerBase` nhận lối thứ năm: `HandleFile(Result<FileDownload> result)`.** Thành công trả nhánh tệp; thất bại đi chung `Failure` với bốn lối kia. Bề mặt lớp cơ sở thành **năm lối dưới bốn tên** `Handle*`.
2. **Hai lối tệp tách nhau bằng KIỂU THAM SỐ, không bằng kỷ luật của người viết.** `HandleExport` chỉ nhận `Result<ExportFile>`, `HandleFile` chỉ nhận `Result<FileDownload>`. Một handler xuất **không biên dịch được** qua `HandleFile`, và ngược lại — nên dấu hiệu cú pháp của cổng S20 (`HandleExport`) không mất nghĩa vì có thêm một lối tệp.
3. **Ba header bắt buộc giữ đúng một nguồn trong `Core.Web`.** Nhánh tải tệp **không** được dựng một lớp kết quả thứ hai đặt lại ba header đó. `ExportFileResult` giữ `internal`, giữ nguyên tên — nó vẫn là dấu hiệu thứ hai của S20 theo [ADR-0064](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) quyết định 4, và đổi tên nó là đổi một hằng số của cổng đang chạy để lấy một cái tên đẹp hơn.
4. **Quyền sở hữu luồng khai ở chữ ký, không khai bằng lời dặn.** `FileDownload.Content` là luồng đã mở mà *người nhận* đóng ([`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md)); người nhận nay là `HandleFile`. Lối đó đóng luồng sau khi ghi xong, kể cả khi client ngắt giữa chừng.
5. **`FilesController.Download` đổi sang `HandleFile` và bỏ hai dòng đặt header tay.** Trong toàn repo còn đúng một hình dạng cho một phản hồi tệp.
6. **Luật §3 của [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) đổi từ "ba chỗ đặt status" thành "bốn tên"**, và câu *"controller không tự dựng `IActionResult` nào"* thôi là mô tả luật-chưa-khớp-code: nó thành mô tả code.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Giữ `File(...)` ở action, dời ba header sang một middleware hoặc `IResultFilter`

**Được:** không đụng lớp cơ sở, bề mặt `ApiControllerBase` đứng yên ở bốn lối — tức trả lại đúng hệ quả tiêu cực mà ADR-0064 đã khai.

**Mất:** middleware phải **đoán** phản hồi nào là tệp. Dấu hiệu khả dĩ duy nhất là `Content-Disposition` đã có, hoặc một allowlist đường dẫn — allowlist thì [ADR-0062](0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md) đã loại một lần cho antiforgery vì cùng lý do: nhận diện theo đường dẫn là một bảng người ta quên cập nhật. Và một lớp đặt header **sau** khi action đã chạy thì không ngăn được action tự đặt giá trị khác.

**Vì sao loại:** nó biến một ràng buộc kiểm được lúc biên dịch (muốn trả tệp thì phải gọi lối này) thành một ràng buộc kiểm lúc chạy, trên một phép đoán.

### Phương án B — `HandleExport` nhận luôn nhánh tải tệp; `FilesController` chuyển `FileDownload` thành `ExportFile`

**Được:** không lối mới, bề mặt giữ nguyên bốn. Một dòng adapter trong action.

**Mất:** cổng S20 nhận diện action xuất bằng lời gọi `HandleExport`. Gộp hai nhánh vào một tên làm `Download` trông **đúng như một endpoint xuất** — ArchTest `EveryGetActionWithSideEffects_RequiresAntiforgery` sẽ đỏ, và cách sửa hiển nhiên nhất là gắn `[RequireAntiforgery]` lên `Download`. Lúc đó FE phải gửi `X-XSRF-TOKEN` cho mọi lần tải tệp — một đổi thay ở hợp đồng [`../contracts/files.md`](../contracts/files.md) do một quyết định về *chỗ đặt header* gây ra.

**Vì sao loại:** nó bắt cổng S20 trả sai câu hỏi của chính nó. Hai nhánh giống nhau ở *hình dạng HTTP*, khác nhau ở *tác dụng phụ* — và S20 tồn tại để canh đúng khác biệt đó.

### Phương án C — Mỗi nhánh một lớp `IActionResult` riêng, mỗi lớp tự đặt ba header

**Vì sao loại:** ba header thành hai bản. Đây là chính cái hỏng đang được sửa, chỉ dời từ *action ↔ lớp kết quả* sang *lớp kết quả ↔ lớp kết quả*, và bản thứ hai nằm trong `Core.Web` nên trông chính đáng hơn — tệ hơn ca hôm nay chứ không nhẹ hơn.

### Phương án D — Không làm gì, ghi nợ B16 rõ hơn rồi chờ cổng

**Được:** không tốn gì hôm nay.

**Vì sao loại:** cổng của B16 (ArchTest quét thân action) đúng là dựng được. Nhưng nó chỉ bắt *action tự dựng `IActionResult`*; muốn nó xanh thì vẫn phải có một lối hợp lệ để `Download` đi qua. Không có `HandleFile` thì cổng B16 vừa dựng xong đã đỏ và không có cách sửa nào ngoài một ngoại lệ có tên — tức nợ đổi hình chứ không trả.

## Hệ quả

### Tích cực

- Ba header của **mọi** phản hồi tệp trong repo đặt ở đúng một chỗ; module hạ nguồn không chạm được vào chúng và cũng không quên được.
- Luật §3 hết chỗ *"code chưa theo câu này"* — bảng *"Có thật hôm nay"* của [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) mất hàng `🚧` cuối cùng của §3.
- Cổng B16 dựng được ngay sau đó mà không cần ngoại lệ nào, vì mọi action đã có một lối hợp lệ.
- Vòng đời luồng của `FileDownload` thành trách nhiệm của một chỗ duy nhất, thay vì một câu dặn trong chú thích của record.

### Tiêu cực

- **Bề mặt `ApiControllerBase` lên năm lối.** ADR-0064 đã khai đây là cái giá khi thêm lối thứ tư; quyết định này trả thêm một lần nữa, và lần này lập luận *"lối này mọi controller đều thấy dù phần lớn không bao giờ dùng"* đúng hơn trước — số controller trả tệp ít hơn hẳn số controller trả JSON. Cái mua được là một **ranh giới biên dịch** giữa hai nhánh tệp, thứ mà phương án B không có; đánh đổi này chấp nhận được **một lần nữa** chứ không phải mãi mãi: lối thứ sáu phải chứng minh lại từ đầu, và ngưỡng để chứng minh cao hơn.
- **Hai tên rất giống nhau ở cùng một lớp cơ sở.** `HandleExport` và `HandleFile` chỉ khác nhau ở kiểu tham số. Trình biên dịch chặn được việc gọi nhầm, nhưng **không** chặn được việc một handler mới khai sai kiểu trả về ngay từ đầu — viết một endpoint xuất trả `FileDownload` thì nó đi lọt qua `HandleFile` và S20 không thấy. Phần bù duy nhất hôm nay là nhánh phản chiếu của S20 (handler có với tới đường ghi nhật ký kiểm toán không), và nhánh đó chỉ bắt được endpoint xuất **có ghi nhật ký** — đúng định nghĩa của [ADR-0062](0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md), nên chưa hở, nhưng nó là chỗ hở sẽ mở ra nếu định nghĩa đó nới.
- **`ExportFileResult` nay phục vụ hai nhánh trong khi tên chỉ nói một.** Giữ tên là chọn *cổng đang chạy* thay vì *cái tên đúng*; người đọc code sẽ gặp một lớp tên "Export" dựng phản hồi cho một lần tải tệp thường.
- **Ba chỗ phải sửa cùng một lượt** — xem mục dưới. Sửa lệch thì cổng S20 mù đúng ca nó sinh ra để bắt.

### Việc code — phải nằm trong cùng một lượt

`backend-expert` thi công. Bốn chỗ, không tách PR:

1. `src/BE/Core/CoreAndSkill.Core.Web/Controllers/ApiControllerBase.cs` — thêm `HandleFile(Result<FileDownload>)`.
2. `src/BE/Core/CoreAndSkill.Core.Web/Http/` — lối dựng phản hồi cho `FileDownload`, dùng lại đúng chỗ đặt ba header đang có, và đóng luồng sau khi ghi.
3. `src/BE/Core/CoreAndSkill.Core.Web/Controllers/FilesController.cs` — `Download` gọi `HandleFile`, gỡ hai dòng `Response.Headers.*`.
4. `src/BE/Tests/CoreAndSkill.ArchTests/AntiforgeryMarkTests.cs` — chạy lại và **chứng minh hai điều**: ca đối chứng *thiếu dấu* vẫn đỏ (cổng còn sống), và `FilesController.Download` **không** bị liệt vào offender (cổng không đổi nghĩa). Điều thứ hai là phần dễ quên nhất, vì nó là một khẳng định *không có gì xảy ra*.
5. [`../wiki-core/be/ly-do/be-api-controller.md`](../wiki-core/be/ly-do/be-api-controller.md) §3 — khối mẫu `ApiControllerBase` ở đó hôm nay mới có ba lối, **thiếu cả `HandleExport`** đã có thật trong code từ [ADR-0064](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md). Luật **D44** đòi mẫu được sửa trong **cùng lượt** với code; lượt này sửa cả hai lối còn thiếu.

Thêm một ca test cho vòng đời luồng: client ngắt giữa chừng thì luồng vẫn đóng. Không có ca đó thì điểm 4 của Quyết định là một câu trong tài liệu.

## Liên quan

- [`0064-nhanh-tra-tep-di-qua-apicontrollerbase.md`](0064-nhanh-tra-tep-di-qua-apicontrollerbase.md) — lối `HandleExport`, và `ExportFileResult` giữ `internal`.
- [`0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md`](0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md) — cổng S20 và định nghĩa "endpoint xuất".
- [`0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md`](0050-v1-chua-quet-ma-doc-siet-zip-va-duoi-tai-xuong.md) — quy tắc đặt tên tệp tải xuống.
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) §3 · [`../contracts/files.md`](../contracts/files.md) · nợ **B16** ở [`../DEBT.md`](../DEBT.md).
