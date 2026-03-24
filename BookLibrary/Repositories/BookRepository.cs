using BookLibrary.Data;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Patterns.Builder;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Repositories;

// =============================================================================
// REPOSITORY: BookRepository
// =============================================================================
// LIFETIME: SCOPED — one instance per HTTP request.
//
// BookRepository depends on LibraryDbContext, which is also Scoped.
// Because both are Scoped, they share the SAME DbContext instance within a
// single request. This means:
//   - Multiple repositories within the same request share one DbContext
//   - SaveChangesAsync() in one repository commits ALL pending changes
//     across all repositories — they form an implicit transaction
//
// KEY EF CORE CONCEPTS DEMONSTRATED HERE:
//   1. .Include()       — Eager loading (prevents N+1 queries)
//   2. .AsNoTracking()  — Read-only optimization
//   3. LINQ → SQL       — Queries translate to parameterized SQL
//   4. SaveChangesAsync — The "commit" that writes to the database
// =============================================================================

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _db;

    // CONSTRUCTOR INJECTION: The DI container calls this constructor and
    // automatically provides the LibraryDbContext it created for this request.
    // You NEVER write 'new BookRepository(new LibraryDbContext(...))' — the
    // container handles object creation and lifetime management for you.
    public BookRepository(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        // .AsNoTracking(): tells the Change Tracker to NOT snapshot these entities.
        // Faster for read-only scenarios — EF Core skips the overhead of tracking
        // every property value for change detection.
        // DO NOT use AsNoTracking if you plan to call SaveChanges on these entities.
        return await _db.Books.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<Book>> GetAllWithDetailsAsync()
    {
        // .Include(b => b.Author): EAGER LOADING
        // Without Include(), book.Author would be null (no lazy loading configured).
        //
        // With Include(), EF Core generates a LEFT JOIN in the SQL query:
        //   SELECT b.*, a.*, c.*
        //   FROM Books b
        //   LEFT JOIN Authors a ON b.AuthorId = a.Id
        //   LEFT JOIN Categories c ON b.CategoryId = c.Id
        //
        // This is ONE query for all data — far better than N+1 queries where
        // each book's Author would require a separate SELECT.
        //
        // Watch the console: because EF SQL logging is enabled in appsettings.json,
        // you will see this exact SQL printed when this method runs.
        return await _db.Books
            .Include(b => b.Author)
            .Include(b => b.Category)
            .AsNoTracking()
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(int id)
    {
        // FindAsync: searches the change tracker first (in-memory), then the database.
        // Slightly more efficient than FirstOrDefaultAsync when you expect the entity
        // to already be tracked in the current request.
        return await _db.Books.FindAsync(id);
    }

    public async Task<Book?> GetByIdWithDetailsAsync(int id)
    {
        // FirstOrDefaultAsync: always goes to the database, returns null if not found.
        // We use it here instead of FindAsync because we need .Include() support,
        // which FindAsync does not support.
        return await _db.Books
            .Include(b => b.Author)
            .Include(b => b.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Book>> GetByAuthorAsync(int authorId)
    {
        // LINQ WHERE clause: EF Core translates this lambda into a SQL WHERE clause.
        // This runs in the DATABASE — it does NOT load all books into memory and
        // then filter in C#. EF Core is smart enough to push the predicate to SQL.
        return await _db.Books
            .Where(b => b.AuthorId == authorId)
            .Include(b => b.Author)
            .Include(b => b.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Book>> GetByCategoryAsync(int categoryId)
    {
        return await _db.Books
            .Where(b => b.CategoryId == categoryId)
            .Include(b => b.Author)
            .Include(b => b.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Book>> SearchAsync(BookSearchQuery query)
    {
        // BUILDER PATTERN IN ACTION: the caller built a BookSearchQuery using
        // BookSearchQueryBuilder. We receive an immutable query object here.
        //
        // IQueryable<T>: This is a DEFERRED query — no SQL has run yet.
        // We compose the query by adding .Where() clauses conditionally.
        // EF Core collects all clauses and generates a single SQL query only
        // when ToListAsync() is called at the end.
        IQueryable<Book> queryable = _db.Books
            .Include(b => b.Author)
            .Include(b => b.Category)
            .AsNoTracking();

        // Conditionally add filters — only add clauses for filters the caller specified.
        // Each 'if' adds a WHERE condition to the SQL. No condition = no WHERE clause.
        if (!string.IsNullOrWhiteSpace(query.Title))
            queryable = queryable.Where(b => b.Title.Contains(query.Title));

        if (!string.IsNullOrWhiteSpace(query.AuthorLastName))
            queryable = queryable.Where(b => b.Author.LastName.Contains(query.AuthorLastName));

        if (query.CategoryId.HasValue)
            queryable = queryable.Where(b => b.CategoryId == query.CategoryId.Value);

        if (query.YearFrom.HasValue)
            queryable = queryable.Where(b => b.PublishedYear >= query.YearFrom.Value);

        if (query.YearTo.HasValue)
            queryable = queryable.Where(b => b.PublishedYear <= query.YearTo.Value);

        if (query.MaxPrice.HasValue)
            queryable = queryable.Where(b => b.Price <= query.MaxPrice.Value);

        if (query.AvailableOnly)
            queryable = queryable.Where(b => b.IsAvailable);

        // ToListAsync() — HERE is where the SQL is actually executed.
        // All the .Where() chains above are just building the query expression tree.
        return await queryable.OrderBy(b => b.Title).ToListAsync();
    }

    public async Task AddAsync(Book book)
    {
        // _db.Books.Add(book): marks the entity as "Added" in the change tracker.
        // Nothing is written to the database yet — the INSERT is pending.
        _db.Books.Add(book);

        // SaveChangesAsync(): THIS is when the SQL INSERT runs.
        // EF Core wraps it in a transaction: if anything fails, the INSERT is rolled back.
        // After SaveChangesAsync, book.Id is populated with the database-generated value.
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Book book)
    {
        // _db.Update(book): marks the entity and all its properties as "Modified".
        // EF Core will generate an UPDATE statement for ALL columns.
        //
        // Alternative: if the entity was loaded via this same DbContext instance
        // (without AsNoTracking), EF Core's change tracker already knows what changed
        // and will generate a more targeted UPDATE for only the changed properties.
        _db.Update(book);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book is not null)
        {
            _db.Books.Remove(book);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        // AnyAsync: generates SELECT EXISTS(...) or SELECT COUNT(1) > 0.
        // More efficient than fetching the full entity just to check existence.
        return await _db.Books.AnyAsync(b => b.Id == id);
    }
}
