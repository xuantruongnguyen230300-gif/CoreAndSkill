using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// docs/wiki-core/be/14-file-storage.md §7. Cổng: mọi action nhận tệp phải khai [UploadSizeLimit]. Điểm mù và ranh giới
// của phép dò nằm ở đầu Support/UploadSizeLimitScanner.cs — đọc chúng trước khi coi cổng xanh là bằng chứng.
public class UploadSizeLimitDeclarationTests
{
    [Fact]
    public void EveryFormFileAction_DeclaresUploadSizeLimit()
    {
        var offenders = new[] { ArchitectureFixture.WebAssembly, ArchitectureFixture.HostAssembly }
            .SelectMany(UploadSizeLimitScanner.FindFileActionsWithoutSizeLimit)
            .ToList();

        offenders.ShouldBeEmpty(
            "action nhận tệp thiếu [UploadSizeLimit] — nội dung bị buffer hết trước khi validator nào kiểm được (14-file-storage.md §7)");
    }

    // T6 (phía BE): một bộ dò quét rỗng luôn PASS. Solution có ÍT NHẤT một action nhận tệp thật (POST /files) — nếu
    // bộ dò không tìm thấy nó thì cổng phía trên đang xanh vì mù, không vì sạch.
    [Fact]
    public void EveryFormFileAction_DeclaresUploadSizeLimit_ScansTheRealUploadAction()
    {
        var fileActions = UploadSizeLimitScanner.FindFileActions(ArchitectureFixture.WebAssembly);

        fileActions.ShouldContain($"{typeof(CoreAndSkill.Core.Web.Controllers.FilesController).FullName}.{nameof(CoreAndSkill.Core.Web.Controllers.FilesController.Upload)}");
    }

    [Fact]
    public void Detector_Catches_AnIFormFileParameterWithoutTheAttribute()
    {
        var offenders = UploadSizeLimitScanner.FindFileActionsWithoutSizeLimit(typeof(FakeUploadController).Assembly);

        offenders.ShouldContain($"{typeof(FakeUploadController).FullName}.{nameof(FakeUploadController.Bare)}");
    }

    [Fact]
    public void Detector_Catches_AModelWithAnIFormFileProperty_WithoutTheAttribute()
    {
        var offenders = UploadSizeLimitScanner.FindFileActionsWithoutSizeLimit(typeof(FakeUploadController).Assembly);

        offenders.ShouldContain($"{typeof(FakeUploadController).FullName}.{nameof(FakeUploadController.Model)}");
    }

    [Fact]
    public void Detector_Catches_ACollectionOfFiles_WithoutTheAttribute()
    {
        var offenders = UploadSizeLimitScanner.FindFileActionsWithoutSizeLimit(typeof(FakeUploadController).Assembly);

        offenders.ShouldContain($"{typeof(FakeUploadController).FullName}.{nameof(FakeUploadController.Many)}");
        offenders.ShouldContain($"{typeof(FakeUploadController).FullName}.{nameof(FakeUploadController.FormCollection)}");
    }

    [Fact]
    public void Detector_Ignores_AFileActionThatDeclaresTheAttribute()
    {
        var offenders = UploadSizeLimitScanner.FindFileActionsWithoutSizeLimit(typeof(FakeUploadController).Assembly);

        offenders.ShouldNotContain($"{typeof(FakeUploadController).FullName}.{nameof(FakeUploadController.Declared)}");
    }

    [Fact]
    public void Detector_Ignores_ActionsThatTakeNoFile()
    {
        var offenders = UploadSizeLimitScanner.FindFileActionsWithoutSizeLimit(typeof(FakeUploadController).Assembly);

        offenders.ShouldNotContain($"{typeof(FakeUploadController).FullName}.{nameof(FakeUploadController.NoFile)}");
        offenders.ShouldNotContain($"{typeof(FakeUploadController).FullName}.{nameof(FakeUploadController.PlainModel)}");
    }

    [ApiController]
    [Route("api/v1/core/fake-upload")]
    private sealed class FakeUploadController : ControllerBase
    {
        [HttpPost("bare")]
        public IActionResult Bare(IFormFile file) => Ok(file.Length);

        [HttpPost("model")]
        public IActionResult Model([FromForm] FileModel model) => Ok(model.Purpose);

        [HttpPost("many")]
        public IActionResult Many(List<IFormFile> files) => Ok(files.Count);

        [HttpPost("form-collection")]
        public IActionResult FormCollection(IFormCollection form) => Ok(form.Count);

        [HttpPost("declared")]
        [UploadSizeLimit]
        public IActionResult Declared(IFormFile file) => Ok(file.Length);

        [HttpPost("no-file")]
        public IActionResult NoFile(string name) => Ok(name);

        [HttpPost("plain-model")]
        public IActionResult PlainModel([FromBody] PlainRequest request) => Ok(request.Name);
    }

    private sealed class FileModel
    {
        public IFormFile? File { get; init; }

        public string? Purpose { get; init; }
    }

    private sealed class PlainRequest
    {
        public string? Name { get; init; }
    }
}
