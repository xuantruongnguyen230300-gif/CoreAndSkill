using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Luật B16 — docs/quy-uoc/be-api-controller.md §3, ADR-0064, ADR-0068: bốn tên `Handle*` của ApiControllerBase là
// chỗ DUY NHẤT một controller đặt status code. Một action tự dựng IActionResult là một nguồn thứ hai cho ánh xạ
// Result → HTTP, cho hình dạng envelope, và — với nhánh tệp — cho ba header bắt buộc; bản quên một trong ba không
// làm gì đỏ. Đó chính là ca đã xảy ra thật ở FilesController.Download trước ADR-0068.
//
// Điểm mù của phép dò nằm ở đầu Support/ControllerResultPathScanner.cs — đọc trước khi coi cổng xanh là bằng chứng.
public class ControllerResultPathTests
{
    // Ngoại lệ CÓ TÊN, khai ở đúng một chỗ — hôm nay RỖNG, và đó là điều kiện ADR-0068 đặt ra để cổng này dựng
    // được ("không cần ngoại lệ nào"). Thêm một tên vào đây là mở một lối đặt status ngoài ApiControllerBase: cần
    // ADR trước, không phải một dòng sửa cho cổng xanh trở lại.
    private static readonly IReadOnlySet<string> NamedExceptions = new HashSet<string>(StringComparer.Ordinal);

    [Fact]
    public void EveryControllerAction_PutsItsStatusCode_ThroughApiControllerBase()
    {
        var offenders = ProductSourceFiles.Core()
            .SelectMany(file => ControllerResultPathScanner.Scan(File.ReadAllText(file), NamedExceptions))
            .ToList();

        offenders.ShouldBeEmpty(
            "controller tự dựng IActionResult là nguồn thứ hai cho status code, envelope và ba header của phản hồi tệp (B16)");
    }

    // T6 — tập đầu vào phải khác rỗng và chạm thứ CÓ THẬT. Thiếu test này, một phép dò trả rỗng vô điều kiện (sai
    // tên attribute HTTP, sai cách đọc thân phương thức) vẫn để cổng trên xanh mãi mãi.
    [Fact]
    public void EveryControllerAction_PutsItsStatusCode_ThroughApiControllerBase_ScansTheRealActions()
    {
        var actions = ProductSourceFiles.Core()
            .SelectMany(file => ControllerResultPathScanner.ActionNames(File.ReadAllText(file)))
            .ToList();

        // Bốn hình dạng khác nhau: thân biểu thức, thân khối nhiều `return`, switch expression có nhánh `throw`,
        // và nhánh tệp — mỗi cái đi một đường trong phép dò.
        actions.ShouldContain("FilesController.Download");
        actions.ShouldContain("AuthController.Login");
        actions.ShouldContain("DiagnosticsController.Probe");
        actions.ShouldContain("UsersController.Export");
    }

    // Canary cắm vào SOURCE THẬT: đưa FilesController.Download về đúng hình dạng nó có TRƯỚC ADR-0068 và đòi phép
    // dò phải kêu. Đây là ca chứng minh cổng còn nhìn thấy action thật — không phải chỉ nhìn thấy văn bản tự chế.
    [Fact]
    public void Detector_B16_Catches_TheRealDownloadAction_WhenItBuildsItsOwnFileResult()
    {
        var controller = ProductSourceFiles.Core().Single(file => Path.GetFileName(file) == "FilesController.cs");
        var real = File.ReadAllText(controller);

        const string now = "=> HandleFile(await mediator.Send(new GetFileQuery(id), ct));";
        real.Contains(now, StringComparison.Ordinal).ShouldBeTrue(
            "chuỗi neo của canary đã đổi — sửa ca này trong CÙNG lượt, đừng để nó thay một chuỗi rỗng");

        const string before = """
            {
                    var result = await mediator.Send(new GetFileQuery(id), ct);
                    return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
                }
            """;

        ControllerResultPathScanner.Scan(real.Replace(now, before, StringComparison.Ordinal), NamedExceptions)
            .ShouldContain(o => o.StartsWith("FilesController.Download", StringComparison.Ordinal));
    }

