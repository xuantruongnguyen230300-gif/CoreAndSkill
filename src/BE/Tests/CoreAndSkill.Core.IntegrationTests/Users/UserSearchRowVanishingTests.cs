using System.Data;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Infrastructure.Users;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Users;

// UserQueryService.SearchAsync đọc BA lần, không trong một ảnh chụp: đếm, lấy id của trang, rồi nạp chi tiết theo tập id.
// Giữa câu thứ hai và câu thứ ba, một người dùng trong trang có thể bị XOÁ MỀM bởi một người quản trị khác — id vẫn nằm
// trong danh sách đã lấy, nhưng câu nạp chi tiết (đi qua bộ lọc soft delete) không trả dòng nào cho nó.
//
// Tra thẳng theo khoá khi ấy ném KeyNotFoundException, và người gọi nhận 500 cho một thao tác hoàn toàn hợp lệ của người
// khác. Không chạm database — CoreDbContext thật, mỗi câu đọc được trả bằng một bảng dựng sẵn (ScriptedReader).
public sealed class UserSearchRowVanishingTests
{
    [Fact]
    public async Task Search_RowSoftDeletedBetweenThePageQueryAndTheDetailQuery_SkipsItInsteadOfThrowing()
    {
        var vanished = Guid.NewGuid();
        var recorder = new SqlRecorder();

        // Thứ tự câu đọc của SearchAsync: đếm → id của trang → chi tiết (KHÔNG còn dòng nào) → vai trò.
        var scripted = new ScriptedReader(CountTable(1), IdTable(vanished), NoRows(), NoRows());

        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), [.. recorder.Interceptors, scripted]);
        var service = new UserQueryService(db);

        var page = await service.SearchAsync(
            new UserSearchCriteria(Page: 1, PageSize: 20, SortBy: "userName", SortDescending: false,
                SearchText: null, RoleId: null, Status: null),
            CancellationToken.None);

        page.Items.ShouldBeEmpty();

        // TotalCount giữ nguyên số của câu đếm: nó là ảnh chụp tại thời điểm đếm, không phải số dòng đã dựng được.
        page.TotalCount.ShouldBe(1);
    }

    private static DataTable CountTable(int count)
    {
        var table = new DataTable();
        table.Columns.Add("c", typeof(int));
        table.Rows.Add(count);
        return table;
    }

    private static DataTable IdTable(params Guid[] ids)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        foreach (var id in ids)
            table.Rows.Add(id);
        return table;
    }

    private static DataTable NoRows()
    {
        var table = new DataTable();
        table.Columns.Add("x", typeof(object));
        return table;
    }
}
