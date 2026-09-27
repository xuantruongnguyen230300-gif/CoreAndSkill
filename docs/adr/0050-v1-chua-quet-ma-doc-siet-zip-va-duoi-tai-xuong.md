---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0050 — v1 cho chia sẻ tệp khi chưa có quét mã độc; bù bằng hai biện pháp siết: không purpose nào nhận `application/zip`, và tên tải xuống mang đuôi theo kiểu hệ thống đã xác định

> **Trạng thái:** Đã chấp nhận (2026-09-21)

## Bối cảnh

Pha B4 dựng tệp đính kèm. Hợp đồng [`../contracts/files.md`](../contracts/files.md) §4 chốt *"quyền của tệp là quyền của bản ghi chủ"*: ai đọc được bản ghi thì tải được tệp đính kèm của nó. Nghĩa là tệp **được chia sẻ giữa người dùng** — trong phạm vi một đơn vị, vì bộ lọc đơn vị áp lên bảng `core.file`.

Cùng lúc đó, ba chỗ trong tài liệu nói quét mã độc là **bắt buộc trước khi** tệp được chia sẻ giữa người dùng, và cũng nói quét **chưa có ở v1**: bảng biện pháp ở §9 và bảng §Áp dụng của [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md), và bảng §Áp dụng của [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md). Ghép lại, tài liệu vừa đòi một điều kiện vừa thiết kế cho nó vi phạm, và không chỗ nào ghi rằng lệch đó là có chủ ý. Một lượt `core-reviewer` soát pha B4 bắt được mâu thuẫn này.

Ba sự thật về code lúc quyết định:

1. **Repo Core không cho ai tải tệp lên.** `purpose` do module khai qua `IFilePurposeSource`; trong `src/BE/Core/` không có bản cài nào của interface đó — chỉ có trong các project test. `src/BE/Core/CoreAndSkill.Core.Application/Files/FilePurposeDefinition.cs` ghi thẳng *"Core không khai purpose nào cho người dùng tải lên"*. Rủi ro vì vậy chỉ bắt đầu khi **module đầu tiên của một dự án hạ nguồn** khai một purpose.
2. **Kiểu tệp xác định bằng nội dung, theo danh sách cho phép hẹp.** `FileContentDetector` nhận PDF, PNG, JPEG, GIF, họ ZIP (Office suy từ tên mục, còn lại là `application/zip`) và văn bản thuần. Mọi thứ khác bị từ chối — một tệp thực thi đổi đuôi không qua được.
3. **Tên tải xuống giữ nguyên đuôi do người tải lên đặt.** `GetFileQueryHandler` trả `file.OriginalName`, `FilesController` truyền nó vào `File(…)`, và `FileNameSanitizer.Sanitize` chỉ bỏ phần đường dẫn và ký tự điều khiển. Suy ra từ đọc code, chưa thử bằng request thật: một tệp văn bản đặt tên `x.bat`, `x.vbs`, `x.hta` qua được nhận diện vì nó là văn bản thuần, và người khác tải về nhận đúng tệp mang đuôi đó; một tệp Word có macro được nhận diện là `Docx` nhưng tải về vẫn mang đuôi `.docm`.

Người dùng chốt phương án ở mục Quyết định, ngày 2026-09-21, sau khi `architect` trình bốn phương án dưới đây.

## Quyết định

Người dùng chốt: v1 **cho** chia sẻ tệp giữa người dùng khi chưa có quét mã độc, và bù bằng hai biện pháp siết do Core ép:

1. **Không purpose nào được nhận `application/zip`.** `FilePurposeCatalog` từ chối lúc khởi động mọi `FilePurposeDefinition` có `application/zip` trong `AllowedContentTypes` — tiến trình không lên, thông điệp nêu khoá purpose. Ba định dạng Office (`docx`, `xlsx`, `pptx`) **không** thuộc lệnh cấm này dù cùng họ ZIP.
2. **Tên tải xuống mang đuôi theo kiểu hệ thống đã xác định**, không theo đuôi do người tải lên đặt: phần tên gốc giữ để hiển thị, phần đuôi lấy từ `FileContentDetector.ExtensionFor(contentType)`.

