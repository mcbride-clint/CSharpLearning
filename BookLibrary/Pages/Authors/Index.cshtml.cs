using BookLibrary.Interfaces;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Authors;

public class IndexModel : PageModel
{
    private readonly ILibraryService _library;

    public IndexModel(ILibraryService library)
    {
        _library = library;
    }

    public IEnumerable<AuthorViewModel> Authors { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var authors = await _library.GetAllAuthorsAsync();
        Authors = authors.Select(a => new AuthorViewModel
        {
            Id        = a.Id,
            FirstName = a.FirstName,
            LastName  = a.LastName,
            BirthDate = a.BirthDate,
            Biography = a.Biography,
            BookCount = a.Books.Count
        });
    }

    // Named handler: form uses asp-page-handler="Delete"
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // LibraryService enforces the business rule: returns false if author has books.
        var deleted = await _library.DeleteAuthorAsync(id);
        if (!deleted)
            TempData["ErrorMessage"] = "Cannot delete this author because they still have books in the library.";
        else
            TempData["SuccessMessage"] = "Author deleted.";

        return RedirectToPage("./Index");
    }
}
