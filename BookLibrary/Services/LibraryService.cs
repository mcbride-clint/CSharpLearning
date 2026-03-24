using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Patterns.Builder;
using BookLibrary.Patterns.GuardClauses;

namespace BookLibrary.Services;

// =============================================================================
// SERVICE: LibraryService — SCOPED LIFETIME + BUSINESS LOGIC
// =============================================================================
// SCOPED: One instance per HTTP request. All classes within the same request
// that depend on ILibraryService receive the SAME LibraryService instance.
//
// WHY SCOPED (not Singleton or Transient)?
//   LibraryService depends on IBookRepository, IAuthorRepository, and
//   ICategoryRepository — all of which are Scoped. A Scoped service can only
//   be safely injected into another Scoped service. Registering LibraryService
//   as Singleton would trigger the captive dependency error at startup.
//
// WHAT BELONGS IN A SERVICE (vs Repository vs Controller)?
//   Repository = "How do I read/write this data?" (pure data access)
//   Service    = "What rules govern this operation?" (business logic)
//   Controller = "What HTTP verb/route? What response format?" (HTTP concerns)
//
//   LibraryService ENFORCES BUSINESS RULES:
//   - Authors with books cannot be deleted (enforced here AND in the DB)
//   - A book must have a valid author and category
//   - Guard clauses validate inputs early (see Guard.cs)
//
// SHARED DbContext (implicit transaction):
//   All three repositories injected here share the SAME LibraryDbContext
//   instance (because all are Scoped, and the container gives the same Scoped
//   instance to all within one request). This means:
//   - If you Add a book AND update statistics in the same request,
//     both changes are tracked by the same DbContext.
//   - One SaveChangesAsync() would commit ALL pending changes atomically.
// =============================================================================

public class LibraryService : ILibraryService
{
    private readonly IBookRepository _books;
    private readonly IAuthorRepository _authors;
    private readonly ICategoryRepository _categories;
    private readonly ILogger<LibraryService> _logger;
    private readonly IOperationIdService _operationId;

    // All dependencies injected via constructor — the DI container supplies them.
    // ILogger<LibraryService> is built into ASP.NET Core — no explicit registration needed.
    // IOperationIdService is Transient — this instance is different from the one in controllers.
    public LibraryService(
        IBookRepository books,
        IAuthorRepository authors,
        ICategoryRepository categories,
        ILogger<LibraryService> logger,
        IOperationIdService operationId)
    {
        _books = books;
        _authors = authors;
        _categories = categories;
        _logger = logger;
        _operationId = operationId;
    }

    // ==========================================================================
    // BOOKS
    // ==========================================================================

    public async Task<IEnumerable<Book>> GetAllBooksAsync()
    {
        _logger.LogDebug("LibraryService.GetAllBooksAsync — OperationId: {OpId}", _operationId.OperationId);
        return await _books.GetAllWithDetailsAsync();
    }

    public async Task<IEnumerable<Book>> SearchBooksAsync(BookSearchQuery query)
    {
        // GUARD CLAUSE: validate inputs at the top before any work begins.
        // See Guard.cs for the pattern explanation.
        Guard.AgainstNull(query, nameof(query));

        _logger.LogInformation("Searching books — Title: {Title}, CategoryId: {Cat}",
            query.Title ?? "(any)", query.CategoryId?.ToString() ?? "(any)");

        return await _books.SearchAsync(query);
    }

    public async Task<Book?> GetBookAsync(int id)
    {
        Guard.AgainstNonPositive(id, nameof(id));
        return await _books.GetByIdWithDetailsAsync(id);
    }

    public async Task<Book> CreateBookAsync(Book book)
    {
        // Guard clauses first — validate before touching the database.
        Guard.AgainstNull(book, nameof(book));
        Guard.AgainstNullOrEmpty(book.Title, nameof(book.Title));
        Guard.AgainstNonPositive(book.AuthorId, nameof(book.AuthorId));
        Guard.AgainstNonPositive(book.CategoryId, nameof(book.CategoryId));

        // Business rule: the author must exist.
        if (!await _authors.ExistsAsync(book.AuthorId))
            throw new InvalidOperationException($"Author with ID {book.AuthorId} does not exist.");

        // Business rule: the category must exist.
        if (!await _categories.ExistsAsync(book.CategoryId))
            throw new InvalidOperationException($"Category with ID {book.CategoryId} does not exist.");

        await _books.AddAsync(book);
        _logger.LogInformation("Created book '{Title}' (ID: {Id})", book.Title, book.Id);
        return book;
    }

