using CoreAndSkill.Core.Domain.Files;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Files;

public class StoredFileTests
{
    private static StoredFile NewFile()
        => StoredFile.Create("quyet-dinh.pdf", "application/pdf", 1234, "hs/2026/09/abc.pdf", "hs").Value;

    [Fact]
    public void Create_KeepsMetadata_AndStartsUnattached()
    {
        var file = NewFile();

        file.OriginalName.ShouldBe("quyet-dinh.pdf");
        file.ContentType.ShouldBe("application/pdf");
        file.SizeBytes.ShouldBe(1234);
        file.StorageKey.ShouldBe("hs/2026/09/abc.pdf");
        file.Purpose.ShouldBe("hs");
        file.IsAttached.ShouldBeFalse();
        file.OwnerTable.ShouldBeNull();
        file.OwnerId.ShouldBeNull();
        file.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void AttachTo_SetsTheOwner()
    {
        var file = NewFile();
        var owner = Guid.NewGuid();

        var result = file.AttachTo("banhang.don_hang", owner);

        result.IsSuccess.ShouldBeTrue();
        file.IsAttached.ShouldBeTrue();
        file.OwnerTable.ShouldBe("banhang.don_hang");
        file.OwnerId.ShouldBe(owner);
    }

    [Fact]
    public void AttachTo_TheSameOwnerAgain_IsIdempotent()
    {
        var file = NewFile();
        var owner = Guid.NewGuid();
        file.AttachTo("banhang.don_hang", owner);

        file.AttachTo("banhang.don_hang", owner).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AttachTo_ADifferentOwner_IsRejected_BecauseTheReadPermissionWouldChangeSilently()
    {
        var file = NewFile();
        file.AttachTo("banhang.don_hang", Guid.NewGuid());

        var otherRecord = file.AttachTo("banhang.don_hang", Guid.NewGuid());
        var otherTable = file.AttachTo("khac.bang", file.OwnerId!.Value);

        otherRecord.IsFailure.ShouldBeTrue();
        otherRecord.Error!.Code.ShouldBe(FileDomainErrors.AlreadyAttached.Code);
        otherTable.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Remove_SoftDeletes_AndKeepsTheStorageKeyForTheReconciliationJob()
    {
        var file = NewFile();

        file.Remove();

        file.IsDeleted.ShouldBeTrue();
        file.StorageKey.ShouldBe("hs/2026/09/abc.pdf");
    }
}
