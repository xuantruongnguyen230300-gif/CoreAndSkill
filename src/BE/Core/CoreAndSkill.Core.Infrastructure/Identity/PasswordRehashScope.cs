namespace CoreAndSkill.Core.Infrastructure.Identity;

// Đánh dấu "lượt ghi AppUser đang diễn ra là Identity BĂM LẠI mật khẩu, không phải đổi mật khẩu" — cho
// AuditLogInterceptor đọc. Khi kiểm mật khẩu đăng nhập, UserManager.CheckPasswordAsync thấy dạng băm cũ
// (SuccessRehashNeeded) thì băm lại CHÍNH mật khẩu đó và lưu: PasswordHash đổi giá trị, SecurityStamp cũng
// xoay — từ ChangeTracker không phân biệt được với một lần đổi mật khẩu thật. Chỉ nơi gọi biết.
//
// Scoped — cùng phạm vi với DbContext và AuditLogInterceptor của request. Chỉ IdentityService mở phạm vi.
public sealed class PasswordRehashScope
{
    private int _depth;

    public bool IsActive => _depth > 0;

    internal IDisposable Begin()
    {
        _depth++;
        return new Exit(this);
    }

    private sealed class Exit(PasswordRehashScope owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            owner._depth--;
        }
    }
}
