using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Controllers;

public class CategoriesController : Controller
{
    private readonly ILibraryService _library;

    public CategoriesController(ILibraryService library)
    {
        _library = library;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _library.GetAllCategoriesAsync();
        var viewModels = categories.Select(c => new CategoryViewModel
        {
            Id        = c.Id,
            Name      = c.Name,
            Description = c.Description,
            BookCount = c.Books.Count
        });
        return View(viewModels);
    }

    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description")] CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var category = new Category { Name = vm.Name, Description = vm.Description };
        await _library.CreateCategoryAsync(category);
        TempData["SuccessMessage"] = $"Category '{category.Name}' was added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _library.GetCategoryAsync(id);
        if (category is null) return NotFound();

        var vm = new CategoryFormViewModel { Id = category.Id, Name = category.Name, Description = category.Description };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] CategoryFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var category = new Category { Id = vm.Id, Name = vm.Name, Description = vm.Description };
        var updated = await _library.UpdateCategoryAsync(category);
        if (!updated) return NotFound();

        TempData["SuccessMessage"] = $"Category '{category.Name}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _library.DeleteCategoryAsync(id);
        if (!deleted)
            TempData["ErrorMessage"] = "Cannot delete this category — it still has books assigned to it.";
        else
            TempData["SuccessMessage"] = "Category deleted.";

        return RedirectToAction(nameof(Index));
    }
}
