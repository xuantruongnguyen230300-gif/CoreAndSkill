---
kind: luat
scope: core
verified: chua-doi-chieu
---

# 01. Một Core tầm trung gồm những gì — Nhóm A và Nhóm B

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Chưa có `src/`. File này mô tả thứ Core phải trở thành.
>
> **Đây là file agent đọc TRƯỚC KHI tự đề xuất thêm một abstraction mới.** Mục [§5 Áp dụng](#5-áp-dụng--core-này-có-gì-cố-ý-thiếu-gì) liệt kê những thứ đã được cân nhắc và **cố ý loại**, kèm lý do. Đề xuất lại một thứ nằm trong bảng đó mà không nói được điều kiện đã đổi là làm mất thời gian của mọi người.

---

## 0. Cách phân nhóm

| Nhóm | Định nghĩa | Hệ quả |
| --- | --- | --- |
| **A** | Thiếu nó thì Core **không dùng được** — dự án đầu tiên dựng lên đã phải tự chế lại | Làm ngay từ đầu, không hoãn |
| **B** | Nên có. Thêm khi có **nhu cầu thật** đã quan sát được | Làm sớm là trả chi phí vĩnh viễn cho một lợi ích chưa tồn tại |

Phép thử để xếp một thứ vào Nhóm A:

> **Nếu bỏ nó ra, dự án thứ nhất và dự án thứ hai có tự viết lại gần như y hệt nhau không?**

Có → Nhóm A (nó thuộc Core theo đúng định nghĩa Core). Không → Nhóm B hoặc thuộc module.

Chi phí của một thành phần Nhóm B thêm quá sớm không phải là thời gian viết nó. Nó là **thuế vĩnh viễn**: một abstraction nữa để người mới phải hiểu, một chỗ nữa có thể hỏng, một câu hỏi *"file này bỏ vào đâu"* nữa cho mọi file sinh sau, và — nặng nhất — một API sai hình dạng bị đóng băng trước khi có ca dùng thật để chỉnh nó cho đúng.

Ngưỡng nâng một thứ lên Core: **từ hai module trở lên cần nó**. Xem [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §4.2.

---

## 1. Nhóm A — thiếu là Core không dùng được

### A1. Kết quả thao tác tường minh — `Result<T>` và catalog mã lỗi

**Giải quyết gì.** Một handler có hai loại đầu ra khác hẳn nhau: *thất bại nghiệp vụ* (email trùng, số dư không đủ — hoàn toàn nằm trong dự kiến) và *lỗi ngoài dự kiến* (mất kết nối DB). Trộn hai thứ vào cùng một cơ chế thì không phân biệt được, và cái giá trả ở tầng biên.

**Dấu hiệu thiếu.** Handler ném exception cho lỗi nghiệp vụ; tầng web phải bắt exception để dựng envelope; và để biết exception nào là nghiệp vụ, người ta viết một cầu nối. Ở dự án tiền nhiệm, cầu nối đó dựng bằng reflection với danh sách kiểu hardcode: khi chữ ký đổi, tra cứu method trả null và **mọi lỗi nghiệp vụ biến thành `NullReferenceException`** — hỏng đúng nhánh lỗi, không lỗi biên dịch, chỉ lộ ra trên Production. Xem [`../../audit/2026-09-05-reflection-envelope.md`](../../audit/2026-09-05-reflection-envelope.md).

**Chi phí thêm.** Mọi handler phải trả `Result`, mọi lời gọi phải kiểm nhánh. Đó là chi phí thật và nó không nhỏ — đổi lại, nhánh lỗi trở thành thứ compiler nhìn thấy.

📖 Quyết định: [`../../adr/0003-result-thuan.md`](../../adr/0003-result-thuan.md) · Thi công: [`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md)

### A2. Envelope HTTP thống nhất và ánh xạ `Result` → HTTP tại đúng một chỗ

**Giải quyết gì.** Client cần **một** hình dạng phản hồi cho mọi endpoint — thành công, lỗi nghiệp vụ, lỗi validation, lỗi hệ thống. Không có nó, mỗi controller tự dựng một kiểu và FE phải viết N nhánh phân tích.

**Dấu hiệu thiếu.** FE có hàm `parseError` với chuỗi `if` dò dần từng hình dạng. Đó là hoá đơn cho việc BE không có envelope.

**Chi phí thêm.** Một base controller và một bảng ánh xạ. Rẻ. Điều kiện: bảng ánh xạ nằm **đúng một chỗ** và **không dùng reflection** — luật R4, R5 ở [`../../RULES.md`](../../RULES.md).

📖 [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md)

### A3. Entity nền — định danh, trường audit, soft delete

**Giải quyết gì.** Mọi bảng nghiệp vụ đều cần biết ai tạo, ai sửa lần cuối, lúc nào, và đã bị xoá mềm chưa. Viết tay ở từng entity thì sẽ có entity quên.

**Dấu hiệu thiếu.** Cột `CreatedBy` có ở bảng này, không có ở bảng kia. Một truy vấn quên lọc bản ghi đã xoá mềm và dữ liệu "đã xoá" hiện lại trên một màn hình.

**Chi phí thêm.** Rẻ, nhưng có bẫy: soft delete kéo theo query filter, kéo theo unique index phải tính tới cột xoá mềm, kéo theo hành vi lạ khi join. Đọc [`10-data-retention.md`](10-data-retention.md) **trước khi** bật soft delete cho một bảng.

### A4. Đơn vị công việc và biên transaction ở tầng request

**Giải quyết gì.** Một command chạm nhiều bảng phải hoặc thành công hết, hoặc không đổi gì. Nếu mỗi repository tự `SaveChanges`, sẽ có ca ghi được nửa chừng.

**Dấu hiệu thiếu.** Trong log có bản ghi cha tồn tại mà bản ghi con thì không. Không ai tìm ra nguyên nhân vì mỗi lần một kiểu.

**Chi phí thêm.** Một pipeline behavior mở transaction cho command và commit khi handler trả thành công. Đây là một trong hai behavior duy nhất của Core — xem [`../../adr/0006-pipeline-behavior.md`](../../adr/0006-pipeline-behavior.md).

> Ở dự án tiền nhiệm **thiếu đúng behavior này**: có Validation, có ExceptionHandling, không có Transaction. Đó là lý do nó nằm trong Nhóm A ở đây chứ không phải "nên có".

### A5. Validation ở biên, chạy trước handler

**Giải quyết gì.** Handler không nên bắt đầu bằng mười dòng kiểm tra null và độ dài. Việc đó thuộc về một lớp chạy trước, và kết quả phải ra được dạng lỗi-theo-từng-ô-nhập để FE gắn vào đúng ô.

**Dấu hiệu thiếu.** Lỗi nhập liệu trả về dạng một câu văn duy nhất, FE không biết gắn vào ô nào, người dùng phải tự đoán.

**Chi phí thêm.** Một behavior và một validator cho mỗi command. Điều kiện bắt buộc: **mọi validator phải được đăng ký** — quên một cái thì nó im lặng không chạy, đúng kiểu hỏng không ai thấy. Luật A11 ở [`../../RULES.md`](../../RULES.md) canh việc này.

### A6. Danh tính và phiên đăng nhập

**Giải quyết gì.** Hash mật khẩu đúng cách, khoá tài khoản sau nhiều lần sai, token đặt lại mật khẩu, quản lý phiên. Đây là nhóm việc **không được tự viết** — sai một chi tiết là lỗ hổng.

**Dấu hiệu thiếu.** Có ai đó trong repo viết hàm băm mật khẩu.

**Chi phí thêm.** Dùng ASP.NET Core Identity kèm phiên cookie. Chi phí thật nằm ở chỗ Identity kéo theo một bộ bảng và một mô hình dữ liệu mình không chọn — phải khoanh vùng nó lại. Xem [`02-identity-auth.md`](02-identity-auth.md).

### A7. Phân quyền theo permission, không theo tên role

**Giải quyết gì.** Mỗi dự án có bộ vai trò khác nhau. Nếu Core biết tên vai trò, Core không mang đi được.

**Dấu hiệu thiếu.** Một hằng số tên vai trò nằm trong Core. Sang dự án thứ hai, hằng số đó vô nghĩa nhưng vẫn phải giữ vì có chỗ đang kiểm nó.

**Chi phí thêm.** Thêm một tầng gián tiếp (vai trò → quyền) và một câu hỏi thật phải trả lời: *tài khoản đầu tiên lấy toàn quyền bằng cách nào khi không có vai trò cứng*. Câu trả lời ở [`02-identity-auth.md`](02-identity-auth.md).

📖 [`../../adr/0005-permission-based.md`](../../adr/0005-permission-based.md)

### A8. Người dùng hiện tại như một interface

**Giải quyết gì.** Tầng Application cần biết ai đang thao tác — để ghi trường audit, để lọc dữ liệu — mà **không** được biết `HttpContext` tồn tại.

**Dấu hiệu thiếu.** `IHttpContextAccessor` xuất hiện trong handler. Từ đó handler không unit-test được nếu không dựng giả một `HttpContext`.

**Chi phí thêm.** Một interface ở Application, một cài đặt ở Web. Rẻ, và nó là thứ giữ luật A2 ở [`../../RULES.md`](../../RULES.md) không bị vi phạm.

### A9. Cấu hình có kiểm tra lúc khởi động

**Giải quyết gì.** Thiếu một khoá cấu hình phải làm app **không khởi động được**, chứ không phải chạy bình thường rồi hỏng lúc 2 giờ sáng ở một nhánh code hiếm.

**Dấu hiệu thiếu.** Một `NullReferenceException` trên Production truy về một khoá `appsettings` chưa ai điền ở môi trường đó.

**Chi phí thêm.** Gần bằng không. Luật A8 ở [`../../RULES.md`](../../RULES.md) canh việc mọi nhóm cấu hình bắt buộc đều có đường kiểm tra lúc khởi động.

### A10. Log có cấu trúc và một mã lần gọi xuyên suốt

**Giải quyết gì.** Khi người dùng báo lỗi, phải tìm được **đúng** chuỗi log của lần gọi đó, xuyên qua mọi tầng.

**Dấu hiệu thiếu.** Điều tra một sự cố bắt đầu bằng việc lọc log theo giờ và đoán.

**Chi phí thêm.** Nhỏ. Có bẫy về thứ **không được** log — xem [`07-observability.md`](07-observability.md).

### A11. Health check phân biệt sống và sẵn sàng

**Giải quyết gì.** Bộ điều phối cần hai câu trả lời khác nhau: *process còn sống không* và *đã sẵn sàng nhận request chưa*. Trộn hai câu này là cách tạo ra vòng lặp khởi động lại.

**Dấu hiệu thiếu.** App bị khởi động lại liên tục vì health check gọi DB mà DB đang chậm.

**Chi phí thêm.** Hai endpoint. Xem [`07-observability.md`](07-observability.md).

### A12. Migration và cơ chế phát hiện DB lệch model

**Giải quyết gì.** Schema phải đi cùng code. Và khi DB thiếu migration, app phải **từ chối khởi động** thay vì chạy rồi hỏng ở truy vấn đầu tiên chạm cột chưa tồn tại.

**Dấu hiệu thiếu.** Một lỗi "column does not exist" trên môi trường thử nghiệm sau khi triển khai.

**Chi phí thêm.** Nhỏ, nhưng quyết định **ai sở hữu migration** thì không nhỏ — xem [`13-core-data-migration.md`](13-core-data-migration.md) và [`../../database/migration-policy.md`](../../database/migration-policy.md). Luật E8 ở [`../../RULES.md`](../../RULES.md) canh việc app từ chối khởi động khi còn migration chưa áp.

### A13. Menu động theo quyền

**Giải quyết gì.** Người dùng chỉ nhìn thấy thứ mình có quyền dùng. Nếu menu hardcode ở FE, mỗi lần đổi quyền phải sửa và triển khai lại FE.

**Dấu hiệu thiếu.** FE có một mảng menu tĩnh kèm chuỗi `if` kiểm vai trò.

**Chi phí thêm.** Một bảng, một endpoint, và một cám dỗ phải cưỡng lại: biến metadata thành một ngôn ngữ lập trình thứ hai. Ranh giới ở [`03-metadata-driven-design.md`](03-metadata-driven-design.md).

### A14. Lớp bảo mật biên — CSRF, CORS, rate limit

**Giải quyết gì.** Phiên cookie tự động được trình duyệt gửi kèm, nên phải có antiforgery. Kiến trúc tách FE/BE nên phải có allowlist nguồn gọi. Endpoint đăng nhập nên phải có giới hạn tần suất.

**Dấu hiệu thiếu.** Bất kỳ endpoint ghi nào nhận được request từ một trang khác mà không cần token. Hoặc: một script thử hàng nghìn mật khẩu mà không gặp trở ngại nào.

**Chi phí thêm.** Trung bình, và có bẫy đã xảy ra thật (đặt trùng tên cookie). Xem [`09-security-beyond-auth.md`](09-security-beyond-auth.md).

### A15. Bộ test kiến trúc

**Giải quyết gì.** Mọi luật ranh giới ở [`../../RULES.md`](../../RULES.md) chỉ là câu văn cho tới khi có test canh nó.

**Dấu hiệu thiếu.** Tài liệu nói "Application không phụ thuộc EF Core" và trong Application có `using Microsoft.EntityFrameworkCore`.

**Chi phí thêm.** Một project test và một kỷ luật: **mọi detector phải có test kiểm chính detector đó** (luật T1). Không có phần sau, bộ ArchTest chỉ tạo cảm giác được bảo vệ. Xem [`04-testing-strategy.md`](04-testing-strategy.md).

---

## 2. Nhóm B — nên có, thêm khi có nhu cầu thật

Mỗi dòng dưới đây kèm **ngưỡng kích hoạt**: sự kiện quan sát được khiến nó chuyển từ "chưa cần" sang "cần".

| Thành phần | Giải quyết gì | Ngưỡng kích hoạt |
| --- | --- | --- |
| **Bộ lập lịch job nền** | Việc chạy định kỳ, dọn dẹp | Có từ hai job định kỳ trở lên; một job thì một hosted service là đủ |
| **Màn hình ĐỌC nhật ký kiểm toán** | Tra cứu ai đã đổi gì, lúc nào. *Bảng `core.audit_log` và đường **ghi** vào nó thuộc v1 — [`10-data-retention.md`](10-data-retention.md) §5* | Có yêu cầu tuân thủ, hoặc có tranh chấp dữ liệu thật đã xảy ra |
| **Feature flag** | Bật/tắt tính năng không cần triển khai lại | Có nhu cầu bật dần theo nhóm người dùng |
| **Metadata cột lưới / form** | Cấu hình hiển thị không cần sửa code | Từ ba màn hình lưới trở lên có cùng nhu cầu tuỳ biến cột |
| **Cache phân tán** | Giảm tải DB, chia sẻ state giữa nhiều instance | Đã **đo** được điểm nghẽn, hoặc đã chạy nhiều instance |
| **Message broker** | Tách nhịp giữa các thành phần, chịu tải đỉnh | Đã tách process, hoặc Outbox không còn đủ |
| **Multi-tenant** | Nhiều tổ chức độc lập trên một bản cài | Có tổ chức thứ hai dùng chung một lần triển khai — **điều kiện này đã thoả ở Core này**, xem §Áp dụng |
| **Tìm kiếm toàn văn** | Tìm theo nội dung, không chỉ theo khoá | Truy vấn `LIKE '%…%'` bắt đầu chậm trên dữ liệu thật |
| **Phiên bản API** | Client cũ không vỡ khi hợp đồng đổi | Có client bên thứ ba không triển khai cùng nhịp |
| **Xuất telemetry ra hệ ngoài** | Truy vết xuyên dịch vụ | Đã có từ hai dịch vụ trở lên gọi nhau |
| **Bản in theo mẫu (PDF)** | Kết xuất văn bản có bố cục cố định để in và lưu trữ | Có nghiệp vụ cần bản in theo mẫu quy định, không phải chỉ cần xuất dữ liệu |
| **Cấu hình theo đơn vị** | Mỗi cơ quan hiển thị và tính khác nhau mà không sửa code | Có đơn vị thứ hai muốn khác đơn vị thứ nhất |
| **Sinh mã nghiệp vụ** | Số phiếu, số văn bản do hệ thống cấp, không trùng | Nghiệp vụ đầu tiên cần mã do hệ thống sinh |

**Cách đọc bảng này:** ngưỡng là **sự kiện đã xảy ra**, không phải dự đoán. "Sau này chắc sẽ nhiều người dùng" không phải ngưỡng. "Tuần trước DB đạt 90% CPU vào giờ cao điểm" là ngưỡng.

---

## 3. Ba thứ trông giống thành phần Core nhưng là bẫy

| Thứ | Vì sao là bẫy |
| --- | --- |
| **Một project tên `Common`** | Không có tiêu chí nào để **từ chối** thứ gì cả — cái gì cũng lý luận được là "dùng chung". Nó biến thành ngăn kéo rác trong vòng vài tháng. Đã bị loại có chủ đích, xem [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §2.3 |
| **Repository tổng quát cho mọi entity** | Nó gói lại một thứ (`DbSet`) vốn đã là abstraction, rồi lộ ra `IQueryable` để dùng được — tức là không gói gì cả, chỉ thêm một tầng. Repository chỉ đáng có khi nó biểu đạt **ý định nghiệp vụ**, không phải khi nó biểu đạt CRUD |
| **"Engine" cấu hình tổng quát** | Metadata đủ tổng quát sẽ trở thành một ngôn ngữ lập trình thứ hai: không có compiler, không có debugger, không có test. Ranh giới cụ thể ở [`03-metadata-driven-design.md`](03-metadata-driven-design.md) |

---

## 4. Trình tự dựng — thứ tự phụ thuộc, không phải thứ tự ưu tiên

```text
A3 Entity nền ─┬─> A4 Transaction ─> A12 Migration
               └─> A1 Result ─> A2 Envelope ─> A5 Validation
A6 Danh tính ──> A7 Permission ──> A13 Menu động
                     │
A8 Người dùng hiện tại┘
A9 Cấu hình ─> A10 Log ─> A11 Health check
A15 ArchTest — dựng SONG SONG, không để cuối
```

**A15 không được để cuối.** Một bộ ArchTest viết sau khi code đã xong sẽ được viết cho khớp code hiện có, tức là nó xác nhận hiện trạng thay vì canh luật. Viết luật trước, code sau.

---

## 5. §Áp dụng — Core này có gì, cố ý thiếu gì

> 📐 Bảng dưới là **đích đến của giai đoạn 2**, không phải hiện trạng. Chưa có `src/` để đối chiếu.

### 5.1 Nhóm A — Core này sẽ có

| Thành phần | Ghi chú thi công |
| --- | --- |
| A1 `Result` + catalog mã lỗi | Result **thuần** — Domain và Application không ném exception cho lỗi nghiệp vụ |
| A2 Envelope + ánh xạ HTTP | Ánh xạ tại đúng một chỗ, **cấm reflection** |
| A3 Entity nền + soft delete | Query filter đặt tên, canh bằng ArchTest |
| A4 Transaction | Một trong **hai** pipeline behavior duy nhất |
| A5 Validation | Behavior thứ hai |
| A6 Danh tính | ASP.NET Core Identity, phiên cookie, kiểu Identity khoanh trong `Core.Infrastructure` |
| A7 Permission | Không có hằng số vai trò trong Core |
| A8 Người dùng hiện tại | Interface ở Application, cài đặt ở Web |
| A9 Cấu hình kiểm lúc khởi động | Mọi nhóm cấu hình bắt buộc |
| A10 Log + mã lần gọi | Serilog, log có cấu trúc |
| A11 Health check | Tách sống / sẵn sàng |
| A12 Migration | **Core sở hữu migration schema `core`** — đảo ngược so với dự án tiền nhiệm |
| A13 Menu động | Theo quyền |
| A14 CSRF / CORS / rate limit | Antiforgery hai lớp |
| A15 ArchTest | Kèm **meta-test cho từng detector** |
| A16 Lưu file | Chốt 2026-09-10 là **thuộc v1**. Interface lưu trữ ở Application, phục vụ qua endpoint có kiểm quyền — [`14-file-storage.md`](14-file-storage.md) |
| A17 Nhập / xuất dữ liệu | Chốt 2026-09-10 là **thuộc v1**. Xuất theo bộ lọc đang xem, quyền xuất riêng, ghi nhật ký kiểm toán — [`15-import-export.md`](15-import-export.md) |
| A18 Thông báo và Outbox | Chốt 2026-09-10 là **thuộc v1**. Outbox đi kèm bắt buộc: nó là đường mà sự kiện nghiệp vụ tới được kênh gửi — [`12-notifications.md`](12-notifications.md) §1.2 |

### 5.2 Cố ý CHƯA làm ở v1 — và vì sao

| Thành phần | Vì sao chưa làm | Điều kiện để làm |
| --- | --- | --- |
| **Cache phân tán (Redis)** | Chưa có số đo nào chỉ ra điểm nghẽn. Thêm cache khi chưa đo là thêm một nguồn dữ liệu cũ và một bài toán vô hiệu hoá cache — bài toán khó nhất trong nhóm này. Chi tiết: [`11-performance-caching.md`](11-performance-caching.md) | Đã đo và xác định được truy vấn nóng cụ thể; **hoặc** chạy nhiều instance và cần chia sẻ state |
| **Message broker** | Modular monolith một process: mọi module nằm cùng tiến trình, cùng DB. Broker thêm một hạ tầng phải vận hành, phải giám sát, phải xử lý thư chết — để đổi lấy một sự tách nhịp mà Outbox trong DB đã cho ở quy mô này. Chi tiết: [`05-cross-module-consistency.md`](05-cross-module-consistency.md) | Tách một module thành process riêng; **hoặc** Outbox không theo kịp tải; **hoặc** cần fan-out tới hệ ngoài |
| **Multi-tenant** | **Không hoãn — trong phạm vi v1**: nhiều cơ quan dùng chung một bản cài. Cột phân biệt, bộ lọc truy vấn toàn cục, cách ly tuyệt đối. Xem [`../../adr/0013-multi-tenant.md`](../../adr/0013-multi-tenant.md) và [`17-multi-tenant.md`](17-multi-tenant.md) | — đã trong phạm vi |
| **Pipeline behavior cho caching** | Cache ở tầng behavior là chỗ **tệ nhất** để bắt đầu: nó cache theo hình dạng request, không theo ngữ nghĩa dữ liệu, nên vô hiệu hoá đúng lúc là bất khả thi. Cache khi cần thì cache ở đúng nơi biết dữ liệu nào vừa đổi | Không có — hướng này bị loại, không hoãn. Cache sẽ làm ở tầng truy vấn cụ thể |
| **Pipeline behavior cho logging và đo hiệu năng** | Trùng với thứ hạ tầng đã cho sẵn (middleware log request, telemetry). Thêm behavior là log hai lần cùng một thứ ở hai định dạng khác nhau. Xem [`../../adr/0006-pipeline-behavior.md`](../../adr/0006-pipeline-behavior.md) | Có nhu cầu đo **riêng ở tầng handler** mà tầng HTTP không thấy được |
| **Bộ lập lịch job nền** | Chưa có job định kỳ nào. Tiến trình phát Outbox — khi Outbox được bật — là hosted service chạy liên tục, không phải job định kỳ | Từ hai job định kỳ trở lên |
| **Phiên bản API** | Chưa có client bên thứ ba | Có client không triển khai cùng nhịp với BE |

### 5.3 Cách dùng bảng 5.2 khi review

Một finding dạng *"Core thiếu cache"* hoặc *"Core nên có message broker"* **không hợp lệ** nếu không kèm bằng chứng rằng điều kiện ở cột thứ ba đã xảy ra. Bảng này tồn tại để chuyển tranh luận từ *"nên có cho chuẩn"* sang *"đã đủ điều kiện chưa"*.

Ngược lại, một thành phần Nhóm A vắng mặt **luôn** là finding — trừ khi có ADR nói khác.

---

## 6. Thêm một thành phần mới vào Core — thủ tục

1. **Kiểm ngưỡng hai module.** Một module cần thì để ở module đó.
2. **Kiểm bảng 5.2.** Nếu nó nằm trong đó, phải chứng minh điều kiện kích hoạt đã xảy ra.
3. **Viết ADR** ở [`../../adr/`](../../adr/) — bối cảnh, phương án đã loại, hệ quả. Khuôn ở [`08-adr-practice.md`](08-adr-practice.md).
4. **Bổ sung luật vào** [`../../RULES.md`](../../RULES.md) kèm cột "ép bằng gì". Chưa có cổng thì dòng đó thuộc mục nợ, không được để trống cột.
5. **Cập nhật file này** — thêm vào Nhóm A hoặc Nhóm B, kèm dấu hiệu thiếu và chi phí thêm.

Bỏ bước 3 và 4 là cách một Core phình to mà không ai nhận ra: mỗi lần thêm đều hợp lý riêng lẻ, và tổng thể thì không mang đi đâu được nữa.

### 6.1 Ba dấu hiệu sớm của một Core đang phình

| Dấu hiệu | Vì sao nguy hiểm |
| --- | --- |
| **Core chứa một khái niệm chỉ một module hiểu** | Dự án thứ hai vẫn phải mang theo nó, vẫn phải bảo trì nó, và vẫn phải giải thích cho người mới nó là gì |
| **Một thành phần Core có tham số "chế độ" để phục vụ hai module khác nhau** | Đó là hai thành phần bị ghép lại. Mỗi lần sửa cho module này là một lần có nguy cơ làm hỏng module kia |
| **Sửa một module bắt buộc phải sửa Core** | Ranh giới đã thủng. Core lẽ ra chỉ đổi khi *cơ chế* đổi, không phải khi *nghiệp vụ* đổi |

Ba dấu hiệu này xuất hiện **trước** khi hậu quả xuất hiện, nên chúng đáng được kiểm định kỳ chứ không đợi tới lúc mang Core sang dự án mới mới phát hiện.

### 6.2 Phép thử cuối cùng

> **Mang Core sang một dự án hoàn toàn khác lĩnh vực. Có phải xoá dòng nào không?**

Phải xoá, dù chỉ một dòng, nghĩa là dòng đó không thuộc Core. Đây là phép thử duy nhất không đánh lừa được — mọi lý lẽ *"cái này dùng chung được mà"* đều gãy trước nó.

---

## 7. Dùng file này khi audit

| Tình huống | Cách xử |
| --- | --- |
| Thấy một thành phần **Nhóm A** vắng mặt | Finding, trừ khi có ADR nói khác |
| Thấy một thành phần **Nhóm B** vắng mặt | **Không** phải finding, trừ khi điều kiện kích hoạt ở §2 đã xảy ra — và phải nêu bằng chứng |
| Thấy một thứ trong bảng §5.2 | Kiểm cột "điều kiện". Chưa xảy ra thì không mở finding |
| Thấy một thứ ở §3 (ba cái bẫy) đang tồn tại trong code | Finding, kèm trích dẫn |
| Thấy một thành phần **không có** trong file này | Câu hỏi đúng không phải "nên có không" mà là "nó đã qua thủ tục §6 chưa" |

Dòng cuối là chỗ file này có giá trị nhất: nó biến câu hỏi *"thế này đã chuẩn chưa"* — vốn không có đáp án — thành câu hỏi *"quyết định này đã được ghi lại chưa"*, vốn trả lời được bằng cách mở [`../../adr/`](../../adr/).