    public static TheoryData<string, string> SelfBuiltResults => new()
    {
        { "File", "return File(stream, contentType, fileName);" },
        { "Ok", "return Ok(payload);" },
        { "StatusCode", "return StatusCode(500, payload);" },
        { "NotFound", "return NotFound();" },
        { "ExportFileResult", "return new ExportFileResult(file);" },
    };

    [Theory]
    [MemberData(nameof(SelfBuiltResults))]
    public void Detector_B16_Catches_AnActionBuildingItsOwnResult(string label, string body)
    {
        var source = $$"""
            public sealed class FakeController : ApiControllerBase
            {
                [HttpGet("{id:guid}")]
                public IActionResult Read(Guid id)
                {
                    {{body}}
                }
            }
            """;

        ControllerResultPathScanner.Scan(source, NamedExceptions)
            .ShouldContain(o => o.StartsWith("FakeController.Read", StringComparison.Ordinal), $"ca {label}");
    }

    // Bốn hình dạng HỢP LỆ đang dùng thật. Một phép dò bắt nhầm chúng sẽ bị sửa bằng cách nới luật — và lúc đó cổng
    // không còn ép gì. Bốn ca này là thứ giữ cho việc nới đó không xảy ra trong im lặng.
    [Fact]
    public void Detector_B16_Ignores_TheFourLegalShapes()
    {
        const string source = """
            public sealed class FakeController : ApiControllerBase
            {
                [HttpGet]
                public async Task<IActionResult> GetList([FromQuery] GetUsersListQuery query, CancellationToken ct)
                    => HandleResult(await mediator.Send(query, ct));

                [HttpPost]
                public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct)
                    => HandleCreated(await mediator.Send(command, ct), location);

                [HttpGet("export")]
                [RequireAntiforgery]
                public async Task<IActionResult> Export([FromQuery] ExportUsersCommand command, CancellationToken ct)
                    => HandleExport(await mediator.Send(command, ct));

                [HttpGet("{id:guid}")]
                public async Task<IActionResult> Download(Guid id, CancellationToken ct)
                    => HandleFile(await mediator.Send(new GetFileQuery(id), ct));
            }
            """;

        ControllerResultPathScanner.Scan(source, NamedExceptions).ShouldBeEmpty();
    }

    // Ba hình dạng hợp lệ khác, mỗi cái là một nhánh riêng của phép bóc lá: switch expression có nhánh `throw`
    // (DiagnosticsController.Probe), thân khối nhiều `return` (AuthController.Login), và `return` nằm trong một
    // lambda bên trong action — cái cuối trả về kiểu khác, không phải phản hồi HTTP.
    [Fact]
    public void Detector_B16_Ignores_SwitchArmsThrowStatements_MultipleReturns_AndReturnsInsideALambda()
    {
        const string source = """
            public sealed class FakeController : ApiControllerBase
            {
                [HttpGet("probe")]
                public IActionResult Probe([FromQuery] string? outcome = null)
                    => outcome switch
                    {
                        "failure" => HandleResult(DiagnosticsProbe.Fail()),
                        "exception" => throw new InvalidOperationException("mô phỏng"),
                        _ => HandleResult(DiagnosticsProbe.Succeed(timeProvider)),
                    };

                [HttpPost("login")]
                public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
                {
                    var result = await mediator.Send(command, ct);
                    if (result.IsFailure)
                        return HandleResult(result);

                    await SignInAsync(result.Value, timeProvider.GetUtcNow());
                    return HandleResult(Result.Success(result.Value.Session));
                }

                [HttpGet("list")]
                public async Task<IActionResult> List(CancellationToken ct)
                {
                    var names = items.Select(item => { return item.Name; }).ToList();
                    return HandleResult(Result.Success(names));
                }
            }
            """;

        ControllerResultPathScanner.Scan(source, NamedExceptions).ShouldBeEmpty();
    }

    // Ngoại lệ có tên phải thật sự miễn — nếu không, cột "trừ ngoại lệ có tên" của sổ nợ là một câu không chạy.
    [Fact]
    public void Detector_B16_Honours_ANamedException()
    {
        const string source = """
            public sealed class FakeController : ApiControllerBase
            {
                [HttpGet]
                public IActionResult Read() => Ok(payload);
            }
            """;

        ControllerResultPathScanner.Scan(source, new HashSet<string>(StringComparer.Ordinal) { "FakeController.Read" })
            .ShouldBeEmpty();
    }
}
