namespace FlowLedger.Infrastructure.Common;

public static class PagingQueryGuard
{
    private static readonly HashSet<int> AllowedPageSizes = [1, 10, 25, 50, 100];

    public static int Page(int page)
    {
        if (page < 1)
        {
            throw new InvalidOperationException("Page must be greater than or equal to 1.");
        }

        return page;
    }

    public static int PageSize(int pageSize)
    {
        if (!AllowedPageSizes.Contains(pageSize))
        {
            throw new InvalidOperationException("Page size must be one of 1, 10, 25, 50, or 100.");
        }

        return pageSize;
    }

    public static string? Search(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var trimmed = search.Trim();
        if (trimmed.Length > 200)
        {
            throw new InvalidOperationException("Search must be 200 characters or fewer.");
        }

        return trimmed;
    }

    public static bool Descending(string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortDirection))
        {
            return true;
        }

        return sortDirection.Trim().ToLowerInvariant() switch
        {
            "desc" => true,
            "asc" => false,
            _ => throw new InvalidOperationException("Sort direction must be asc or desc.")
        };
    }

    public static string SortBy(string? sortBy, string defaultSort, params string[] allowedSorts)
    {
        var value = string.IsNullOrWhiteSpace(sortBy) ? defaultSort : sortBy.Trim();
        if (!allowedSorts.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Sort column '{value}' is not supported.");
        }

        return value;
    }
}
