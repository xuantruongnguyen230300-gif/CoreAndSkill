---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0053 — Chiến lược thử lại của Core không thử lại lỗi hết thời hạn chờ và không chạy lại đơn vị công việc khi commit không rõ kết quả

> **Trạng thái:** Đã chấp nhận (2026-09-22) · Bổ sung bởi [ADR-0055](0055-commit-khong-nhan-token-huy-va-het-han-mo-ket-noi-khong-thu-lai.md) (2026-09-22)

## Bối cảnh

Ngày 2026-09-22, `backend-expert` bật chiến lược thử lại đúng như [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) §7.2 quy định. Tệp `src/BE/Core/CoreAndSkill.Core.Infrastructure/DependencyInjection/CoreInfrastructureServiceCollectionExtensions.cs` nay mang chuỗi `npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)` và `npgsql.CommandTimeout(30)`. Agent đó báo bốn hệ quả mà chưa luật nào xử lý. Kiến trúc sư đối chiếu lại từng điểm, cùng ngày:

- **Thử lại chạy lại cả đơn vị công việc.** Trong `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/UnitOfWork.cs`, `strategy.ExecuteAsync` bọc toàn bộ: handler (`operation(ct)`), `SaveChangesAsync` và `transaction.CommitAsync(ct)`. Lỗi được coi là tạm thời ở bất kỳ bước nào cũng làm handler chạy lại từ đầu.
- **Phân loại của Npgsql.** Kiến trúc sư chạy một chương trình thử trên Npgsql 10.0.3, bản repo đang khoá. Chương trình nằm ở thư mục nháp, không ở trong repo. Kết quả: `PostgresException.IsTransient` là `true` với `55P03` (lock_not_available), `40001`, `40P01`, `08006`, `57P01`, và `false` với `57014`, `23505`, `25P02`. `NpgsqlException` có `InnerException` là `TimeoutException` hoặc `IOException` thì `IsTransient` là `true`.
- **Điểm a, hết thời hạn chờ khoá bị thử lại.** `src/BE/Core/CoreAndSkill.Core.Infrastructure/Permissions/PermissionMatrixService.cs` đặt `SET LOCAL lock_timeout = '5s'`. Lỗi `55P03` bị thử lại nên người dùng chờ tới bốn lần 5 giây cộng các khoảng trễ rồi mới nhận 500. Báo cáo gốc bỏ sót một ca cùng họ, nặng hơn: Npgsql báo hết `CommandTimeout` phía client bằng `NpgsqlException` bọc `TimeoutException`. Điều này theo tài liệu Npgsql, **chưa** tái hiện trên database thật. Nếu đúng như vậy, một câu truy vấn chạy quá 30 giây sẽ bị chạy lại thêm ba lần, nhắm đúng vào một database đang chậm.
- **Điểm b, commit không rõ kết quả.** Lỗi đường truyền đúng lúc `CommitAsync` không cho biết máy chủ đã commit hay chưa. Chiến lược vẫn chạy lại cả đơn vị công việc. Command tạo mới sinh id UUID v7 mới ở mỗi lượt, nên không ràng buộc duy nhất nào chặn được bản ghi thứ hai.
- **Điểm c, tác dụng ngoài database.** `UploadFileCommandHandler` gọi `storage.SaveAsync` và `StartImportCommandHandler` gọi `storage.SaveTempAsync` bên trong transaction. Cả hai tua lại được đầu vào: `FileContentDetector` đặt `stream.Position = 0`, còn handler import đặt `content.Position = 0`. Tệp thừa từ một lượt chạy hỏng thành rác. Đối soát đĩa với database trong `FileMaintenanceHostedService` dọn rác đó sau một khoảng an toàn. `OutboxDispatcher` gọi handler outbox bên trong `ExecuteInTransactionAsync`, và bên nhận đã phải chịu xử lý trùng theo [`../wiki-core/be/05-cross-module-consistency.md`](../wiki-core/be/05-cross-module-consistency.md) §4.3 và §5. Hôm nay chưa có tác dụng nào ra ngoài mà không đi qua một trong hai đường trên, nhưng cũng chưa luật nào cấm.
- **Điểm d, test chậm.** Theo đo của `backend-expert`, bộ test không cần Docker tăng từ khoảng 1 phút 20 giây lên khoảng 5 phút. Kiến trúc sư **chưa đo lại**. `src/BE/Tests/CoreAndSkill.Core.IntegrationTests/Support/CoreWebApplicationFactory.cs` mặc định dùng `UnreachableConnectionString`, nên mọi thao tác chạm database trong chế độ đó chắc chắn hỏng. Có thử lại thì mỗi lần hỏng tốn thêm ba lượt chờ.

## Quyết định

Kiến trúc sư chốt bốn điều:

