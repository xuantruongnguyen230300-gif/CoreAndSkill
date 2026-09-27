using System.Text;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Infrastructure.Files;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Files;

// docs/wiki-core/be/14-file-storage.md §1, §2 (bố cục, tên do hệ thống sinh, đường dẫn qua cấu hình), §5.1 (ghi nguyên
// tử), §6 (tệp tạm). Chạy trên đĩa thật (thư mục tạm) — không cần Postgres.
public sealed class LocalFileStorageTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 0, 0, TimeSpan.Zero);

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("coreandskill-storage-test-");
    private readonly FixedClock _clock = new(Now);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private LocalFileStorage Storage()
        => new(Options.Create(new CoreFileOptions { RootPath = _root.FullName }), _clock);

    public void Dispose() => _root.Delete(recursive: true);

    private static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));

    private static async Task<string> ReadAsync(Stream stream)
    {
        await using (stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }

    [Fact]
    public async Task Save_LaysOutByPurposeThenYearThenMonth_WithASystemGeneratedName()
    {
        var key = await Storage().SaveAsync(Bytes("nội dung"), "ho-so", ".pdf", default);

        key.ShouldMatch(@"^ho-so/2026/09/[0-9a-f]{32}\.pdf$");
        File.Exists(Path.Combine(_root.FullName, key.Replace('/', Path.DirectorySeparatorChar))).ShouldBeTrue();
    }

    [Fact]
    public async Task Save_TwoFilesWithTheSameContent_GetDifferentKeys()
    {
        var storage = Storage();

        var a = await storage.SaveAsync(Bytes("x"), "ho-so", ".txt", default);
        var b = await storage.SaveAsync(Bytes("x"), "ho-so", ".txt", default);

        a.ShouldNotBe(b);
    }

    [Fact]
    public async Task Save_NoExtension_IsAllowed()
        => (await Storage().SaveAsync(Bytes("x"), "ho-so", string.Empty, default)).ShouldMatch(@"^ho-so/2026/09/[0-9a-f]{32}$");

    [Theory]
    [InlineData("../evil", ".pdf")]
    [InlineData("Ho-So", ".pdf")]
    [InlineData("ho so", ".pdf")]
    [InlineData("_tmp", ".pdf")]
    [InlineData("ho-so", "/../x")]
    [InlineData("ho-so", ".pdf.exe.zip.x")]
    [InlineData("ho-so", "pdf")]
    public async Task Save_RefusesAPurposeOrExtensionThatCouldEscapeOrBreakTheLayout(string purpose, string extension)
        => await Should.ThrowAsync<ArgumentException>(() => Storage().SaveAsync(Bytes("x"), purpose, extension, default));

    [Fact]
    public async Task OpenAsync_ReturnsExactlyWhatWasSaved()
    {
        var storage = Storage();
        var key = await storage.SaveAsync(Bytes("Xin chào — nội dung tệp"), "ho-so", ".txt", default);

        var opened = await storage.OpenAsync(key, default);

        opened.IsSuccess.ShouldBeTrue();
        (await ReadAsync(opened.Value)).ShouldBe("Xin chào — nội dung tệp");
    }

    [Fact]
    public async Task OpenAsync_AMissingFile_IsAResultNotAnException()
    {
        var opened = await Storage().OpenAsync("ho-so/2026/09/" + new string('a', 32) + ".pdf", default);

        opened.IsFailure.ShouldBeTrue();
        opened.Error!.Code.ShouldBe(FileErrors.ContentMissing.Code);
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("ho-so/../../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("ho-so/2026/09/../../../secret.txt")]
    [InlineData("ho-so/2026/09/notahexname.pdf")]
    [InlineData("")]
    [InlineData("_tmp/0123456789abcdef0123456789abcdef")]
    public async Task EveryOperation_RefusesAKeyThatIsNotInTheSystemGeneratedShape(string key)
    {
        var storage = Storage();

        await Should.ThrowAsync<ArgumentException>(() => storage.OpenAsync(key, default));
        await Should.ThrowAsync<ArgumentException>(() => storage.DeleteAsync(key, default));
        await Should.ThrowAsync<ArgumentException>(() => storage.ExistsAsync(key, default));
    }

    [Fact]
    public async Task ExistsAndDelete_Work_AndDeleteIsIdempotent()
    {
        var storage = Storage();
        var key = await storage.SaveAsync(Bytes("x"), "ho-so", ".txt", default);

        (await storage.ExistsAsync(key, default)).ShouldBeTrue();
        await storage.DeleteAsync(key, default);
        (await storage.ExistsAsync(key, default)).ShouldBeFalse();
        await Should.NotThrowAsync(() => storage.DeleteAsync(key, default));
    }

    [Fact]
    public async Task Save_FailingMidWay_LeavesNoPartFile_AndNoFinalFile()
    {
        var storage = Storage();

        await Should.ThrowAsync<IOException>(() => storage.SaveAsync(new ExplodingStream(), "ho-so", ".txt", default));

        Directory.EnumerateFiles(_root.FullName, "*", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public async Task List_ReturnsPermanentFilesOnly_WithTheirWriteTime_AndSkipsTempAndStrangers()
    {
        var storage = Storage();
        var kept = await storage.SaveAsync(Bytes("a"), "ho-so", ".txt", default);
        await storage.SaveTempAsync(Bytes("tạm"), default);
        File.WriteAllText(Path.Combine(_root.FullName, "ghi-chu.txt"), "không phải của ta");
        Directory.CreateDirectory(Path.Combine(_root.FullName, "ho-so", "linh-tinh"));
        File.WriteAllText(Path.Combine(_root.FullName, "ho-so", "linh-tinh", "la.txt"), "x");

        var listed = new List<StoredObject>();
        await foreach (var item in storage.ListAsync(default))
            listed.Add(item);

        listed.Select(l => l.Key).ShouldBe([kept]);
        listed.Single().LastWriteUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task List_IncludesLeftoverPartFiles_SoTheReconciliationJobCanCollectThem()
    {
        var storage = Storage();
        var directory = Path.Combine(_root.FullName, "ho-so", "2026", "09");
        Directory.CreateDirectory(directory);
        var name = new string('a', 32) + ".pdf.part";
        File.WriteAllText(Path.Combine(directory, name), "dở dang");

        var listed = new List<StoredObject>();
        await foreach (var item in storage.ListAsync(default))
            listed.Add(item);

        listed.Single().Key.ShouldBe("ho-so/2026/09/" + name);
    }

    [Fact]
    public async Task List_AnEmptyRoot_IsEmpty()
    {
        var listed = new List<StoredObject>();
        await foreach (var item in Storage().ListAsync(default))
            listed.Add(item);

        listed.ShouldBeEmpty();
    }

    // ---- Kho tạm ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Temp_SaveOpenDelete_RoundTrips_InADirectorySeparateFromPermanentFiles()
    {
        var storage = Storage();

        var key = await storage.SaveTempAsync(Bytes("tệp gốc"), default);

        key.ShouldMatch(@"^_tmp/[0-9a-f]{32}$");
        Directory.Exists(Path.Combine(_root.FullName, "_tmp")).ShouldBeTrue();
        (await ReadAsync((await storage.OpenTempAsync(key, default)).Value)).ShouldBe("tệp gốc");

        await storage.DeleteTempAsync(key, default);
        (await storage.OpenTempAsync(key, default)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Temp_APermanentKeyIsNotAcceptedAsATempKey_AndViceVersa()
    {
        var storage = Storage();
        var permanent = await storage.SaveAsync(Bytes("x"), "ho-so", ".txt", default);

        await Should.ThrowAsync<ArgumentException>(() => storage.OpenTempAsync(permanent, default));
        await Should.ThrowAsync<ArgumentException>(() => storage.DeleteTempAsync(permanent, default));
    }

    [Fact]
    public async Task PurgeTemp_RemovesOnlyFilesOlderThanTheCutoff()
    {
        var storage = Storage();
        var old = await storage.SaveTempAsync(Bytes("cũ"), default);
        var fresh = await storage.SaveTempAsync(Bytes("mới"), default);
        File.SetLastWriteTimeUtc(Path.Combine(_root.FullName, "_tmp", old[5..]), DateTime.UtcNow.AddDays(-3));

        var deleted = await storage.PurgeTempAsync(DateTimeOffset.UtcNow.AddDays(-1), default);

        deleted.ShouldBe(1);
        (await storage.OpenTempAsync(old, default)).IsFailure.ShouldBeTrue();
        var stillThere = await storage.OpenTempAsync(fresh, default);
        stillThere.IsSuccess.ShouldBeTrue();
        await stillThere.Value.DisposeAsync();
    }

    [Fact]
    public async Task PurgeTemp_NoTempDirectoryYet_IsZero()
        => (await Storage().PurgeTempAsync(DateTimeOffset.UtcNow, default)).ShouldBe(0);

    [Fact]
    public async Task PurgeTemp_NeverTouchesPermanentFiles()
    {
        var storage = Storage();
        var permanent = await storage.SaveAsync(Bytes("x"), "ho-so", ".txt", default);
        File.SetLastWriteTimeUtc(Path.Combine(_root.FullName, permanent.Replace('/', Path.DirectorySeparatorChar)), DateTime.UtcNow.AddYears(-1));

        await storage.PurgeTempAsync(DateTimeOffset.UtcNow, default);

        (await storage.ExistsAsync(permanent, default)).ShouldBeTrue();
    }

    private sealed class ExplodingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("đĩa hỏng giữa chừng");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new IOException("đĩa hỏng giữa chừng");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

// docs/wiki-core/be/14-file-storage.md §2: cấu hình được kiểm SÂU lúc khởi động — thư mục không tồn tại, không ghi
// được, hay đặt sai chỗ phải làm app không khởi động, chứ không phải hỏng ở lần tải tệp đầu tiên.
public sealed class CoreFileOptionsValidatorTests : IDisposable
{
    private readonly DirectoryInfo _outside = Directory.CreateTempSubdirectory("coreandskill-validator-");

    public void Dispose() => _outside.Delete(recursive: true);

    private static CoreFileOptionsValidator Validator(string contentRoot)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(contentRoot);
        return new CoreFileOptionsValidator(environment);
    }

    private static CoreFileOptions Options(string? path) => new() { RootPath = path! };

    [Fact]
    public void AnExistingWritableDirectoryOutsideTheApplication_IsValid()
        => Validator(Path.Combine(Path.GetTempPath(), "app-root-khac")).Validate(null, Options(_outside.FullName)).Succeeded.ShouldBeTrue();

    [Fact]
    public void AMissingKey_IsLeftToDataAnnotations_NotReportedTwice()
        => Validator("/app").Validate(null, Options(null)).Succeeded.ShouldBeTrue();

    [Fact]
    public void ARelativePath_IsRejected_BecauseItDependsOnTheWorkingDirectory()
    {
        var result = Validator("/app").Validate(null, Options("data/files"));

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("tuyệt đối");
    }

    [Fact]
    public void ADirectoryThatDoesNotExist_IsRejected_AndCoreDoesNotCreateIt()
    {
        var missing = Path.Combine(_outside.FullName, "chua-co");

        var result = Validator("/app").Validate(null, Options(missing));

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("không tồn tại");
        Directory.Exists(missing).ShouldBeFalse("Core không tự tạo thư mục gốc — thiếu thì phải là lỗi cấu hình");
    }

    [Fact]
    public void ADirectoryInsideTheContentRoot_IsRejected_BecauseARedeployWipesItAndStaticFilesServeIt()
    {
        var inside = Path.Combine(_outside.FullName, "wwwroot", "uploads");
        Directory.CreateDirectory(inside);

        var result = Validator(_outside.FullName).Validate(null, Options(inside));

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("thư mục ứng dụng");
    }

    [Fact]
    public void TheContentRootItself_IsRejected()
        => Validator(_outside.FullName).Validate(null, Options(_outside.FullName)).Failed.ShouldBeTrue();

    [Fact]
    public void ASiblingWhoseNameMerelyStartsWithTheContentRootName_IsNotMistakenForInside()
    {
        var app = Path.Combine(_outside.FullName, "app");
        var sibling = Path.Combine(_outside.FullName, "app-data");
        Directory.CreateDirectory(app);
        Directory.CreateDirectory(sibling);

        Validator(app).Validate(null, Options(sibling)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void ADirectoryTheProcessCannotWrite_IsRejected()
    {
        if (!OperatingSystem.IsWindows())
            return; // chỉ Windows đặt được ACL từ chối ghi mà không cần quyền đặc biệt — phần dưới dùng ACL.

        var readOnly = Path.Combine(_outside.FullName, "chi-doc");
        var directory = Directory.CreateDirectory(readOnly);
        var security = directory.GetAccessControl();
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent().User!;
        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
            identity,
            System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles,
            System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
            System.Security.AccessControl.PropagationFlags.None,
            System.Security.AccessControl.AccessControlType.Deny));
        directory.SetAccessControl(security);

        try
        {
            var result = Validator("/app").Validate(null, Options(readOnly));

            result.Failed.ShouldBeTrue();
            result.FailureMessage.ShouldContain("không ghi được");
        }
        finally
        {
            security.RemoveAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                identity,
                System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles,
                System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                System.Security.AccessControl.PropagationFlags.None,
                System.Security.AccessControl.AccessControlType.Deny));
            directory.SetAccessControl(security);
        }
    }

    [Fact]
    public void ThePurposeCatalogValidator_ReportsItsCountAtStartup()
    {
        var catalog = new FilePurposeCatalog([]);

        var service = new FilePurposeCatalogValidationHostedService(
            catalog, Microsoft.Extensions.Logging.Abstractions.NullLogger<FilePurposeCatalogValidationHostedService>.Instance);

        service.StartAsync(default).IsCompletedSuccessfully.ShouldBeTrue();
        service.StopAsync(default).IsCompletedSuccessfully.ShouldBeTrue();
    }
}
