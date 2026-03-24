using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Patterns.Strategy;
using BookLibrary.Patterns.Builder;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookLibrary.Controllers;

// =============================================================================
// MVC CONTROLLER: BooksController
// =============================================================================
// Demonstrates the full MVC cycle for CRUD operations:
//
//   GET  /Books           → Index (list)
//   GET  /Books/Details/5 → Details (read one)
//   GET  /Books/Create    → Create form
//   POST /Books/Create    → Process form submission
//   GET  /Books/Edit/5    → Edit form (pre-filled)
//   POST /Books/Edit/5    → Process edit submission
//   POST /Books/Delete/5  → Delete (POST-only for safety)
//
// KEY PATTERNS SHOWN:
//   1. PRG (Post-Redirect-Get) — prevents double submission on browser refresh
//   2. ModelState validation — Data Annotations drive form validation
//   3. [Bind] — prevents over-posting attacks
//   4. TempData — survives exactly ONE redirect (for success/failure messages)
//   5. SelectList — populates dropdowns in views
//   6. Strategy Pattern — ?sort= query param selects sort algorithm
// =============================================================================

public class BooksController : Controller
{
    private readonly ILibraryService _library;
    private readonly BookSorter _sorter;
    private readonly ILogger<BooksController> _logger;

    public BooksController(ILibraryService library, BookSorter sorter, ILogger<BooksController> logger)
    {
        _library = library;
        _sorter = sorter;
        _logger = logger;
    }

    // =========================================================================
    // GET /Books?sort=author&search=dune
    // =========================================================================
    public async Task<IActionResult> Index(string? sort, string? search, int? categoryId)
    {
        // BUILDER PATTERN: construct the search query with only the filters the user specified.
        // The builder's fluent API makes it clear which filters are active.
        var queryBuilder = new BookSearchQueryBuilder();

        if (!string.IsNullOrWhiteSpace(search))
            queryBuilder.WithTitle(search);

        if (categoryId.HasValue)
            queryBuilder.InCategory(categoryId.Value);

        var books = await _library.SearchBooksAsync(queryBuilder.Build());

        // STRATEGY PATTERN: the ?sort= param selects the sort algorithm at runtime.
        // BookSorter holds all registered strategies and picks the right one.
        var sorted = _sorter.Sort(books, sort);

        // Map domain models → ViewModels (no domain entity reaches the view directly)
        var viewModels = sorted.Select(b => new BookViewModel
        {
            Id           = b.Id,
            Title        = b.Title,
            ISBN         = b.ISBN,
            PublishedYear= b.PublishedYear,
            PageCount    = b.PageCount,
            Price        = b.Price,
            IsAvailable  = b.IsAvailable,
            AuthorName   = b.Author?.FullName ?? "Unknown",
            CategoryName = b.Category?.Name ?? "Unknown"
        });

        // Pass sort options to the view so it can render the sort selector
        ViewBag.CurrentSort       = sort;
        ViewBag.CurrentSearch     = search;
        ViewBag.CurrentCategoryId = categoryId;
        ViewBag.SortOptions       = _sorter.AvailableStrategies
            .Select(s => new SelectListItem(s.DisplayName, s.Key, s.Key == sort));

        return View(viewModels);
    }

    // =========================================================================
    // GET /Books/Details/5
    // =========================================================================
    public async Task<IActionResult> Details(int id)
    {
        var book = await _library.GetBookAsync(id);
        if (book is null)
            return NotFound(); // returns HTTP 404

        var vm = new BookViewModel
        {
            Id           = book.Id,
            Title        = book.Title,
            ISBN         = book.ISBN,
            PublishedYear= book.PublishedYear,
            PageCount    = book.PageCount,
            Price        = book.Price,
            IsAvailable  = book.IsAvailable,
            Description  = book.Description,
            AuthorName   = book.Author?.FullName ?? "Unknown",
            CategoryName = book.Category?.Name ?? "Unknown"
        };

        return View(vm);
    }

    // =========================================================================
    // GET /Books/Create — Show the empty create form
    // =========================================================================
    public async Task<IActionResult> Create()
    {
        // Populate dropdown data before showing the form.
        // SelectList wraps a collection into items for <select asp-items="...">
        var vm = new BookFormViewModel
        {
            Authors    = await GetAuthorSelectListAsync(),
            Categories = await GetCategorySelectListAsync()
        };
        return View(vm);
    }

