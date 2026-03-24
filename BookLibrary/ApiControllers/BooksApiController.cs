using System.Text;
using BookLibrary.DTOs;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using BookLibrary.Patterns.Builder;
using BookLibrary.Patterns.Factory;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.ApiControllers;

// =============================================================================
// API CONTROLLER: BooksApiController
// =============================================================================
// DIFFERENCES FROM MVC CONTROLLERS:
//
//   [ApiController] attribute gives you for free:
//     - Automatic 400 Bad Request when ModelState is invalid (no manual check needed)
//     - [FromBody] inferred on complex POST/PUT parameters
//     - Problem Details (RFC 7807) JSON error format for 4xx/5xx responses
//     - Attribute routing REQUIRED (no conventional routing)
//
//   ControllerBase (NOT Controller):
//     - No Razor view support (no View(), ViewBag, TempData)
//     - Lighter weight — just HTTP request/response handling
//
// HTTP VERBS AND STATUS CODES — REST CONVENTIONS:
//   GET    → 200 Ok          (resource found) or 404 NotFound
//   POST   → 201 Created     (resource created, Location header set)
//   PUT    → 204 NoContent   (resource updated, no body needed) or 404
//   DELETE → 204 NoContent   (resource deleted) or 404
//   Any    → 400 BadRequest  (invalid input)
//   Any    → 500             (unhandled — caught by GlobalExceptionMiddleware)
//
// ROUTE BINDING — where does the value come from?
//   [FromRoute]  — URL segment:         GET /api/books/5  (id = 5)
//   [FromQuery]  — Query string:        GET /api/books?sort=title  (sort = "title")
//   [FromBody]   — Request JSON body:   POST /api/books  {"title":"Dune",...}
//   [FromHeader] — HTTP header value
//   [ApiController] auto-infers [FromBody] for complex types on POST/PUT.
// =============================================================================

// NOTE ON NAMING:
// [controller] strips "Controller" from the class name: BooksApiController → "BooksApi" → /api/booksapi
// To avoid this, we use an explicit route string "api/books" instead of the [controller] token.
// Alternatively, you could rename the class to BooksController, but that conflicts with the MVC controller.
[ApiController]
[Route("api/books")]
public class BooksApiController : ControllerBase
{
    private readonly ILibraryService _library;
    private readonly BookReportFormatterFactory _formatterFactory;

    public BooksApiController(ILibraryService library, BookReportFormatterFactory formatterFactory)
    {
        _library = library;
        _formatterFactory = formatterFactory;
    }

    // =========================================================================
    // GET /api/books
    // Returns all books as JSON.
    // =========================================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAll()
    {
        // ActionResult<T>: the return type that allows both returning T directly (200 OK)
        // AND returning other IActionResults like NotFound(), BadRequest().
        // If you return T, ASP.NET Core wraps it in a 200 Ok automatically.
        var books = await _library.GetAllBooksAsync();
        return Ok(books.Select(MapToDto));
    }

    // =========================================================================
    // GET /api/books/5
    // Returns one book by ID, or 404 if not found.
    // =========================================================================
    [HttpGet("{id:int}")]  // {id:int} is a route CONSTRAINT — only matches integers
    public async Task<ActionResult<BookDto>> GetById(int id)
    {
        var book = await _library.GetBookAsync(id);

        // Pattern: null → 404 NotFound. This is the REST convention for "this resource doesn't exist".
        if (book is null)
            return NotFound(new { error = $"Book with ID {id} was not found." });

        return Ok(MapToDto(book));
    }

    // =========================================================================
    // GET /api/books/search?title=dune&categoryId=3&yearFrom=1960
    // Demonstrates the Builder pattern via query string parameters.
    // =========================================================================
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BookDto>>> Search(
        [FromQuery] string? title,
        [FromQuery] string? authorLastName,
        [FromQuery] int? categoryId,
        [FromQuery] int? yearFrom,
        [FromQuery] int? yearTo,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool availableOnly = false)
    {
        // BUILDER PATTERN IN ACTION:
        // Build the search query fluently from whatever query parameters were provided.
        // The builder adds only the filters that have values — null params are skipped.
        var query = new BookSearchQueryBuilder()
            .WithTitle(title ?? string.Empty)
            .WithAuthorLastName(authorLastName ?? string.Empty)
            .PublishedBetween(yearFrom ?? 1, yearTo ?? DateTime.Now.Year + 1)
            .AvailableOnly()  // will be applied if availableOnly=true
            .Build();

        // Simpler approach for this action:
        var builder = new BookSearchQueryBuilder();
        if (!string.IsNullOrEmpty(title))           builder.WithTitle(title);
        if (!string.IsNullOrEmpty(authorLastName))  builder.WithAuthorLastName(authorLastName);
        if (categoryId.HasValue)                    builder.InCategory(categoryId.Value);
        if (yearFrom.HasValue)                      builder.PublishedFrom(yearFrom.Value);
        if (yearTo.HasValue)                        builder.PublishedBefore(yearTo.Value);
        if (maxPrice.HasValue)                      builder.WithMaxPrice(maxPrice.Value);
        if (availableOnly)                          builder.AvailableOnly();

        var books = await _library.SearchBooksAsync(builder.Build());
        return Ok(books.Select(MapToDto));
    }

