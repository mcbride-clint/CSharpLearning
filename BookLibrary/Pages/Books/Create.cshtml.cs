using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookLibrary.Pages.Books;

// =============================================================================
// RAZOR PAGES: Books/CreateModel (was BooksController.Create GET + POST)
// =============================================================================
// MVC had two separate action methods sharing the same name (method overloading):
//   [HttpGet]  public IActionResult Create() { ... }
//   [HttpPost] public IActionResult Create(BookFormViewModel vm) { ... }
//
// Razor Pages uses named handlers instead:
//   public async Task OnGetAsync()             → handles GET /Books/Create
//   public async Task<IActionResult> OnPostAsync() → handles POST /Books/Create
//
// [BIND PROPERTY]:
//   MVC: [Bind("Title,ISBN,...")] BookFormViewModel vm  — whitelists form fields
//        on the action PARAMETER to prevent over-posting.
//
//   Razor Pages: [BindProperty] on the PROPERTY — opts in specific properties
//        for form binding. Properties without [BindProperty] are never bound
//        from a form POST, achieving the same over-posting protection.
//
//   BookFormViewModel contains only form-relevant properties (no IsAdmin, etc.),
//   so binding the whole ViewModel is safe. The Authors and Categories
//   SelectList properties are NOT in the form, so they'll be null after POST —
//   that's expected; we repopulate them when re-showing the form on error.
// =============================================================================
public class CreateModel : PageModel
{
    private readonly ILibraryService _library;

    public CreateModel(ILibraryService library)
    {
        _library = library;
    }

    [BindProperty]
    public BookFormViewModel Book { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Populate dropdown data before showing the empty form.
        Book.Authors    = await GetAuthorSelectListAsync();
        Book.Categories = await GetCategorySelectListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // ModelState.IsValid runs all Data Annotation validators on Book.
        // [Required], [Range], [MaxLength] etc. all evaluate here.
        if (!ModelState.IsValid)
        {
            // Re-populate dropdowns — SelectList data is not round-tripped in
            // the form POST (only the selected ID is submitted, not all options).
            Book.Authors    = await GetAuthorSelectListAsync(Book.AuthorId);
            Book.Categories = await GetCategorySelectListAsync(Book.CategoryId);
            return Page(); // re-show form with validation errors
        }

        try
        {
            var book = new Book
            {
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

            await _library.CreateBookAsync(book);

            TempData["SuccessMessage"] = $"'{book.Title}' was added to the library.";
            return RedirectToPage("./Index"); // PRG: redirect after successful POST
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Book.Authors    = await GetAuthorSelectListAsync(Book.AuthorId);
            Book.Categories = await GetCategorySelectListAsync(Book.CategoryId);
            return Page();
        }
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
