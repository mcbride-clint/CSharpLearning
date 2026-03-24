using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Authors;

public class CreateModel : PageModel
{
    private readonly ILibraryService _library;

    public CreateModel(ILibraryService library)
    {
        _library = library;
    }

    [BindProperty]
    public AuthorFormViewModel Author { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var author = new Author
        {
            FirstName = Author.FirstName,
            LastName  = Author.LastName,
            BirthDate = Author.BirthDate,
            Biography = Author.Biography
        };

        await _library.CreateAuthorAsync(author);
        TempData["SuccessMessage"] = $"{author.FullName} was added.";
        return RedirectToPage("./Index");
    }
}
