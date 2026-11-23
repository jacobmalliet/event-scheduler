namespace EventScheduler.Api.Common;

/// <summary>
/// Paging limits shared by every list endpoint that returns a <see cref="PagedResponse{T}"/>.
/// </summary>
public static class Paging
{
    /// <summary>
    /// The page size used when a request doesn't specify one.
    /// </summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// The largest page size a request may ask for.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// The highest page number a request may ask for. Keeps <c>(page - 1) * pageSize</c> within
    /// <see cref="int"/> for every allowed page size.
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;
}
