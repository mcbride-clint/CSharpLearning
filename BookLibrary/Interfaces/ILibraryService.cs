using BookLibrary.Models;
using BookLibrary.Patterns.Builder;

namespace BookLibrary.Interfaces;

// =============================================================================
// INTERFACE: ILibraryService
// =============================================================================
// The service layer sits ABOVE the repositories. It orchestrates multiple
// repositories and enforces BUSINESS RULES.
//
// SEPARATION OF CONCERNS:
//   - Repositories: "How do I read/write data?"
//   - Services:     "Should this operation be allowed? What rules apply?"
//   - Controllers:  "What HTTP request came in? What response do I return?"
//
// Services are the right place for:
//   - Rules that span multiple entities ("can't delete author with books")
//   - Operations that require multiple repository calls in sequence
//   - Business logic that would clutter controllers or pollute repositories
// =============================================================================

public interface ILibraryService
{
    // ---- Books ---------------------------------------------------------------
    Task<IEnumerable<Book>> GetAllBooksAsync();
    Task<IEnumerable<Book>> SearchBooksAsync(BookSearchQuery query);
    Task<Book?> GetBookAsync(int id);
    Task<Book> CreateBookAsync(Book book);
    Task<bool> UpdateBookAsync(Book book);
    Task<bool> DeleteBookAsync(int id);

    // ---- Authors -------------------------------------------------------------
    Task<IEnumerable<Author>> GetAllAuthorsAsync();
    Task<Author?> GetAuthorAsync(int id);
    Task<Author> CreateAuthorAsync(Author author);
    Task<bool> UpdateAuthorAsync(Author author);

    /// <summary>
    /// Deletes an author. Returns false (and does NOT delete) if the author
    /// still has books in the library — a business rule enforced at the service level.
    /// </summary>
    Task<bool> DeleteAuthorAsync(int id);

    // ---- Categories ----------------------------------------------------------
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryAsync(int id);
    Task<Category> CreateCategoryAsync(Category category);
    Task<bool> UpdateCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(int id);
}
