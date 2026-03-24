using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Categories;

public class CreateModel : PageModel
{
    private readonly ILibraryService _library;

    public CreateModel(ILibraryService library)
    {
        _library = library;
    }

    [BindProperty]
    public CategoryFormViewModel Category { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var category = new Category { Name = Category.Name, Description = Category.Description };
        await _library.CreateCategoryAsync(category);
        TempData["SuccessMessage"] = $"Category '{category.Name}' was added.";
        return RedirectToPage("./Index");
    }
}
