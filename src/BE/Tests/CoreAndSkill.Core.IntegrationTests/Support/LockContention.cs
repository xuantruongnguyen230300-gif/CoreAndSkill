using Npgsql;
using Shouldly;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Ép hai lượt ghi CHỒNG nhau trên PostgreSQL thật thay vì mong chúng tình cờ chồng: "người kia" là một kết nối Npgsql riêng giữ
// một transaction dở dang (đã ghi, chưa commit — đang giữ khoá dòng mà câu ghi của nó lấy). Request thật đi qua HTTP tới lượt
// nó phải CHỜ khoá đó; test đợi tới khi thấy nó chờ rồi mới commit phía kia. Dùng cho test RequiresDocker.
internal sealed class HeldTransaction : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    private HeldTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public static async Task<HeldTransaction> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return new HeldTransaction(connection, await connection.BeginTransactionAsync());
    }

    // Chống test rỗng: câu dựng tình huống phải chạm đúng một dòng — không thì không có khoá nào để request thật phải chờ.
    public async Task ExecuteOneRowAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, _connection, _transaction);
        command.Parameters.AddRange(parameters);
        (await command.ExecuteNonQueryAsync()).ShouldBe(1, $"chống test rỗng: câu dựng tình huống phải chạm đúng một dòng — {sql}");
    }

    public Task CommitAsync() => _transaction.CommitAsync();

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync(); // chưa commit thì quay lại — test đỏ giữa chừng không để lại khoá
        await _connection.DisposeAsync();
    }
}

internal static class LockContention
{
    // Nhỏ hơn hẳn lock_timeout 5 giây của Core (Persistence/LockTimeout): request phải bị THẤY đang chờ trước khi nó tự hết
    // hạn chờ.
    public static readonly TimeSpan ProbeLimit = TimeSpan.FromSeconds(4);

    // Chống test rỗng: request phải THẬT SỰ đứng chờ khoá của transaction dở dang, không được xong trước (nối đuôi thay vì
    // chồng nhau) — nếu không, kết quả nói về một kịch bản tuần tự, không phải kịch bản tranh chấp đang được test.
    public static async Task WaitUntilSomeSessionWaitsOnALockAsync(PostgresFixture db, Task<HttpResponseMessage> request)
    {
        var deadline = DateTime.UtcNow + ProbeLimit;
        while (DateTime.UtcNow < deadline)
        {
            if (request.IsCompleted)
            {
                var early = await request;
                throw new ShouldAssertException(
                    $"chống test rỗng: request xong ({(int)early.StatusCode}) trước khi chờ khoá — {await early.Content.ReadAsStringAsync()}");
            }

            var waiting = await db.CountAsync(
                "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock'");
            if (waiting > 0)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new ShouldAssertException($"chống test rỗng: sau {ProbeLimit.TotalSeconds} giây không phiên nào chờ khoá.");
    }
}
