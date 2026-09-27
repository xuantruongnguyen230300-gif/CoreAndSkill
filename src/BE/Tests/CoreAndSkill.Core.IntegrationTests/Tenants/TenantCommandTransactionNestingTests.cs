using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Lệnh ghi của khu quản trị đơn vị đi qua pipeline MediatR THẬT (ValidationBehavior → TransactionBehavior → handler)
// tới TenantProvisioningService THẬT. Chỉ IUnitOfWork bị thay — bằng một bản mô phỏng đúng ràng buộc của Npgsql mà
// UnitOfWork thật vấp phải: MỘT kết nối không mở được transaction thứ hai khi transaction trước chưa đóng
// ("A transaction is already in progress; nested/concurrent transactions aren't supported").
//
// docs/quy-uoc/be-cqrs-handler.md §3.1, §5.3: transaction của một command do TransactionBehavior mở — service
// được handler gọi KHÔNG mở thêm. Bản ghi sâu nhất phải đúng bằng 1: 0 nghĩa là lệnh bị chặn trước behavior (test
// rỗng), 2 nghĩa là service tự mở transaction lồng — trên PostgreSQL thật đó là 500 ở mọi lệnh ghi của
// /system/tenants. Không cần DB: độ sâu được ghi TRƯỚC mọi truy vấn; lỗi kết nối tới DB không tồn tại xảy ra sau đó
// và không phải thứ test này khẳng định.
public sealed class TenantCommandTransactionNestingTests
{
    public static TheoryData<string> Commands => new()
    {
        nameof(CreateTenantCommand),
        nameof(SetTenantActiveCommand),
        nameof(RecoveryResetTenantAdminPasswordCommand),
        nameof(CreateTenantAdminCommand),
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task TenantWriteCommand_ThroughMediatorPipeline_OpensExactlyOneTransaction(string commandName)
    {
        var recorder = new TransactionDepthRecorder();
        using var factory = new CoreWebApplicationFactory(configureServices: services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IUnitOfWork)).ToList())
                services.Remove(descriptor);
            services.AddSingleton(recorder);
            services.AddScoped<IUnitOfWork, SingleConnectionUnitOfWork>();
        });

        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            await SendAsync(sender, commandName);
        }
        catch (Exception) when (recorder.MaxDepth > 0)
        {
            // Lỗi sau khi transaction ngoài đã mở (DB không tồn tại, hoặc lỗi lồng) — thứ được khẳng định là độ sâu.
        }

        recorder.MaxDepth.ShouldBe(1, $"{commandName}: mở {recorder.MaxDepth} transaction lồng nhau trên một kết nối");
    }

    // Chiều kia của cùng hợp đồng: service không tự mở transaction thì một lời gọi thẳng KHÔNG bọc (một runner mới,
    // một job) sẽ để mỗi lần lưu của UserManager tự commit riêng — ADR-0023 §1 hỏng im lặng. Service ném ngay, TRƯỚC
    // truy vấn đầu tiên (DB ở đây không tồn tại: ném vì lý do khác là ném sai chỗ).
    [Fact]
    public async Task ProvisioningService_CalledOutsideCallerTransaction_ThrowsBeforeTouchingDatabase()
    {
        using var factory = new CoreWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => service.SetTenantActiveAsync(Guid.CreateVersion7(), isActive: false, CancellationToken.None));

        ex.Message.ShouldContain("transaction của người gọi");
    }

    private static Task SendAsync(ISender sender, string commandName)
    {
        var tenantId = Guid.CreateVersion7();
        return commandName switch
        {
            nameof(CreateTenantCommand) => sender.Send(new CreateTenantCommand(
                "DV-NEST", "Đơn vị lồng", "quantri", "quantri@vd.vn", "Quản trị", "Passw0rd-Test1")),
            nameof(SetTenantActiveCommand) => sender.Send(new SetTenantActiveCommand(tenantId, false)),
            nameof(RecoveryResetTenantAdminPasswordCommand) => sender.Send(
                new RecoveryResetTenantAdminPasswordCommand(tenantId, "quantri", "Passw0rd-Test1")),
            nameof(CreateTenantAdminCommand) => sender.Send(new CreateTenantAdminCommand(
                tenantId, "quantri2", "quantri2@vd.vn", "Quản trị 2", "Passw0rd-Test1")),
            _ => throw new ArgumentOutOfRangeException(nameof(commandName), commandName, null),
        };
    }

    private sealed class TransactionDepthRecorder
    {
        public int MaxDepth { get; set; }
    }

    // Một kết nối cho cả request (Scoped) — đúng khuôn UnitOfWork thật (be-cqrs-handler.md §4). Lần mở thứ hai khi
    // lần đầu chưa đóng ném đúng loại lỗi Npgsql ném.
    private sealed class SingleConnectionUnitOfWork(TransactionDepthRecorder recorder) : IUnitOfWork
    {
        private int _depth;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<TransactionOutcome<T>>> operation, CancellationToken ct = default)
        {
            _depth++;
            recorder.MaxDepth = Math.Max(recorder.MaxDepth, _depth);
            try
            {
                if (_depth > 1)
                    throw new InvalidOperationException(
                        "A transaction is already in progress; nested/concurrent transactions aren't supported.");

                return (await operation(ct)).Value;
            }
            finally
            {
                _depth--;
            }
        }
    }
}
