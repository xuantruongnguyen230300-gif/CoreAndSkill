namespace CoreAndSkill.Core.Application.Common.Paging;

// docs/quy-uoc/be-cqrs-handler.md §9.1. KHÔNG có TotalPages — suy từ TotalCount/PageSize.
public sealed class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
