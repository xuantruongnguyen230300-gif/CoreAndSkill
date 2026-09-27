using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Files;

// docs/contracts/files.md §2-§4, docs/luong/N2-dinh-kem-tep.md §6, 14-file-storage.md §3.1, §4.
// "Người không có quyền thì KHÔNG tải được, kể cả khi có đường dẫn" — mục 1 của nghiệm thu B4.
public class FileAccessAndDownloadTests
{
    private const string OwnerTable = "banhang.don_hang";

    private readonly Guid _me = Guid.NewGuid();
    private readonly FakeCurrentUser _currentUser;
    private readonly InMemoryFileStorage _storage = new();
    private readonly InMemoryFileRepository _repository = new();
    private readonly IFileOwnerAccessChecker _checker = Substitute.For<IFileOwnerAccessChecker>();

    public FileAccessAndDownloadTests()
    {
        _currentUser = new FakeCurrentUser(_me, "an.nv");
        _checker.OwnerTable.Returns(OwnerTable);
    }

    private FileAccessPolicy Policy(params IFileOwnerAccessChecker[] checkers) => new(_currentUser, checkers);

    private async Task<StoredFile> SeedAsync(string? uploadedBy = "an.nv", bool withContent = true)
    {
        var file = StoredFile.Create("hop-dong.pdf", "application/pdf", 5, $"hs/2026/09/{Guid.NewGuid():N}.pdf", "hs").Value;
        file.CreatedBy = uploadedBy;
        await _repository.AddAsync(file, CancellationToken.None);
        if (withContent)
            _storage.Files[file.StorageKey] = [1, 2, 3, 4, 5];

        return file;
    }

    private GetFileQueryHandler GetHandler(FileAccessPolicy policy)
        => new(_repository, policy, _storage, NullLogger<GetFileQueryHandler>.Instance);

    // ---- FileAccessPolicy ------------------------------------------------------------------

