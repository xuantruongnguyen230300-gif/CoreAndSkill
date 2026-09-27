using System.Reflection;

namespace CoreAndSkill.ArchTests.Support;

// Luật E13 (docs/RULES.md §4) — hai vế, hai tầm quét khác nhau. Phép dò chung: một handler nhận cổng bị cấm qua
// constructor — trực tiếp, hay lồng trong một kiểu generic (IEnumerable<IEmailSender>, Lazy<HttpClient>) hoặc mảng. Khớp
// theo TÊN ĐẦY ĐỦ của kiểu, để meta-test dựng được mẫu vi phạm trong một assembly tổng hợp.
//
// Vế 1 — cổng gửi ra ngoài (docs/quy-uoc/be-cqrs-handler.md §4 luật 1). Tầm: handler của COMMAND (cài
// IRequestHandler<TRequest, …> với TRequest cài ICommandBase). Handler của command chạy bên trong
// IUnitOfWork.ExecuteInTransactionAsync, và execution strategy có thể chạy lại cả handler: gửi thư, gọi HTTP ra ngoài
// gọi thẳng trong handler thì chạy lại N lần; nó phải đi qua outbox. Handler của query không nằm trong transaction có thử
// lại — không thuộc vế này.
//
// Vế 2 — cổng xếp việc nền (docs/quy-uoc/be-cqrs-handler.md §10.3 ràng buộc 3: handler KHÔNG gọi
// IBackgroundJobScheduler, nó ghi một dòng outbox). Tầm: MỌI IRequestHandler, command lẫn query — lý do của ràng buộc
// không nằm ở việc chạy lại mà ở chỗ việc nền được xếp trước commit và không mang đơn vị, người kích hoạt đi theo dòng
// outbox; điều đó đúng với bất kỳ handler nào. Người dùng hợp lệ của scheduler là handler của outbox
// (IOutboxEventHandler), không phải IRequestHandler — nên vế này không cần allowlist.
//
// NGOÀI TẦM: cổng lấy gián tiếp (qua IServiceProvider, qua một dịch vụ trung gian tự gửi thư hay tự xếp việc), handler
// không phải IRequestHandler (INotificationHandler của MediatR, IOutboxEventHandler), và vế "tác dụng còn lại chỉ để lại
// rác có job dọn" — phán đoán của người review.
internal static class OutboundSideEffectPortScanner
{
    public static readonly IReadOnlyList<string> ForbiddenPorts =
    [
        "CoreAndSkill.Core.Application.Notifications.IEmailSender",
        "System.Net.Http.HttpClient",
        "System.Net.Http.IHttpClientFactory",
    ];

    public const string BackgroundJobScheduler = "CoreAndSkill.Core.Application.Common.Interfaces.IBackgroundJobScheduler";

    private const string CommandMarker = "CoreAndSkill.Core.Application.Common.Cqrs.ICommandBase";

    // Vế 1: handler của command × cổng gửi ra ngoài.
    public static IReadOnlyList<string> FindViolations(IEnumerable<Assembly> assemblies)
        => Scan(FindCommandHandlers(assemblies), ForbiddenPorts);

    // Vế 2: mọi IRequestHandler × IBackgroundJobScheduler.
    public static IReadOnlyList<string> FindSchedulerViolations(IEnumerable<Assembly> assemblies)
        => Scan(FindRequestHandlers(assemblies), [BackgroundJobScheduler]);

    public static IReadOnlyList<Type> FindCommandHandlers(IEnumerable<Assembly> assemblies)
        => FindRequestHandlers(assemblies).Where(HandlesCommand).ToList();

    public static IReadOnlyList<Type> FindRequestHandlers(IEnumerable<Assembly> assemblies)
        => assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && RequestHandlerContracts(type).Any())
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<string> Scan(IEnumerable<Type> handlers, IReadOnlyList<string> forbidden)
        => handlers
            .SelectMany(handler => handler
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SelectMany(ctor => ctor.GetParameters())
                .Where(parameter => Mentions(parameter.ParameterType, forbidden))
                .Select(parameter => $"{handler.FullName}: tham số '{parameter.Name}' kiểu {parameter.ParameterType}"))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static IEnumerable<Type> RequestHandlerContracts(Type type)
        => type.GetInterfaces().Where(contract =>
            contract.IsGenericType
            && contract.GetGenericTypeDefinition().FullName is "MediatR.IRequestHandler`1" or "MediatR.IRequestHandler`2");

    private static bool HandlesCommand(Type type)
        => RequestHandlerContracts(type).Any(contract => IsCommand(contract.GetGenericArguments()[0]));

    private static bool IsCommand(Type request)
        => request.FullName == CommandMarker || request.GetInterfaces().Any(i => i.FullName == CommandMarker);

    private static bool Mentions(Type type, IReadOnlyList<string> forbidden)
    {
        if (type.FullName is { } name && forbidden.Contains(name, StringComparer.Ordinal))
            return true;

        if (type.HasElementType && Mentions(type.GetElementType()!, forbidden))
            return true;

        return type.IsGenericType && type.GetGenericArguments().Any(argument => Mentions(argument, forbidden));
    }
}
