---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0003 — Domain và Application trả `Result<T>`, không ném exception cho lỗi nghiệp vụ

> **Trạng thái:** Đã chấp nhận (2026-09-08)

## Bối cảnh

Ở dự án tiền nhiệm tồn tại **hai cơ chế lỗi song song**: tầng Domain ném exception nghiệp vụ, tầng Application trả về một envelope kết quả. Vì hai cơ chế không nói chung một ngôn ngữ, phải có một cầu nối — một pipeline behavior bắt exception rồi dựng envelope.

Cầu nối đó được dựng bằng **phản chiếu**: tra phương thức factory theo tên rồi gọi nó, với danh sách kiểu tham số hardcode ở một file khác. Bẫy đã nổ thật trên Production: khi chữ ký phương thức factory được thêm tham số, phép tra khớp chữ ký đầy đủ nên trả về null, và toán tử khẳng-định-không-null biến null đó thành `NullReferenceException`. Kết quả: **mọi lỗi nghiệp vụ biến thành `NullReferenceException`**, và chỉ lộ ra ở Production vì hỏng đúng nhánh xử lý lỗi.

Chi tiết đầy đủ ở [`../audit/2026-09-05-reflection-envelope.md`](../audit/2026-09-05-reflection-envelope.md).

Họ đã vá triệu chứng. Nguyên nhân gốc — hai cơ chế lỗi cần một cầu nối — vẫn còn.

## Quyết định

`Core.Domain` và `Core.Application` **không ném exception cho lỗi nghiệp vụ**. Chúng trả `Result<T>`.

- `Result<T>` mang hoặc giá trị, hoặc một `Error(Code, MessageTemplate, ErrorType)`.
- `ErrorType` là tập đóng: `Validation` · `NotFound` · `Conflict` · `Forbidden` · `BusinessRule`.
- Ánh xạ `ErrorType` → HTTP status nằm ở **đúng một chỗ** trong `Core.Web`, viết bằng khớp mẫu (pattern matching), **không dùng phản chiếu**.
- Exception chỉ dành cho **lỗi ngoài dự kiến**: mất kết nối DB, cấu hình thiếu, lập trình sai. Những lỗi đó không có mã nghiệp vụ và không cần FE hiểu.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Lai: exception ở Domain, envelope ở Application

Đây chính là cách dự án tiền nhiệm làm.

**Được:** entity method viết ngắn — ném là thoát ngay, không phải trả Result ngược qua từng lớp gọi.

**Vì sao loại:** cái giá không nằm ở entity method mà nằm ở **cầu nối**. Hai cơ chế lỗi bắt buộc phải có một chỗ dịch giữa chúng, và chỗ dịch đó:

- phải biết mọi loại exception nghiệp vụ và mọi hình dạng envelope, tức là nó tập trung toàn bộ tri thức về lỗi vào một điểm mà compiler không kiểm được;
- nằm ở **nhánh xử lý lỗi** — nhánh ít được chạy nhất trong test thủ công, nên hỏng ở đó lộ ra muộn nhất;
- ở dự án tiền nhiệm đã được dựng bằng phản chiếu, và đã nổ trên Production.

Bỏ một trong hai cơ chế thì cầu nối biến mất hoàn toàn. Đó là lý do chính.

### Phương án B — Exception thuần: cả Domain lẫn Application đều ném

**Được:** một cơ chế duy nhất, không cầu nối, entity method ngắn.

**Vì sao loại — ba lý do:**

- **Chữ ký hàm không nói gì về tập lỗi.** Một hàm trả `Task<User>` có thể ném năm loại exception khác nhau và không có gì trong chữ ký báo điều đó. Người gọi phải đọc thân hàm — hoặc đọc thân của mọi hàm nó gọi — mới biết phải bắt gì. `Result<T>` đưa khả năng thất bại vào **kiểu trả về**, nên compiler nhắc.
- **FE không biết trước lỗi nào có thể xảy ra.** Hợp đồng API ở [`../contracts/`](../contracts/) phải liệt kê được tập mã lỗi của từng endpoint. Với exception, tập đó không suy ra được từ code mà phải nhớ bằng tay — và cái nhớ bằng tay thì lệch.
- **Exception cho luồng nghiệp vụ tốn chi phí thật.** Ném và bắt kéo theo unwinding và dựng stack trace. Với lỗi hiếm thì không sao; với validation trong luồng nhập liệu hàng loạt thì lỗi là chuyện thường ngày chứ không phải ngoại lệ.

### Phương án C — Result ở Application, exception ở Domain, không có cầu nối tự động

**Được:** entity method vẫn ngắn.

**Vì sao loại:** vẫn là hai cơ chế; chỉ chuyển việc dịch từ một chỗ tập trung sang **mọi handler**. Từ một chỗ dễ hỏng thành nhiều chỗ dễ hỏng, và không chỗ nào bị kiểm.

## Hệ quả

### Tích cực

- **Cầu nối exception → envelope biến mất hoàn toàn**, cùng với cả lớp lỗi mà nó sinh ra.
- **Ánh xạ lỗi → HTTP viết bằng khớp mẫu**, nên thêm một `ErrorType` mà quên xử lý là lỗi biên dịch, không phải lỗi lúc chạy. Luật R5 ở [`../RULES.md`](../RULES.md) cấm phản chiếu trên đúng đường này.
- **Tập lỗi của một use case đọc được từ chữ ký và từ catalog**, nên hợp đồng API sinh ra từ code chứ không từ trí nhớ.

### Tiêu cực — cái giá thật, phải ghi thẳng

- **Entity method dài dòng hơn.** Nơi trước đây một dòng ném là xong, nay phải dựng `Result` thất bại và trả về. Một method kiểm ba điều kiện sẽ có ba nhánh trả về thay vì ba dòng ném.
- **Phải truyền `Result` ngược lên từng tầng.** Handler gọi domain method, kiểm thất bại, trả tiếp lên. Nếu một handler gọi ba domain method thì có ba lần kiểm-và-trả — lặp lại về mặt thị giác, và người viết sẽ thấy phiền.
- **Quên kiểm `Result` là lỗi im lặng.** Ném exception thì bỏ qua không được; trả `Result` mà không đọc trạng thái thất bại thì chương trình chạy tiếp như không có gì. Đây là điểm yếu thật của phương án này, và cách bù duy nhất là quy ước cộng review — không có cổng nào bắt được nó một cách tổng quát.
- **Hai thế giới trong một codebase.** Thư viện ngoài vẫn ném exception. Ranh giới giữa "lỗi nghiệp vụ trả Result" và "lỗi hạ tầng ném exception" phải được giữ có kỷ luật, nếu không sẽ có người bọc mọi thứ vào `Result` kể cả lỗi mất kết nối DB — và khi đó `Result` trở thành cách nuốt lỗi hạ tầng.

## Liên quan

- [`../audit/2026-09-05-reflection-envelope.md`](../audit/2026-09-05-reflection-envelope.md) — sự cố sinh ra quyết định này
- [`../RULES.md`](../RULES.md) — luật R1 tới R8
- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) — hợp đồng `Result<T>` đầy đủ và cách truyền ngược lên
- [`../quy-uoc/be-api-controller.md`](../quy-uoc/be-api-controller.md) — ánh xạ `Result` → HTTP
