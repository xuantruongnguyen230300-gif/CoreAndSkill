using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.UnitTests.Support;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Import;

// docs/wiki-core/be/15-import-export.md §6, docs/contracts/exports.md §2, docs/quy-uoc/be-cqrs-handler.md §10.3.
// Nghiệm thu B4 mục 5: "Tệp vượt Core:Import:MaxRows -> 422 ngay, không có bản ghi core.job nào được tạo".
public class StartImportCommandHandlerTests
{
    private readonly Guid _me = Guid.NewGuid();
    private readonly ScriptedImportDefinition _definition = new();
    private readonly InMemoryFileStorage _storage = new();
    private readonly InMemoryJobRepository _jobs = new();

    private StartImportCommandHandler Handler(FakeTabularReader reader, int maxRows = 5, int maxUploadMb = 20, IImportDefinition[]? definitions = null)
        => new(
            definitions ?? [_definition],
            reader,
            _storage,
            _jobs,
            new FakeCurrentUser(_me, "an.nv"),
            Options.Create(new CoreImportOptions { MaxRows = maxRows }),
            Options.Create(new CoreFileOptions { RootPath = "x", MaxUploadMb = maxUploadMb }));

    private static FakeTabularReader Reader(int rows, string[]? headers = null)
        => new(headers ?? ["Email", "Name"], Enumerable.Range(2, rows).Select(n => FakeTabularReader.Row(n, ("Email", $"u{n}@vd.vn"), ("Name", "x"))));

    private static StartImportCommand Command(string type = "x.import", byte[]? bytes = null)
        => new(type, new MemoryStream(bytes ?? [1, 2, 3]), "du-lieu.csv");

    [Fact]
    public async Task Handle_ValidFile_SavesTheSourceToTempStorage_ThenCreatesTheJob_AndReturnsItsId()
    {
        var result = await Handler(Reader(3)).Handle(Command(), default);

        result.IsSuccess.ShouldBeTrue();
        var job = _jobs.Jobs.Values.ShouldHaveSingleItem();
        job.Id.ShouldBe(result.Value);
        job.Type.ShouldBe("x.import");
        job.Status.ShouldBe(JobStatus.Queued);
        job.CreatedByUserId.ShouldBe(_me);

        // Tệp gốc ở kho TẠM, và khoá của nó đi theo sự kiện (core.job không có cột đầu vào).
        var tempKey = _storage.Temp.Keys.ShouldHaveSingleItem();
        var queued = job.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<JobQueuedEvent>();
        queued.Input.ShouldBe(tempKey);
        _storage.Temp[tempKey].ShouldBe([1, 2, 3], "nội dung tệp gốc phải được ghi trọn, sau khi đã đọc để đếm dòng");
    }

    [Fact]
    public async Task Handle_TheHandlerNeverCallsTheScheduler_ItOnlyRecordsAnEventForTheOutbox()
    {
        // Ràng buộc 3 của be-cqrs-handler.md §10.3: handler không nhận IBackgroundJobScheduler nào — chữ ký
        // constructor chính là bằng chứng; test này ghim nó.
        typeof(StartImportCommandHandler).GetConstructors().Single().GetParameters()
            .Select(p => p.ParameterType.Name)
            .ShouldNotContain("IBackgroundJobScheduler");

        await Handler(Reader(1)).Handle(Command(), default);

        _jobs.Jobs.Values.Single().DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<JobQueuedEvent>();
    }

    [Fact]
    public async Task Handle_MoreRowsThanTheCap_Is422_WithActualAndLimit_AndCreatesNothing()
    {
        var result = await Handler(Reader(6), maxRows: 5).Handle(Command(), default);

        result.Error!.Code.ShouldBe(ImportErrors.TooManyRows.Code);
        result.Error.Type.ShouldBe(ErrorType.BusinessRule);
        result.Error.Params["RowCount"].ShouldBe("6");
        result.Error.Params["MaxRows"].ShouldBe("5");
        _jobs.Jobs.ShouldBeEmpty("không có bản ghi core.job nào được tạo");
        _storage.Temp.ShouldBeEmpty("không có tệp tạm nào bị bỏ lại");
    }

