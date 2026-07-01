#nullable enable

namespace ssmsmcp.Application.Abstractions.Shared;

/// <summary>
/// Represents pagination parameters for list queries.
/// </summary>
public sealed record PageRequest
{
    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the page size (number of items per page).
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets the optional property name to sort by.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether to sort in descending order.
    /// </summary>
    public bool SortDescending { get; init; }

    /// <summary>
    /// Calculates the number of items to skip based on page and page size.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Gets the number of items to take (same as PageSize).
    /// </summary>
    public int Take => PageSize;

    /// <summary>
    /// Validates the pagination parameters.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
    public void Validate()
    {
        if (Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Page), Page, "Page must be greater than or equal to 1.");
        }

        if (PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, "PageSize must be greater than or equal to 1.");
        }

        if (PageSize > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(PageSize), PageSize, "PageSize must be less than or equal to 100.");
        }
    }
}
