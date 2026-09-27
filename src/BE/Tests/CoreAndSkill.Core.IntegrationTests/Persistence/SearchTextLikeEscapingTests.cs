using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Infrastructure.Roles;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.Infrastructure.Users;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// `searchText` là "khớp một phần" trên các cột card khai (docs/contracts/users.md, roles.md, tenants.md) — chuỗi người
// dùng gõ được so theo NGHĨA ĐEN. `%` và `_` là ký tự đại diện của ILIKE: không thoát thì tìm `a_b` khớp cả `axb`, tìm
// `%` ra mọi dòng. Mọi chỗ tìm bằng ILIKE trong Core phải thoát cả hai (và chính ký tự thoát).
//
// Không cần Docker: SqlRecorder chặn lệnh đọc đầu tiên và ghi lại câu SQL kèm giá trị tham số. ĐIỂM MÙ: đây chỉ chứng minh
// mẫu truyền xuống đã được thoát và câu SQL khai ESCAPE — hành vi khớp trên PostgreSQL thật là việc của
// PersistenceResilienceDatabaseTests (RequiresDocker, chưa chạy).
public sealed class SearchTextLikeEscapingTests
{
    public static TheoryData<string> Searches => new() { "users", "roles", "tenants" };

    [Theory]
    [MemberData(nameof(Searches))]
    public async Task SearchText_WithLikeWildcards_IsSentAsLiteralPattern(string search)
    {
        var recorder = new SqlRecorder();
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), recorder.Interceptors);
        const string searchText = @"50%_a\b";

        Func<Task> run = search switch
        {
            "users" => () => new UserQueryService(db).SearchAsync(
                new UserSearchCriteria(1, 20, "userName", false, searchText, null, null), CancellationToken.None),
            "roles" => () => new RoleQueryService(db).SearchAsync(
                new RoleSearchCriteria(1, 20, "name", false, searchText), CancellationToken.None),
            "tenants" => () => new TenantAdminQueryService(db).SearchAsync(
                new TenantSearchCriteria(1, 20, "name", false, searchText), CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(search), search, null),
        };

        await Should.ThrowAsync<SqlRecorder.ReaderReachedException>(run);

        var sql = recorder.Executed.ShouldHaveSingleItem();
        // Không khai ký tự thoát thì Npgsql tự gắn ESCAPE '' — TẮT luôn dấu thoát mặc định `\`, mẫu đã thoát thành sai.
        sql.ShouldContain(@"ESCAPE '\'", Case.Sensitive, $"ILIKE phải khai ký tự thoát `\\` tường minh. SQL: {sql}");
        sql.ShouldContain(@"%50\%\_a\\b%", Case.Sensitive, $"mẫu phải thoát \\, % và _. SQL: {sql}");
    }
}
