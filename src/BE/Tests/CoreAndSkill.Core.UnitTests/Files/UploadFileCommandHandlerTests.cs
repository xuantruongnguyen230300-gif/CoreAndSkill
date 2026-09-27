using System.Text;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.UnitTests.Support;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Files;

// docs/contracts/files.md §1, 14-file-storage.md §5.1 (thứ tự thao tác), §7 (giới hạn), 09 §9 (kiểm nội dung).
public class UploadFileCommandHandlerTests
{
    private sealed class Source(params FilePurposeDefinition[] purposes) : IFilePurposeSource
    {
        public IReadOnlyCollection<FilePurposeDefinition> GetPurposes() => purposes;
    }

    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\n%âãÏÓ\n1 0 obj\n<<>>\nendobj\n");

    private readonly InMemoryFileStorage _storage = new();
    private readonly InMemoryFileRepository _repository;
    private readonly FilePurposeCatalog _catalog;

    public UploadFileCommandHandlerTests()
    {
        _repository = new InMemoryFileRepository(_storage.Operations);
        _catalog = new FilePurposeCatalog([
            new Source(
                new FilePurposeDefinition("ho-so", [FileContentDetector.Pdf, FileContentDetector.Png]),
                new FilePurposeDefinition("nho", [FileContentDetector.Pdf], MaxBytes: 20),
                new FilePurposeDefinition("lon", [FileContentDetector.Pdf], MaxBytes: 10L * 1024 * 1024 * 1024)),
        ]);
    }

    private UploadFileCommandHandler Handler(int maxUploadMb = 1)
        => new(_catalog, Options.Create(new CoreFileOptions { RootPath = "x", MaxUploadMb = maxUploadMb }), _storage, _repository);

    private static UploadFileCommand Command(byte[] bytes, string purpose = "ho-so", string name = "quyet-dinh.pdf")
        => new(new MemoryStream(bytes), name, purpose);

