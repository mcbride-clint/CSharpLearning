namespace BookLibrary.Patterns.Builder;

// =============================================================================
// BUILDER PATTERN — Step 1: The Product (the object being built)
// =============================================================================
// BookSearchQuery is an IMMUTABLE object — its properties are set once via the
// builder and cannot be changed afterward. Immutability is enforced using
// C# 'init' accessors: properties can only be set during object initialization.
//
// WHY IMMUTABLE?
//   Once you build a query and pass it to the repository, you don't want any
//   code along the way to quietly modify the search criteria. Immutability
//   makes bugs easier to find: if results are wrong, the query object is fixed.
//
// See BookSearchQueryBuilder.cs for how to construct this object.
// =============================================================================

/// <summary>
/// Represents search criteria for filtering books.
/// Construct instances using <see cref="BookSearchQueryBuilder"/>.
/// </summary>
public sealed class BookSearchQuery
{
    // 'init' means: settable only during object initialization (i.e., 'new BookSearchQuery { Title = "..." }')
    // After construction, all properties are effectively read-only.
    public string? Title { get; init; }
    public string? AuthorLastName { get; init; }
    public int? CategoryId { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool AvailableOnly { get; init; }

    // Private constructor: forces callers to use the builder (or the factory method below).
    // This is optional — you could make it public — but private reinforces the pattern.
    internal BookSearchQuery() { }

    /// <summary>Creates an empty query that matches all books.</summary>
    public static BookSearchQuery Empty => new BookSearchQueryBuilder().Build();
}
