---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0055 — Commit của đơn vị công việc không nhận token huỷ của request; hết hạn mở kết nối hay chờ pool không được thử lại, có chủ đích

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

[`0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md`](0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md) chốt hai điều: strategy của Core không thử lại lỗi hết thời hạn chờ, và `UnitOfWork` bọc lỗi không phải `PostgresException` ném từ commit thành ngoại lệ không tạm thời. Sau khi thi công, `backend-expert` báo hai hệ quả mà ADR đó không nói tới. Kiến trúc sư đối chiếu ngày 2026-09-22:

- **Hệ quả a.** Trong `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/UnitOfWork.cs`, `CommitAsync` gọi `transaction.CommitAsync(ct)` với token của request, rồi bắt bằng `catch (Exception ex) when (ex is not PostgresException)`. `OperationCanceledException` lọt vào bộ lọc đó. Client huỷ đúng lúc commit thì request đi ra thành `CommitOutcomeUnknownException`, tức 500 kèm một dòng log mức Error đòi người đối soát. `src/BE/Core/CoreAndSkill.Core.Web/ExceptionHandling/CoreExceptionHandler.cs` không có nhánh riêng nào cho `OperationCanceledException`.
- **Hệ quả b.** `src/BE/Core/CoreAndSkill.Core.Infrastructure/Persistence/CoreExecutionStrategy.cs` loại mọi `NpgsqlException` có `InnerException` là `TimeoutException`, không phân biệt lỗi phát sinh ở bước nào. Theo hiểu biết của kiến trúc sư về Npgsql, hết hạn mở kết nối và hết hạn chờ một kết nối trống từ pool cũng mang đúng hình dạng đó. Điều này **chưa** tái hiện trên database thật. Nếu đúng, `connection.OpenAsync(ct)` bên trong `strategy.ExecuteAsync` hết hạn thì không được thử lại. Lỗi mở kết nối hỏng nhanh, như bị từ chối hay bị ngắt, vẫn được thử lại theo phân loại của Npgsql.

Hai câu hỏi đặt ra: mỗi hệ quả có phải chủ đích không.

## Quyết định

Kiến trúc sư chốt:

1. **Hệ quả a không phải chủ đích. `UnitOfWork` gọi `CommitAsync` với `CancellationToken.None`.** Khi đơn vị công việc đã tới bước commit, việc client rời đi không đổi câu trả lời cho câu hỏi dữ liệu có nên được ghi hay không. Huỷ **trước** commit, trong handler hay trong `SaveChangesAsync`, vẫn đi như thường: transaction rollback, `OperationCanceledException` đi ra, không bị bọc. Bộ lọc bọc lỗi commit giữ nguyên, vì nó vẫn cần cho lỗi đường truyền.
2. **Hệ quả b là chủ đích và được khai tường minh.** Nguyên tắc của 0053 được viết lại cho đủ: **một lỗi đã tiêu hết một thời hạn chờ thì không thử lại**, dù thời hạn đó là `lock_timeout`, `CommandTimeout`, thời hạn mở kết nối hay thời hạn chờ pool. Lỗi hỏng nhanh vẫn được thử lại.

