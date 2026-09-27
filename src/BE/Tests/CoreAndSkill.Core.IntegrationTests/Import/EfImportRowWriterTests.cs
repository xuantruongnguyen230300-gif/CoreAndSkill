using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Infrastructure.Import;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Import;

// docs/wiki-core/be/15-import-export.md §4.2, §6.1. Phần CƠ CHẾ của việc huỷ theo dõi và ánh xạ lỗi DB thành lỗi CỦA DÒNG —
// dùng một DbContext giả có ChangeTracker thật nhưng SaveChanges ném theo kịch bản, nên không cần database.
//
// Thứ test này KHÔNG chứng minh: dòng lỗi nằm GIỮA các dòng đúng thật sự không có trong bảng. Đó là
// ImportJobDatabaseTests.MiddleErrorRow_IsNotInTheDatabase (RequiresDocker) — không có test đó thì cơ chế
// có thể bị gỡ trong một lần tái cấu trúc mà không ai biết (§4.2 "Test bắt buộc").
public class EfImportRowWriterTests
{
    private sealed class ProbeRow
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;
    }

    private sealed class ProbeContext(DbContextOptions<ProbeContext> options, Func<Exception?> onSave) : DbContext(options)
    {
        public DbSet<ProbeRow> Rows => Set<ProbeRow>();

        public int SaveCalls { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<ProbeRow>().HasKey(r => r.Id);

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            var failure = onSave();
            return failure is null ? Task.FromResult(1) : throw failure;
        }
    }

    private static ProbeContext NewContext(Func<Exception?> onSave)
        => new(
            new DbContextOptionsBuilder<ProbeContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x").Options,
            onSave);

    private static PostgresException Pg(string sqlState)
        => new("lỗi", "ERROR", "ERROR", sqlState);

    [Fact]
    public async Task Save_Success_SavesEveryRegisteredContext()
    {
        using var first = NewContext(() => null);
        using var second = NewContext(() => null);

        var result = await new EfImportRowWriter([first, second]).SaveRowAsync(default);

        result.IsSuccess.ShouldBeTrue();
        first.SaveCalls.ShouldBe(1);
        second.SaveCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Save_AUniqueViolation_IsADuplicateRow_AndTheTrackedRowIsDiscarded()
    {
        using var context = NewContext(() => new DbUpdateException("x", Pg(PostgresErrorCodes.UniqueViolation)));
        context.Rows.Add(new ProbeRow { Name = "dòng lỗi" });

        var result = await new EfImportRowWriter([context]).SaveRowAsync(default);

        result.Error!.Code.ShouldBe(ImportErrors.DuplicateRow.Code);
        context.ChangeTracker.Entries().ShouldBeEmpty("phần dòng này đã thêm phải bị huỷ, nếu không lần lưu kế sẽ thử ghi lại nó");
    }

    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.NotNullViolation)]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    public async Task Save_AnyOtherConstraintViolation_IsRowNotSaved(string sqlState)
    {
        using var context = NewContext(() => new DbUpdateException("x", Pg(sqlState)));
        context.Rows.Add(new ProbeRow());

        var result = await new EfImportRowWriter([context]).SaveRowAsync(default);

        result.Error!.Code.ShouldBe(ImportErrors.RowNotSaved.Code);
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task Save_ADbUpdateExceptionWithoutAPostgresCause_IsRowNotSaved()
    {
        using var context = NewContext(() => new DbUpdateConcurrencyException("đua nhau"));
        context.Rows.Add(new ProbeRow());

        var result = await new EfImportRowWriter([context]).SaveRowAsync(default);

        result.Error!.Code.ShouldBe(ImportErrors.RowNotSaved.Code);
    }

    [Fact]
    public async Task Save_AnInfrastructureFailure_IsNotAnImportRowError_ItPropagatesSoTheJobFails()
    {
        using var context = NewContext(() => new NpgsqlException("mất kết nối"));

        await Should.ThrowAsync<NpgsqlException>(() => new EfImportRowWriter([context]).SaveRowAsync(default));
    }

    [Fact]
    public void Discard_DetachesEveryPendingEntity_OfEveryContext()
    {
        using var first = NewContext(() => null);
        using var second = NewContext(() => null);
        first.Rows.Add(new ProbeRow());
        first.Rows.Add(new ProbeRow());
        second.Rows.Add(new ProbeRow());

        new EfImportRowWriter([first, second]).DiscardTrackedChanges();

        first.ChangeTracker.Entries().ShouldBeEmpty();
        second.ChangeTracker.Entries().ShouldBeEmpty();
    }

    // Bẫy §4.2 viết thành kịch bản: dòng 5 thêm entity rồi bị coi là lỗi; nếu KHÔNG huỷ, entity đó vẫn nằm trong bộ theo
    // dõi và bị ghi cùng dòng 6.
    [Fact]
    public async Task TheTrap_AnEntityOfAFailedRow_IsNotSavedTogetherWithTheNextRow_WhenWeDiscard()
    {
        var saved = new List<string>();
        using var context = NewContext(() => null);
        var writer = new EfImportRowWriter([context]);

        // Dòng 5: thêm rồi phát hiện sai nghiệp vụ -> bộ chạy huỷ.
        context.Rows.Add(new ProbeRow { Name = "dòng 5 (lỗi)" });
        writer.DiscardTrackedChanges();

        // Dòng 6: đúng.
        context.Rows.Add(new ProbeRow { Name = "dòng 6" });
        saved.AddRange(context.ChangeTracker.Entries<ProbeRow>().Where(e => e.State == EntityState.Added).Select(e => e.Entity.Name));
        (await writer.SaveRowAsync(default)).IsSuccess.ShouldBeTrue();

        saved.ShouldBe(["dòng 6"]);
    }

    [Fact]
    public async Task TheTrap_WithoutDiscarding_TheFailedRowsEntityRidesAlongWithTheNextRow()
    {
        // Chứng minh bẫy CÓ THẬT (nếu test này đổi kết quả thì test phía trên không còn ý nghĩa): không huỷ thì
        // cả hai entity cùng ở trạng thái chờ thêm khi lần lưu kế tiếp chạy.
        using var context = NewContext(() => null);

        context.Rows.Add(new ProbeRow { Name = "dòng 5 (lỗi)" });
        context.Rows.Add(new ProbeRow { Name = "dòng 6" });

        context.ChangeTracker.Entries<ProbeRow>().Count(e => e.State == EntityState.Added).ShouldBe(2);
        await Task.CompletedTask;
    }
}
