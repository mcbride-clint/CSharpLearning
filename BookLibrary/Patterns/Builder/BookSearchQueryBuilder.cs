namespace BookLibrary.Patterns.Builder;

// =============================================================================
// BUILDER PATTERN — Step 2: The Builder
// =============================================================================
// INTENT: Construct complex objects step by step using a fluent interface.
//
// PROBLEM SOLVED (without Builder):
//   BookSearchQuery has 7 optional filters. You COULD write a constructor:
//     new BookSearchQuery("Dune", null, 3, 1960, 2000, 14.99m, true)
//   But this is unreadable. Which argument is which? What if you want only
//   filters 1, 3, and 7? You'd need many constructor overloads.
//
// SOLUTION (with Builder):
//   new BookSearchQueryBuilder()
//       .WithTitle("Dune")
//       .InCategory(3)
//       .PublishedBetween(1960, 2000)
//       .AvailableOnly()
//       .Build()
//
//   Each method name documents what it does. You pick exactly the filters
//   you need. Adding a new filter (e.g., WithMinRating) doesn't break any
//   existing builder call sites.
//
// FLUENT INTERFACE:
//   Each 'With*' method returns 'this' (the builder itself), which is what
//   enables chaining: builder.WithTitle("X").InCategory(3).Build()
//
// ADVANTAGE — Open/Closed Principle:
//   You can add new filters by adding a new 'With*' method. Existing code
//   that already calls Build() doesn't need to change.
// =============================================================================

public class BookSearchQueryBuilder
{
    // Mutable internal state — only this builder class touches these fields.
    private string? _title;
    private string? _authorLastName;
    private int? _categoryId;
    private int? _yearFrom;
    private int? _yearTo;
    private decimal? _maxPrice;
    private bool _availableOnly;

    // Each method sets one filter and returns 'this' to enable chaining.

    public BookSearchQueryBuilder WithTitle(string title)
    {
        _title = title;
        return this; // return 'this' enables method chaining
    }

    public BookSearchQueryBuilder WithAuthorLastName(string lastName)
    {
        _authorLastName = lastName;
        return this;
    }

    public BookSearchQueryBuilder InCategory(int categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public BookSearchQueryBuilder PublishedBetween(int yearFrom, int yearTo)
    {
        _yearFrom = yearFrom;
        _yearTo = yearTo;
        return this;
    }

    public BookSearchQueryBuilder PublishedFrom(int year)
    {
        _yearFrom = year;
        return this;
    }

    public BookSearchQueryBuilder PublishedBefore(int year)
    {
        _yearTo = year;
        return this;
    }

    public BookSearchQueryBuilder WithMaxPrice(decimal maxPrice)
    {
        _maxPrice = maxPrice;
        return this;
    }

    public BookSearchQueryBuilder AvailableOnly()
    {
        _availableOnly = true;
        return this;
    }

    /// <summary>
    /// Constructs and returns the immutable <see cref="BookSearchQuery"/>.
    /// The builder can be reused after calling Build() — call Reset() first.
    /// </summary>
    public BookSearchQuery Build()
    {
        return new BookSearchQuery
        {
            Title          = _title,
            AuthorLastName = _authorLastName,
            CategoryId     = _categoryId,
            YearFrom       = _yearFrom,
            YearTo         = _yearTo,
            MaxPrice       = _maxPrice,
            AvailableOnly  = _availableOnly
        };
    }

    /// <summary>Resets the builder so it can be reused to build a different query.</summary>
    public BookSearchQueryBuilder Reset()
    {
        _title = null;
        _authorLastName = null;
        _categoryId = null;
        _yearFrom = null;
        _yearTo = null;
        _maxPrice = null;
        _availableOnly = false;
        return this;
    }
}
