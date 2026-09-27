using System.Text.RegularExpressions;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// docs/adr/0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md quyết định 2: mỗi tên trong tập AppUserUniqueIndexes được
// đối chiếu với tên index trong mô hình EF — và với script thật chạy vào database (database/scripts/core/;
// docs/database/script-runbook.md §0: schema áp bằng script, không bằng mô hình). Đổi tên index ở một nơi mà quên nơi kia thì
// một test ở đây đỏ, thay vì ca ghi đồng thời âm thầm quay lại trả 500.
//
// Mỗi tên còn phải trỏ ĐÚNG cột: UserNameIndex dịch thành DuplicateUserName nên phải là index trên normalized_user_name — hai
// tên bị tráo thì người dùng thấy "email đã được dùng" cho một tên đăng nhập trùng.
public sealed class AppUserUniqueIndexesTests
{
    // Kỳ vọng cho TỪNG tên của tập: thuộc tính EF, cột trong script, mã lỗi Identity. Theory chạy trên chính tập đang canh —
    // tập thêm một tên mà bảng này chưa có thì test đỏ (KeyNotFoundException), không bỏ sót tên mới.
    private static readonly Dictionary<string, (string Property, string Column, string IdentityCode)> Expected = new(StringComparer.Ordinal)
    {
        [AppUserUniqueIndexes.UserName] = (nameof(AppUser.NormalizedUserName), "normalized_user_name", "DuplicateUserName"),
        [AppUserUniqueIndexes.Email] = (nameof(AppUser.NormalizedEmail), "normalized_email", "DuplicateEmail"),
    };

    public static TheoryData<string> Names => new(AppUserUniqueIndexes.ConstraintNames);

    // Chống test rỗng: tập đang canh đúng hai index người dùng đã chốt (ADR-0089 quyết định 5) — không rỗng, không thừa.
    [Fact]
    public void TheSet_IsExactlyTheTwoAppUserIndexes()
        => AppUserUniqueIndexes.ConstraintNames.ShouldBe([AppUserUniqueIndexes.UserName, AppUserUniqueIndexes.Email], ignoreOrder: true);

    [Theory]
    [MemberData(nameof(Names))]
    public void EachName_IsAUniqueIndexOfTheModel_OnTheTenantAndTheMatchingColumn(string constraint)
    {
        var property = Expected[constraint].Property;
        using var db = OfflineCoreDbContext.Create(Guid.NewGuid());
        var entity = db.Model.FindEntityType(typeof(AppUser)).ShouldNotBeNull();

        var index = entity.GetIndexes().SingleOrDefault(i => i.GetDatabaseName() == constraint)
            .ShouldNotBeNull($"mô hình EF không có index '{constraint}' trên AppUser");

        index.IsUnique.ShouldBeTrue();
        index.Properties.Select(p => p.Name).ShouldBe([nameof(AppUser.TenantId), property]);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EachName_IsCreatedByTheSchemaScripts_AsAUniqueIndexOnTheMatchingColumn(string constraint)
    {
        var column = Expected[constraint].Column;
        var scripts = Directory.GetFiles(PostgresFixture.LocateScriptsDirectory(), "*.sql");
        scripts.ShouldNotBeEmpty("chống test rỗng: phải đọc được script schema");

        var pattern = new Regex($@"CREATE UNIQUE INDEX ""{Regex.Escape(constraint)}"" ON core\.app_user \((?<columns>[^)]*)\)");
        var definitions = scripts
            .SelectMany(path => pattern.Matches(File.ReadAllText(path)))
            .Select(match => match.Groups["columns"].Value)
            .ToList();

        definitions.ShouldHaveSingleItem($"script phải tạo đúng một index '{constraint}' trên core.app_user")
            .Split(',', StringSplitOptions.TrimEntries)
            .ShouldBe(["tenant_id", column]);
    }

    // Tên → lỗi Identity: dịch ra ĐÚNG mã bộ kiểm Identity dùng cho cột mà index đó canh.
    [Theory]
    [MemberData(nameof(Names))]
    public void EachName_TranslatesToTheIdentityErrorOfItsColumn(string constraint)
    {
        var identityCode = Expected[constraint].IdentityCode;
        var user = AppUser.NewAccount("binh.tv", "binh@vd.vn", "Trần Văn Bình");
        var exception = new DbUpdateException(
            "lưu hỏng", new Npgsql.PostgresException("duplicate key", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.UniqueViolation,
                constraintName: constraint));

        AppUserUniqueIndexes.Translate(exception, user, new IdentityErrorDescriber())
            .ShouldNotBeNull().Code.ShouldBe(identityCode);
    }

    // Composition root thật: kho người dùng mà UserManager nhận là AppUserStore — thiếu đăng ký thì mọi test offline ở
    // AppUserUniqueViolationTests xanh trong khi host vẫn chạy kho mặc định.
    [Fact]
    public void TheHost_ResolvesTheCoreUserStore_AsTheOnlyUserStore()
    {
        using var factory = new CoreWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetServices<IUserStore<AppUser>>().ShouldHaveSingleItem().ShouldBeOfType<AppUserStore>();
        scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().ShouldNotBeNull();
    }
}
