using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// B5 — docs/RULES.md: handler không tự gọi SaveChangesAsync / BeginTransactionAsync (docs/quy-uoc/be-cqrs-handler.md §3.1).
// Tầm quét: mọi tệp .cs của Core.Application; phép dò chỉ xét thân KIỂU CÀI IRequestHandler<…>.
public class HandlerSelfPersistenceTests
{
    [Fact]
    public void RequestHandlers_MustNotCall_SaveChangesOrBeginTransaction()
    {
        var offenders = ApplicationSourceFiles()
            .SelectMany(file => HandlerSelfPersistenceScanner.Scan(File.ReadAllText(file), file))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — tầm quét chạm handler thật, VÀ chạm một lời gọi SaveChangesAsync thật nằm ngoài handler (ImportJobExecutor):
    // lời gọi đó nằm trong tầm đọc mà không bị bắt, tức phạm vi "handler" là thứ loại nó — không phải tầm quét bỏ sót tệp.
    [Fact]
    public void RequestHandlers_MustNotCall_SaveChangesOrBeginTransaction_ScansRealHandlersAndARealNonHandlerSave()
    {
        var files = ApplicationSourceFiles();

        files.SelectMany(f => HandlerSelfPersistenceScanner.HandlerNames(File.ReadAllText(f)))
            .ShouldContain("CreateUserCommandHandler");

        var executor = files.Single(f => f.EndsWith("ImportJobExecutor.cs", StringComparison.Ordinal));
        var executorSource = File.ReadAllText(executor);
        executorSource.ShouldContain(".SaveChangesAsync(");
        HandlerSelfPersistenceScanner.HandlerNames(executorSource).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("await unitOfWork.SaveChangesAsync(ct);")]
    [InlineData("await db.Database.BeginTransactionAsync(ct);")]
    [InlineData("await uow.ExecuteInTransactionAsync(_ => Task.FromResult(new TransactionOutcome<int>(1, true)), ct);")]
    [InlineData("context?.SaveChanges();")]
    public void Detector_B5_Catches_HandlerThatPersistsItself(string statement)
    {
        var source = $$"""
            internal sealed class FakeCommandHandler(IUnitOfWork unitOfWork, FakeDb db, IUnitOfWork uow, FakeDb? context)
                : IRequestHandler<FakeCommand, Result>
            {
                public async Task<Result> Handle(FakeCommand command, CancellationToken ct)
                {
                    {{statement}}
                    return Result.Success();
                }
            }
            """;

        HandlerSelfPersistenceScanner.Scan(source, "fake.cs").ShouldNotBeEmpty();
    }

    // Đúng hình dạng thật của ImportJobExecutor: một lớp KHÔNG cài IRequestHandler, chạy trong job nền, lưu từng dòng.
    [Fact]
    public void Detector_B5_Ignores_SaveOutsideRequestHandlers()
    {
        const string source = """
            internal sealed class FakeJobExecutor(IUnitOfWork unitOfWork) : IJobExecutor
            {
                public async Task RunAsync(CancellationToken ct) => await unitOfWork.SaveChangesAsync(ct);
            }
            """;

        HandlerSelfPersistenceScanner.Scan(source, "fake.cs").ShouldBeEmpty();
    }

    // Handler gọi seam nghiệp vụ (không lưu) — hình dạng của mọi handler hiện có.
    [Fact]
    public void Detector_B5_Ignores_HandlerCallingDomainSeams()
    {
        const string source = """
            internal sealed class FakeQueryHandler(IRoleQueryService roles) : MediatR.IRequestHandler<FakeQuery, Result<int>>
            {
                public async Task<Result<int>> Handle(FakeQuery query, CancellationToken ct)
                    => Result.Success((await roles.FindExistingIdsAsync([], ct)).Count);
            }
            """;

        HandlerSelfPersistenceScanner.Scan(source, "fake.cs").ShouldBeEmpty();
        HandlerSelfPersistenceScanner.HandlerNames(source).ShouldBe(["FakeQueryHandler"], "chống test rỗng: kiểu phải được nhận là handler");
    }

    private static IReadOnlyList<string> ApplicationSourceFiles([CallerFilePath] string here = "")
    {
        var separator = Path.DirectorySeparatorChar;
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "Core", "CoreAndSkill.Core.Application"));

        return ProductSourceFiles.Core()
            .Where(f => f.StartsWith(root + separator, StringComparison.Ordinal))
            .ToList();
    }
}
