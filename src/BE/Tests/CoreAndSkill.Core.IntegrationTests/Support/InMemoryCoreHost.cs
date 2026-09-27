using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Toàn bộ pipeline HTTP THẬT của Core (định tuyến, xác thực, antiforgery, phân quyền, ràng buộc kích thước, envelope,
// exception handler, MediatR + validator + transaction behavior, controller) — nhưng KHÔNG có PostgreSQL: các seam hạ tầng
// chạm DB được thay bằng bản trong bộ nhớ (luật T8 cho phép giả lập seam hạ tầng), còn kho tệp, bộ đọc/ghi CSV-Excel và
// mọi lớp Web/Application là mã thật.
//
// Thứ nó chứng minh: hợp đồng HTTP (mã trạng thái, envelope, header, quyền theo bản ghi chủ, giới hạn, CSRF).
// Thứ nó KHÔNG chứng minh: SQL, bộ lọc đơn vị/xoá mềm, giao dịch, Outbox thật — việc của các test RequiresDocker.
internal sealed class InMemoryCoreHost : IAsyncDisposable
{
    public const string TestScheme = "Test";
    public const string OwnerTable = "test.tai_lieu";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public InMemoryFileRepo Files { get; } = new();
    public InMemoryJobRepo Jobs { get; } = new();
    public InMemoryNotificationRepo Notifications { get; } = new();
    public InMemoryUserQuery Users { get; } = new();
    public RecordingAudit Audit { get; } = new();
    public FakePermissions Permissions { get; } = new();
    public FakeOwnerChecker OwnerChecker { get; } = new();

    public CoreWebApplicationFactory Factory { get; }

    public InMemoryCoreHost(IReadOnlyDictionary<string, string?>? extraConfig = null, Action<IServiceCollection>? configure = null)
    {
        var config = new Dictionary<string, string?> { ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString };
        if (extraConfig is not null)
        {
            foreach (var pair in extraConfig)
                config[pair.Key] = pair.Value;
        }

        Factory = new CoreWebApplicationFactory(config, services =>
        {
            // Xác thực thử: đặt danh tính bằng header, không cần đăng nhập (đăng nhập cần DB).
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestScheme;
                    options.DefaultAuthenticateScheme = TestScheme;
                    options.DefaultChallengeScheme = TestScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestScheme, _ => { });

            // Antiforgery cần khoá bảo vệ dữ liệu — mặc định nằm trong DB; ở đây tạm trong bộ nhớ.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            // Seam hạ tầng chạm DB -> bộ nhớ.
            // Scoped để đóng dấu created_by bằng người gọi hiện tại — việc AuditInterceptor làm ở bản thật;
            // checker và chính sách "người tải lên" của tệp chưa gắn đọc đúng trường đó.
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IFileRepository)).ToList())
                services.Remove(descriptor);
            services.AddSingleton(Files);
            services.AddScoped<IFileRepository, StampingFileRepo>();
            Replace<IJobRepository>(services, Jobs);
            Replace<INotificationRepository>(services, Notifications);
            Replace<IUserQueryService>(services, Users);
            Replace<IAuditTrail>(services, Audit);
            Replace<IPermissionChecker>(services, Permissions);
            Replace<IUnitOfWork>(services, new PassThroughUow());

            // Checker cho bảng chủ của test; purpose do "module" khai.
            services.AddSingleton(OwnerChecker);
            services.AddScoped<IFileOwnerAccessChecker, ScopedOwnerChecker>();
            services.AddSingleton<IFilePurposeSource>(new TestPurposes());

            // Không để nền cố nối DB ở mỗi nhịp trong lúc test HTTP chạy.
            services.AddSingleton<ILoggerProvider>(NullLoggerProvider.Instance);

            // Seam riêng của một lớp test (vd. vai trò trong bộ nhớ) — chạy SAU cùng để thay được mọi đăng ký ở trên.
            configure?.Invoke(services);
        });
    }

    private static void Replace<T>(IServiceCollection services, T instance)
        where T : class
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(descriptor);

        services.AddSingleton(instance);
    }

    public HttpClient CreateClient(Guid? userId = null, string userName = "an.nv", Guid? tenantId = null, bool withCsrf = true)
    {
        var client = Factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        if (userId is { } id)
        {
            client.DefaultRequestHeaders.Add("X-Test-User", id.ToString());
            client.DefaultRequestHeaders.Add("X-Test-UserName", userName);
            client.DefaultRequestHeaders.Add("X-Test-Tenant", (tenantId ?? DefaultTenant).ToString());
        }

        if (withCsrf && userId is not null)
        {
            var response = client.GetAsync("/api/v1/core/antiforgery/token").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var token = response.Content.ReadFromJsonAsync<JsonElement>(Json).GetAwaiter().GetResult()
                .GetProperty("data").GetProperty("token").GetString()!;
            client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        }

        return client;
    }

    public static readonly Guid DefaultTenant = Guid.Parse("11111111-1111-7111-8111-111111111111");

    // Đưa mọi kho trong bộ nhớ về trống — host dùng chung cho cả lớp test nên mỗi test bắt đầu từ trạng thái sạch.
    public void Reset()
    {
        Files.Items.Clear();
        Jobs.Items.Clear();
        Notifications.Clear();
        Users.Items.Clear();
        Audit.Entries.Clear();
        Permissions.Clear();
        OwnerChecker.Readers.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Factory.Dispose();
        return ValueTask.CompletedTask;
    }

    // ---- Bản thay thế -----------------------------------------------------------------------------

    // Dùng lại ở test RequiresDocker cần danh tính mà không đi qua đăng nhập thật (TenantsEndpointDatabaseTests).
    // Header X-Test-Operator: true ⇒ claim cờ vận hành — thứ RequireSystemOperatorFilter đọc.
    internal sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var user))
                return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity(
            [
                new Claim(CoreClaimTypes.UserId, user.ToString()),
                new Claim(CoreClaimTypes.UserName, Request.Headers["X-Test-UserName"].ToString()),
                new Claim(CoreClaimTypes.TenantId, Request.Headers["X-Test-Tenant"].ToString()),
            ], TestScheme);

            if (Request.Headers.TryGetValue("X-Test-Operator", out var isOperator))
                identity.AddClaim(new Claim(CoreClaimTypes.IsSystemOperator, isOperator.ToString()));

            // Header X-Test-MustChangePassword ⇒ claim mà PasswordChangeRequiredMiddleware đọc.
            if (Request.Headers.TryGetValue("X-Test-MustChangePassword", out var mustChange))
                identity.AddClaim(new Claim(CoreClaimTypes.MustChangePassword, mustChange.ToString()));

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), TestScheme)));
        }

        // 401/403 đi qua ĐÚNG người ghi envelope mà cookie scheme thật dùng (OnRedirectToLogin /
        // OnRedirectToAccessDenied) — nếu không, scheme thử trả thân rỗng và test không so được hình dạng dây.
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
            => SecurityEnvelopeWriter.WriteEnvelopeAsync(Context, SecurityErrors.NotAuthenticated);

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
            => SecurityEnvelopeWriter.WriteEnvelopeAsync(Context, SecurityErrors.Forbidden);
    }

    private sealed class NullLoggerProvider : ILoggerProvider
    {
        public static readonly NullLoggerProvider Instance = new();

        public ILogger CreateLogger(string categoryName) => Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        public void Dispose()
        {
        }
    }

    private sealed class PassThroughUow : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<TransactionOutcome<T>>> operation, CancellationToken ct = default)
            => (await operation(ct)).Value;
    }

    private sealed class TestPurposes : IFilePurposeSource
    {
        public IReadOnlyCollection<FilePurposeDefinition> GetPurposes() =>
        [
            new("ho-so", [FileContentDetector.Pdf, FileContentDetector.Png, FileContentDetector.Csv]),
            // Nhận văn bản thuần và Docx — để kiểm tên tải về lấy đuôi theo kiểu đã xác định, không theo đuôi client đặt.
            new("ghi-chu", [FileContentDetector.PlainText, FileContentDetector.Docx]),
        ];
    }

    public sealed class InMemoryFileRepo : IFileRepository
    {
        public ConcurrentDictionary<Guid, StoredFile> Items { get; } = new();

        public Task AddAsync(StoredFile file, CancellationToken ct)
        {
            Items[file.Id] = file;
            return Task.CompletedTask;
        }

        public Task<StoredFile?> FindByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(Items.TryGetValue(id, out var file) && !file.IsDeleted ? file : null);
    }

    public sealed class InMemoryJobRepo : IJobRepository
    {
        public ConcurrentDictionary<Guid, Job> Items { get; } = new();

        public Task AddAsync(Job job, CancellationToken ct)
        {
            Items[job.Id] = job;
            return Task.CompletedTask;
        }

        public Task<Job?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.GetValueOrDefault(id));

        public Task<Job?> FindForReadAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.GetValueOrDefault(id));

        public Task<bool> TryClaimAsync(Guid id, DateTimeOffset now, CancellationToken ct) => Task.FromResult(false);

        public Task UpdateProgressAsync(Guid id, int progress, CancellationToken ct) => Task.CompletedTask;
    }

    public sealed class InMemoryNotificationRepo : INotificationRepository
    {
        private readonly object _gate = new();
        private readonly List<(Notification Notification, NotificationRecipient Recipient)> _rows = [];

        public void Clear()
        {
            lock (_gate)
                _rows.Clear();
        }

        public void Seed(Guid userId, string code, IReadOnlyDictionary<string, string> parameters, bool read = false)
        {
            var notification = Notification.Create(code, System.Text.Json.JsonSerializer.Serialize(parameters), null).Value;
            var recipient = NotificationRecipient.Create(notification.Id, userId).Value;
            if (read)
                recipient.MarkRead(DateTimeOffset.UtcNow);

            notification.CreatedAt = DateTimeOffset.UtcNow;
            lock (_gate)
                _rows.Add((notification, recipient));
        }

        public Guid LastNotificationIdFor(Guid userId)
        {
            lock (_gate)
                return _rows.Last(r => r.Recipient.UserId == userId).Notification.Id;
        }

        public Task AddAsync(Notification notification, IReadOnlyCollection<NotificationRecipient> recipients, CancellationToken ct)
        {
            lock (_gate)
            {
                foreach (var recipient in recipients)
                    _rows.Add((notification, recipient));
            }

            return Task.CompletedTask;
        }

        public Task<PagedList<NotificationDto>> ListForUserAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct)
        {
            lock (_gate)
            {
                var mine = _rows.Where(r => r.Recipient.UserId == userId && (!unreadOnly || r.Recipient.ReadAt is null)).ToList();
                return Task.FromResult(new PagedList<NotificationDto>
                {
                    Items = [.. mine.Skip((page - 1) * pageSize).Take(pageSize).Select(r => new NotificationDto(
                        r.Notification.Id,
                        r.Notification.Code,
                        r.Notification.Params is null
                            ? new Dictionary<string, string>()
                            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(r.Notification.Params)!,
                        r.Notification.CreatedAt,
                        r.Recipient.ReadAt))],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = mine.Count,
                });
            }
        }

        public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct)
        {
            lock (_gate)
                return Task.FromResult(_rows.Count(r => r.Recipient.UserId == userId && r.Recipient.ReadAt is null));
        }

        public Task<NotificationRecipient?> FindRecipientAsync(Guid notificationId, Guid userId, CancellationToken ct)
        {
            lock (_gate)
                return Task.FromResult<NotificationRecipient?>(
                    _rows.FirstOrDefault(r => r.Notification.Id == notificationId && r.Recipient.UserId == userId).Recipient);
        }

        public Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
        {
            lock (_gate)
            {
                var unread = _rows.Where(r => r.Recipient.UserId == userId && r.Recipient.ReadAt is null).ToList();
                foreach (var row in unread)
                    row.Recipient.MarkRead(now);

                return Task.FromResult(unread.Count);
            }
        }
    }

    public sealed class InMemoryUserQuery : IUserQueryService
    {
        public List<UserListItemDto> Items { get; } = [];

        public Task<PagedList<UserListItemDto>> SearchAsync(UserSearchCriteria criteria, CancellationToken ct)
            => Task.FromResult(new PagedList<UserListItemDto> { Items = Items, Page = 1, PageSize = 20, TotalCount = Items.Count });

        public Task<UserListItemDto?> FindByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<int> CountAsync(UserSearchCriteria criteria, CancellationToken ct) => Task.FromResult(Items.Count);

        public async IAsyncEnumerable<UserListItemDto> StreamAsync(
            UserSearchCriteria criteria, int maxRows, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            foreach (var item in Items.Take(maxRows))
                yield return item;
        }
    }

    public sealed class RecordingAudit : IAuditTrail
    {
        public ConcurrentQueue<AuditEntry> Entries { get; } = new();

        public void Record(AuditEntry entry) => Entries.Enqueue(entry);
    }

    public sealed class FakePermissions : IPermissionChecker
    {
        private readonly ConcurrentDictionary<(Guid, string), bool> _granted = new();

        public void Grant(Guid userId, string permission) => _granted[(userId, permission)] = true;

        public void Clear() => _granted.Clear();

        public Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken ct)
            => Task.FromResult(_granted.ContainsKey((userId, permission)));

        public Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct)
            => Task.FromResult<IReadOnlySet<string>>(_granted.Keys.Where(k => k.Item1 == userId).Select(k => k.Item2).ToHashSet());

        public Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetPermissionsForRolesAsync(
            IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlySet<string>>>(
                roleIds.Distinct().ToDictionary(id => id, IReadOnlySet<string> (_) => new HashSet<string>()));
    }

    // Danh sách "ai đọc được bản ghi chủ nào" — bản ghi chủ của module là thứ Core không biết, nên test khai tay.
    public sealed class FakeOwnerChecker
    {
        public ConcurrentDictionary<(Guid User, Guid Owner), bool> Readers { get; } = new();

        public void AllowRead(Guid userId, Guid ownerId) => Readers[(userId, ownerId)] = true;
    }

    private sealed class ScopedOwnerChecker(FakeOwnerChecker state, ICurrentUser currentUser) : IFileOwnerAccessChecker
    {
        public string OwnerTable => InMemoryCoreHost.OwnerTable;

        public Task<bool> CanReadAsync(Guid ownerId, CancellationToken ct)
            => Task.FromResult(currentUser.UserId is { } user && state.Readers.ContainsKey((user, ownerId)));

        public Task<bool> CanWriteAsync(Guid ownerId, CancellationToken ct) => Task.FromResult(false);
    }

    private sealed class StampingFileRepo(InMemoryFileRepo store, ICurrentUser currentUser) : IFileRepository
    {
        public Task AddAsync(StoredFile file, CancellationToken ct)
        {
            file.CreatedBy = currentUser.UserName;
            return store.AddAsync(file, ct);
        }

        public Task<StoredFile?> FindByIdAsync(Guid id, CancellationToken ct) => store.FindByIdAsync(id, ct);
    }
}
