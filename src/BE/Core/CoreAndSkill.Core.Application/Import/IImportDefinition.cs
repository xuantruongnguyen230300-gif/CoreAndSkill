using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Import;

// Một cột của tệp nhập. Example điền vào dòng ví dụ của tệp mẫu.
public sealed record ImportColumn(string Name, bool Required, string? Example = null);

// Kết quả xử lý MỘT dòng. Field (tên cột) để người dùng tìm đúng ô trong tệp của họ (§4.3).
public sealed record ImportRowResult(Error? Error, string? Field)
{
    public bool IsSuccess => Error is null;

    public static ImportRowResult Ok { get; } = new(null, null);

    public static ImportRowResult Failed(Error error, string? field = null) => new(error, field);
}

// Định nghĩa MỘT luồng nhập — do MODULE (hoặc Core cho tài nguyên của Core) khai; docs/contracts/exports.md §2:
// "Mỗi tài nguyên có endpoint nhập riêng, theo đúng khuôn". Core giữ cơ chế (đếm dòng, trần, việc nền,
// lỗi theo từng dòng, huỷ theo dõi sau mỗi dòng, báo cáo kết quả); định nghĩa chỉ nói CÓ GÌ trong tệp
// và GHI dòng thế nào.
//
// Type là loại việc trong core.job (<= 50 ký tự, duy nhất) — ví dụ "banhang.don-hang.import".
public interface IImportDefinition
{
    string Type { get; }

    IReadOnlyList<ImportColumn> Columns { get; }

    // KHOÁ TỰ NHIÊN của dòng — phần BẮT BUỘC của định nghĩa một luồng nhập. Dòng trùng khoá với bản ghi
    // đã có, hoặc với một dòng đã ghi trước trong cùng tệp, BỊ BỎ QUA (không ghi đè) và xuất hiện ở
    // `failed` với CORE.IMPORT.DUPLICATE_ROW — nhập lại cùng một tệp không nhân đôi dữ liệu (§4.4).
    // Không bao giờ được rỗng; khoá thiếu là lỗi của DÒNG ĐÓ nên định nghĩa xử lý ở ImportRowAsync và
    // trả một khoá dự phòng duy nhất theo dòng (ví dụ "row:" + số dòng) để nó không đụng dòng nào khác.
    string NaturalKey(TabularRow row);

    Task<bool> NaturalKeyExistsAsync(string naturalKey, CancellationToken ct);

    // Kiểm hợp lệ và THÊM entity vào ngữ cảnh dữ liệu — KHÔNG SaveChanges (bộ chạy lưu từng dòng và
    // huỷ theo dõi sau mỗi dòng). Dòng sai trả Failed; định nghĩa KHÔNG tự dọn entity đã thêm dở —
    // bộ chạy làm việc đó (đây chính là bẫy §4.2: entity của dòng lỗi rò sang dòng sau).
    //
    // Dòng mang một cột định danh trỏ tới bản ghi của đơn vị KHÁC thì thao tác nhập thành thao tác sửa
    // dữ liệu đơn vị khác: bộ lọc đơn vị chặn được khi ĐỌC, phần GHI phải kiểm tường minh (luồng N4 §5).
    Task<ImportRowResult> ImportRowAsync(TabularRow row, CancellationToken ct);
}

// Ghi và HUỶ THEO DÕI thay đổi của MỘT dòng — docs/wiki-core/be/15-import-export.md §4.2, §6.1.
// Tách thành seam vì Application không biết EF Core (luật A2) và không bắt được DbUpdateException.
public interface IImportRowWriter
{
    // Lưu những gì dòng hiện tại đã thêm. Cơ sở dữ liệu từ chối (trùng khoá đua nhau, vi phạm ràng
    // buộc) ⇒ tự huỷ phần đã thêm rồi trả Failure — dòng bị báo lỗi, việc KHÔNG chết. Lỗi hạ tầng
    // (mất kết nối) vẫn là exception: đó là việc `failed`, không phải lỗi của một dòng.
    Task<Result> SaveRowAsync(CancellationToken ct);

    // Gỡ khỏi bộ theo dõi MỌI entity đang chờ / đã sửa. PHẢI gọi sau dòng lỗi (nếu không entity của
    // dòng lỗi được ghi cùng dòng sau — dòng bị báo "không nhập" mà DB vẫn có nó) và cũng sau dòng
    // đúng (bộ theo dõi giữ mọi entity đã đi qua nó, nên phình theo số dòng đã xử lý).
    void DiscardTrackedChanges();
}
