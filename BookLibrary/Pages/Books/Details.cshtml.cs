using BookLibrary.Interfaces;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Books;

// =============================================================================
// RAZOR PAGES: Books/DetailsModel (was BooksController.Details)
// =============================================================================
// ROUTE: @page "{id:int}" in Details.cshtml makes the id part of the URL path.
//   URL: /Books/Details/5
//   id is bound from the route segment, not from a query string.
//
// Returning NotFound() from a handler works exactly as in MVC — it produces HTTP 404.
// =============================================================================
public class DetailsModel : PageModel
{
    private readonly ILibraryService _library;

    public DetailsModel(ILibraryService library)
    {
        _library = library;
    }

    public BookViewModel Book { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var book = await _library.GetBookAsync(id);
        if (book is null)
            return NotFound();

        Book = new BookViewModel
        {
            Id            = book.Id,
            Title         = book.Title,
            ISBN          = book.ISBN,
            PublishedYear = book.PublishedYear,
            PageCount     = book.PageCount,
            Price         = book.Price,
            IsAvailable   = book.IsAvailable,
            Description   = book.Description,
            AuthorName    = book.Author?.FullName ?? "Unknown",
            CategoryName  = book.Category?.Name   ?? "Unknown"
        };

        return Page(); // Page() is the Razor Pages equivalent of View() — renders the .cshtml
    }
}
