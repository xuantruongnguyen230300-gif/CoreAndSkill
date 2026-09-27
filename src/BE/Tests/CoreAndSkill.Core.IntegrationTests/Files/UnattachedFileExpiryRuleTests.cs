using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Infrastructure.Files;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Files;

// docs/wiki-core/be/14-file-storage.md §3.1, §5.2 — tệp tải lên mà KHÔNG BAO GIỜ được gắn (mở form, tải tệp, bỏ form) phải
// có hạn. Không có bước này thì bản ghi owner_table NULL, is_deleted = false được đối soát coi là "còn sống" mãi mãi: tệp
// nằm vô thời hạn, người tải lên vẫn đọc được, không chỉ số nào báo.
//
// Test này chạy phép chọn (biểu thức EF) trên bộ nhớ — không cần Docker. Phép dịch sang SQL và việc gỡ thật trên PostgreSQL:
// FilesDatabaseTests.AnUnattachedFile_IsUnlinked_AfterTheRetention_ThenItsBytesFollowTheRemovedRecordPath (RequiresDocker).
public class UnattachedFileExpiryRuleTests
{
    private static readonly DateTimeOffset Cutoff = new(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);

    private static StoredFile File(DateTimeOffset? createdAt, bool attached = false, bool removed = false)
    {
        var file = StoredFile.Create("a.pdf", "application/pdf", 10, "ho-so/x.pdf", "ho-so").Value;
        file.CreatedAt = createdAt;
        if (attached)
            file.AttachTo("test.tai_lieu", Guid.NewGuid()).IsSuccess.ShouldBeTrue();
        if (removed)
            file.Remove();
        return file;
    }

    private static bool Selected(StoredFile file)
        => FileMaintenanceHostedService.IsUnattachedBefore(Cutoff).Compile()(file);

    [Fact]
    public void AnUnattachedFile_OlderThanTheCutoff_IsSelected()
        => Selected(File(Cutoff.AddSeconds(-1))).ShouldBeTrue();

    [Fact]
    public void AnUnattachedFile_ExactlyAtOrAfterTheCutoff_IsKept_TheFormMayStillBeOpen()
    {
        Selected(File(Cutoff)).ShouldBeFalse();
        Selected(File(Cutoff.AddHours(1))).ShouldBeFalse();
    }

    [Fact]
    public void AnAttachedFile_IsNeverSelected_HowOldSoEver()
        => Selected(File(Cutoff.AddDays(-365), attached: true)).ShouldBeFalse();

    [Fact]
    public void AnAlreadyRemovedFile_IsNotSelectedAgain_ItsUpdatedAtMustNotBePushedForward()
        => Selected(File(Cutoff.AddDays(-1), removed: true)).ShouldBeFalse(
            "gỡ lại sẽ đẩy UpdatedAt tới 'bây giờ' — tệp vật lý của nó không bao giờ qua được khoảng an toàn");

    [Fact]
    public void ARecordWithoutCreatedAt_IsNotSelected()
        => Selected(File(null)).ShouldBeFalse();
}
