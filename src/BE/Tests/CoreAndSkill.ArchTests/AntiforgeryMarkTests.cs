using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Luật S20 — docs/RULES.md §6, docs/adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md quyết định 2 và 3:
// action dùng method AN TOÀN mà xuất dữ liệu hoặc ghi nhật ký kiểm toán phải khai [RequireAntiforgery]. Dấu là metadata
// của endpoint, nên middleware không phải đoán theo đường dẫn và cổng này soi được từng action.
//
// Điểm mù của phép dò nằm ở đầu Support/AntiforgeryMarkScanner.cs — đọc trước khi coi cổng xanh là bằng chứng.
public class AntiforgeryMarkTests
{
    // Tên kiểu request mà handler của nó với tới đường ghi nhật ký kiểm toán — dựng MỘT lần, dùng cho cả cổng và T6.
    private static readonly IReadOnlySet<string> AuditWritingRequests = AntiforgeryMarkScanner.AuditWritingRequests(
        AntiforgeryMarkScanner.DirectAuditWriters(ProductSourceFiles.Core()),
        ArchitectureFixture.ApplicationAssembly,
        ArchitectureFixture.InfrastructureAssembly,
        ArchitectureFixture.WebAssembly);

    [Fact]
    public void EveryGetActionWithSideEffects_RequiresAntiforgery()
    {
        var offenders = ProductSourceFiles.Core()
            .SelectMany(file => AntiforgeryMarkScanner.Scan(File.ReadAllText(file), AuditWritingRequests))
            .ToList();

        offenders.ShouldBeEmpty(
            "GET có tác dụng phụ mà thiếu [RequireAntiforgery] là một dòng nhật ký kiểm toán ai cũng giả được (ADR-0062)");
    }

    // T6 — ba tập đầu vào phải khác rỗng và chạm thứ CÓ THẬT. Thiếu test này, một phép dò trả rỗng vô điều kiện (sai tên
    // attribute HTTP, sai cách đọc thân phương thức, đồ thị constructor không đi tới đâu) vẫn để cổng trên xanh mãi mãi.
    [Fact]
    public void EveryGetActionWithSideEffects_RequiresAntiforgery_ScansRealActionsAndFindsTheRealAuditWritingRequest()
    {
        // 1. Có action method an toàn thật trong tầm quét — trong đó có chính action xuất.
        var safeActions = ProductSourceFiles.Core()
            .SelectMany(file => AntiforgeryMarkScanner.SafeMethodActions(File.ReadAllText(file)))
            .ToList();

        safeActions.ShouldContain("UsersController.Export");
        safeActions.ShouldContain("FilesController.Download");

        // 2. Đồ thị handler tìm đúng request ghi nhật ký kiểm toán của Core.
        AuditWritingRequests.ShouldContain(nameof(CoreAndSkill.Core.Application.Users.ExportUsersCommand));

        // 3. Hạt giống "ghi thẳng entity AuditLog" thấy hai đường ghi thật của Core.
        var directWriters = AntiforgeryMarkScanner.DirectAuditWriters(ProductSourceFiles.Core());
        directWriters.ShouldContain("AuditLogInterceptor");
        directWriters.ShouldContain("TenantProvisioningService");
    }

    // Đúng hình dạng thật của action xuất, chỉ thiếu dấu.
    [Fact]
    public void Detector_S20_Catches_AnExportActionWithoutTheMark()
    {
        const string source = """
            [ApiController]
            public sealed class FakeExportController : ApiControllerBase
            {
                [HttpGet("export")]
                [RequirePermission(CorePermissions.UserExport)]
                public async Task<IActionResult> Export([FromQuery] ExportUsersCommand command, CancellationToken ct)
                {
                    var result = await mediator.Send(command, ct);
                    return result.IsSuccess ? new ExportFileResult(result.Value) : HandleResult(result);
                }
            }
            """;

        AntiforgeryMarkScanner.Scan(source, new HashSet<string>(StringComparer.Ordinal))
            .ShouldContain(o => o.StartsWith("FakeExportController.Export", StringComparison.Ordinal));
    }

    // Hình dạng CHÍNH THỨC của nhánh trả tệp sau ADR-0064: action gọi ApiControllerBase.HandleExport, không tự dựng
    // ExportFileResult (lớp đó giữ internal nên module không với tới). auditWritingRequests rỗng có chủ đích — ca này
    // phải bị bắt bằng riêng nhánh CÚ PHÁP, không nhờ nhánh phản chiếu đỡ hộ.
    [Fact]
    public void Detector_S20_Catches_AnExportActionUsingHandleExport_WithoutTheMark()
    {
        const string source = """
            [ApiController]
            public sealed class FakeExportController : ApiControllerBase
            {
                [HttpGet("export")]
                [RequirePermission(CorePermissions.UserExport)]
                public async Task<IActionResult> Export([FromQuery] ExportUsersCommand command, CancellationToken ct)
                    => HandleExport(await mediator.Send(command, ct));
            }
            """;

        AntiforgeryMarkScanner.Scan(source, new HashSet<string>(StringComparer.Ordinal))
            .ShouldContain(o => o.StartsWith("FakeExportController.Export", StringComparison.Ordinal));
    }

