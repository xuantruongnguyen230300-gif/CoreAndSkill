using System.Text.Json;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Import;

// docs/wiki-core/be/15-import-export.md §4 (lỗi từng dòng), §4.2 (rò entity chưa lưu từ dòng lỗi sang dòng
// sau), §6.1. Nghiệm thu B4 mục 4: "Nhập một tệp có một dòng sai ở giữa -> các dòng hợp lệ khác không bị dở dang".
//
// Thứ test này KHÔNG chứng minh được: dòng lỗi thật sự không có trong DB. Đó là việc của integration test trên
// PostgreSQL thật (luật T8) — ImportRowIntegrationTests.MiddleErrorRow_IsNotWritten_WhenTheNextValidRowIsSaved.
// Ở đây chứng minh CƠ CHẾ: bộ chạy HUỶ THEO DÕI sau mỗi dòng, đúng thứ tự.
public class ImportJobExecutorTests
{
    private const string TempKey = "_tmp/0123456789abcdef0123456789abcdef";

    private readonly ScriptedImportDefinition _definition = new();
    private readonly RecordingRowWriter _rowWriter = new();
    private readonly InMemoryFileStorage _storage = new();
    private readonly InMemoryFileRepository _files = new();
    private readonly PassThroughUnitOfWork _uow = new();
    private readonly CapturingWriterFactory _writerFactory = new();
    private readonly List<int> _progress = [];

    public ImportJobExecutorTests() => _storage.Temp[TempKey] = [1, 2, 3];

    private ImportJobExecutor Executor(FakeTabularReader reader, int maxFailedInResult = 100)
        => new(
            [_definition], reader, _writerFactory, _storage, _files, _rowWriter, _uow,
            Options.Create(new CoreImportOptions { MaxFailedRowsInResult = maxFailedInResult }),
            NullLogger<ImportJobExecutor>.Instance);

    private static FakeTabularReader Rows(params (int Line, string Email)[] rows)
        => new(["Email", "Name"], rows.Select(r => FakeTabularReader.Row(r.Line, ("Email", r.Email), ("Name", "x"))));

    private JobExecutionContext Context(string? input = TempKey, string type = "x.import")
        => new(Guid.NewGuid(), type, input, p =>
        {
            _progress.Add(p);
            return Task.CompletedTask;
        });

    private static JsonElement ParseResult(JobOutcome outcome) => JsonDocument.Parse(outcome.ResultJson!).RootElement;

    [Fact]
    public void Handles_EveryTypeADefinitionDeclared_AndNothingElse()
    {
        var executor = Executor(Rows());

        executor.Handles("x.import").ShouldBeTrue();
        executor.Handles("loai.la").ShouldBeFalse();
    }

    [Fact]
    public async Task Execute_AllRowsValid_SavesEachOne_AndReportsThem()
    {
        var outcome = await Executor(Rows((2, "a@vd.vn"), (3, "b@vd.vn"), (4, "c@vd.vn"))).ExecuteAsync(Context(), default);

        outcome.IsSuccess.ShouldBeTrue();
        var result = ParseResult(outcome);
        result.GetProperty("totalRows").GetInt32().ShouldBe(3);
        result.GetProperty("succeeded").GetInt32().ShouldBe(3);
        result.GetProperty("failedCount").GetInt32().ShouldBe(0);
        result.GetProperty("failed").GetArrayLength().ShouldBe(0);
        outcome.ResultFileId.ShouldBeNull("không có dòng lỗi thì không có tệp kết quả");
        _definition.Processed.ShouldBe([2, 3, 4]);
    }

