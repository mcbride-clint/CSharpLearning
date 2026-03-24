using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Categories;

public class EditModel : PageModel
{
    private readonly ILibraryService _library;

    public EditModel(ILibraryService library)
    {
        _library = library;
    }

    [BindProperty]
    public CategoryFormViewModel Category { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _library.GetCategoryAsync(id);
        if (category is null)
            return NotFound();

        Category = new CategoryFormViewModel
        {
            Id          = category.Id,
            Name        = category.Name,
            Description = category.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Category.Id = id;

        if (!ModelState.IsValid)
            return Page();

        var category = new Category { Id = id, Name = Category.Name, Description = Category.Description };
        var updated = await _library.UpdateCategoryAsync(category);
        if (!updated)
            return NotFound();

        TempData["SuccessMessage"] = $"Category '{category.Name}' was updated.";
        return RedirectToPage("./Index");
    }
}
