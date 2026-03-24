using BookLibrary.Interfaces;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Categories;

public class IndexModel : PageModel
{
    private readonly ILibraryService _library;

    public IndexModel(ILibraryService library)
    {
        _library = library;
    }

    public IEnumerable<CategoryViewModel> Categories { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var categories = await _library.GetAllCategoriesAsync();
        Categories = categories.Select(c => new CategoryViewModel
        {
            Id          = c.Id,
            Name        = c.Name,
            Description = c.Description,
            BookCount   = c.Books.Count
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var deleted = await _library.DeleteCategoryAsync(id);
        if (!deleted)
            TempData["ErrorMessage"] = "Cannot delete this category — it still has books assigned to it.";
        else
            TempData["SuccessMessage"] = "Category deleted.";

        return RedirectToPage("./Index");
    }
}