    [Fact]
    public async Task Execute_ARowInTheMiddleFails_DiscardsItsTrackedChanges_BeforeTheNextRowIsSaved()
    {
        // Đây là bẫy §4.2: dòng 3 (giữa) sai; nếu bộ theo dõi không được huỷ, entity của nó bị ghi cùng dòng 4.
        _definition.Results[3] = ImportRowResult.Failed(
            new Error("CORE.VALIDATION.FAILED", "Email sai định dạng", ErrorType.Validation), field: "Email");

        var outcome = await Executor(Rows((2, "a@vd.vn"), (3, "sai"), (4, "c@vd.vn"))).ExecuteAsync(Context(), default);

        // Dòng 2: save + discard. Dòng 3: KHÔNG save, nhưng discard. Dòng 4: save + discard. Cuối cùng còn một
        // discard nữa của việc lưu tệp kết quả (bản ghi tệp vừa được lưu, không để nó nằm lại trong bộ theo dõi).
        _rowWriter.Calls.ShouldBe(["save", "discard", "discard", "save", "discard", "discard"]);

        var result = ParseResult(outcome);
        result.GetProperty("totalRows").GetInt32().ShouldBe(3);
        result.GetProperty("succeeded").GetInt32().ShouldBe(2, "các dòng hợp lệ khác không bị dở dang");
        var failed = result.GetProperty("failed").EnumerateArray().ShouldHaveSingleItem();
        failed.GetProperty("row").GetInt32().ShouldBe(3, "số dòng là số dòng trong tệp gốc");
        failed.GetProperty("code").GetString().ShouldBe("CORE.VALIDATION.FAILED");
        failed.GetProperty("field").GetString().ShouldBe("Email");
        failed.GetProperty("message").GetString().ShouldBe("Email sai định dạng");
    }

    [Fact]
    public async Task Execute_TheRowNumberIsTheFileLine_EvenWhenBlankLinesAreSkippedByTheReader()
    {
        _definition.Results[9] = ImportRowResult.Failed(ImportErrors.RowNotSaved);

        var outcome = await Executor(Rows((2, "a@vd.vn"), (9, "b@vd.vn"))).ExecuteAsync(Context(), default);

        ParseResult(outcome).GetProperty("failed")[0].GetProperty("row").GetInt32().ShouldBe(9);
    }

    [Fact]
    public async Task Execute_DiscardsAfterEveryRow_EvenTheValidOnes_SoTheTrackerDoesNotGrowWithTheRowCount()
    {
        await Executor(Rows((2, "a@vd.vn"), (3, "b@vd.vn"), (4, "c@vd.vn"))).ExecuteAsync(Context(), default);

        _rowWriter.Calls.Count(c => c == "discard").ShouldBe(3);
    }

    [Fact]
    public async Task Execute_TheDatabaseRejectsARow_ItIsReportedAsFailed_AndTheOthersStillLand()
    {
        _rowWriter.RejectSaveNumbers.Add(2);

        var outcome = await Executor(Rows((2, "a@vd.vn"), (3, "b@vd.vn"), (4, "c@vd.vn"))).ExecuteAsync(Context(), default);

        var result = ParseResult(outcome);
        result.GetProperty("succeeded").GetInt32().ShouldBe(2);
        var failed = result.GetProperty("failed").EnumerateArray().ShouldHaveSingleItem();
        failed.GetProperty("row").GetInt32().ShouldBe(3);
        failed.GetProperty("code").GetString().ShouldBe(ImportErrors.RowNotSaved.Code);
    }

    [Fact]
    public async Task Execute_ARowWhoseKeyAlreadyExists_IsSkippedAsDuplicate_NotOverwritten()
    {
        _definition.ExistingKeys.Add("b@vd.vn");

        var outcome = await Executor(Rows((2, "a@vd.vn"), (3, "b@vd.vn"))).ExecuteAsync(Context(), default);

        _definition.Processed.ShouldBe([2], "dòng trùng KHÔNG được xử lý nên không thể ghi đè");
        ParseResult(outcome).GetProperty("failed")[0].GetProperty("code").GetString().ShouldBe("CORE.IMPORT.DUPLICATE_ROW");
    }

