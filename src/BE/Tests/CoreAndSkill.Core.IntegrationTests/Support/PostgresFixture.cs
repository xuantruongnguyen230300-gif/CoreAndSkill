using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// PostgreSQL THẬT cho integration test — luật T2 (docs/RULES.md), docs/wiki-core/be/04-testing-strategy.md §4.
// MỘT container cho cả lượt chạy (§4.2): khai qua PostgresCollection (ICollectionFixture), nên mọi test
// class mang [Collection(PostgresCollection.Name)] dùng chung, và các class đó chạy TUẦN TỰ với nhau
// (cùng collection) — cần thiết vì chúng dùng chung một database.
//
// Schema áp bằng ĐÚNG đường của docs/database/script-runbook.md §3.3 bước 2: `psql -v ON_ERROR_STOP=1
// -f` từng script trong database/scripts/core/ theo thứ tự số — KHÔNG dựng bằng EnsureCreated hay
// Migrate() (04-testing-strategy.md §4.4: dựng từ mô hình thì sai lệch mô hình↔migration vô hình
// trong test; ADR-0009: không auto-migrate ở đâu cả). Script chạy bằng tài khoản superuser của
// container, nên bảng quyền của tài khoản ứng dụng (luật M13, runbook §3.6) KHÔNG được kiểm ở đây.
//
// Cô lập dữ liệu giữa các test = xoá dữ liệu (§4.3 cách 2): ResetAsync() chạy ở đầu MỖI test. Không
// dùng transaction-rollback vì code đang test tự mở transaction (UnitOfWork).
public sealed class PostgresFixture : IAsyncLifetime
{
    // Cùng phiên bản với docker-compose.yml (nguồn phiên bản PostgreSQL của repo) và runbook §6.
    private const string Image = "postgres:18";
    private const string DatabaseName = "coreandskill_test";
    private const string SuperUser = "postgres";
    private const string SuperPassword = "postgres";

    // Bảng DỮ LIỆU của đơn vị. KHÔNG có core.permission / core.permission_resource (danh mục quyền vào
    // bằng migration — 0002 — phải sống suốt lượt chạy), core.__ef_migrations_history,
    // core.schema_script_history và core.data_protection_key. CASCADE chỉ để không phụ thuộc thứ tự
    // khoá ngoại; mọi bảng có thể bị kéo theo đều nằm sẵn trong danh sách này.
    private const string ResetSql = """
        TRUNCATE TABLE
            core.outbox_message,
            core.notification_recipient,
            core.notification,
            core.job,
            core.file,
            core.audit_log,
            core.menu_item_role,
            core.menu_item,
            core.role_permission,
            core.app_user_token,
            core.app_user_role,
            core.app_user_login,
            core.app_user_claim,
            core.app_role_claim,
            core.app_user,
            core.app_role,
            core.tenant
        CASCADE
        """;

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image)
        .WithDatabase(DatabaseName)
        .WithUsername(SuperUser)
        .WithPassword(SuperPassword)
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ApplySchemaScriptsAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(ResetSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    // Đọc/đếm THẲNG qua Npgsql, không qua CoreDbContext — kiểm chứng phải độc lập với chính code đang
    // test (không bộ lọc tenant, không xoá mềm, không interceptor nào che khuất kết quả).
    public async Task<long> CountAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);

        var rows = new List<T>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add(map(reader));

        return rows;
    }

    private async Task ApplySchemaScriptsAsync()
    {
        var scriptsDirectory = LocateScriptsDirectory();
        var scripts = Directory.GetFiles(scriptsDirectory, "*.sql")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToList();

        // Tập rỗng thì test bên dưới chạy trên database không có bảng — báo to ngay ở đây (luật T6).
        if (scripts.Count == 0)
            throw new InvalidOperationException($"Không thấy script schema nào trong '{scriptsDirectory}'.");

        foreach (var script in scripts)
        {
            var name = Path.GetFileName(script);
            var remotePath = $"/tmp/{name}";

            await _container.CopyAsync(WithoutUtf8Bom(await File.ReadAllBytesAsync(script)), remotePath);

            // psql trong container, kết nối qua socket cục bộ (không cần mật khẩu) — cùng công cụ và cùng
            // cờ -v ON_ERROR_STOP=1 như runbook §3.3 bước 2. Lỗi ở script nào thì DỪNG ngay ở script đó.
            var result = await _container.ExecAsync(
                ["psql", "-U", SuperUser, "-d", DatabaseName, "-v", "ON_ERROR_STOP=1", "-f", remotePath]);

            if (result.ExitCode != 0)
                throw new InvalidOperationException(
                    $"Áp script schema '{name}' thất bại (psql thoát mã {result.ExitCode}).{Environment.NewLine}" +
                    $"stderr: {result.Stderr}{Environment.NewLine}stdout: {result.Stdout}");
        }
    }

    // Script do `dotnet ef migrations script` sinh có BOM UTF-8 ở đầu file; bỏ nó để không phụ thuộc
    // phiên bản psql có nuốt BOM hay không.
    private static byte[] WithoutUtf8Bom(byte[] content)
        => content is [0xEF, 0xBB, 0xBF, ..] ? content[3..] : content;

    // Đi ngược từ thư mục chạy test lên tới khi thấy database/scripts/core — không giả định độ sâu
    // của bin/Debug/net10.0. internal: test không cần Docker đọc script bằng đúng đường này (AppUserUniqueIndexesTests).
    internal static string LocateScriptsDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "database", "scripts", "core");
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException(
            $"Không tìm thấy database/scripts/core đi ngược từ '{AppContext.BaseDirectory}'.");
    }
}