    [Fact]
    public async Task Handle_ValidPdf_StoresContent_AddsRecord_AndReturnsOnlyTheFourWireFields()
    {
        var result = await Handler().Handle(Command(PdfBytes), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.OriginalName.ShouldBe("quyet-dinh.pdf");
        result.Value.ContentType.ShouldBe(FileContentDetector.Pdf);
        result.Value.SizeBytes.ShouldBe(PdfBytes.Length);

        var stored = _repository.Files.Values.ShouldHaveSingleItem();
        stored.Id.ShouldBe(result.Value.Id);
        stored.Purpose.ShouldBe("ho-so");
        stored.IsAttached.ShouldBeFalse("tệp mới tải lên chưa gắn bản ghi chủ nào");
        _storage.Files.ContainsKey(stored.StorageKey).ShouldBeTrue();
        _storage.Files[stored.StorageKey].ShouldBe(PdfBytes);
    }

    [Fact]
    public async Task Handle_WritesTheFileBeforeTheRecord_SoAFailedTransactionLeavesHarmlessGarbage()
    {
        await Handler().Handle(Command(PdfBytes), CancellationToken.None);

        _storage.Operations.ShouldBe(["storage.save", "repository.add"]);
    }

    [Fact]
    public async Task Handle_StorageKeyAndExtension_AreSystemGenerated_NotFromTheClientName()
    {
        await Handler().Handle(Command(PdfBytes, name: "../../etc/evil.exe"), CancellationToken.None);

        var stored = _repository.Files.Values.Single();
        stored.StorageKey.ShouldStartWith("ho-so/");
        stored.StorageKey.ShouldEndWith(".pdf");
        stored.StorageKey.ShouldNotContain("evil");
        stored.OriginalName.ShouldBe("evil.exe", "tên gốc chỉ là siêu dữ liệu hiển thị, và đã bị bóc đường dẫn");
    }

    [Fact]
    public async Task Handle_UnknownPurpose_IsAValidationErrorOnThePurposeField()
    {
        var result = await Handler().Handle(Command(PdfBytes, purpose: "khong-co"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ValidationFailed.Code);
        result.Error.FieldErrors.ShouldContainKey("Purpose");
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyFile_IsAValidationErrorOnTheFileField()
    {
        var result = await Handler().Handle(Command([]), CancellationToken.None);

        result.Error!.Code.ShouldBe(CommonErrors.ValidationFailed.Code);
        result.Error.FieldErrors["File"].Single().Code.ShouldBe(CommonErrors.Required.Code);
    }

    [Fact]
    public async Task Handle_NonSeekableStream_IsRejected_BecauseTheHeaderMustBeReadThenRewound()
    {
        var command = new UploadFileCommand(new ForwardOnlyStream(PdfBytes), "a.pdf", "ho-so");

        var result = await Handler().Handle(command, CancellationToken.None);

        result.Error!.Code.ShouldBe(CommonErrors.ValidationFailed.Code);
        result.Error.FieldErrors["File"].Single().Code.ShouldBe(CommonErrors.Format.Code);
    }

    [Fact]
    public async Task Handle_LargerThanTheGlobalCap_IsTooLarge_AndNamesTheLimit()
    {
        var oneMegabyteAndOne = new byte[(1024 * 1024) + 1];
        Array.Copy(PdfBytes, oneMegabyteAndOne, PdfBytes.Length);

        var result = await Handler(maxUploadMb: 1).Handle(Command(oneMegabyteAndOne), CancellationToken.None);

        result.Error!.Code.ShouldBe(FileErrors.TooLarge.Code);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Params["MaxBytes"].ShouldBe((1024 * 1024).ToString());
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_APurposeCapTighterThanTheGlobalOne_Wins()
    {
        var result = await Handler().Handle(Command(PdfBytes, purpose: "nho"), CancellationToken.None);

        result.Error!.Code.ShouldBe(FileErrors.TooLarge.Code);
        result.Error.Params["MaxBytes"].ShouldBe("20");
    }

    [Fact]
    public async Task Handle_APurposeCapLooserThanTheGlobalOne_CannotRaiseTheGlobalCap()
    {
        var big = new byte[(1024 * 1024) + 1];
        Array.Copy(PdfBytes, big, PdfBytes.Length);

        var result = await Handler(maxUploadMb: 1).Handle(Command(big, purpose: "lon"), CancellationToken.None);

        result.Error!.Code.ShouldBe(FileErrors.TooLarge.Code);
        result.Error.Params["MaxBytes"].ShouldBe((1024 * 1024).ToString());
    }

    [Fact]
    public async Task Handle_PdfExtensionOnAnExecutable_IsRejectedByContent()
    {
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0xFF, 0xFE, 0x00 };

        var result = await Handler().Handle(Command(exe, name: "hop-dong.pdf"), CancellationToken.None);

        result.Error!.Code.ShouldBe(FileErrors.TypeNotAllowed.Code);
        _storage.Files.ShouldBeEmpty();
        _repository.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ARealTypeOutsideThePurposeAllowlist_IsRejected()
    {
        var gif = Encoding.ASCII.GetBytes("GIF89a......");

        var result = await Handler().Handle(Command(gif, name: "anh.gif"), CancellationToken.None);

        result.Error!.Code.ShouldBe(FileErrors.TypeNotAllowed.Code);
    }

    [Fact]
    public void Validator_RequiresTheFileAndThePurpose_WithCatalogCodes()
    {
        var validator = new UploadFileCommandValidator();

        var result = validator.Validate(new UploadFileCommand(null, null, ""));

        result.Errors.Select(e => e.ErrorCode).ShouldAllBe(code => code == CommonErrors.Required.Code);
        result.Errors.Select(e => e.PropertyName).ShouldBe(["File", "Purpose"], ignoreOrder: true);
    }

    [Fact]
    public void Validator_RejectsAPurposeLongerThanTheColumn()
        => new UploadFileCommandValidator()
            .Validate(new UploadFileCommand(new MemoryStream([1]), "a", new string('a', 51)))
            .Errors.ShouldContain(e => e.ErrorCode == CommonErrors.MaxLength.Code);

    private sealed class ForwardOnlyStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
