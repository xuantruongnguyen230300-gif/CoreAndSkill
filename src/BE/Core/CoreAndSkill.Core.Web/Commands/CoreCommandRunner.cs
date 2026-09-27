using System.ComponentModel.DataAnnotations;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Web.Commands;

// Runner lệnh vận hành — docs/quy-uoc/be-architecture.md §2.1, §3; docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md.
// Nhận dòng lệnh của host, chạy lệnh bootstrap qua ITenantProvisioningService rồi báo host thoát —
// KHÔNG BAO GIỜ mở cổng. Host chỉ có đúng MỘT dòng: `if (await app.RunCoreCommandAsync(args)) return;`
public static class CoreCommandRunner
{
    public static async Task<bool> RunCoreCommandAsync(this WebApplication app, string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "core", StringComparison.Ordinal))
            return false;

        if (args.Length < 2)
        {
            await Console.Error.WriteLineAsync("Thiếu động từ sau 'core'. Xem docs/quy-uoc/be-architecture.md §3.");
            Environment.ExitCode = 1;
            return true;
        }

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        switch (args[1])
        {
            case "bootstrap":
                await RunBootstrapAsync(services);
                break;

            case "reset-operator-password":
                await RunResetOperatorPasswordAsync(services);
                break;

            case "outbox-replay":
                await RunOutboxReplayAsync(services, args[2..]);
                break;

            default:
                await Console.Error.WriteLineAsync($"Lệnh không xác định: core {args[1]}.");
                Environment.ExitCode = 1;
                break;
        }

        return true;
    }

    // docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md §3. Kiểm đủ đầu vào TRƯỚC dòng ghi đầu tiên —
    // docs/quy-uoc/be-architecture.md §4.4 (ngoại lệ CoreBootstrapOptions, KHÔNG ValidateOnStart).
    //
    // Đường này chạy trước app.Run() nên KHÔNG hosted service nào khởi động — kể cả phép kiểm nguồn seed lúc khởi động.
    // Nguồn seed được kiểm bên trong CreateTenantAsync, trước dòng ghi đầu tiên (TenantSeedValidator); runner không kiểm
    // lại, chỉ báo mã lỗi service trả.
    // internal: test gọi thẳng với một IServiceProvider dựng tay — cùng lý do với RunOutboxReplayAsync.
    internal static async Task RunBootstrapAsync(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<CoreBootstrapOptions>>().Value;

        if (!TryValidate(options, out var errors))
        {
            await Console.Error.WriteLineAsync("Cấu hình Core:Bootstrap:* thiếu hoặc không hợp lệ. Không ghi dòng nào:");
            foreach (var error in errors)
                await Console.Error.WriteLineAsync($"  - {error}");
            Environment.ExitCode = 1;
            return;
        }

        var provisioning = services.GetRequiredService<ITenantProvisioningService>();
        var uow = services.GetRequiredService<IUnitOfWork>();

        // Hai lời gọi, HAI transaction (ADR-0023 §1): đơn vị hệ thống đã commit mà đơn vị nghiệp vụ hỏng thì
        // chạy lại lệnh — quy tắc 3 bỏ qua phần đã có.
        var systemResult = await InTransactionAsync(uow, ct => provisioning.CreateTenantAsync(
            new CreateTenantInput(
                options.SystemTenantCode,
                options.SystemTenantName,
                IsSystem: true,
                options.OperatorUserName,
                options.OperatorPassword,
                AdminHasPermissionBypass: false,
                AdminIsSystemOperator: true,
                AdminEmail: options.OperatorEmail),
            ct));

        if (systemResult.IsFailure)
        {
            await ReportFailureAsync("Lỗi tạo đơn vị hệ thống", systemResult.Error!, OperatorAccountKeys);
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine(systemResult.Value.TenantWasCreated
            ? $"Đã tạo đơn vị hệ thống '{options.SystemTenantCode}' và tài khoản vận hành '{options.OperatorUserName}'."
            : $"Đơn vị hệ thống '{options.SystemTenantCode}' đã có — bỏ qua.");

        var firstResult = await InTransactionAsync(uow, ct => provisioning.CreateTenantAsync(
            new CreateTenantInput(
                options.FirstTenantCode,
                options.FirstTenantName,
                IsSystem: false,
                options.AdminUserName,
                options.AdminPassword,
                AdminHasPermissionBypass: true,
                AdminIsSystemOperator: false,
                AdminEmail: options.AdminEmail),
            ct));

        if (firstResult.IsFailure)
        {
            await ReportFailureAsync("Lỗi tạo đơn vị nghiệp vụ đầu tiên", firstResult.Error!, AdminAccountKeys);
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine(firstResult.Value.TenantWasCreated
            ? $"Đã tạo đơn vị nghiệp vụ đầu tiên '{options.FirstTenantCode}' và tài khoản quản trị '{options.AdminUserName}'."
            : $"Đơn vị nghiệp vụ đầu tiên '{options.FirstTenantCode}' đã có — bỏ qua.");
    }

    // docs/adr/0023-dich-vu-tao-don-vi-dung-chung.md §5. Đường DUY NHẤT đặt lại mật khẩu tài khoản
    // vận hành — không endpoint nào làm được việc này.
    private static async Task RunResetOperatorPasswordAsync(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<CoreBootstrapOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.OperatorUserName) || string.IsNullOrWhiteSpace(options.OperatorPassword))
        {
            await Console.Error.WriteLineAsync(
                "Thiếu Core:Bootstrap:OperatorUserName hoặc Core:Bootstrap:OperatorPassword. Không ghi dòng nào.");
            Environment.ExitCode = 1;
            return;
        }

        var provisioning = services.GetRequiredService<ITenantProvisioningService>();
        var result = await InTransactionAsync(
            services.GetRequiredService<IUnitOfWork>(),
            ct => provisioning.ResetOperatorPasswordAsync(options.OperatorUserName, options.OperatorPassword, ct));

        if (result.IsFailure)
        {
            await ReportFailureAsync("Lỗi", result.Error!, OperatorAccountKeys);
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine($"Đã đặt lại mật khẩu tài khoản vận hành '{options.OperatorUserName}'.");
    }

    // docs/database/script-runbook.md §10, docs/quy-uoc/be-architecture.md §3 (bảng "Lệnh của runner").
    // Tham số sau động từ chỉ CHỌN ĐỐI TƯỢNG: `--id <id>` (một dòng) HOẶC `--all-dead` (mọi dòng dead) —
    // đúng một trong hai. Không có giá trị nào đi qua dòng lệnh ngoài định danh dòng. Chỉ chạm dòng `dead`
    // (một dòng pending/done mà phát lại là phát trùng); mỗi dòng ghi một dòng nhật ký kiểm toán.
    // internal: test gọi thẳng với một IServiceProvider giả — không cần dựng cả WebApplication.
    internal static async Task RunOutboxReplayAsync(IServiceProvider services, string[] args)
    {
        var replayAll = args.Contains("--all-dead", StringComparer.Ordinal);
        var idIndex = Array.IndexOf(args, "--id");
        var hasId = idIndex >= 0;

        if (replayAll == hasId)
        {
            await Console.Error.WriteLineAsync("Cần đúng MỘT trong hai: --id <id> hoặc --all-dead. Không ghi dòng nào.");
            Environment.ExitCode = 1;
            return;
        }

        var replay = services.GetRequiredService<IOutboxReplayService>();

        if (replayAll)
        {
            var all = await replay.ReplayAllDeadAsync(CancellationToken.None);
            if (all.IsFailure)
            {
                await ReportFailureAsync("Lỗi", all.Error!, NoConfigKeys);
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Đã đặt {all.Value} dòng outbox dead về pending. Bộ phát nhặt ở nhịp quét kế.");
            return;
        }

        if (idIndex + 1 >= args.Length || !Guid.TryParse(args[idIndex + 1], out var id))
        {
            await Console.Error.WriteLineAsync("--id cần một định danh (GUID) hợp lệ. Không ghi dòng nào.");
            Environment.ExitCode = 1;
            return;
        }

        var one = await replay.ReplayAsync(id, CancellationToken.None);
        if (one.IsFailure)
        {
            await ReportFailureAsync("Lỗi", one.Error!, NoConfigKeys);
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine($"Đã đặt dòng outbox {id} về pending. Bộ phát nhặt ở nhịp quét kế.");
    }

    // Runner đứng ngoài MediatR nên không có TransactionBehavior — tự bọc, CÙNG điều kiện commit với behavior
    // (docs/quy-uoc/be-cqrs-handler.md §5.3): commit chỉ khi Result thành công. ITenantProvisioningService
    // không tự mở transaction (TenantProvisioningService.cs, chú thích đầu lớp).
    private static Task<TResult> InTransactionAsync<TResult>(
        IUnitOfWork uow, Func<CancellationToken, Task<TResult>> operation)
        where TResult : IResult
        => uow.ExecuteInTransactionAsync(async ct =>
        {
            var result = await operation(ct);
            return new TransactionOutcome<TResult>(result, result.IsSuccess);
        }, CancellationToken.None);

    // Khoá fieldErrors của ITenantProvisioningService là tên field của card POST /system/tenants (docs/contracts/tenants.md
    // §2) — người vận hành không gõ những tên đó, họ gõ khoá Core:Bootstrap:*. Mỗi lời gọi tạo tài khoản đọc từ một bộ khoá
    // khác nhau, nên bảng nào đi với lời gọi nào là việc của chỗ gọi. Khoá không có trong bảng thì in nguyên như service trả.
    private static readonly IReadOnlyDictionary<string, string> OperatorAccountKeys = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AdminUserName"] = nameof(CoreBootstrapOptions.OperatorUserName),
        ["AdminEmail"] = nameof(CoreBootstrapOptions.OperatorEmail),
        ["AdminTempPassword"] = nameof(CoreBootstrapOptions.OperatorPassword),
    };

    private static readonly IReadOnlyDictionary<string, string> AdminAccountKeys = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AdminUserName"] = nameof(CoreBootstrapOptions.AdminUserName),
        ["AdminEmail"] = nameof(CoreBootstrapOptions.AdminEmail),
        ["AdminTempPassword"] = nameof(CoreBootstrapOptions.AdminPassword),
    };

    private static readonly IReadOnlyDictionary<string, string> NoConfigKeys = new Dictionary<string, string>(StringComparer.Ordinal);

    // Mã gốc KHÔNG nói lỗi nằm ở ô nào (ADMIN_CREATE_FAILED gộp tên đăng nhập, email và mật khẩu) — lý do nằm ở fieldErrors, nên
    // in cả mã lẫn khoá của từng lỗi, kèm tham số (MinLength của chính sách mật khẩu…). Không tham số nào mang mật khẩu:
    // IdentityErrorMapper chỉ đặt tên đăng nhập, email và ngưỡng chính sách vào đó.
    private static async Task ReportFailureAsync(string what, Error error, IReadOnlyDictionary<string, string> configKeyByField)
    {
        await Console.Error.WriteLineAsync(
            $"{what}: {error.Code} — {MessageTemplateRenderer.Render(error.MessageTemplate, error.Params)}");

        foreach (var (field, fieldErrors) in error.FieldErrors)
        {
            var where = configKeyByField.TryGetValue(field, out var configKey)
                ? $"{field} (khoá {CoreBootstrapOptions.SectionName}:{configKey})"
                : field;

            foreach (var fieldError in fieldErrors)
            {
                var parameters = fieldError.Params.Count == 0
                    ? string.Empty
                    : $" ({string.Join(", ", fieldError.Params.Select(p => $"{p.Key}={p.Value}"))})";
                await Console.Error.WriteLineAsync($"  - {where}: {fieldError.Code}{parameters}");
            }
        }
    }

    private static bool TryValidate(CoreBootstrapOptions options, out IReadOnlyList<string> errors)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        errors = results.Select(r => r.ErrorMessage ?? "Giá trị không hợp lệ.").ToList();
        return isValid;
    }
}