1. **Core không thử lại lỗi hết thời hạn chờ.** Core dùng một execution strategy riêng thay cho `EnableRetryOnFailure` trần. Strategy này giữ số lần thử (3), trễ tối đa (5 giây) và phân loại tạm thời của Npgsql, nhưng loại hai ca: `PostgresException` mã `55P03`, và `NpgsqlException` có `InnerException` là `TimeoutException`. Hai ca đó đi thẳng ra thành 500.
2. **`UnitOfWork` không chạy lại đơn vị công việc khi commit không rõ kết quả.** Lỗi đường truyền ném ra từ `CommitAsync`, tức mọi ngoại lệ không phải `PostgresException`, được bọc thành một ngoại lệ không tạm thời, ghi log mức Error kèm trace id, rồi đi ra thành 500. `PostgresException` ném từ commit là máy chủ đã trả lời rằng nó không commit, nên vẫn theo phân loại thường.
3. **Mọi operation truyền vào `ExecuteInTransactionAsync` phải chạy lại được.** Tác dụng ngoài database chỉ được phép khi một lượt chạy thừa để lại **rác mà một job đã có dọn được**, và đầu vào đọc lại được. Mọi tác dụng nhìn thấy từ bên ngoài hệ thống, như gửi thư, gọi HTTP ra ngoài hay đẩy thông báo qua kênh ngoài, đi qua outbox. Không tác dụng nào kiểu đó được gọi trực tiếp trong operation.
4. **Host test ở chế độ không có database thì không thử lại. Chế độ PostgreSQL thật giữ nguyên strategy của production.** Một test khẳng định chế độ PostgreSQL thật vẫn dùng đúng strategy của Core.

