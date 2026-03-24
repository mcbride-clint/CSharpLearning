using BookLibrary.Models;
using BookLibrary.Patterns.Builder;

namespace BookLibrary.Interfaces;

// =============================================================================
// INTERFACE: IBookRepository
// =============================================================================
// WHY PROGRAM TO AN INTERFACE?
//
// The service layer (LibraryService) depends on IBookRepository, NOT on the
// concrete BookRepository class. This has several key benefits:
//
//   1. TESTABILITY: In unit tests you can inject a FakeBookRepository or use
//      a mocking library (e.g., Moq) to simulate database behavior without
//      needing an actual database connection.
//
//   2. SWAPPABILITY: You can swap SQLite for SQL Server, PostgreSQL, or even
//      an in-memory store by writing a new implementation and changing ONE
//      line in Program.cs. No other code changes required.
//
//   3. SOLID — Dependency Inversion Principle (the 'D' in SOLID):
//      High-level modules (LibraryService) should not depend on low-level
//      modules (BookRepository). Both should depend on abstractions (this interface).
//
// The repository pattern encapsulates all data access logic for a single entity.
// Callers never write EF Core code — they only call these methods.
// =============================================================================

public interface IBookRepository
{
    /// <summary>Returns all books without navigation properties (Author/Category not loaded).</summary>
    Task<IEnumerable<Book>> GetAllAsync();

    /// <summary>Returns all books with Author and Category eagerly loaded via JOIN.</summary>
    Task<IEnumerable<Book>> GetAllWithDetailsAsync();

    /// <summary>Returns a single book by ID, or null if not found.</summary>
    Task<Book?> GetByIdAsync(int id);

    /// <summary>Returns a single book with Author and Category loaded, or null.</summary>
    Task<Book?> GetByIdWithDetailsAsync(int id);

    /// <summary>Returns all books by a given author.</summary>
    Task<IEnumerable<Book>> GetByAuthorAsync(int authorId);

    /// <summary>Returns all books in a given category.</summary>
    Task<IEnumerable<Book>> GetByCategoryAsync(int categoryId);

    /// <summary>
    /// Searches books using a <see cref="BookSearchQuery"/> built with the Builder pattern.
    /// Applies filters for title, category, year range, and availability.
    /// </summary>
    Task<IEnumerable<Book>> SearchAsync(BookSearchQuery query);

    /// <summary>Adds a new book and persists it to the database.</summary>
    Task AddAsync(Book book);

    /// <summary>Updates an existing book and persists changes.</summary>
    Task UpdateAsync(Book book);

    /// <summary>Deletes a book by ID.</summary>
    Task DeleteAsync(int id);

    /// <summary>Returns true if a book with the given ID exists.</summary>
    Task<bool> ExistsAsync(int id);
}
