using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookLibrary.Pages.Books;

// =============================================================================
// RAZOR PAGES: Books/EditModel (was BooksController.Edit GET + POST)
// =============================================================================
// ROUTE: @page "{id:int}" in Edit.cshtml makes id part of the URL path.
//   GET  /Books/Edit/5  → OnGetAsync(5)   — load and display the form
//   POST /Books/Edit/5  → OnPostAsync(5)  — validate and save changes
//
// The id comes from the route, not the hidden form field, so we don't need
// <input type="hidden" asp-for="Book.Id" /> in the view. We set Book.Id = id
// from the route parameter to ensure the route id drives the save, not a
// potentially tampered hidden field.
// =============================================================================
public class EditModel : PageModel
{
    private readonly ILibraryService _library;

    public EditModel(ILibraryService library)
    {
        _library = library;
    }

    [BindProperty]
    public BookFormViewModel Book { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var book = await _library.GetBookAsync(id);
        if (book is null)
            return NotFound();

        Book = new BookFormViewModel
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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Use the route id as the authoritative source — prevents tampered form submissions.
        Book.Id = id;

        if (!ModelState.IsValid)
        {
            Book.Authors    = await GetAuthorSelectListAsync(Book.AuthorId);
            Book.Categories = await GetCategorySelectListAsync(Book.CategoryId);
            return Page();
        }

        var book = new Book
        {
            Id            = id,
            Title         = Book.Title,
            ISBN          = Book.ISBN,
            PublishedYear = Book.PublishedYear,
            PageCount     = Book.PageCount,
            Price         = Book.Price,
            IsAvailable   = Book.IsAvailable,
            Description   = Book.Description,
            AuthorId      = Book.AuthorId,
            CategoryId    = Book.CategoryId
        };

        var updated = await _library.UpdateBookAsync(book);
        if (!updated)
            return NotFound();

        TempData["SuccessMessage"] = $"'{book.Title}' was updated.";
        return RedirectToPage("./Index");
    }

    private async Task<IEnumerable<SelectListItem>> GetAuthorSelectListAsync(int selectedId = 0)
    {
        var authors = await _library.GetAllAuthorsAsync();
        return authors.Select(a => new SelectListItem(a.FullName, a.Id.ToString(), a.Id == selectedId));
    }

    private async Task<IEnumerable<SelectListItem>> GetCategorySelectListAsync(int selectedId = 0)
    {
        var categories = await _library.GetAllCategoriesAsync();
        return categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == selectedId));
    }
}