Hình dạng luật nằm ở [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) §7.2, [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4 và [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §7. Luật E11 đến E13 ở [`../RULES.md`](../RULES.md) §4.

## Phương án đã cân nhắc và vì sao loại

### Điểm a — hết thời hạn chờ

**Giữ nguyên, chấp nhận khoảng 24 giây.** Loại. Quy tắc 3 ở `06-concurrency-control.md` §7 có mặt để một xung đột không lan thành sự cố toàn hệ. Mỗi lượt thử lại giữ một kết nối và một transaction thêm 5 giây trong lúc bên giữ khoá vẫn kẹt, và các request đến sau xếp hàng sau nó. Pool kết nối cạn nhanh gấp bốn. Thời hạn chờ vẫn còn, nhưng không còn làm được việc nó được đặt ra để làm.

**Hạ `lock_timeout` xuống khoảng 1 giây để tổng thời gian chờ quay về cỡ 5 giây.** Loại. Hành vi giữ kết nối trong lúc thử lại vẫn nguyên như cũ. Mọi chỗ đặt khoá sau này phải nhớ chia thời hạn cho số lần thử, một quy ước không ai ép được.

**Loại `55P03` bằng tham số `errorCodesToAdd`.** Không làm được. Tham số đó chỉ **thêm** mã vào danh sách thử lại, không bớt được mã nào.

**Bỏ `lock_timeout`, dựa vào `CommandTimeout`.** Loại. Hết `CommandTimeout` cũng bị thử lại, và nó dài gấp sáu lần.

### Điểm b — commit không rõ kết quả

**Giữ nguyên.** Loại. Command tạo mới sẽ ghi hai lần mà không ai biết. Đây là loại hỏng đắt nhất: không lỗi, không log, và dữ liệu sai nằm lại mãi.

**`verifySucceeded` bằng một bảng dấu giao dịch.** Mỗi transaction ghi một dòng dấu, rồi hỏi lại dòng đó khi commit không rõ kết quả. Cách này cho kết quả đúng một lần. Loại **ở thời điểm này**: nó đòi thêm một bảng vào schema `core`, thêm một lần ghi vào mọi transaction và một job dọn dòng dấu cũ. Đổi lại nó chỉ che một khoảng thời gian cỡ một vòng mạng. Điều kiện xét lại ghi ở mục Hệ quả.

**Bỏ hẳn chiến lược thử lại.** Loại. Cách này mất đúng thứ §7.2 bật chiến lược thử lại để có: một nhịp chớp mạng không thành 500. Nó cũng không làm điểm b tốt hơn quyết định 2, vì hai cách để lại cùng một ca xấu nhất.

### Điểm c — tác dụng ngoài database

**Dời mọi thao tác trên kho tệp ra sau commit.** Loại. Cách này đảo ngược thứ tự *ghi tệp trước, bản ghi sau* mà `UploadFileCommandHandler` giữ có chủ đích: tiến trình chết giữa hai bước thì còn bản ghi trỏ vào hư không, và người dùng nhìn thấy nó. Rác trên đĩa thì không ai nhìn thấy.

**Command có tác dụng ngoài thì cài `INoTransaction`.** Loại. Command đó mất tính nguyên tử của lần ghi, chỉ để né một ca hiếm.

### Điểm d — test chậm

**Giữ thử lại ở mọi chế độ test, chấp nhận khoảng 5 phút.** Loại. Mấy phút thêm đó không kiểm được gì: ở chế độ không có database, mọi lượt thử lại chắc chắn hỏng.

**Tắt thử lại ở mọi test.** Loại. Chế độ PostgreSQL thật là nơi duy nhất thử lại gặp transaction thật: một deadlock được chạy lại, `ChangeTracker.Clear()` ở đầu mỗi lượt. Lời lo của `backend-expert` rằng tắt thử lại làm host test khác production *ở đúng điểm đang canh* là đúng cho chế độ này, và **không** đúng cho chế độ không có database.

**Khoá cấu hình `Core:Database:MaxRetryCount`.** Loại. Đó là một núm cấu hình trong Core mà bên dùng thật duy nhất là test. Người vận hành cũng có thể lặng lẽ đặt nó về 0 ở production.

**Thay kho khoá DataProtection và gỡ dịch vụ nền ở chế độ không có database.** Loại. Cách này phải liệt kê từng thành phần chạm database, và danh sách đó cũ đi ngay khi có thành phần mới. Thay strategy xử lý mọi thành phần tại một điểm.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Core có thêm một lớp phải bảo trì, lớp này bám vào danh sách mã lỗi của Npgsql** | Nâng Npgsql có thể đổi danh sách lỗi tạm thời, ví dụ thêm một mã hết thời hạn mới, và strategy của Core sẽ thử lại mã đó cho tới khi có người thấy. Chỉ test ghim phân loại (luật E11) báo được thay đổi này |
| **Hết thời hạn chờ nay ra 500 ngay** | Ca bên giữ khoá nhả ở giây thứ 5,1, tức ca mà một lượt thử lại lẽ ra cứu được, nay thành lỗi cho người dùng |
| **Commit không rõ kết quả nay ra 500 dù dữ liệu có thể đã ghi** | Người dùng thấy lỗi rồi bấm lại. Với command tạo mới, lần bấm đó sinh bản ghi thứ hai. Ca này **bằng đúng** mức nền khi chưa bật thử lại (phản hồi mất trên đường về client), nên không tệ hơn trước, nhưng cũng không được giải |
| **Luật 3 chỉ ép được một phần** | Cổng máy chỉ thấy command handler phụ thuộc vào cổng gửi ra ngoài. "Rác có job dọn" là phán đoán của người review |
| **Host test không có database không phản ánh độ trễ production khi database mất** | Test nào ở chế độ đó khẳng định về thời gian, hay về số lần thử, là test đặt sai chế độ |
| **DbContext của module đọc ngoài transaction chưa có đường dùng strategy này** | `UnitOfWork` dùng strategy của context đầu tiên, tức của Core, nên transaction được phủ. Truy vấn ngoài transaction trên DbContext của module thì theo cấu hình của module. Strategy để `internal` vì chưa module nào tồn tại ([`../kien-truc-core-module.md`](../kien-truc-core-module.md) §4). Module đầu tiên đến thì mới xét mở ra |

### Tích cực

- Quy tắc 3 ở `06-concurrency-control.md` §7 lại có nghĩa: thời hạn chờ là trần cho cả request, không phải cho một lượt thử.
- Ca xấu nhất của điểm b quay về mức nền trước khi bật thử lại, thay vì tệ hơn.
- Luật về tác dụng ngoài database được viết ra trước khi có module đầu tiên, không phải sau sự cố đầu tiên.
- Bộ test không có database lấy lại tốc độ mà chế độ PostgreSQL thật vẫn chạy strategy của production.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- Lỗi 500 vì `55P03` xuất hiện ở tải bình thường. Nghĩa là tranh chấp khoá có thật, và việc cần sửa là thời gian giữ khoá, không phải bật lại thử lại.
- Ngoại lệ commit không rõ kết quả xuất hiện nhiều hơn vài lần mỗi tháng, hoặc có báo cáo bản ghi trùng từ command tạo mới. Khi đó phương án bảng dấu giao dịch đáng được xét lại.
- Test ghim phân loại đỏ sau khi nâng Npgsql.
- Bộ test không có database vẫn chậm sau khi thay strategy. Nghĩa là chẩn đoán điểm d sai, và thời gian đang đi vào chỗ khác.

### Rút lui nếu sai

Mọi thứ ở đây là code, không đổi lược đồ, không sinh dữ liệu. Quay về `EnableRetryOnFailure` trần là thay một dòng trong `CoreInfrastructureServiceCollectionExtensions.cs` và xoá lớp strategy. Bỏ quyết định 2 là gỡ khối bọc quanh `CommitAsync` trong `UnitOfWork.cs`. Việc phải làm kèm: hạ luật E11 và E12 ở [`../RULES.md`](../RULES.md), và viết một ADR mới nêu lý do.

## Liên quan

- [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) §7.2 · [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4 · [`../wiki-core/be/06-concurrency-control.md`](../wiki-core/be/06-concurrency-control.md) §7
- [`../wiki-core/be/05-cross-module-consistency.md`](../wiki-core/be/05-cross-module-consistency.md) §5: bên nhận outbox chịu xử lý trùng
- [`0025-luu-du-lieu-module-mot-transaction.md`](0025-luu-du-lieu-module-mot-transaction.md): một transaction trên mọi DbContext