    [Fact]
    public async Task Unattached_OnlyTheUploaderCanRead()
    {
        var mine = await SeedAsync("an.nv");
        var theirs = await SeedAsync("binh.tt");

        (await Policy().CanReadAsync(mine, default)).ShouldBeTrue();
        (await Policy().CanReadAsync(theirs, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task Unattached_AnAnonymousCaller_CanReadNothing()
    {
        var file = await SeedAsync(uploadedBy: null);
        _currentUser.UserName = null;

        (await Policy().CanReadAsync(file, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task Attached_ReadFollowsTheOwnerRecordChecker_NotTheUploader()
    {
        var file = await SeedAsync("binh.tt");
        var owner = Guid.NewGuid();
        file.AttachTo(OwnerTable, owner);
        _checker.CanReadAsync(owner, Arg.Any<CancellationToken>()).Returns(true);

        (await Policy(_checker).CanReadAsync(file, default)).ShouldBeTrue();
        await _checker.Received(1).CanReadAsync(owner, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Attached_ButTheUploaderCannotReadTheOwnerRecord_IsDenied()
    {
        var file = await SeedAsync("an.nv");
        var owner = Guid.NewGuid();
        file.AttachTo(OwnerTable, owner);
        _checker.CanReadAsync(owner, Arg.Any<CancellationToken>()).Returns(false);

        (await Policy(_checker).CanReadAsync(file, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task Attached_ToATableNobodyDeclaredAChecker_IsDeniedByDefault()
    {
        var file = await SeedAsync("an.nv");
        file.AttachTo("bang.la", Guid.NewGuid());

        (await Policy(_checker).CanReadAsync(file, default)).ShouldBeFalse();
        (await Policy().CanWriteAsync(file, default)).ShouldBeFalse();
    }

    [Fact]
    public async Task Write_UsesTheWriteCheck_NotTheReadCheck()
    {
        var file = await SeedAsync();
        var owner = Guid.NewGuid();
        file.AttachTo(OwnerTable, owner);
        _checker.CanReadAsync(owner, Arg.Any<CancellationToken>()).Returns(true);
        _checker.CanWriteAsync(owner, Arg.Any<CancellationToken>()).Returns(false);

        (await Policy(_checker).CanWriteAsync(file, default)).ShouldBeFalse();
    }

    // ---- GET /files/{id} ---------------------------------------------------------------------

    [Fact]
    public async Task Get_Uploader_OfAnUnattachedFile_GetsTheContentWithTheSystemDecidedType()
    {
        var file = await SeedAsync("an.nv");

        var result = await GetHandler(Policy()).Handle(new GetFileQuery(file.Id), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ContentType.ShouldBe("application/pdf");
        result.Value.FileName.ShouldBe("hop-dong.pdf");
        using var reader = new MemoryStream();
        await result.Value.Content.CopyToAsync(reader);
        reader.ToArray().ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task Get_ANonUploader_GetsNotFound_NotForbidden_SoTheIdIsNotConfirmedToExist()
    {
        var file = await SeedAsync("binh.tt");

        var result = await GetHandler(Policy()).Handle(new GetFileQuery(file.Id), default);

        result.Error!.Code.ShouldBe(FileErrors.NotFound.Code);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_ANonExistentId_LooksIdenticalToAForbiddenOne()
    {
        var forbidden = await GetHandler(Policy()).Handle(new GetFileQuery((await SeedAsync("binh.tt")).Id), default);
        var missing = await GetHandler(Policy()).Handle(new GetFileQuery(Guid.NewGuid()), default);

        missing.Error!.Code.ShouldBe(forbidden.Error!.Code);
        missing.Error.Type.ShouldBe(forbidden.Error.Type);
    }

    [Fact]
    public async Task Get_APermittedFileWhoseContentIsGone_ReturnsContentMissing_NotNotFound()
    {
        var file = await SeedAsync("an.nv", withContent: false);

        var result = await GetHandler(Policy()).Handle(new GetFileQuery(file.Id), default);

        result.Error!.Code.ShouldBe(FileErrors.ContentMissing.Code);
    }

    [Fact]
    public async Task Get_NeverOpensTheContentBeforeThePermissionCheckPasses()
    {
        var storage = Substitute.For<IFileStorage>();
        var file = await SeedAsync("binh.tt");
        var handler = new GetFileQueryHandler(_repository, Policy(), storage, NullLogger<GetFileQueryHandler>.Instance);

        await handler.Handle(new GetFileQuery(file.Id), default);

        await storage.DidNotReceiveWithAnyArgs().OpenAsync(default!, default);
    }

    // ---- DELETE /files/{id} ------------------------------------------------------------------

    [Fact]
    public async Task Delete_ByTheUploader_SoftDeletes_AndLeavesThePhysicalFile()
    {
        var file = await SeedAsync("an.nv");

        var result = await new DeleteFileCommandHandler(_repository, Policy()).Handle(new DeleteFileCommand(file.Id), default);

        result.IsSuccess.ShouldBeTrue();
        file.IsDeleted.ShouldBeTrue();
        _storage.Files.ContainsKey(file.StorageKey).ShouldBeTrue("tệp vật lý dọn ở job đối soát, không xoá ngay trong request");
    }

    [Fact]
    public async Task Delete_ByANonWriter_IsNotFound_AndChangesNothing()
    {
        var file = await SeedAsync("binh.tt");

        var result = await new DeleteFileCommandHandler(_repository, Policy()).Handle(new DeleteFileCommand(file.Id), default);

        result.Error!.Code.ShouldBe(FileErrors.NotFound.Code);
        file.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_AnUnknownId_IsNotFound()
    {
        var result = await new DeleteFileCommandHandler(_repository, Policy()).Handle(new DeleteFileCommand(Guid.NewGuid()), default);

        result.Error!.Code.ShouldBe(FileErrors.NotFound.Code);
    }

    // ---- Gắn vào bản ghi chủ (bước 5 của N2) ---------------------------------------------------

    [Fact]
    public async Task Attach_ByTheUploader_LinksTheFileToTheOwnerRecord()
    {
        var file = await SeedAsync("an.nv");
        var owner = Guid.NewGuid();

        var result = await new FileAttachment(_repository, Policy()).AttachAsync(file.Id, OwnerTable, owner, default);

        result.IsSuccess.ShouldBeTrue();
        file.OwnerTable.ShouldBe(OwnerTable);
        file.OwnerId.ShouldBe(owner);
    }

    [Fact]
    public async Task Attach_ByAnotherUser_IsNotFound_BecauseAFileIdIsNotASecret()
    {
        var file = await SeedAsync("binh.tt");

        var result = await new FileAttachment(_repository, Policy()).AttachAsync(file.Id, OwnerTable, Guid.NewGuid(), default);

        result.Error!.Code.ShouldBe(FileErrors.NotFound.Code);
        file.IsAttached.ShouldBeFalse();
    }

    [Fact]
    public async Task Attach_AnAlreadyAttachedFile_FollowsTheWriteCheckOfItsCurrentOwner()
    {
        var file = await SeedAsync("an.nv");
        var owner = Guid.NewGuid();
        file.AttachTo(OwnerTable, owner);
        _checker.CanWriteAsync(owner, Arg.Any<CancellationToken>()).Returns(true);

        var sameOwner = await new FileAttachment(_repository, Policy(_checker)).AttachAsync(file.Id, OwnerTable, owner, default);
        var otherOwner = await new FileAttachment(_repository, Policy(_checker)).AttachAsync(file.Id, OwnerTable, Guid.NewGuid(), default);

        sameOwner.IsSuccess.ShouldBeTrue();
        otherOwner.Error!.Code.ShouldBe(FileDomainErrors.AlreadyAttached.Code);
    }

    [Fact]
    public async Task Attach_AnUnknownFile_IsNotFound()
    {
        var result = await new FileAttachment(_repository, Policy()).AttachAsync(Guid.NewGuid(), OwnerTable, Guid.NewGuid(), default);

        result.Error!.Code.ShouldBe(FileErrors.NotFound.Code);
    }
}
