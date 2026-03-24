using BookLibrary.Models;

namespace BookLibrary.Patterns.Strategy;

// =============================================================================
// STRATEGY PATTERN — Step 1: The Strategy Interface
// =============================================================================
// INTENT: Define a family of algorithms, encapsulate each one, and make them
// interchangeable. The client doesn't care HOW sorting works — just THAT it does.
//
// PROBLEM WITHOUT STRATEGY:
//   Every time you add a sort option you modify BookSorter with a new switch case:
//
//   public IEnumerable<Book> Sort(IEnumerable<Book> books, string sortBy)
//   {
//       return sortBy switch
//       {
//           "title"  => books.OrderBy(b => b.Title),
//           "author" => books.OrderBy(b => b.Author?.LastName),
//           "year"   => books.OrderBy(b => b.PublishedYear),
//           "price"  => books.OrderBy(b => b.Price),   // ← added later, required modifying this method
//           _        => books
//       };
//   }
//
//   This violates Open/Closed Principle: the class must be MODIFIED to be EXTENDED.
//
// SOLUTION WITH STRATEGY:
//   - Define IBookSortStrategy (this file) as the shared contract
//   - Each sort algorithm is its own class (SortByTitle, SortByAuthor, etc.)
//   - BookSorter.cs holds a reference to whichever strategy is active
//   - To add SortByPrice: add ONE new class, change NOTHING else
//
// WHERE IT'S USED:
//   BooksController.Index(string? sort) reads the ?sort= query param and
//   selects the appropriate strategy. BookSorter applies it.
// =============================================================================

public interface IBookSortStrategy
{
    /// <summary>Sorts the given book collection and returns the result.</summary>
    IEnumerable<Book> Sort(IEnumerable<Book> books);

    /// <summary>Human-readable name for this sort (used in views for the sort selector).</summary>
    string DisplayName { get; }

    /// <summary>Query string key that selects this strategy (e.g., "title", "author").</summary>
    string Key { get; }
}
