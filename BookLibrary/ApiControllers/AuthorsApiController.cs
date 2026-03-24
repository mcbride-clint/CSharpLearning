using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.ApiControllers;

[ApiController]
[Route("api/authors")]
public class AuthorsApiController : ControllerBase
{
    private readonly ILibraryService _library;

    public AuthorsApiController(ILibraryService library) => _library = library;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var authors = await _library.GetAllAuthorsAsync();
        return Ok(authors.Select(a => new
        {
            a.Id,
            a.FirstName,
            a.LastName,
            a.FullName,
            a.BirthDate,
            a.Biography,
            BookCount = a.Books.Count
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var author = await _library.GetAuthorAsync(id);
        if (author is null) return NotFound();

        return Ok(new
        {
            author.Id,
            author.FirstName,
            author.LastName,
            author.FullName,
            author.BirthDate,
            author.Biography,
            Books = author.Books.Select(b => new { b.Id, b.Title, b.PublishedYear })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Author author)
    {
        var created = await _library.CreateAuthorAsync(author);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Returns false if the author has books — business rule from LibraryService.
        var deleted = await _library.DeleteAuthorAsync(id);
        if (!deleted)
            return Conflict(new { error = "Cannot delete author who still has books in the library." });

        return NoContent();
    }
}
