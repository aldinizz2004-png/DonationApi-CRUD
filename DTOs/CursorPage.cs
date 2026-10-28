namespace DonationApi.DTOs;

/// <summary>One page of results + the metadata the client needs to get the next page.</summary>
public class CursorPage<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>The page size that was applied.</summary>
    public int PageSize { get; init; }

    /// <summary>true if there are more items after this page.</summary>
    public bool HasNextPage { get; init; }

    /// <summary>Send this back as ?cursor= to get the next page. null on the last page.</summary>
    public string? NextCursor { get; init; }

    /// <summary>Total matching items (with the same filters). Only when includeTotal=true.</summary>
    public int? TotalCount { get; init; }
}
