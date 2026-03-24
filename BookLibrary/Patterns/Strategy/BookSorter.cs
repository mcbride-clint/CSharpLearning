using BookLibrary.Models;

namespace BookLibrary.Patterns.Strategy;

// =============================================================================
// STRATEGY PATTERN — Step 3: The Context
// =============================================================================
// BookSorter is the "context" — it holds a reference to the active strategy
// and delegates sorting to it. BookSorter never knows WHICH algorithm runs;
// it just knows the interface contract.
//
// DI INTEGRATION:
//   All IBookSortStrategy implementations are registered in Program.cs.
//   BookSorter is constructed with ALL of them via IEnumerable<IBookSortStrategy>.
//   This pattern (injecting all implementations of an interface) is called
//   "open-generic registration" or "strategy collection injection".
//
//   In Program.cs:
//     builder.Services.AddSingleton<IBookSortStrategy, SortByTitle>();
//     builder.Services.AddSingleton<IBookSortStrategy, SortByAuthor>();
//     builder.Services.AddSingleton<IBookSortStrategy, SortByYear>();
//     builder.Services.AddSingleton<IBookSortStrategy, SortByPrice>();
//     builder.Services.AddSingleton<BookSorter>();
//
//   BookSorter's constructor receives all four strategies from DI.
//   Then SelectStrategy("author") picks the right one at runtime.
// =============================================================================

public class BookSorter
{
    private readonly IReadOnlyDictionary<string, IBookSortStrategy> _strategies;
    private readonly IBookSortStrategy _defaultStrategy;

    // DI injects ALL registered IBookSortStrategy implementations.
    // We index them by Key for O(1) lookup.
    public BookSorter(IEnumerable<IBookSortStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        _defaultStrategy = _strategies.GetValueOrDefault("title")
            ?? strategies.First();
    }

    /// <summary>
    /// Returns all available sort strategies — useful for rendering a sort dropdown in the view.
    /// </summary>
    public IEnumerable<IBookSortStrategy> AvailableStrategies => _strategies.Values;

    /// <summary>
    /// Sorts the books using the strategy matching <paramref name="sortKey"/>.
    /// Falls back to the default (title) strategy if the key is not recognised.
    /// </summary>
    public IEnumerable<Book> Sort(IEnumerable<Book> books, string? sortKey)
    {
        // RUNTIME STRATEGY SELECTION: the key comes from the HTTP query string (?sort=author).
        // The strategy is picked at runtime — this is the power of the Strategy pattern.
        var strategy = !string.IsNullOrEmpty(sortKey) && _strategies.TryGetValue(sortKey, out var found)
            ? found
            : _defaultStrategy;

        return strategy.Sort(books);
    }
}
