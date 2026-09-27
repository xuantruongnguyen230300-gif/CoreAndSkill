using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// docs/database/script-runbook.md §5.1, §5.2. App TỪ CHỐI KHỞI ĐỘNG khi DB thiếu migration mà
// assembly biết (luật E8); ngược lại (DB có thừa) chỉ cảnh báo, vẫn khởi động.
public sealed record SchemaDriftReport(
    string ContextName,
    IReadOnlyList<string> MissingInDatabase,
    IReadOnlyList<string> UnknownToApplication);

public interface ISchemaVerifier
{
    Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct);
}

internal sealed class SchemaVerifier(IEnumerable<DbContext> contexts) : ISchemaVerifier
{
    public async Task<IReadOnlyList<SchemaDriftReport>> InspectAsync(CancellationToken ct)
    {
        var reports = new List<SchemaDriftReport>();

        foreach (var context in contexts)
        {
            var known = context.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
            var applied = (await context.Database.GetAppliedMigrationsAsync(ct))
                .ToHashSet(StringComparer.Ordinal);

            reports.Add(new SchemaDriftReport(
                ContextName: context.GetType().Name,
                MissingInDatabase: known.Except(applied).Order().ToList(),
                UnknownToApplication: applied.Except(known).Order().ToList()));
        }

        return reports;
    }
}
