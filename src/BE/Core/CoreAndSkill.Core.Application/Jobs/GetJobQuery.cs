using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Jobs;

// GET /api/v1/core/jobs/{id} — docs/contracts/jobs.md §1. Mỗi field ứng với một cột của core.job
// (Kind <-> type). Result và Error là jsonb đi thẳng ra dây, không qua kiểu C# nào.
public sealed record GetJobQuery(Guid Id) : IQuery<JobDto>;
