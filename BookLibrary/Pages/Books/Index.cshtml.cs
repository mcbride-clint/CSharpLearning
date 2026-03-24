using BookLibrary.Interfaces;
using BookLibrary.Patterns.Builder;
using BookLibrary.Patterns.Strategy;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookLibrary.Pages.Books;

// =============================================================================
// RAZOR PAGES: Books/IndexModel (was BooksController.Index + DeleteConfirmed)
// =============================================================================
// In MVC, the list page and the delete action were separate action methods
// on the same controller. In Razor Pages, they live on the same PageModel:
//   OnGetAsync(...)         → was BooksController.Index()
//   OnPostDeleteAsync(id)   → was BooksController.DeleteConfirmed(id)
//
// Named POST handlers:
//   A form with asp-page-handler="Delete" POSTs to ?handler=Delete.
//   The framework routes it to the method named OnPost + "Delete" + Async.
//   This is how a single page handles multiple POST actions without needing
//   a separate controller action per operation.
// =============================================================================
public class IndexModel : PageModel
{
    private readonly ILibraryService _library;
    private readonly BookSorter _sorter;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILibraryService library, BookSorter sorter, ILogger<IndexModel> logger)
    {
        _library = library;
        _sorter  = sorter;
        _logger  = logger;
    }

    // Output properties — populated by OnGetAsync, read by the page.
    // Replaces ViewBag.* from the MVC controller.
    public IEnumerable<BookViewModel> Books { get; private set; } = [];
    public string? CurrentSort { get; private set; }
    public string? CurrentSearch { get; private set; }
    public int? CurrentCategoryId { get; private set; }
    public IEnumerable<SelectListItem> SortOptions { get; private set; } = [];

    public async Task OnGetAsync(string? sort, string? search, int? categoryId)
    {
        CurrentSort       = sort;
        CurrentSearch     = search;
        CurrentCategoryId = categoryId;

        var queryBuilder = new BookSearchQueryBuilder();
        if (!string.IsNullOrWhiteSpace(search))  queryBuilder.WithTitle(search);
        if (categoryId.HasValue)                  queryBuilder.InCategory(categoryId.Value);

        var books  = await _library.SearchBooksAsync(queryBuilder.Build());
        var sorted = _sorter.Sort(books, sort);

        Books = sorted.Select(b => new BookViewModel
        {
            Id            = b.Id,
            Title         = b.Title,
            ISBN          = b.ISBN,
            PublishedYear = b.PublishedYear,
            PageCount     = b.PageCount,
            Price         = b.Price,
            IsAvailable   = b.IsAvailable,
            AuthorName    = b.Author?.FullName ?? "Unknown",
            CategoryName  = b.Category?.Name   ?? "Unknown"
        });

        SortOptions = _sorter.AvailableStrategies
            .Select(s => new SelectListItem(s.DisplayName, s.Key, s.Key == sort));
    }

    // NAMED POST HANDLER:
    // The delete form uses asp-page-handler="Delete" which POSTs to ?handler=Delete.
    // The framework routes it here. No [HttpPost] attribute needed — Razor Pages
    // handlers are HTTP-verb + handler-name by convention.
    //
    // ANTI-FORGERY: Razor Pages validates the anti-forgery token automatically on
    // all POST handlers. The [ValidateAntiForgeryToken] attribute is not needed
    // (it's equivalent to having it on every POST handler by default).
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var deleted = await _library.DeleteBookAsync(id);
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
            deleted ? "Book deleted." : "Book not found.";

        // RAZOR PAGES PRG: RedirectToPage replaces RedirectToAction.
        // "./Index" is relative to the current page's folder (Pages/Books/).
        return RedirectToPage("./Index");
    }
}
