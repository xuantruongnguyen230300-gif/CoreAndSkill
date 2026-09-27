using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Infrastructure.Permissions;
using CoreAndSkill.Core.Infrastructure.Roles;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// docs/quy-uoc/be-performance.md (N+1): hai seam tra THEO TẬP mà luật chống leo thang đặc quyền (docs/contracts/users.md
// §2 Luật 1) dùng cho mọi vai trò bị chạm. Số câu SQL không được tăng theo số vai trò — một vòng `await` cho mỗi vai trò
// là 2N câu ở mỗi request gán vai trò.
//
// Đếm câu SQL THẬT mà EF phát ra (DbCommandInterceptor), không đếm lời gọi seam: hiện thực viết lại thành một vòng lặp
// bên trong seam thì test ở UnitTests vẫn xanh, test này đỏ. Không database — mỗi câu đọc được đếm rồi trả tập rỗng.
public sealed class PermissionSetQueryCountTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    public async Task FindExistingRoleIds_IssuesOneQuery_WhateverTheNumberOfIds(int count)
    {
        var counter = new ReaderCounter();
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), [.. new SqlRecorder().Interceptors, counter]);

        await new RoleQueryService(db).FindExistingIdsAsync(Ids(count), CancellationToken.None);

        counter.Queries.ShouldBe(1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    public async Task GetPermissionsForRoles_IssuesOneQuery_WhateverTheNumberOfRoles(int count)
    {
        var counter = new ReaderCounter();
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), [.. new SqlRecorder().Interceptors, counter]);
        var ids = Ids(count);

        var result = await new PermissionChecker(db).GetPermissionsForRolesAsync(ids, CancellationToken.None);

        counter.Queries.ShouldBe(1);
        result.Keys.ShouldBe(ids, ignoreOrder: true); // hợp đồng của seam: mọi id được hỏi đều có mặt
    }

    [Fact]
    public async Task SetQueries_IssueNoQuery_ForAnEmptyList()
    {
        var counter = new ReaderCounter();
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), [.. new SqlRecorder().Interceptors, counter]);

        await new RoleQueryService(db).FindExistingIdsAsync([], CancellationToken.None);
        await new PermissionChecker(db).GetPermissionsForRolesAsync([], CancellationToken.None);

        counter.Queries.ShouldBe(0);
    }

    private static List<Guid> Ids(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

    // Đếm mỗi câu đọc EF phát ra — dạng trả dòng (trả tập rỗng) lẫn dạng trả một giá trị như Any/Count (trả false) — không
    // câu nào tới database.
    private sealed class ReaderCounter : DbCommandInterceptor
    {
        private int _queries;

        public int Queries => _queries;

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Interlocked.Increment(ref _queries);
            return InterceptionResult<object>.SuppressWithResult(false);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ScalarExecuting(command, eventData, result));

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            => InterceptionResult<DbDataReader>.SuppressWithResult(Count());

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(Count()));

        private DbDataReader Count()
        {
            Interlocked.Increment(ref _queries);
            var table = new DataTable();
            table.Columns.Add("x", typeof(object));
            return table.CreateDataReader();
        }
    }
}
