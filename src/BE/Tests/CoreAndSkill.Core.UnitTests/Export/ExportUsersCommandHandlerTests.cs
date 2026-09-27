using System.Text;
using System.Text.Json;
using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Export;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.UnitTests.Support;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Export;

// docs/contracts/exports.md §1, docs/wiki-core/be/15-import-export.md §5, luồng N3.
public class ExportUsersCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 4, 5, TimeSpan.Zero);

    private readonly IUserQueryService _users = Substitute.For<IUserQueryService>();
    private readonly IRoleQueryService _roles = Substitute.For<IRoleQueryService>();
    private readonly IAuditTrail _audit = Substitute.For<IAuditTrail>();
    private readonly CapturingWriterFactory _writers = new();

    private ExportUsersCommandHandler Handler(int maxRows = 100)
        => new(_users, _roles, _writers, _audit, Options.Create(new CoreExportOptions { MaxRows = maxRows }), new FixedTimeProvider(Now));

    private static UserListItemDto User(string name, bool locked = false, params string[] roles)
        => new(Guid.NewGuid(), name, $"{name}@vd.vn", $"Họ {name}", [.. roles.Select(r => new UserRoleSummaryDto(Guid.NewGuid(), r, false))],
            locked, null, false, false, Now, "v");

    private static async IAsyncEnumerable<UserListItemDto> Stream(params UserListItemDto[] items)
    {
        await Task.CompletedTask;
        foreach (var item in items)
            yield return item;
    }

    private static async Task<string> WriteAsync(ExportFile file)
    {
        using var buffer = new MemoryStream();
        await file.WriteToAsync(buffer, default);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    [Fact]
    public async Task Handle_CountsBeforeBuildingTheFile_AndOverTheCapIsTooManyRows_WithActualAndLimit()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(101);

        var result = await Handler(maxRows: 100).Handle(new ExportUsersCommand(), default);

        result.Error!.Code.ShouldBe(ExportErrors.TooManyRows.Code);
        result.Error.Type.ShouldBe(ErrorType.BusinessRule);
        result.Error.Params["RowCount"].ShouldBe("101");
        result.Error.Params["MaxRows"].ShouldBe("100");
        _audit.ReceivedCalls().ShouldBeEmpty("không xuất được gì thì không có dòng nhật ký 'đã xuất'");
        _users.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IUserQueryService.StreamAsync)).ShouldBe(0, "chưa đọc dữ liệu khi đã biết vượt trần");
    }

    [Fact]
    public async Task Handle_ExactlyAtTheCap_IsAccepted()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(100);

        (await Handler(maxRows: 100).Handle(new ExportUsersCommand(), default)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UsesTheSameFilterAndSortAsTheList_AndIgnoresPaging()
    {
        var roleId = Guid.NewGuid();
        _roles.ExistsAsync(roleId, Arg.Any<CancellationToken>()).Returns(true);
        _users.CountAsync(default!, default).ReturnsForAnyArgs(1);
        var command = new ExportUsersCommand(ExportFormats.Csv, "fullName", true, "an", roleId, "locked");

        await Handler().Handle(command, default);

        var criteria = (UserSearchCriteria)_users.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(IUserQueryService.CountAsync)).GetArguments()[0]!;
        criteria.SortBy.ShouldBe("fullName");
        criteria.SortDescending.ShouldBeTrue();
        criteria.SearchText.ShouldBe("an");
        criteria.RoleId.ShouldBe(roleId);
        criteria.Status.ShouldBe("locked");
    }

    [Fact]
    public async Task Handle_DefaultsTheSortToUserName_LikeTheList()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(0);

        await Handler().Handle(new ExportUsersCommand(), default);

        ((UserSearchCriteria)_users.ReceivedCalls().Single().GetArguments()[0]!).SortBy.ShouldBe("userName");
    }

    [Fact]
    public async Task Handle_AnUnknownRoleFilter_IsAnErrorNotAnEmptyFile()
    {
        var roleId = Guid.NewGuid();
        _roles.ExistsAsync(roleId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler().Handle(new ExportUsersCommand(RoleId: roleId), default);

        result.Error!.Code.ShouldBe(UserErrors.RoleNotFound.Code);
        _audit.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WritesExactlyOneAuditRow_WithWhoWhatFilterAndCount_AndNoUserData()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(2);

        await Handler().Handle(new ExportUsersCommand(ExportFormats.Xlsx, "email", false, "nguyen", null, "active"), default);

        var entry = (AuditEntry)_audit.ReceivedCalls().Single().GetArguments()[0]!;
        entry.ActionCode.ShouldBe(AuditActions.UserExport);
        entry.ActionCode.ShouldBe("core.user.export");
        entry.TargetType.ShouldBe("core.user");
        using var after = JsonDocument.Parse(entry.AfterValueJson!);
        after.RootElement.GetProperty("rowCount").GetInt32().ShouldBe(2);
        after.RootElement.GetProperty("format").GetString().ShouldBe("xlsx");
        after.RootElement.GetProperty("searchText").GetString().ShouldBe("nguyen");
        after.RootElement.GetProperty("status").GetString().ShouldBe("active");
        after.RootElement.GetProperty("sortBy").GetString().ShouldBe("email");
        entry.AfterValueJson!.ShouldNotContain("@vd.vn", Case.Insensitive, "nhật ký không chứa dữ liệu của người bị xuất (luật S13)");
    }

    [Fact]
    public async Task Handle_NamesTheFileAfterTheResourceAndTheInstant_WithTheRequestedFormat()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(0);

        var csv = (await Handler().Handle(new ExportUsersCommand(), default)).Value;
        var xlsx = (await Handler().Handle(new ExportUsersCommand(ExportFormats.Xlsx), default)).Value;

        csv.FileName.ShouldBe("users-20260921T030405Z.csv");
        csv.ContentType.ShouldBe("text/csv; charset=utf-8");
        xlsx.FileName.ShouldBe("users-20260921T030405Z.xlsx");
        xlsx.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task WriteTo_StreamsEveryRow_ThroughTheWriter_WithHeadersAndCodesNotSentences()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(2);
        _users.StreamAsync(default!, default, default).ReturnsForAnyArgs(Stream(User("an", false, "Admin", "Kế toán"), User("binh", true)));

        var file = (await Handler().Handle(new ExportUsersCommand(), default)).Value;
        var text = await WriteAsync(file);

        _writers.Format.ShouldBe(TabularFormat.Csv);
        _writers.Completed.ShouldBeTrue();
        _writers.Rows[0].ShouldBe(["UserName", "FullName", "Email", "Roles", "Status", "CreatedAt"]);
        _writers.Rows[1].ShouldBe(["an", "Họ an", "an@vd.vn", "Admin; Kế toán", "active", Now.ToString("O")]);
        _writers.Rows[2][4].ShouldBe("locked");
        text.ShouldContain("an@vd.vn");
    }

    [Fact]
    public async Task WriteTo_PassesTheCapToTheStream_SoDataGrowingAfterTheCountCannotExceedIt()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(1);
        _users.StreamAsync(default!, default, default).ReturnsForAnyArgs(Stream());

        var file = (await Handler(maxRows: 77).Handle(new ExportUsersCommand(), default)).Value;
        await WriteAsync(file);

        _users.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(IUserQueryService.StreamAsync)).GetArguments()[1].ShouldBe(77);
    }

    [Fact]
    public async Task WriteTo_TheXlsxFormat_SelectsTheSpreadsheetWriter()
    {
        _users.CountAsync(default!, default).ReturnsForAnyArgs(0);
        _users.StreamAsync(default!, default, default).ReturnsForAnyArgs(Stream());

        var file = (await Handler().Handle(new ExportUsersCommand(ExportFormats.Xlsx), default)).Value;
        await WriteAsync(file);

        _writers.Format.ShouldBe(TabularFormat.Xlsx);
    }

    // ---- Validator ---------------------------------------------------------------------------

    [Theory]
    [InlineData("csv", true)]
    [InlineData("xlsx", true)]
    [InlineData("pdf", false)]
    [InlineData("CSV", false)]
    [InlineData("", false)]
    public void Validator_FormatMustBeCsvOrXlsx(string format, bool valid)
    {
        var result = new ExportUsersCommandValidator().Validate(new ExportUsersCommand(format));

        result.IsValid.ShouldBe(valid);
        result.Errors.ShouldAllBe(e => e.ErrorCode == CommonErrors.Format.Code);
    }

    [Fact]
    public void Validator_UsesTheSameAllowlistsAsTheListEndpoint()
    {
        var export = new ExportUsersCommandValidator();
        var list = new GetUsersListQueryValidator();

        foreach (var sortBy in new[] { "userName", "fullName", "email", "createdAt", "password", "id" })
        {
            export.Validate(new ExportUsersCommand(SortBy: sortBy)).IsValid
                .ShouldBe(list.Validate(new GetUsersListQuery(SortBy: sortBy)).IsValid, $"sortBy={sortBy}");
        }

        foreach (var status in new[] { "active", "locked", "deleted" })
        {
            export.Validate(new ExportUsersCommand(Status: status)).IsValid
                .ShouldBe(list.Validate(new GetUsersListQuery(Status: status)).IsValid, $"status={status}");
        }

        export.Validate(new ExportUsersCommand(SearchText: new string('a', 201))).Errors
            .ShouldContain(e => e.ErrorCode == CommonErrors.MaxLength.Code);
    }

    [Fact]
    public void ExportFormats_ContentTypes()
    {
        ExportFormats.IsSupported("csv").ShouldBeTrue();
        ExportFormats.IsSupported(null).ShouldBeFalse();
        ExportFormats.ContentType("csv").ShouldStartWith("text/csv");
    }
}