Lời hoãn quét mã độc sống ở **ADR này**. Ba chỗ tài liệu kể trên trỏ về đây, không tự khai điều kiện riêng.

## Phương án đã cân nhắc và vì sao loại

### Phương án B — chấp nhận nguyên trạng, chỉ ghi nợ

**Được:** không tốn công nào. **Mất:** hai đường chở mã chạy được — tệp nén chứa tệp thực thi, và tệp văn bản mang đuôi của trình thông dịch — vẫn mở. **Vì sao loại:** đó là hai đường rẻ nhất để đưa mã chạy được tới máy người khác, và bịt chúng gần như miễn phí so với cái giá của quét thật.

### Phương án C — chặn chia sẻ tệp cho tới khi có quét

**Được:** không có rủi ro mã độc lây qua tệp đính kèm. **Mất:** tính năng đính kèm tệp của B4 mất tác dụng — tệp chỉ người tải lên đọc được thì không còn là đính kèm của một bản ghi dùng chung. **Vì sao loại:** trả toàn bộ giá trị tính năng để chặn một rủi ro chưa đo được, trong khi có biện pháp bịt phần rẻ nhất của rủi ro đó.

### Phương án D — thêm ClamAV (`clamd` chạy cạnh ứng dụng) ngay ở v1

**Được:** có quét thật, bắt được mẫu mã độc đã biết. **Mất:** một phụ thuộc ngoài vận hành mãi mãi — cập nhật chữ ký, giám sát, image lớn thêm, và một quyết định khó: máy quét sập thì cho tệp qua (mất tác dụng đúng lúc cần) hay chặn tải lên (máy quét thành điểm hỏng của cả tính năng). Chữ ký cũng không bắt được lỗ khai thác chưa công bố trong trình xem PDF hay Office. **Vì sao loại ở v1:** chưa có dự án hạ nguồn nào, chưa có purpose thật nào, nên chưa đo được rủi ro mà chi phí vận hành này mua về. Nó là đích khi một điều kiện mở lại ở dưới xảy ra — không phải phương án sai.

### Phương án E — thêm sẵn interface `IFileScanner` với bản cài không làm gì

**Được:** có sẵn chỗ để cắm máy quét sau này. **Mất:** một abstraction trong Core có **không** bản cài thật nào — dấu hiệu Core phình to ở [`../kien-truc-core-module.md`](../kien-truc-core-module.md). Tệ hơn, một bản cài không làm gì tạo tín hiệu *"tệp đã đi qua bước quét"* trong khi chưa tệp nào được quét. **Vì sao loại:** khi máy quét thật về, lúc đó mới biết hình dạng interface cần có — gọi đồng bộ hay bất đồng bộ, cách lưu trạng thái quét, cách xử lý tệp chờ quét. Đoán trước bây giờ là đoán sai.

## Hệ quả

### Tích cực

- Tài liệu hết tự mâu thuẫn: một chỗ khai lời hoãn, ba chỗ trỏ về.
- Không thêm thành phần nào phải cài, giám sát, nâng cấp.
- Hai đường chở mã chạy được rẻ nhất bị Core bịt, không phụ thuộc module nào nhớ.
- Biện pháp 1 ép lúc khởi động: một module khai sai thì tiến trình không lên, không có cửa sổ nào tệp nén lọt qua trong lúc chạy.

### Tiêu cực

