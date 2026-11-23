namespace EventScheduler.Api.Common;

/// <summary>
/// One page of results plus the totals a client needs to render paging controls.
/// </summary>
/// <param name="Items">The items on this page; empty when <paramref name="Page"/> is past the last page.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Maximum number of items per page.</param>
/// <param name="TotalCount">Number of items across all pages.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>
    /// Number of pages; 0 when there are no items.
    /// </summary>
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
