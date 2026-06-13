namespace FlowLedger.Application.Common.Paging;

public enum SortDirection
{
    Asc,
    Desc
}

public abstract record PagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}
