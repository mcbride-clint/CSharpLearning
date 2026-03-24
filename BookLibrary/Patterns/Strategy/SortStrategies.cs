using BookLibrary.Models;

namespace BookLibrary.Patterns.Strategy;

// =============================================================================
// STRATEGY PATTERN — Step 2: Concrete Strategy Implementations
// =============================================================================
// Each class is a small, focused algorithm that sorts books one specific way.
// All implement IBookSortStrategy so BookSorter can use them interchangeably.
//
// OPEN/CLOSED PRINCIPLE IN ACTION:
//   To add "Sort by Page Count":
//     1. Add 'public class SortByPageCount : IBookSortStrategy { ... }'
//     2. Register it in Program.cs
//   Done. No existing class needs to change.
// =============================================================================

/// <summary>Sorts books alphabetically by title (A → Z).</summary>
public class SortByTitle : IBookSortStrategy
{
    public string DisplayName => "Title (A–Z)";
    public string Key => "title";
    public IEnumerable<Book> Sort(IEnumerable<Book> books) => books.OrderBy(b => b.Title);
}

/// <summary>Sorts books alphabetically by author's last name.</summary>
public class SortByAuthor : IBookSortStrategy
{
    public string DisplayName => "Author";
    public string Key => "author";

    public IEnumerable<Book> Sort(IEnumerable<Book> books)
        => books.OrderBy(b => b.Author?.LastName).ThenBy(b => b.Author?.FirstName);
}

/// <summary>Sorts books from newest to oldest by publication year.</summary>
public class SortByYear : IBookSortStrategy
{
    public string DisplayName => "Newest First";
    public string Key => "year";
    public IEnumerable<Book> Sort(IEnumerable<Book> books) => books.OrderByDescending(b => b.PublishedYear);
}

/// <summary>Sorts books from lowest to highest price.</summary>
public class SortByPrice : IBookSortStrategy
{
    public string DisplayName => "Price (Low–High)";
    public string Key => "price";
    public IEnumerable<Book> Sort(IEnumerable<Book> books) => books.OrderBy(b => b.Price);
}