    [Fact]
    public async Task Execute_ADuplicateOfAnEarlierRowInTheSameFile_IsSkipped()
    {
        var outcome = await Executor(Rows((2, "a@vd.vn"), (3, "a@vd.vn"))).ExecuteAsync(Context(), default);

        _definition.Processed.ShouldBe([2]);
        var result = ParseResult(outcome);
        result.GetProperty("succeeded").GetInt32().ShouldBe(1);
        result.GetProperty("failed")[0].GetProperty("row").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Execute_ARowThatFailedValidation_DoesNotReserveItsKey_ForALaterCorrectedRow()
    {
        _definition.Results[2] = ImportRowResult.Failed(ImportErrors.RowNotSaved, "Email");

        var outcome = await Executor(Rows((2, "a@vd.vn"), (3, "a@vd.vn"))).ExecuteAsync(Context(), default);

        _definition.Processed.ShouldBe([2, 3]);
        ParseResult(outcome).GetProperty("succeeded").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Execute_ReRunningTheSameFile_DoesNotDuplicate_BecauseEveryKeyNowExists()
    {
        // Lần 1: cả hai dòng được ghi.
        await Executor(Rows((2, "a@vd.vn"), (3, "b@vd.vn"))).ExecuteAsync(Context(), default);
        // Giả lập DB sau lần 1.
        _definition.ExistingKeys.UnionWith(["a@vd.vn", "b@vd.vn"]);
        _storage.Temp[TempKey] = [1, 2, 3];
        _definition.Processed.Clear();

        var second = await Executor(Rows((2, "a@vd.vn"), (3, "b@vd.vn"))).ExecuteAsync(Context(), default);

        _definition.Processed.ShouldBeEmpty();
        var result = ParseResult(second);
        result.GetProperty("succeeded").GetInt32().ShouldBe(0);
        result.GetProperty("failedCount").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Execute_WithFailures_WritesTheFullListToAResultFile_OwnedByTheJob_AndSavesItBeforeFinishing()
    {
        _definition.Results[2] = ImportRowResult.Failed(new Error("A.B.C", "Sai {X}", ErrorType.Validation).WithParams(("X", "ở đây")), "Name");
        _definition.Results[3] = ImportRowResult.Failed(ImportErrors.RowNotSaved);
        var context = Context();

        var outcome = await Executor(Rows((2, "a@vd.vn"), (3, "b@vd.vn"), (4, "c@vd.vn"))).ExecuteAsync(context, default);

        var fileId = outcome.ResultFileId.ShouldNotBeNull();
        var file = _files.Files[fileId];
        file.OwnerTable.ShouldBe("core.job");
        file.OwnerId.ShouldBe(context.JobId);
        file.Purpose.ShouldBe(CoreFilePurposes.JobResult);
        file.ContentType.ShouldBe(FileContentDetector.Csv);
        file.SizeBytes.ShouldBeGreaterThan(0);
        _storage.Files.ContainsKey(file.StorageKey).ShouldBeTrue();

        // Bản ghi tệp được LƯU ngay (khoá ngoại job.result_file_id đòi nó có trước khi ghi trạng thái cuối).
        _uow.Saves.ShouldBe(1);

        _writerFactory.Format.ShouldBe(TabularFormat.Csv);
        _writerFactory.Completed.ShouldBeTrue();
        _writerFactory.Rows[0].ShouldBe(["Row", "Code", "Field", "Message", "Value"]);
        _writerFactory.Rows[1].ShouldBe(["2", "A.B.C", "Name", "Sai ở đây", "x"]);
        _writerFactory.Rows[2].Take(3).ShouldBe(["3", ImportErrors.RowNotSaved.Code, null]);
    }

    [Fact]
    public async Task Execute_TheEmbeddedFailedList_IsCapped_ButTheCountAndTheFileAreComplete()
    {
        for (var line = 2; line <= 6; line++)
            _definition.Results[line] = ImportRowResult.Failed(ImportErrors.RowNotSaved);

        var rows = Enumerable.Range(2, 5).Select(n => (n, $"u{n}@vd.vn")).ToArray();
        var outcome = await Executor(Rows(rows), maxFailedInResult: 2).ExecuteAsync(Context(), default);

        var result = ParseResult(outcome);
        result.GetProperty("failedCount").GetInt32().ShouldBe(5);
        result.GetProperty("failed").GetArrayLength().ShouldBe(2);
        _writerFactory.Rows.Count.ShouldBe(1 + 5, "tệp kết quả giữ ĐẦY ĐỦ năm dòng lỗi");
    }

    [Fact]
    public async Task Execute_ReportsProgress_EveryFiftyRows_NeverReaching100BeforeTheJobIsRecordedAsDone()
    {
        var rows = Enumerable.Range(2, 120).Select(n => (n, $"u{n}@vd.vn")).ToArray();

        await Executor(Rows(rows)).ExecuteAsync(Context(), default);

        _progress.ShouldBe([41, 83]);
        _progress.ShouldAllBe(p => p < 100);
    }

    [Fact]
    public async Task Execute_TheTempSourceIsDeleted_AtTheEnd_OnSuccess()
    {
        await Executor(Rows((2, "a@vd.vn"))).ExecuteAsync(Context(), default);

        _storage.Temp.ShouldBeEmpty();
    }

    [Fact]
    public async Task Execute_NoInputReference_FailsAsSourceUnreadable()
    {
        var outcome = await Executor(Rows()).ExecuteAsync(Context(input: null), default);

        outcome.Error!.Code.ShouldBe(ImportErrors.SourceUnreadable.Code);
    }

    [Fact]
    public async Task Execute_TheTempFileHasExpired_FailsAsSourceUnreadable()
    {
        _storage.Temp.Clear();

        var outcome = await Executor(Rows((2, "a@vd.vn"))).ExecuteAsync(Context(), default);

        outcome.Error!.Code.ShouldBe(ImportErrors.SourceUnreadable.Code);
        _definition.Processed.ShouldBeEmpty();
    }

    [Fact]
    public async Task Execute_ACorruptFile_FailsTheJob_OnlyBecauseTheFileCannotBeRead_AndCleansUp()
    {
        var reader = new FakeTabularReader(["Email"], [], openFailure: new TabularFormatException("hỏng"));

        var outcome = await Executor(reader).ExecuteAsync(Context(), default);

        outcome.Error!.Code.ShouldBe(ImportErrors.SourceUnreadable.Code);
        _storage.Temp.ShouldBeEmpty();
    }

    [Fact]
    public async Task Execute_TheFileBreaksMidWay_TheRowsAlreadyWrittenStay_AndTheJobFails()
    {
        // Đếm lần đầu đọc trọn được, lần đọc thứ hai hỏng sau dòng 2: nhập một phần rồi tệp hỏng.
        var reader = new FlakyReader();

        var outcome = await new ImportJobExecutor(
            [_definition], reader, _writerFactory, _storage, _files, _rowWriter, _uow,
            Options.Create(new CoreImportOptions()), NullLogger<ImportJobExecutor>.Instance)
            .ExecuteAsync(Context(), default);

        outcome.Error!.Code.ShouldBe(ImportErrors.SourceUnreadable.Code);
        _definition.Processed.ShouldBe([2], "dòng đã ghi trước khi tệp hỏng giữ nguyên");
    }

    [Fact]
    public async Task Execute_AnExceptionFromTheDefinition_IsNotSwallowedPerRow()
    {
        var definition = new ThrowingDefinition();
        var executor = new ImportJobExecutor(
            [definition], Rows((2, "a@vd.vn")), _writerFactory, _storage, _files, _rowWriter, _uow,
            Options.Create(new CoreImportOptions()), NullLogger<ImportJobExecutor>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() => executor.ExecuteAsync(Context(type: "boom.import"), default));

        _storage.Temp.ShouldBeEmpty("dù ném, tệp tạm vẫn được dọn (finally)");
    }

    // Lần mở đầu (đếm) chạy trọn; lần mở sau hỏng sau khi đưa ra dòng đầu.
    private sealed class FlakyReader : ITabularReader
    {
        private int _opens;

        public Task<ITabularSource> OpenAsync(Stream content, CancellationToken ct)
        {
            _opens++;
            var rows = new[] { FakeTabularReader.Row(2, ("Email", "a@vd.vn")), FakeTabularReader.Row(3, ("Email", "b@vd.vn")) };
            return Task.FromResult<ITabularSource>(new FakeTabularSource(
                ["Email"], _opens == 1 ? rows : rows[..1], _opens == 1 ? null : new TabularFormatException("hỏng giữa chừng")));
        }
    }

    private sealed class ThrowingDefinition : IImportDefinition
    {
        public string Type => "boom.import";

        public IReadOnlyList<ImportColumn> Columns { get; } = [new("Email", true)];

        public string NaturalKey(TabularRow row) => "k";

        public Task<bool> NaturalKeyExistsAsync(string naturalKey, CancellationToken ct) => Task.FromResult(false);

        public Task<ImportRowResult> ImportRowAsync(TabularRow row, CancellationToken ct)
            => throw new InvalidOperationException("lỗi lập trình trong định nghĩa");
    }
}