Hình dạng luật nằm ở [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4 luật 2 và [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) §7.2 mục *Lỗi KHÔNG được thử lại*. Luật E11 và E14 ở [`../RULES.md`](../RULES.md) §4.

## Phương án đã cân nhắc và vì sao loại

### Điểm a

**Loại `OperationCanceledException` khỏi bộ lọc bọc, để nó đi ra như một lần huỷ thường.** Loại. Nếu việc huỷ xảy ra khi lệnh `COMMIT` đã gửi đi, máy chủ có thể đã commit. Để ngoại lệ đi ra như một lần huỷ thường là xoá mất đúng dòng log mà ca commit không rõ kết quả cần để người vận hành đối soát. Cách này đổi một báo động giả lấy một ca im lặng thật.

**Giữ nguyên, chấp nhận 500 kèm log Error.** Loại. Mỗi lần người dùng đóng tab đúng lúc lưu sinh một dòng log đòi người đối soát. Loại log đó sẽ nhanh chóng bị lờ đi, và lúc có một ca thật thì không ai đọc.

### Điểm b

**Chỉ loại timeout của câu lệnh, vẫn thử lại timeout mở kết nối.** Loại. Hết hạn chờ pool nghĩa là mọi kết nối đang bận. Thử lại thì request đó giữ chỗ trong hàng chờ thêm ba lượt, trên một hệ thống đang quá tải, đúng lập luận 0053 đã dùng cho `lock_timeout`. Hết hạn mở kết nối nghĩa là máy chủ không trả lời trong thời hạn đó. Thử lại nhân thời gian người dùng chờ lên bốn lần trước khi họ nhận 500. Muốn tách được hai ca này thì strategy phải đọc thông điệp lỗi của Npgsql, và thông điệp không phải hợp đồng.

## Hệ quả

### Tiêu cực — cái giá thật

| Cái giá | Nghĩa là |
| --- | --- |
| **Commit có thể chạy tiếp sau khi client đã rời đi** | Request đã bị huỷ vẫn giữ kết nối cho tới khi commit xong hoặc hết thời hạn. Thời hạn nào bao được lệnh commit thì **chưa** ai kiểm |
| **Client huỷ ở chỗ khác vẫn ra 500 kèm log Error** | Quyết định 1 chỉ gỡ ca ở commit. `OperationCanceledException` từ handler hay từ `SaveChangesAsync` vẫn đi tới `CoreExceptionHandler`, và ở đó không có nhánh riêng. Câu hỏi này còn mở, chưa ADR nào chốt |
| **Một lần chuyển máy chủ database chậm hơn thời hạn mở kết nối thành 500 ngay** | Chính ca này là ca mà một lượt thử lại lẽ ra cứu được |
| **Quyết định 2 dựa vào hình dạng ngoại lệ chưa tái hiện** | Nếu Npgsql báo hết hạn mở kết nối bằng hình dạng khác thì nguyên tắc vẫn đúng mà code không làm theo. Chỉ một test trên PostgreSQL thật trả lời được câu này |

### Tích cực

- Log mức Error của ca commit không rõ kết quả chỉ còn chứa ca thật.
- Nguyên tắc thử lại của Core nay nói được bằng một câu, và câu đó phủ mọi loại thời hạn.

### Điều gì là dấu hiệu quyết định này bắt đầu sai

- Kết nối bị giữ lâu sau khi request đã huỷ, thấy được qua số kết nối đang bận tăng lúc tải cao mà không có request tương ứng. Nghĩa là lệnh commit không bị thời hạn nào bao.
- Lỗi 500 vì hết hạn mở kết nối xuất hiện dồn thành cụm vài chục giây, trùng với lúc chuyển máy chủ database. Nghĩa là thời hạn mở kết nối ngắn hơn thời gian chuyển máy chủ, và việc cần sửa là thời hạn đó, không phải bật lại thử lại.

### Rút lui nếu sai

Mọi thứ là code, không đổi lược đồ, không sinh dữ liệu. Bỏ quyết định 1 là trả token của request vào lời gọi commit trong `UnitOfWork.cs`. Bỏ quyết định 2 là thêm vào `IsWaitLimitExceeded` một phép phân biệt bước mở kết nối. Cả hai đi kèm việc hạ luật E11 hoặc E14 ở [`../RULES.md`](../RULES.md) và một ADR mới nêu lý do.

## Liên quan

- [`0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md`](0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md): quyết định được bổ sung. Mọi điều của 0053 giữ nguyên hiệu lực
- [`../quy-uoc/be-cqrs-handler.md`](../quy-uoc/be-cqrs-handler.md) §4 · [`../quy-uoc/be-performance.md`](../quy-uoc/be-performance.md) §7.2