    // =========================================================================
    // POST /Books/Create — Process the submitted form
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken] // CSRF protection — verifies the hidden __RequestVerificationToken field
    public async Task<IActionResult> Create(
        [Bind("Title,ISBN,PublishedYear,PageCount,Price,IsAvailable,Description,AuthorId,CategoryId")]
        BookFormViewModel vm)
    // [Bind]: OVER-POSTING PROTECTION — only the listed properties are bound from the form.
    // Without [Bind], a malicious user could POST "IsAdmin=true" or "Id=999" and it would
    // be silently set on the model. [Bind] is a whitelist of safe, expected properties.
    {
        // ModelState.IsValid: runs ALL Data Annotation validators on the submitted vm.
        // [Required], [MaxLength], [Range] etc. from BookFormViewModel all run here.
        // If anything fails, IsValid is false and Errors contains the messages.
        if (!ModelState.IsValid)
        {
            // IMPORTANT: re-populate dropdowns before returning the view.
            // SelectList data is not round-tripped in the form POST — it must be
            // rebuilt every time the form is re-shown with errors.
            vm.Authors    = await GetAuthorSelectListAsync();
            vm.Categories = await GetCategorySelectListAsync();
            return View(vm); // re-show the form with validation errors displayed
        }

        try
        {
            var book = new Book
            {
                Title         = vm.Title,
                ISBN          = vm.ISBN,
                PublishedYear = vm.PublishedYear,
                PageCount     = vm.PageCount,
                Price         = vm.Price,
                IsAvailable   = vm.IsAvailable,
                Description   = vm.Description,
                AuthorId      = vm.AuthorId,
                CategoryId    = vm.CategoryId
            };

            await _library.CreateBookAsync(book);

            // PRG — POST-REDIRECT-GET PATTERN:
            // After a successful POST, we REDIRECT to the list page rather than
            // returning a View directly. This prevents the browser "re-submit form?"
            // dialog when the user refreshes the page.
            //
            // TempData: used to pass the success message ACROSS the redirect.
            // TempData is stored in a session cookie and deleted after ONE read.
            // If we used ViewBag here instead, the message would be lost on redirect.
            TempData["SuccessMessage"] = $"'{book.Title}' was added to the library.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation from LibraryService (e.g., author not found)
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Authors    = await GetAuthorSelectListAsync();
            vm.Categories = await GetCategorySelectListAsync();
            return View(vm);
        }
    }

    // =========================================================================
    // GET /Books/Edit/5 — Show the pre-filled edit form
    // =========================================================================
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _library.GetBookAsync(id);
        if (book is null)
            return NotFound();

        var vm = new BookFormViewModel
        {
            Id            = book.Id,
            Title         = book.Title,
            ISBN          = book.ISBN,
            PublishedYear = book.PublishedYear,
            PageCount     = book.PageCount,
            Price         = book.Price,
            IsAvailable   = book.IsAvailable,
            Description   = book.Description,
            AuthorId      = book.AuthorId,
            CategoryId    = book.CategoryId,
            Authors       = await GetAuthorSelectListAsync(book.AuthorId),
            Categories    = await GetCategorySelectListAsync(book.CategoryId)
        };

        return View(vm);
    }

    // =========================================================================
    // POST /Books/Edit/5 — Process the edit submission
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,Title,ISBN,PublishedYear,PageCount,Price,IsAvailable,Description,AuthorId,CategoryId")]
        BookFormViewModel vm)
    {
        // Ensure the route ID matches the form's hidden Id field — prevents tampered requests.
        if (id != vm.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            vm.Authors    = await GetAuthorSelectListAsync(vm.AuthorId);
            vm.Categories = await GetCategorySelectListAsync(vm.CategoryId);
            return View(vm);
        }

        var book = new Book
        {
            Id            = vm.Id,
            Title         = vm.Title,
            ISBN          = vm.ISBN,
            PublishedYear = vm.PublishedYear,
            PageCount     = vm.PageCount,
            Price         = vm.Price,
            IsAvailable   = vm.IsAvailable,
            Description   = vm.Description,
            AuthorId      = vm.AuthorId,
            CategoryId    = vm.CategoryId
        };

        var updated = await _library.UpdateBookAsync(book);
        if (!updated)
            return NotFound();

        TempData["SuccessMessage"] = $"'{book.Title}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // POST /Books/Delete/5
    // =========================================================================
    // Delete is POST-only (not GET) to prevent accidental deletion via
    // crawlers, link prefetching, or simple URL visits.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _library.DeleteBookAsync(id);
        TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
            deleted ? "Book deleted." : "Book not found.";
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // PRIVATE HELPERS — reusable SelectList builders
    // =========================================================================

    private async Task<IEnumerable<SelectListItem>> GetAuthorSelectListAsync(int selectedId = 0)
    {
        var authors = await _library.GetAllAuthorsAsync();
        return authors.Select(a => new SelectListItem(
            text: a.FullName,
            value: a.Id.ToString(),
            selected: a.Id == selectedId));
    }

    private async Task<IEnumerable<SelectListItem>> GetCategorySelectListAsync(int selectedId = 0)
    {
        var categories = await _library.GetAllCategoriesAsync();
        return categories.Select(c => new SelectListItem(
            text: c.Name,
            value: c.Id.ToString(),
            selected: c.Id == selectedId));
    }
}