- **PDF, tệp Office và ảnh độc vẫn tới được người dùng khác trong cùng đơn vị.** Mã khai thác lỗ của trình xem PDF hay Office đi qua nguyên vẹn. Lớp bảo vệ duy nhất còn lại là phần mềm trên máy trạm của người mở tệp — thứ Core không kiểm soát và không biết có tồn tại hay không.
- **Module nào thật sự cần nhận tệp nén thì bị chặn**, và không có đường ngoại lệ theo purpose. Muốn mở thì phải có quét — tức phải lật ADR này.
- **Ba định dạng Office vẫn là họ ZIP** và vẫn được nhận. Một tệp `.docm` được nhận diện là `Docx` và, sau biện pháp 2, tải về mang đuôi `.docx`; Office có chịu mở macro trong tệp đổi tên như vậy hay không là điều **chưa kiểm** lúc quyết định.
- **Người dùng mất đuôi tệp gốc của mình** khi đuôi đó không khớp kiểu đã xác định — một tệp văn bản đặt tên `.md`, `.log` hay `.json` tải về thành `.txt`.
- **Dữ liệu sinh ra trong thời gian không quét là nợ có lãi.** Mọi tệp lưu từ bây giờ tới lúc có quét đều chưa từng được quét, và con số đó chỉ tăng — xem mục rút lui ở dưới.
- Người chấp nhận rủi ro ở từng dự án hạ nguồn **chưa có tên**. ADR này chốt cho Core; mỗi dự án hạ nguồn mở purpose đầu tiên là đang thừa hưởng lời chấp nhận đó mà không ai ở dự án đó ký vào.

## Điều kiện mở lại

Xét lại ADR này — và nhiều khả năng chuyển sang phương án D — khi **một** trong các điều kiện sau xảy ra:

1. Một module xin nhận `application/zip`.
2. Bản cài được mở ra ngoài mạng nội bộ.
3. Tệp đến từ nguồn bên ngoài tổ chức: người dân, đối tác, hộp thư điện tử.
4. Có tài khoản khách, hoặc người ngoài đơn vị đọc được bản ghi có tệp đính kèm.
5. Một sự cố mã độc lây qua tệp đính kèm — kèm một mục ở [`../audit/`](../audit/).

## Rút lui và việc bắt buộc khi thêm quét

- **Gỡ biện pháp 1:** xoá một điều kiện trong `FilePurposeCatalog` và test canh nó. Không chạm dữ liệu.
- **Gỡ biện pháp 2:** trả phần đuôi về tên gốc. Không chạm dữ liệu — tên gốc vẫn lưu nguyên ở cột `original_name`.
- **Thêm quét:** không được tuyên bố *"đã có quét"* ngay khi máy quét nhận tệp mới. Mọi tệp đã lưu trước đó chưa từng được quét, nên **trước** tuyên bố đó phải chạy một lượt quét lùi toàn bộ kho — liệt kê được qua `IFileStorage.ListAsync` đã có — và quyết định trước cách xử lý tệp bị gắn cờ trong kho cũ: cách ly, báo chủ bản ghi, hay xoá. Tài liệu chỉ được lật nhãn *"quét mã độc"* sang có thật sau khi lượt quét lùi xong.

## Dấu hiệu quyết định bắt đầu sai

- Một module khai purpose chứa `application/zip` và phải sửa lại — tần suất việc đó tăng là điều kiện 1 đang tới gần.
- Người dùng than tệp tải về đổi đuôi — biện pháp 2 đang chạm vào một nhu cầu thật chưa biết.
- Một dự án hạ nguồn bắt đầu nhận tệp từ ngoài tổ chức mà không ai mở lại ADR này.

## Luật sinh ra

Hai luật ở [`../RULES.md`](../RULES.md) §6: **S15** (không purpose nào nhận `application/zip`) và **S16** (tên tải xuống mang đuôi theo kiểu đã xác định).

## Liên quan

- [`../contracts/files.md`](../contracts/files.md) §4 — quyền của tệp là quyền của bản ghi chủ
- [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md) §9, §13
- [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §9
- [`../kien-truc-core-module.md`](../kien-truc-core-module.md) — dấu hiệu Core phình to, lý do loại phương án E
