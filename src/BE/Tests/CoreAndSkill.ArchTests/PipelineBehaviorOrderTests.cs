using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// B2 — docs/RULES.md: ValidationBehavior chạy TRƯỚC TransactionBehavior. A10 chỉ canh mỗi behavior đăng ký đúng một lần,
// không canh thứ tự; đảo thứ tự thì mọi request sai định dạng cũng mở một transaction rồi rollback — kết quả không sai,
// nên không test hành vi nào đỏ. Test này so thứ tự đăng ký trong code với danh sách khai ở
// docs/quy-uoc/be-cqrs-handler.md §5.1, đọc thẳng từ tài liệu — không chép lại danh sách vào đây.
public class PipelineBehaviorOrderTests
{
    [Fact]
    public void PipelineBehaviors_AreRegistered_InTheDocumentedOrder()
    {
        var registered = PipelineBehaviorOrderScanner.ReadRegistrationOrder(File.ReadAllText(RegistrationPath()));
        var documented = PipelineBehaviorOrderScanner.ReadDocumentedOrder(File.ReadAllText(DocPath()));

        registered.ShouldBe(documented);
    }

    // T6 — hai phía đều đọc ra danh sách thật, và danh sách đó chứa đúng cặp mà B2 canh. Một bộ đọc trả rỗng ở cả hai
    // phía thì test trên xanh vì hai tập rỗng bằng nhau.
    [Fact]
    public void PipelineBehaviors_AreRegistered_InTheDocumentedOrder_ReadsRealLists()
    {
        var registered = PipelineBehaviorOrderScanner.ReadRegistrationOrder(File.ReadAllText(RegistrationPath()));
        var documented = PipelineBehaviorOrderScanner.ReadDocumentedOrder(File.ReadAllText(DocPath()));

        registered.ShouldContain("ValidationBehavior");
        registered.ShouldContain("TransactionBehavior");
        documented.ShouldContain("ValidationBehavior");
        documented.ShouldContain("TransactionBehavior");
    }

    [Fact]
    public void Detector_B2_Catches_SwappedOrder()
    {
        const string source = """
            public static class DependencyInjection
            {
                public static IServiceCollection AddCoreApplication(this IServiceCollection services)
                {
                    services.AddMediatR(cfg =>
                    {
                        cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
                        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                    });
                    return services;
                }
            }
            """;

        var registered = PipelineBehaviorOrderScanner.ReadRegistrationOrder(source);
        var documented = PipelineBehaviorOrderScanner.ReadDocumentedOrder(File.ReadAllText(DocPath()));

        registered.ShouldNotBe(documented);
    }

    // Lời gọi ở phương thức KHÁC (ví dụ một hàm phụ đăng ký thêm) không làm lệch thứ tự đọc của AddCoreApplication, và
    // chú thích nhắc tên behavior không bị đọc thành một lời đăng ký.
    [Fact]
    public void Detector_B2_Reads_OnlyCallsInsideAddCoreApplication()
    {
        const string source = """
            public static class DependencyInjection
            {
                public static IServiceCollection AddCoreApplication(this IServiceCollection services)
                {
                    // cfg.AddOpenBehavior(typeof(TransactionBehavior<,>)); — chú thích, không phải lời gọi
                    services.AddMediatR(cfg =>
                    {
                        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                        cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
                    });
                    return services;
                }

                private static void Other(MediatRServiceConfiguration cfg)
                    => cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            }
            """;

        PipelineBehaviorOrderScanner.ReadRegistrationOrder(source).ShouldBe(["ValidationBehavior", "TransactionBehavior"]);
    }

    private static string RegistrationPath([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "..", "Core", "CoreAndSkill.Core.Application", "DependencyInjection.cs"));

    private static string DocPath([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "..", "..", "..", "docs", "quy-uoc", "be-cqrs-handler.md"));
}
