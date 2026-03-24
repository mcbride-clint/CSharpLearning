using BookLibrary.Interfaces;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Authors;

public class DetailsModel : PageModel
{
    private readonly ILibraryService _library;

    public DetailsModel(ILibraryService library)
    {
        _library = library;
    }

    public AuthorViewModel Author { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var author = await _library.GetAuthorAsync(id);
        if (author is null)
            return NotFound();

        Author = new AuthorViewModel
        {
            Id        = author.Id,
            FirstName = author.FirstName,
            LastName  = author.LastName,
            BirthDate = author.BirthDate,
            Biography = author.Biography,
            BookCount = author.Books.Count
        };

        return Page();
    }
}
