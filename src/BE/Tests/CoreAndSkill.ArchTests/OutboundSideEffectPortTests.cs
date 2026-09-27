using System.Reflection;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// E13 — docs/RULES.md §4, docs/adr/0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md quyết định 3.
// Hai vế, tầm quét khác nhau — lý do của từng tầm ở đầu Support/OutboundSideEffectPortScanner.cs:
//   vế 1 — handler của COMMAND không nhận cổng gửi ra ngoài (thư, HTTP) qua constructor; đi qua outbox;
//   vế 2 — MỌI IRequestHandler không nhận IBackgroundJobScheduler (docs/quy-uoc/be-cqrs-handler.md §10.3 ràng buộc 3);
//          xếp việc nền đi qua một dòng outbox.
//
// Tầm quét: mọi assembly Core có thể mang handler — Application, Infrastructure, Web — và host.
//
// T12:cap-xanh-do — mọi ca Detector_* kỳ vọng XANH có một ca kỳ vọng ĐỎ đi qua cùng phép dò, dựng từ CÙNG khuôn nguồn
// và khác đúng một đối số của khuôn (docs/RULES.md §8 luật T12); chú thích "cặp đỏ:" ngay trên mỗi ca xanh nói cặp ở đâu.
public class OutboundSideEffectPortTests
{
    private static readonly Assembly[] ProductAssemblies =
    [
        ArchitectureFixture.ApplicationAssembly,
        ArchitectureFixture.InfrastructureAssembly,
        ArchitectureFixture.WebAssembly,
        ArchitectureFixture.HostAssembly,
    ];

    private const string Scheduler = OutboundSideEffectPortScanner.BackgroundJobScheduler;

    // ------------------------------------------------------------------ vế 1 — cổng gửi ra ngoài, handler của command

    [Fact]
    public void CommandHandlers_MustNotDependOn_OutboundSideEffectPorts()
    {
        OutboundSideEffectPortScanner.FindViolations(ProductAssemblies).ShouldBeEmpty();
    }

    // T6 — phép khớp theo tên phải chạm handler command THẬT, kể cả hai handler ADR-0053 nêu tên (tác dụng ngoài database
    // trong transaction). Không có nó, một lần đổi tên ICommandBase hay nâng MediatR làm cổng trên xanh vì tập rỗng.
    [Fact]
    public void CommandHandlers_MustNotDependOn_OutboundSideEffectPorts_ScansRealCommandHandlers()
    {
        var handlers = OutboundSideEffectPortScanner.FindCommandHandlers(ProductAssemblies).Select(t => t.Name).ToList();

        handlers.ShouldContain("UploadFileCommandHandler");
        handlers.ShouldContain("StartImportCommandHandler");
        handlers.ShouldNotContain(name => name.EndsWith("QueryHandler", StringComparison.Ordinal),
            "handler của query lọt vào tập — phép nhận biết command đang sai");
    }

    [Theory]
    [InlineData("CoreAndSkill.Core.Application.Notifications.IEmailSender")]
    [InlineData("System.Net.Http.HttpClient")]
    [InlineData("System.Net.Http.IHttpClientFactory")]
    [InlineData("System.Collections.Generic.IEnumerable<CoreAndSkill.Core.Application.Notifications.IEmailSender>")]
    public void Detector_E13_Catches_CommandHandlerTakingOutboundPort(string portType)
    {
        var assembly = RequestHandlerAssembly("FakeE13Catches", isCommand: true, portType);

        OutboundSideEffectPortScanner.FindViolations([assembly]).ShouldHaveSingleItem().ShouldContain("SendReportHandler");
    }

    // Handler của QUERY không chạy trong transaction có thử lại của TransactionBehavior — không thuộc vế 1.
    // Cặp đỏ: Detector_E13_Catches_CommandHandlerTakingOutboundPort("System.Net.Http.HttpClient") — cùng khuôn, cùng
    // tham số HttpClient, khác đúng cờ isCommand.
    [Fact]
    public void Detector_E13_Ignores_QueryHandlerTakingOutboundPort()
    {
        var assembly = RequestHandlerAssembly("FakeE13IgnoresQuery", isCommand: false, "System.Net.Http.HttpClient");

        OutboundSideEffectPortScanner.FindViolations([assembly]).ShouldBeEmpty();
    }

