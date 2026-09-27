namespace CoreAndSkill.Core.Application.Maintenance;

// Seam để Infrastructure của Core dọn dữ liệu quá hạn CỦA MODULE mà không tham chiếu ngược
// Modules.* — docs/quy-uoc/be-architecture.md §1.5. StaleDataCleanupHostedService
// (Core.Infrastructure, Singleton) resolve cleaner trong một phạm vi DI MỚI cho mỗi cleaner ở mỗi lượt quét — cleaner
// được phép đăng ký Scoped và nhận DbContext qua constructor.
//
// Job KHÔNG truyền mốc thời gian nào (docs/adr/0060-cleaner-tu-khai-nguong-luu-giu.md): mỗi cleaner tự khai ngưỡng lưu
// giữ của chính nó trong options riêng (module khai cho cleaner của module), kèm ValidateOnStart và luật ngưỡng > 0, rồi
// tự tính mốc cắt bằng TimeProvider. Core không có ngưỡng mặc định nào — bảng gợi ý theo loại dữ liệu ở
// docs/wiki-core/be/10-data-retention.md §3 cho thấy các loại cần những khoảng khác hẳn nhau. Cleaner chưa có ngưỡng khai
// tường minh thì KHÔNG được đăng ký (luật B13, docs/RULES.md §10).
//
// v1 KHÔNG có hiện thực nào đăng ký — job xoá cứng theo chính sách lưu giữ cần quyết định NGHIỆP VỤ trước (thời hạn
// lưu bao lâu, ai quyết), docs/wiki-core/be/10-data-retention.md §3, §8. Cơ chế (job nền, log bắt đầu/kết thúc, chạy
// theo lô qua CleanAsync) đã sẵn sàng để KHÔNG phải sửa lược đồ khi có nguồn.
public interface IStaleDataCleaner
{
    // Tên hiển thị trong log — docs/wiki-core/be/07-observability.md §6.
    string Name { get; }

    // Trả về số bản ghi đã xoá — ghi vào log kết thúc.
    Task<int> CleanAsync(CancellationToken ct);
}
