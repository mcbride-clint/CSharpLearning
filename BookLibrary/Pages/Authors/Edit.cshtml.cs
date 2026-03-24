using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Authors;

public class EditModel : PageModel
{
    private readonly ILibraryService _library;

    public EditModel(ILibraryService library)
    {
        _library = library;
    }

    [BindProperty]
    public AuthorFormViewModel Author { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var author = await _library.GetAuthorAsync(id);
        if (author is null)
            return NotFound();

        Author = new AuthorFormViewModel
        {
            Id        = author.Id,
            FirstName = author.FirstName,
            LastName  = author.LastName,
            BirthDate = author.BirthDate,
            Biography = author.Biography
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Author.Id = id;

        if (!ModelState.IsValid)
            return Page();

        var author = new Author
        {
            Id        = id,
            FirstName = Author.FirstName,
            LastName  = Author.LastName,
            BirthDate = Author.BirthDate,
            Biography = Author.Biography
        };

        var updated = await _library.UpdateAuthorAsync(author);
        if (!updated)
            return NotFound();

        TempData["SuccessMessage"] = $"{author.FullName} was updated.";
        return RedirectToPage("./Index");
    }
}