    // =========================================================================
    // POST /api/books
    // Creates a new book. Body is JSON matching CreateBookRequest.
    // =========================================================================
    [HttpPost]
    public async Task<ActionResult<BookDto>> Create([FromBody] CreateBookRequest request)
    {
        // With [ApiController], ModelState is checked BEFORE this method runs.
        // If the JSON body is invalid (missing required fields, wrong types),
        // a 400 is returned automatically. You don't need 'if (!ModelState.IsValid)'.

        var book = new Book
        {
            Title         = request.Title,
            ISBN          = request.ISBN,
            PublishedYear = request.PublishedYear,
            PageCount     = request.PageCount,
            Price         = request.Price,
            IsAvailable   = request.IsAvailable,
            Description   = request.Description,
            AuthorId      = request.AuthorId,
            CategoryId    = request.CategoryId
        };

        var created = await _library.CreateBookAsync(book);
        var dto = MapToDto(created);

        // 201 Created: REST convention for successful resource creation.
        // The Location header points to where the new resource can be retrieved.
        // CreatedAtAction generates: Location: /api/books/42
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
    }

    // =========================================================================
    // PUT /api/books/5
    // Replaces a book entirely. Returns 204 No Content on success.
    // =========================================================================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBookRequest request)
    {
        var existing = await _library.GetBookAsync(id);
        if (existing is null)
            return NotFound(new { error = $"Book with ID {id} was not found." });

        // Apply partial update: only set fields that were provided (non-null).
        // This makes the API more forgiving — clients can send only changed fields.
        existing.Title         = request.Title         ?? existing.Title;
        existing.ISBN          = request.ISBN          ?? existing.ISBN;
        existing.PublishedYear = request.PublishedYear ?? existing.PublishedYear;
        existing.PageCount     = request.PageCount     ?? existing.PageCount;
        existing.Price         = request.Price         ?? existing.Price;
        existing.IsAvailable   = request.IsAvailable   ?? existing.IsAvailable;
        existing.Description   = request.Description   ?? existing.Description;
        existing.AuthorId      = request.AuthorId      ?? existing.AuthorId;
        existing.CategoryId    = request.CategoryId    ?? existing.CategoryId;

        await _library.UpdateBookAsync(existing);

        // 204 No Content: REST convention for "success, but nothing to return".
        // The client already knows what it sent — no need to echo it back.
        return NoContent();
    }

    // =========================================================================
    // DELETE /api/books/5
    // =========================================================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _library.DeleteBookAsync(id);
        if (!deleted)
            return NotFound(new { error = $"Book with ID {id} was not found." });

        return NoContent(); // 204 — success, no body
    }

    // =========================================================================
    // GET /api/books/export?format=csv
    // FACTORY PATTERN DEMONSTRATION
    // =========================================================================
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format = "json")
    {
        IBookReportFormatter formatter;
        try
        {
            // FACTORY PATTERN: the factory decides which formatter to return.
            // If format="csv" → CsvBookReportFormatter
            // If format="json" → JsonBookReportFormatter
            // Adding XML: add XmlBookReportFormatter + register it. This line doesn't change.
            formatter = _formatterFactory.GetFormatter(format);
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                supportedFormats = _formatterFactory.SupportedFormats
            });
        }

        var books = await _library.GetAllBooksAsync();
        var content = formatter.Format(books);
        var bytes = Encoding.UTF8.GetBytes(content);

        // File(): returns the bytes as a downloadable file with the correct MIME type.
        return File(bytes, formatter.ContentType, $"books-export.{formatter.FileExtension}");
    }

    // =========================================================================
    // PRIVATE: Domain model → DTO mapping
    // =========================================================================
    // Manual mapping: explicit, easy to understand, no magic.
    // In larger projects, AutoMapper or Mapster automate this.
    private static BookDto MapToDto(Book book) => new(
        Id:           book.Id,
        Title:        book.Title,
        ISBN:         book.ISBN,
        PublishedYear: book.PublishedYear,
        PageCount:    book.PageCount,
        Price:        book.Price,
        IsAvailable:  book.IsAvailable,
        Description:  book.Description,
        AuthorName:   book.Author?.FullName  ?? "Unknown",
        AuthorId:     book.AuthorId,
        CategoryName: book.Category?.Name    ?? "Unknown",
        CategoryId:   book.CategoryId
    );
}
