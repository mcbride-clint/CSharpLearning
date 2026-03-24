using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Patterns.Builder;
using BookLibrary.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace BookLibrary.Patterns.Decorator;

// =============================================================================
// DECORATOR PATTERN
// =============================================================================
// INTENT: Add new behavior (caching) to an existing object WITHOUT modifying it
// and WITHOUT subclassing it. The decorated object is unaware it's being wrapped.
//
// PROBLEM:
//   BookRepository queries the database on every call to GetAllWithDetailsAsync().
//   For the main book list page, which rarely changes, this is wasteful.
//   We want caching — but we don't want to pollute BookRepository with cache logic.
//   Mixing concerns makes code harder to test and maintain.
//
// SOLUTION: Wrap BookRepository in CachedBookRepository.
//   CachedBookRepository implements the SAME IBookRepository interface.
//   The rest of the app (controllers, services) are unaware of the cache layer —
//   they just ask for IBookRepository and get whichever implementation DI provides.
//
// KEY INSIGHT:
//   The class being decorated (BookRepository) doesn't change at all.
//   You compose the behavior by wrapping it in another class.
//   This is the Decorator pattern: object composition over inheritance.
//
// DI WIRING IN PROGRAM.CS:
//   // Register the concrete repository (NOT as the interface)
//   builder.Services.AddScoped<BookRepository>();
//
//   // Register the cached decorator AS the interface
//   builder.Services.AddScoped<IBookRepository>(sp =>
//       new CachedBookRepository(
//           sp.GetRequiredService<BookRepository>(),    // inject the real repo
//           sp.GetRequiredService<IMemoryCache>()));    // inject the cache
//
//   Now any class that requests IBookRepository gets CachedBookRepository.
//   BookRepository never changes. The cache is transparently inserted between
//   the caller and the real repository.
//
// REAL-WORLD USAGE:
//   This exact pattern is used for caching, logging, validation, retrying
//   (e.g., Polly retry policies wrapped around HTTP clients), and metrics.
// =============================================================================

public class CachedBookRepository : IBookRepository
{
    private readonly BookRepository _inner;   // the real repository being decorated
    private readonly IMemoryCache _cache;

    // Cache keys — string constants prevent typos when invalidating cache entries.
    private const string AllBooksCacheKey = "books:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedBookRepository(BookRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<IEnumerable<Book>> GetAllWithDetailsAsync()
    {
        // Cache-aside pattern:
        //   1. Check the cache first.
        //   2. On a cache HIT: return cached data immediately (no DB call).
        //   3. On a cache MISS: query the DB, store result in cache, then return.
        if (_cache.TryGetValue(AllBooksCacheKey, out IEnumerable<Book>? cached))
            return cached!;

        // Cache miss — fall through to the real repository.
        var books = await _inner.GetAllWithDetailsAsync();

        // Store in cache with an absolute expiry of 5 minutes.
        // After 5 minutes, the next request will go to the DB again.
        _cache.Set(AllBooksCacheKey, books, CacheDuration);

        return books;
    }

    // WRITE-THROUGH CACHE INVALIDATION:
    // Any operation that modifies data must evict the stale cache entry.
    // Otherwise callers would get old data for up to 5 minutes after a change.
    public async Task AddAsync(Book book)
    {
        await _inner.AddAsync(book);
        _cache.Remove(AllBooksCacheKey); // invalidate — data has changed
    }

    public async Task UpdateAsync(Book book)
    {
        await _inner.UpdateAsync(book);
        _cache.Remove(AllBooksCacheKey);
    }

    public async Task DeleteAsync(int id)
    {
        await _inner.DeleteAsync(id);
        _cache.Remove(AllBooksCacheKey);
    }

    // All other methods pass through directly to the inner repository.
    // We only cache the "get all" operation — it's the most expensive and most repeated.
    public Task<IEnumerable<Book>> GetAllAsync()                     => _inner.GetAllAsync();
    public Task<Book?> GetByIdAsync(int id)                          => _inner.GetByIdAsync(id);
    public Task<Book?> GetByIdWithDetailsAsync(int id)               => _inner.GetByIdWithDetailsAsync(id);
    public Task<IEnumerable<Book>> GetByAuthorAsync(int authorId)    => _inner.GetByAuthorAsync(authorId);
    public Task<IEnumerable<Book>> GetByCategoryAsync(int categoryId)=> _inner.GetByCategoryAsync(categoryId);
    public Task<IEnumerable<Book>> SearchAsync(BookSearchQuery query) => _inner.SearchAsync(query);
    public Task<bool> ExistsAsync(int id)                            => _inner.ExistsAsync(id);
}