    public async Task<bool> UpdateBookAsync(Book book)
    {
        Guard.AgainstNull(book, nameof(book));
        Guard.AgainstNonPositive(book.Id, nameof(book.Id));

        if (!await _books.ExistsAsync(book.Id))
            return false;

        await _books.UpdateAsync(book);
        return true;
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        Guard.AgainstNonPositive(id, nameof(id));

        if (!await _books.ExistsAsync(id))
            return false;

        await _books.DeleteAsync(id);
        _logger.LogInformation("Deleted book ID: {Id}", id);
        return true;
    }

    // ==========================================================================
    // AUTHORS
    // ==========================================================================

    public async Task<IEnumerable<Author>> GetAllAuthorsAsync()
    {
        return await _authors.GetAllAsync();
    }

    public async Task<Author?> GetAuthorAsync(int id)
    {
        Guard.AgainstNonPositive(id, nameof(id));
        return await _authors.GetByIdWithBooksAsync(id);
    }

    public async Task<Author> CreateAuthorAsync(Author author)
    {
        Guard.AgainstNull(author, nameof(author));
        Guard.AgainstNullOrEmpty(author.FirstName, nameof(author.FirstName));
        Guard.AgainstNullOrEmpty(author.LastName, nameof(author.LastName));

        await _authors.AddAsync(author);
        _logger.LogInformation("Created author '{Name}' (ID: {Id})", author.FullName, author.Id);
        return author;
    }

    public async Task<bool> UpdateAuthorAsync(Author author)
    {
        Guard.AgainstNull(author, nameof(author));
        Guard.AgainstNonPositive(author.Id, nameof(author.Id));

        if (!await _authors.ExistsAsync(author.Id))
            return false;

        await _authors.UpdateAsync(author);
        return true;
    }

    public async Task<bool> DeleteAuthorAsync(int id)
    {
        Guard.AgainstNonPositive(id, nameof(id));

        // BUSINESS RULE: An author who still has books in the library cannot be deleted.
        // This prevents orphaned data and enforces referential integrity at the application
        // layer (the DB also enforces this via DeleteBehavior.Restrict, but we catch it
        // early here with a friendly message rather than a database exception).
        //
        // This is why business rules belong in the SERVICE, not the repository:
        //   - Repository: "I can delete this row"
        //   - Service: "But SHOULD I? Not if the author has books."
        if (await _authors.HasBooksAsync(id))
        {
            _logger.LogWarning(
                "Refused to delete Author ID {Id} — author still has books in the library.", id);
            return false;
        }

        await _authors.DeleteAsync(id);
        _logger.LogInformation("Deleted Author ID: {Id}", id);
        return true;
    }

    // ==========================================================================
    // CATEGORIES
    // ==========================================================================

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _categories.GetAllAsync();
    }

    public async Task<Category?> GetCategoryAsync(int id)
    {
        Guard.AgainstNonPositive(id, nameof(id));
        return await _categories.GetByIdAsync(id);
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        Guard.AgainstNull(category, nameof(category));
        Guard.AgainstNullOrEmpty(category.Name, nameof(category.Name));

        await _categories.AddAsync(category);
        return category;
    }

    public async Task<bool> UpdateCategoryAsync(Category category)
    {
        Guard.AgainstNull(category, nameof(category));

        if (!await _categories.ExistsAsync(category.Id))
            return false;

        await _categories.UpdateAsync(category);
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        Guard.AgainstNonPositive(id, nameof(id));

        if (await _categories.HasBooksAsync(id))
        {
            _logger.LogWarning("Refused to delete Category ID {Id} — category still has books.", id);
            return false;
        }

        await _categories.DeleteAsync(id);
        return true;
    }
}
