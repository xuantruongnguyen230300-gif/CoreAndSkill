---
kind: quyet-dinh
scope: core
verified: chua-doi-chieu
---

# ADR-0060 — Seam `IStaleDataCleaner` không truyền mốc thời gian; mỗi cleaner tự khai ngưỡng lưu giữ của chính nó

> **Trạng thái:** Đã chấp nhận (2026-09-22)

## Bối cảnh

Job dọn dữ liệu quá hạn của Core gọi từng `IStaleDataCleaner` đã đăng ký. Kiến trúc sư đối chiếu ngày 2026-09-22:

| Đo gì | Kết quả |
| --- | --- |
| Seam | `src/BE/Core/CoreAndSkill.Core.Application/Maintenance/IStaleDataCleaner.cs` có `Task<int> CleanAsync(DateTimeOffset olderThan, CancellationToken ct)` |
| Job truyền gì | `src/BE/Core/CoreAndSkill.Core.Infrastructure/Jobs/StaleDataCleanupHostedService.cs` có `var olderThan = timeProvider.GetUtcNow();`, tức **thời điểm chạy**, không phải một ngưỡng |
| Ngưỡng ở đâu | Không có khoá cấu hình hay con số nào. [`../wiki-core/be/10-data-retention.md`](../wiki-core/be/10-data-retention.md) §3 chỉ có bảng gợi ý theo **loại dữ liệu**: outbox thì ngắn, nhật ký kiểm toán thì dài, bản ghi xoá mềm do nghiệp vụ và pháp lý quyết |
| Ai đăng ký cleaner | Chưa ai. Core cố ý không đăng ký cleaner thật nào (§8 của file trên) |

Một cleaner viết theo đúng tên tham số, `WHERE created_at < @olderThan`, sẽ xoá **mọi dòng** ngay ở lượt quét lúc khởi động. Test `HostedService_OnStartup_RunsRegisteredCleaner_WithThresholdFromRealClock` đang ghim chính hành vi đó.

## Quyết định

Kiến trúc sư chốt:

1. `CleanAsync` **bỏ tham số `olderThan`**. Chữ ký mới là `Task<int> CleanAsync(CancellationToken ct)`.
2. **Mỗi cleaner tự khai ngưỡng lưu giữ của chính nó**, trong options riêng của cleaner đó (module khai cho cleaner của module), kèm `ValidateOnStart` và luật ngưỡng > 0. Cleaner tự tính mốc cắt bằng `TimeProvider`.
3. Cleaner chưa có ngưỡng khai tường minh thì **không được đăng ký**. Core không có ngưỡng mặc định nào, nên không có con số nghiệp vụ nào phải chốt ở Core.
4. Test đang ghim hành vi cũ sửa trong cùng PR với seam: cleaner giả được gọi, và job không truyền mốc nào.

## Phương án đã cân nhắc và vì sao loại

### Phương án A — Một khoá cấu hình chung, ví dụ `Core:BackgroundJobs:StaleDataRetentionDays`, kèm mặc định và `ValidateOnStart`

**Được:** seam giữ nguyên, sửa một chỗ trong job.

**Mất:** một con số áp cho mọi loại dữ liệu. Bảng §3 của file lưu giữ cho thấy các loại cần những khoảng khác hẳn nhau. Outbox đã phát nên dọn sau vài ngày, còn nhật ký kiểm toán phải giữ nhiều năm. Một khoá chung buộc chọn giữa hai điều: dọn dữ liệu ngắn hạn quá muộn, hoặc xoá dữ liệu dài hạn quá sớm. Con số mặc định còn là quyết định nghiệp vụ mà hôm nay chưa có cơ sở nào.

**Vì sao loại:** nó đặt vào Core một tham số cấu hình mà giá trị đúng tuỳ từng loại dữ liệu của từng dự án. Đó là một trong các dấu hiệu Core phình ra.

### Phương án B — Giữ tham số nhưng đổi tên thành `runStartedAt`

**Được:** các cleaner trong cùng một lượt dùng chung một "giờ hiện tại".

**Vì sao loại:** tên mới không ngăn được lỗi. Người viết cleaner vẫn nhận một mốc thời gian và vẫn có thể so thẳng với nó. Bỏ hẳn tham số thì người viết buộc phải nghĩ tới ngưỡng của mình.

## Hệ quả

### Tích cực

- Không còn đường nào để một cleaner "xoá theo thời điểm chạy" chỉ vì làm đúng theo tên tham số.
- Ngưỡng nằm cạnh dữ liệu mà nó chi phối. Khi đọc một cleaner, người đọc thấy nó giữ dữ liệu bao lâu.

### Tiêu cực

- **Không còn chỗ nào nhìn thấy mọi ngưỡng cùng lúc.** Muốn biết hệ thống giữ gì bao lâu thì phải đọc từng cleaner, hoặc từng mục cấu hình.
- **Các cleaner trong một lượt đọc đồng hồ ở những thời điểm hơi khác nhau.** Chênh lệch này không đáng kể so với ngưỡng tính bằng ngày.
- **Đổi chữ ký một seam công khai của `Core.Application`.** Hôm nay chưa ai cài seam này, nên không ai phải sửa. Sau này đổi lại sẽ phá mọi cleaner đã viết.
- Luật *"cleaner chưa có ngưỡng thì không đăng ký"* chưa có cổng. Cổng phải đọc được đăng ký DI **và** options của từng cleaner, và hôm nay chưa có cleaner nào để làm đầu vào.

### Rút lui nếu sai

Nếu sau này cần một mốc chung, thêm một overload hoặc một seam mới **bên cạnh** seam này, kèm ADR mới. Chữ ký hiện tại không bị phá.

## Liên quan

- [`../quy-uoc/be-architecture.md`](../quy-uoc/be-architecture.md) §1.5: seam.
- [`0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md`](0047-job-dinh-ky-khong-di-qua-seam-lap-lich.md): job định kỳ đứng riêng.
