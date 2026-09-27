namespace CoreAndSkill.Core.Application.Identity;

// Danh tính của HỆ THỐNG khi không có người thực hiện (job nền, lệnh bootstrap): tên ghi vào created_by / updated_by /
// actor_display — docs/database/schema-core.md §3.2, docs/quy-uoc/be-architecture.md §1.1.
//
// Tên này DÀNH RIÊNG: không đường tạo tài khoản nào được nhận nó (CreateUserCommand, CreateTenantCommand.AdminUserName,
// CreateTenantAdminCommand.UserName, cấu hình của lệnh bootstrap). created_by giữ TÊN ĐĂNG NHẬP và được so với tên đăng nhập
// của người đang gọi (FileAccessPolicy.IsUploader) — một tài khoản tên "system" sẽ trùng danh tính với mọi dòng hệ thống ghi.
// Tài khoản mang tên này đã có từ trước (nếu có) không bị đổi gì: phép chặn chỉ đứng ở đường TẠO.
public static class SystemActor
{
    // Nguồn DUY NHẤT của giá trị: mọi chỗ ghi danh tính hệ thống (interceptor audit, repository, dịch vụ nền của
    // Infrastructure) dùng hằng này — không gõ lại chuỗi. Script SQL seed và migration cũ mang giá trị đã ghi, là lịch sử,
    // không đọc hằng này.
    public const string UserName = "system";

    // So như Identity so tên đăng nhập: sau Trim, qua ĐÚNG phép của UpperInvariantLookupNormalizer — bộ chuẩn hoá
    // AddIdentityCore đăng ký (Normalize() rồi ToUpperInvariant()), tức phép sinh NormalizedUserName. Application không tham
    // chiếu Identity nên lặp lại phép đó ở đây; test ở IntegrationTests đối chiếu với ILookupNormalizer thật của host
    // (SystemActorNormalizationTests) — đổi bộ chuẩn hoá mà quên chỗ này thì test đỏ.
    public static bool IsReservedUserName(string? userName)
        => userName is not null
           && string.Equals(NormalizeLikeIdentity(userName.Trim()), NormalizeLikeIdentity(UserName), StringComparison.Ordinal);

    private static string NormalizeLikeIdentity(string name) => name.Normalize().ToUpperInvariant();
}
