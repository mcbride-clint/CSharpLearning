using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Controllers;

public class AuthorsController : Controller
{
    private readonly ILibraryService _library;

    public AuthorsController(ILibraryService library)
    {
        _library = library;
    }

    public async Task<IActionResult> Index()
    {
        var authors = await _library.GetAllAuthorsAsync();
        var viewModels = authors.Select(a => new AuthorViewModel
        {
            Id        = a.Id,
            FirstName = a.FirstName,
            LastName  = a.LastName,
            BirthDate = a.BirthDate,
            Biography = a.Biography,
            BookCount = a.Books.Count
        });
        return View(viewModels);
    }

    public async Task<IActionResult> Details(int id)
    {
        var author = await _library.GetAuthorAsync(id);
        if (author is null) return NotFound();

        var vm = new AuthorViewModel
        {
            Id        = author.Id,
            FirstName = author.FirstName,
            LastName  = author.LastName,
            BirthDate = author.BirthDate,
            Biography = author.Biography,
            BookCount = author.Books.Count
        };
        return View(vm);
    }

    public IActionResult Create() => View(new AuthorFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("FirstName,LastName,BirthDate,Biography")] AuthorFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var author = new Author
        {
            FirstName = vm.FirstName,
            LastName  = vm.LastName,
            BirthDate = vm.BirthDate,
            Biography = vm.Biography
        };

        await _library.CreateAuthorAsync(author);
        TempData["SuccessMessage"] = $"{author.FullName} was added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var author = await _library.GetAuthorAsync(id);
        if (author is null) return NotFound();

        var vm = new AuthorFormViewModel
        {
            Id        = author.Id,
            FirstName = author.FirstName,
            LastName  = author.LastName,
            BirthDate = author.BirthDate,
            Biography = author.Biography
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,FirstName,LastName,BirthDate,Biography")] AuthorFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var author = new Author
        {
            Id        = vm.Id,
            FirstName = vm.FirstName,
            LastName  = vm.LastName,
            BirthDate = vm.BirthDate,
            Biography = vm.Biography
        };

        var updated = await _library.UpdateAuthorAsync(author);
        if (!updated) return NotFound();

        TempData["SuccessMessage"] = $"{author.FullName} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // LibraryService enforces the business rule: returns false if author has books.
        // The controller just decides what HTTP response to send based on the result.
        var deleted = await _library.DeleteAuthorAsync(id);
        if (!deleted)
            TempData["ErrorMessage"] = "Cannot delete this author because they still have books in the library.";
        else
            TempData["SuccessMessage"] = "Author deleted.";

        return RedirectToAction(nameof(Index));
    }
}