    [Fact]
    public async Task Handle_ExactlyAtTheCap_IsAccepted()
    {
        var result = await Handler(Reader(5), maxRows: 5).Handle(Command(), default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ARequiredColumnMissing_IsColumnsMismatch_BeforeAnythingIsCreated()
    {
        var result = await Handler(Reader(2, headers: ["Name"])).Handle(Command(), default);

        result.Error!.Code.ShouldBe(ImportErrors.ColumnsMismatch.Code);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Params["MissingColumns"].ShouldBe("Email");
        _jobs.Jobs.ShouldBeEmpty();
        _storage.Temp.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_HeaderMatching_IgnoresCase()
    {
        var result = await Handler(Reader(2, headers: ["EMAIL", "name"])).Handle(Command(), default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_AnUnreadableFile_IsAValidationErrorOnTheFileField_WithoutEchoingItsContent()
    {
        var reader = new FakeTabularReader(["Email"], [], openFailure: new TabularFormatException("nội dung nhạy cảm: Abc@12345"));

        var result = await Handler(reader).Handle(Command(), default);

        result.Error!.Code.ShouldBe(CommonErrors.ValidationFailed.Code);
        result.Error.FieldErrors["File"].Single().Code.ShouldBe(CommonErrors.Format.Code);
        result.Error.MessageTemplate.ShouldNotContain("Abc@12345");
        _jobs.Jobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_AFileThatBreaksMidWayWhileCounting_IsAlsoAValidationError()
    {
        var reader = new FakeTabularReader(["Email"], [FakeTabularReader.Row(2, ("Email", "a@vd.vn"))], failAfterRows: new TabularFormatException("hỏng"));

        var result = await Handler(reader).Handle(Command(), default);

        result.Error!.Code.ShouldBe(CommonErrors.ValidationFailed.Code);
        _jobs.Jobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_NoDataRows_IsAValidationError()
    {
        var result = await Handler(Reader(0)).Handle(Command(), default);

        result.Error!.FieldErrors["File"].Single().Code.ShouldBe(CommonErrors.Required.Code);
        _jobs.Jobs.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_AnEmptyStream_IsAValidationError()
    {
        var result = await Handler(Reader(1)).Handle(Command(bytes: []), default);

        result.Error!.FieldErrors["File"].Single().Code.ShouldBe(CommonErrors.Required.Code);
    }

    [Fact]
    public async Task Handle_LargerThanTheUploadCap_IsTooLarge()
    {
        var oversize = new byte[(1024 * 1024) + 1];

        var result = await Handler(Reader(1), maxUploadMb: 1).Handle(Command(bytes: oversize), default);

        result.Error!.Code.ShouldBe(FileErrors.TooLarge.Code);
    }

    [Fact]
    public async Task Handle_UnknownImportType_IsRejected()
    {
        var result = await Handler(Reader(1)).Handle(Command(type: "khong.co"), default);

        result.Error!.Code.ShouldBe(ImportErrors.UnknownType.Code);
        result.Error.Params["ImportType"].ShouldBe("khong.co");
    }

    [Fact]
    public async Task Handle_WithoutASession_IsAProgrammingError()
    {
        var handler = new StartImportCommandHandler(
            [_definition], Reader(1), _storage, _jobs, new FakeCurrentUser(),
            Options.Create(new CoreImportOptions()), Options.Create(new CoreFileOptions { RootPath = "x" }));

        await Should.ThrowAsync<InvalidOperationException>(() => handler.Handle(Command(), default));
    }

    [Fact]
    public void Validator_RequiresTheFileAndTheType()
    {
        var result = new StartImportCommandValidator().Validate(new StartImportCommand("", null, null));

        result.Errors.Select(e => e.PropertyName).ShouldBe(["File", "Type"], ignoreOrder: true);
        result.Errors.ShouldAllBe(e => e.ErrorCode == CommonErrors.Required.Code);
    }

    [Fact]
    public void ImportErrorCatalog_HasTheDocumentedCodesAndTypes()
    {
        ImportErrors.ColumnsMismatch.Code.ShouldBe("CORE.IMPORT.COLUMNS_MISMATCH");
        ImportErrors.ColumnsMismatch.Type.ShouldBe(ErrorType.Validation);
        ImportErrors.TooManyRows.Code.ShouldBe("CORE.IMPORT.TOO_MANY_ROWS");
        ImportErrors.TooManyRows.Type.ShouldBe(ErrorType.BusinessRule);
        ImportErrors.DuplicateRow.Code.ShouldBe("CORE.IMPORT.DUPLICATE_ROW");
    }
}
