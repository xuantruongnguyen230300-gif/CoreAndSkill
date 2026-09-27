using System.Data;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Infrastructure.Menu;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Menu;

// Thứ tự mục menu trong response — docs/contracts/meta-menu.md §1 (`displayOrder`: "Thứ tự trong cùng một cấp").
//
// KHÔNG chạm database: CoreDbContext thật trên OfflineCoreDbContext, mỗi câu đọc được trả bằng một bảng dựng sẵn theo
// đúng thứ tự GetVisibleMenuAsync phát lệnh. Thứ nó chứng minh được là phép SẮP XẾP TRONG BỘ NHỚ của service — đúng chỗ
// hai mục cùng display_order có thể đổi chỗ giữa hai lần tải. Thứ nó KHÔNG chứng minh được là câu SQL chạy trên
// PostgreSQL; đó là việc của test RequiresDocker.
public sealed class MenuQueryServiceOrderingTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // Hai mục CÙNG display_order mặc định (0), database trả về theo thứ tự ngược bảng chữ cái — đúng thứ một câu truy vấn
    // không ORDER BY được phép làm. Thiếu tiêu chí phụ ổn định, sidebar đổi thứ tự giữa hai lần tải mà không ai đổi gì.
    [Fact]
    public async Task GetVisibleMenu_TwoItemsSharingDisplayOrder_AreOrderedByCode()
    {
        var items = new[]
        {
            MenuRow("z-cuoi", parentId: null, displayOrder: 0),
            MenuRow("a-dau", parentId: null, displayOrder: 0),
        };

        var menu = await RunAsync(items);

        menu.Select(m => m.Code).ShouldBe(["a-dau", "z-cuoi"]);
    }

    // Đối chứng: display_order vẫn là tiêu chí CHÍNH — tiêu chí phụ chỉ phân xử khi nó hoà.
    [Fact]
    public async Task GetVisibleMenu_DisplayOrderStillWinsOverCode()
    {
        var items = new[]
        {
            MenuRow("a-dau", parentId: null, displayOrder: 90),
            MenuRow("z-cuoi", parentId: null, displayOrder: 10),
        };

        var menu = await RunAsync(items);

        menu.Select(m => m.Code).ShouldBe(["z-cuoi", "a-dau"]);
    }

    // Mục cấp một đứng trước mục con, rồi mới tới display_order và mã — cây đúng MỘT cấp (schema-core.md §6.1).
    [Fact]
    public async Task GetVisibleMenu_RootsComeBeforeChildren_ThenDisplayOrderThenCode()
    {
        var parentId = Guid.NewGuid();
        var items = new[]
        {
            MenuRow("con-b", parentId, displayOrder: 10),
            MenuRow("con-a", parentId, displayOrder: 10),
            MenuRow("cha", parentId: null, displayOrder: 90, id: parentId),
        };

        var menu = await RunAsync(items);

        menu.Select(m => m.Code).ShouldBe(["cha", "con-a", "con-b"]);
    }

    private static async Task<IReadOnlyList<CoreAndSkill.Core.Application.Menu.MenuItemDto>> RunAsync(MenuRowData[] items)
    {
        var recorder = new SqlRecorder();

        // Thứ tự câu đọc của GetVisibleMenuAsync: mục menu → (bỏ qua bảng quyền vì không mục nào gắn khoá) →
        // vai trò của người dùng → dòng ghim mục theo vai trò.
        var scripted = new ScriptedReader(MenuTable(items), UserRoleTable(), MenuItemRoleTable());

        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), [.. recorder.Interceptors, scripted]);

        var checker = Substitute.For<IPermissionChecker>();
        checker.GetEffectivePermissionsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal));

        var service = new MenuQueryService(db, checker, []);

        return await service.GetVisibleMenuAsync(UserId, CancellationToken.None);
    }

    private sealed record MenuRowData(Guid Id, Guid? ParentId, string Code, int DisplayOrder);

    private static MenuRowData MenuRow(string code, Guid? parentId, int displayOrder, Guid? id = null)
        => new(id ?? Guid.NewGuid(), parentId, code, displayOrder);

    // Cột theo ĐÚNG thứ tự phép chiếu trong MenuQueryService.GetVisibleMenuAsync.
    private static DataTable MenuTable(IEnumerable<MenuRowData> rows)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("parent_id", typeof(Guid));
        table.Columns.Add("code", typeof(string));
        table.Columns.Add("label_key", typeof(string));
        table.Columns.Add("icon", typeof(string));
        table.Columns.Add("route", typeof(string));
        table.Columns.Add("display_order", typeof(int));
        table.Columns.Add("required_permission_id", typeof(Guid));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.Id,
                row.ParentId is { } parentId ? parentId : DBNull.Value,
                row.Code,
                $"menu.{row.Code}",
                DBNull.Value,
                row.ParentId is null ? DBNull.Value : $"/{row.Code}",
                row.DisplayOrder,
                DBNull.Value);
        }

        return table;
    }

    private static DataTable UserRoleTable()
    {
        var table = new DataTable();
        table.Columns.Add("role_id", typeof(Guid));
        return table;
    }

    private static DataTable MenuItemRoleTable()
    {
        var table = new DataTable();
        table.Columns.Add("menu_item_id", typeof(Guid));
        table.Columns.Add("role_id", typeof(Guid));
        return table;
    }
}