    // Đúng hình dạng hợp lệ: handler command ghi outbox qua một cổng nội bộ, không nhận cổng gửi ra ngoài.
    // Cặp đỏ: Detector_E13_Catches_CommandHandlerTakingOutboundPort — cùng khuôn, cùng cờ command, khác đúng kiểu tham số.
    [Fact]
    public void Detector_E13_Ignores_CommandHandlerWithoutOutboundPort()
    {
        var assembly = RequestHandlerAssembly("FakeE13IgnoresClean", isCommand: true, "Fake.IOutboxWriter");

        OutboundSideEffectPortScanner.FindViolations([assembly]).ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ vế 2 — IBackgroundJobScheduler, mọi IRequestHandler

    [Fact]
    public void RequestHandlers_MustNotDependOn_BackgroundJobScheduler()
    {
        OutboundSideEffectPortScanner.FindSchedulerViolations(ProductAssemblies).ShouldBeEmpty();
    }

    // T6 — vế 2 quét MỌI IRequestHandler: tập phải chạm cả handler command thật lẫn handler query thật. StartImportCommandHandler
    // là handler §10.3 mô tả; GetJobQueryHandler là vế polling của cùng khuôn. Tập chỉ có command nghĩa là vế 2 đã thu hẹp
    // về tầm của vế 1 mà không ai hay.
    [Fact]
    public void RequestHandlers_MustNotDependOn_BackgroundJobScheduler_ScansRealQueryAndCommandHandlers()
    {
        var handlers = OutboundSideEffectPortScanner.FindRequestHandlers(ProductAssemblies).Select(t => t.Name).ToList();

        handlers.ShouldContain("StartImportCommandHandler");
        handlers.ShouldContain("GetJobQueryHandler");
    }

    // T6 — phép dò khớp theo TÊN ĐẦY ĐỦ. Một tên gõ sai, hoặc kiểu thật dời namespace, làm cả hai vế xanh vì không tham số
    // nào mang tên đó — và ca Detector_* vẫn xanh vì assembly tổng hợp khai kiểu mốc theo đúng chuỗi trong danh sách. Ca này
    // đối chiếu từng tên với kiểu THẬT trong đồ thị sản phẩm, không với assembly tổng hợp nào.
    [Theory]
    [MemberData(nameof(AllForbiddenPortNames))]
    public void EveryForbiddenPortName_ResolvesToARealType(string portName)
    {
        Assembly[] realGraph =
        [
            .. ProductAssemblies,
            typeof(HttpClient).Assembly,
            Assembly.Load("Microsoft.Extensions.Http"),
        ];

        realGraph.Select(assembly => assembly.GetType(portName, throwOnError: false)).ShouldContain(type => type != null,
            $"không kiểu thật nào mang tên '{portName}' — phép dò E13 đang khớp một tên đã chết");
    }

    public static TheoryData<string> AllForbiddenPortNames()
    {
        var names = new TheoryData<string>();
        foreach (var name in OutboundSideEffectPortScanner.ForbiddenPorts.Append(Scheduler))
            names.Add(name);
        return names;
    }

    [Theory]
    [InlineData(true, Scheduler)]
    [InlineData(false, Scheduler)]
    [InlineData(false, "System.Collections.Generic.IEnumerable<" + Scheduler + ">")]
    [InlineData(false, "System.Lazy<" + Scheduler + ">")]
    [InlineData(false, Scheduler + "[]")]
    public void Detector_E13_Catches_RequestHandlerTakingBackgroundJobScheduler(bool isCommand, string parameterType)
    {
        var assembly = RequestHandlerAssembly("FakeE13CatchesScheduler", isCommand, parameterType);

        OutboundSideEffectPortScanner.FindSchedulerViolations([assembly]).ShouldHaveSingleItem().ShouldContain("SendReportHandler");
    }

    // IRequestHandler<TRequest> một tham số kiểu (request không trả giá trị) cũng là handler.
    [Fact]
    public void Detector_E13_Catches_VoidRequestHandlerTakingBackgroundJobScheduler()
    {
        var assembly = Compile("FakeE13CatchesVoid", $$"""
            public sealed class SendReport { }

            public sealed class SendReportHandler({{Scheduler}} scheduler) : MediatR.IRequestHandler<SendReport>
            {
                public object Scheduler { get; } = scheduler;
            }
            """);

        OutboundSideEffectPortScanner.FindSchedulerViolations([assembly]).ShouldHaveSingleItem().ShouldContain("SendReportHandler");
    }

    // Đúng người dùng hợp lệ của scheduler: handler của outbox gọi nó SAU commit (hình dạng của JobQueuedOutboxHandler).
    // Cặp đỏ: Detector_E13_Catches_OutboxHandlerThatIsAlsoARequestHandler — cùng khuôn, khác đúng việc lớp đó cài thêm
    // IRequestHandler.
    [Fact]
    public void Detector_E13_Ignores_OutboxEventHandlerTakingBackgroundJobScheduler()
    {
        var assembly = OutboxHandlerAssembly("FakeE13IgnoresOutbox", alsoRequestHandler: false);

        OutboundSideEffectPortScanner.FindSchedulerViolations([assembly]).ShouldBeEmpty();
    }

    [Fact]
    public void Detector_E13_Catches_OutboxHandlerThatIsAlsoARequestHandler()
    {
        var assembly = OutboxHandlerAssembly("FakeE13CatchesOutbox", alsoRequestHandler: true);

        OutboundSideEffectPortScanner.FindSchedulerViolations([assembly]).ShouldHaveSingleItem().ShouldContain("QueueReportHandler");
    }

    // ------------------------------------------------------------------ khuôn nguồn

    // Khuôn chung của mọi ca handler request: request là command (cài ICommandBase) hay query, handler nhận MỘT tham số
    // constructor kiểu parameterType. Ca xanh và ca đỏ cặp đôi khác nhau đúng một đối số của khuôn này.
    private static Assembly RequestHandlerAssembly(string name, bool isCommand, string parameterType)
        => Compile(name, $$"""
            public interface IOutboxWriter { }

            public sealed class SendReport {{(isCommand ? ": CoreAndSkill.Core.Application.Common.Cqrs.ICommandBase" : "")}} { }

            public sealed class SendReportHandler({{parameterType}} port) : MediatR.IRequestHandler<SendReport, int>
            {
                public object Port { get; } = port;
            }
            """);

    private static Assembly OutboxHandlerAssembly(string name, bool alsoRequestHandler)
        => Compile(name, $$"""
            public sealed class SendReport { }

            public sealed class QueueReportHandler({{Scheduler}} scheduler)
                : CoreAndSkill.Core.Application.Outbox.IOutboxEventHandler{{(alsoRequestHandler ? ", MediatR.IRequestHandler<SendReport, int>" : "")}}
            {
                public object Scheduler { get; } = scheduler;
            }
            """);

    // Assembly tổng hợp tự khai các kiểu mốc cùng TÊN ĐẦY ĐỦ với kiểu thật (IRequestHandler của MediatR; ICommandBase,
    // IEmailSender, IBackgroundJobScheduler, IOutboxEventHandler của Core; IHttpClientFactory của Microsoft.Extensions.Http)
    // — detector khớp theo tên, nên mẫu vi phạm không phải kéo các gói đó vào trình biên dịch. Tên có thật hay không do
    // EveryForbiddenPortName_ResolvesToARealType canh. HttpClient là kiểu thật của System.Net.Http. Tên assembly mang GUID:
    // nạp hai assembly cùng tên thì lần sau nhận lại assembly đã nạp, và các ca của một Theory chạy trên cùng một mẫu.
    private static Assembly Compile(string name, string handlerSource)
        => SyntheticAssembly.Compile($"{name}{Guid.NewGuid():N}", $$"""
            namespace MediatR
            {
                public interface IRequestHandler<in TRequest> { }
                public interface IRequestHandler<in TRequest, TResponse> { }
            }
            namespace CoreAndSkill.Core.Application.Common.Cqrs { public interface ICommandBase { } }
            namespace CoreAndSkill.Core.Application.Common.Interfaces { public interface IBackgroundJobScheduler { } }
            namespace CoreAndSkill.Core.Application.Notifications { public interface IEmailSender { } }
            namespace CoreAndSkill.Core.Application.Outbox { public interface IOutboxEventHandler { } }
            namespace System.Net.Http { public interface IHttpClientFactory { } }
            namespace Fake
            {
                {{handlerSource}}
            }
            """, typeof(HttpClient).Assembly);
}