    // T6 cho ADR-0064 — canary cắm vào SOURCE THẬT. Phép dò nhận diện action xuất bằng một cái TÊN; đổi tên helper ở
    // ApiControllerBase mà quên phép dò thì cổng trên vẫn xanh vĩnh viễn vì nó không còn thấy action xuất nào cả.
    // Ca này gỡ đúng dòng [RequireAntiforgery] khỏi UsersController.cs thật và đòi phép dò phải kêu.
    [Fact]
    public void Detector_S20_Catches_TheRealExportAction_WhenItsMarkIsRemoved()
    {
        var controller = ProductSourceFiles.Core().Single(f => Path.GetFileName(f) == "UsersController.cs");
        var withoutMark = File.ReadAllText(controller).Replace("[RequireAntiforgery]", string.Empty, StringComparison.Ordinal);

        AntiforgeryMarkScanner.Scan(withoutMark, new HashSet<string>(StringComparer.Ordinal))
            .ShouldContain(o => o.StartsWith("UsersController.Export", StringComparison.Ordinal),
                "phép dò không còn nhận ra hình dạng nhánh trả tệp đang dùng thật — cổng S20 đã mù");
    }

    // ADR-0068 — thêm lối HandleFile KHÔNG được làm cổng S20 đổi nghĩa. FilesController.Download là một GET KHÔNG
    // tác dụng phụ: nó nằm trong tầm quét (ca T6 ở trên khẳng định điều đó) và phải KHÔNG bị liệt vào offender.
    // Khẳng định "không có gì xảy ra" này là phần dễ quên nhất của ADR, và nó là thứ phân biệt "cổng còn đúng" với
    // "cổng đã mù": hai lối tệp tách nhau bằng KIỂU THAM SỐ, nên dấu hiệu cú pháp `HandleExport` vẫn chỉ trỏ vào
    // endpoint xuất. Gộp hai nhánh vào một tên sẽ làm ca này đỏ — đó là phương án B mà ADR loại.
    [Fact]
    public void Detector_S20_Ignores_TheRealDownloadAction_BecauseItGoesThroughHandleFile()
    {
        var controller = ProductSourceFiles.Core().Single(file => Path.GetFileName(file) == "FilesController.cs");
        var source = File.ReadAllText(controller);

        source.Contains("HandleFile(", StringComparison.Ordinal).ShouldBeTrue(
            "ca này chỉ có nghĩa khi Download thật đi qua HandleFile — ADR-0068");

        AntiforgeryMarkScanner.SafeMethodActions(source).ShouldContain("FilesController.Download");
        AntiforgeryMarkScanner.Scan(source, AuditWritingRequests)
            .ShouldNotContain(o => o.StartsWith("FilesController.Download", StringComparison.Ordinal),
                "tải tệp là GET không tác dụng phụ — đòi [RequireAntiforgery] ở đây là bắt FE gửi token cho mọi lần tải");
    }

    // Ca thứ hai của luật: GET không trả tệp nhưng handler của request nó gửi có ghi nhật ký kiểm toán.
    [Fact]
    public void Detector_S20_Catches_AGetActionSendingAnAuditWritingRequest()
    {
        const string source = """
            public sealed class FakeReportController : ApiControllerBase
            {
                [HttpGet("{id:guid}")]
                public async Task<IActionResult> Read(Guid id, CancellationToken ct)
                    => HandleResult(await mediator.Send(new ReadSensitiveRecordQuery(id), ct));
            }
            """;

        var audited = new HashSet<string>(StringComparer.Ordinal) { "ReadSensitiveRecordQuery" };

        AntiforgeryMarkScanner.Scan(source, audited)
            .ShouldContain(o => o.StartsWith("FakeReportController.Read", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_S20_Ignores_AnExportActionThatDeclaresTheMark()
    {
        const string source = """
            public sealed class FakeExportController : ApiControllerBase
            {
                [HttpGet("export")]
                [RequireAntiforgery]
                public async Task<IActionResult> Export([FromQuery] ExportUsersCommand command, CancellationToken ct)
                {
                    var result = await mediator.Send(command, ct);
                    return result.IsSuccess ? new ExportFileResult(result.Value) : HandleResult(result);
                }
            }
            """;

        AntiforgeryMarkScanner.Scan(source, new HashSet<string>(StringComparer.Ordinal) { "ExportUsersCommand" }).ShouldBeEmpty();
    }

    // Hai ca KHÔNG phải vi phạm: GET đọc thuần (không tác dụng phụ), và lệnh GHI (middleware đã kiểm theo method — dấu ở
    // đó là thừa, và đòi nó sẽ biến cổng thành một danh sách attribute phải chép lên mọi action).
    [Fact]
    public void Detector_S20_Ignores_APlainReadGet_AndAnyWriteAction()
    {
        const string source = """
            public sealed class FakeUsersController : ApiControllerBase
            {
                [HttpGet]
                public async Task<IActionResult> GetList([FromQuery] GetUsersListQuery query, CancellationToken ct)
                    => HandleResult(await mediator.Send(query, ct));

                [HttpPost]
                public async Task<IActionResult> Create([FromBody] CreateUserRequest body, CancellationToken ct)
                    => HandleResult(await mediator.Send(new CreateUserCommand(body.UserName), ct));
            }
            """;

        var audited = new HashSet<string>(StringComparer.Ordinal) { "CreateUserCommand" };

        AntiforgeryMarkScanner.Scan(source, audited).ShouldBeEmpty();
    }
}
