# Book Library — ASP.NET Core Learning Project

A hands-on reference project built with **ASP.NET Core 8**, combining a Razor Pages web app and a REST API in a single solution. Every file is written as a teaching artifact: the code works, and the comments explain *why* each pattern exists — not just *what* it does.

**Tech stack:** .NET 8 · C# 12 · EF Core 8 · SQLite · Bootstrap 5 · Swagger/OpenAPI

---

## Getting Started

```bash
# 1. Restore and build
dotnet build

# 2. Apply migrations and seed the database (creates library.db)
dotnet ef database update

# 3. Run
dotnet run
```

Open `http://localhost:<port>` in your browser (the port is printed to the console on startup).

The database is pre-seeded with 3 authors, 5 categories, and 3 books so you have something to explore immediately.

---

## Project Structure

```
BookLibrary/
├── Program.cs                  ← START HERE — DI registration + middleware pipeline
├── appsettings.json            ← Configuration (connection string, log levels)
│
├── Models/                     ← EF Core domain entities (map to database tables)
│   ├── Book.cs
│   ├── Author.cs
│   └── Category.cs
│
├── Data/
│   ├── LibraryDbContext.cs     ← EF Core hub (change tracker, Fluent API, seed data)
│   └── Migrations/             ← Auto-generated schema snapshots
│
├── Interfaces/                 ← Contracts for DI (program to abstractions)
├── Repositories/               ← EF Core data access (Scoped lifetime)
├── Services/                   ← Business logic (Scoped, Singleton, Transient demos)
├── Middleware/                 ← Custom request pipeline components
│
├── Pages/                      ← Razor Pages (.cshtml + .cshtml.cs pairs)
│   ├── _ViewImports.cshtml     ← Global using/namespace/tag helper declarations
│   ├── _ViewStart.cshtml       ← Sets default layout for all pages
│   ├── Shared/
│   │   ├── _Layout.cshtml      ← Master page template
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Index.cshtml/.cs        ← Dashboard (was HomeController.Index)
│   ├── DiDemo.cshtml/.cs       ← DI lifetime demo (was HomeController.DiDemo)
│   ├── Books/                  ← Book CRUD pages
│   ├── Authors/                ← Author CRUD pages
│   └── Categories/             ← Category CRUD pages
│
├── ApiControllers/             ← REST API controllers (JSON, HTTP verbs)
├── ViewModels/                 ← Shared shapes for Razor Pages and API
├── DTOs/                       ← Shapes for API request/response JSON
│
└── Patterns/
    ├── GuardClauses/           ← Guard.cs — early validation helpers
    ├── Strategy/               ← Sort algorithms (Strategy pattern)
    ├── Builder/                ← BookSearchQueryBuilder (Builder pattern)
    ├── Decorator/              ← CachedBookRepository (Decorator pattern)
    └── Factory/                ← BookReportFormatterFactory (Factory pattern)
```

---

## Concepts Guide

### 1. Dependency Injection (DI)

**Start here:** [`Program.cs`](Program.cs) — Section 4 "Three Lifetimes"

ASP.NET Core has a built-in DI container. You register components once; the framework creates them and injects them wherever needed. You never call `new MyService()` in application code.

#### The Three Lifetimes

| Lifetime | Created | Disposed | Use when |
|----------|---------|----------|----------|
| **Singleton** | Once at app start | App shuts down | Stateless or thread-safe shared state |
| **Scoped** | Once per HTTP request | Request ends | Per-request state (e.g., DbContext) |
| **Transient** | Every injection | End of scope | Lightweight, stateless, never shared |

**Where to see them:**

| Lifetime | Class | Why it's correct |
|----------|-------|-----------------|
| Singleton | [`StatisticsService`](Services/StatisticsService.cs) | Safely increments a counter with `Interlocked` across threads |
| Scoped | [`LibraryService`](Services/LibraryService.cs), repositories | Must share the same `DbContext` instance within one request |
| Transient | [`OperationIdService`](Services/OperationIdService.cs) | Generates a fresh `Guid` on every injection to prove it's a new instance |

**Live demo:** Visit `/DiDemo` — the page logs two `IOperationIdService` GUIDs (one from the constructor, one from `[FromServices]`) and shows they are different, proving the Transient lifetime creates a new instance per injection.

> **Captive Dependency Warning:** Never inject a shorter-lived service into a longer-lived one. Injecting a Scoped service into a Singleton's constructor is the most common mistake — ASP.NET Core will throw `InvalidOperationException` at startup in Development mode to catch this.

---

### 2. Entity Framework Core

**Start here:** [`Data/LibraryDbContext.cs`](Data/LibraryDbContext.cs)

EF Core is an Object-Relational Mapper (ORM). It translates C# classes into database tables and LINQ queries into SQL.

#### Key Concepts

**DbContext** is the heart of EF Core. It:
- Tracks entity changes (the *change tracker*)
- Translates LINQ to SQL
- Manages database connections
- Coordinates transactions via `SaveChangesAsync()`

```csharp
// Nothing is written to the DB until SaveChangesAsync() is called
_db.Books.Add(book);
await _db.SaveChangesAsync(); // ← this is the "commit"
```

**DbSet\<T\>** represents a database table. Querying it builds a SQL statement that runs *in the database*, not in C#:

```csharp
// This LINQ expression is translated to SQL — it does NOT load all books into memory
var books = await _db.Books
    .Where(b => b.Title.Contains("Dune"))
    .ToListAsync();
```

#### Eager Loading vs N+1

See: [`Repositories/BookRepository.cs`](Repositories/BookRepository.cs)

Without `.Include()`, navigation properties (like `book.Author`) are `null`. Without eager loading you'd make one query per book to fetch its author — the N+1 problem:

```csharp
// BAD — N+1: one query for books, then one per book for its author
var books = await _db.Books.ToListAsync();
foreach (var book in books)
    Console.WriteLine(book.Author.FullName); // each access hits the DB again

// GOOD — one JOIN query for everything
var books = await _db.Books
    .Include(b => b.Author)
    .Include(b => b.Category)
    .ToListAsync();
```

> **Tip:** Set `"Microsoft.EntityFrameworkCore.Database.Command": "Information"` in `appsettings.json` (already done) to see every SQL query printed to the console. This makes the difference between a JOIN and N+1 queries immediately visible.

#### AsNoTracking

```csharp
// Read-only query — skip change tracking for better performance
var books = await _db.Books.AsNoTracking().ToListAsync();
```

Use `AsNoTracking()` on list/search queries where you don't intend to update the entities. Do NOT use it if you plan to call `SaveChanges` on those same entities.

#### Data Annotations vs Fluent API

See: [`Models/Book.cs`](Models/Book.cs) for Data Annotations, [`Data/LibraryDbContext.cs`](Data/LibraryDbContext.cs) for Fluent API.

Both configure the EF Core schema. Fluent API wins if both conflict.

```csharp
// Data Annotation (on the model class)
[Required, MaxLength(300)]
public string Title { get; set; }

// Fluent API equivalent (in OnModelCreating)
entity.Property(b => b.Title).IsRequired().HasMaxLength(300);
```

#### Migrations

```bash
# Create a migration snapshot after changing a model
dotnet ef migrations add AddBookRating

# Apply pending migrations to the database
dotnet ef database update

# See the SQL that would run (without applying it)
dotnet ef migrations script

# Roll back one migration
dotnet ef database update PreviousMigrationName
```

Migrations are C# classes with `Up()` and `Down()` methods. EF Core tracks which have been applied in the `__EFMigrationsHistory` table.

---

### 3. Middleware & The Request Pipeline

**Start here:** [`Middleware/RequestLoggingMiddleware.cs`](Middleware/RequestLoggingMiddleware.cs), [`Middleware/GlobalExceptionMiddleware.cs`](Middleware/GlobalExceptionMiddleware.cs), and the pipeline section in [`Program.cs`](Program.cs).

Middleware components form a pipeline. Each component can do work *before* and *after* the next component runs — like Russian nesting dolls:

```
Request:  Exception → Logging → StaticFiles → Routing → Page
Response: Exception ← Logging ← StaticFiles ← Routing ← Page
```

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // BEFORE — runs on the way IN
    var sw = Stopwatch.StartNew();

    await _next(context); // pass to next middleware/page

    // AFTER — runs on the way OUT
    sw.Stop();
    _logger.LogInformation("Completed in {ms}ms", sw.ElapsedMilliseconds);
}
```

**Order is critical.** `GlobalExceptionMiddleware` is registered first so it wraps everything else — any exception from any downstream component is caught in one place.

#### Injecting Scoped Services into Middleware

Middleware instances are Singletons. You **cannot** inject Scoped services (like `DbContext`) via the constructor — doing so creates a captive dependency. Instead, declare them as `InvokeAsync` parameters:

```csharp
// WRONG — DbContext captured by Singleton middleware
public MyMiddleware(RequestDelegate next, LibraryDbContext db) { }

// CORRECT — DI resolves it fresh from the request scope each call
public async Task InvokeAsync(HttpContext context, IStatisticsService stats)
{
    stats.RecordRequest(); // stats is Singleton here, safe in constructor too
    await _next(context);
}
```

---

### 4. Razor Pages

**Start here:** [`Pages/Books/Create.cshtml.cs`](Pages/Books/Create.cshtml.cs) and [`Pages/Books/Create.cshtml`](Pages/Books/Create.cshtml)

Razor Pages organise each URL as a self-contained pair of files:
- **`Page.cshtml`** — the HTML template (the view)
- **`Page.cshtml.cs`** — the `PageModel` class (the controller + view model combined)

#### File-System Routing

Routes come from where the file lives in the `Pages/` folder — no route table needed:

```
Pages/Index.cshtml          → /
Pages/Books/Index.cshtml    → /Books or /Books/Index
Pages/Books/Details.cshtml  → /Books/Details/{id}  (when @page "{id:int}")
Pages/Books/Edit.cshtml     → /Books/Edit/{id}
```

Compare to MVC's conventional route table:
```csharp
// MVC — explicit route table
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

// Razor Pages — no route table; files are the routes
app.MapRazorPages();
```

#### Handler Methods

MVC action methods are replaced by named handler methods based on the HTTP verb:

```csharp
// MVC Controller
[HttpGet]  public IActionResult Edit(int id) { ... }
[HttpPost] public IActionResult Edit(int id, BookFormViewModel vm) { ... }

// Razor Pages PageModel — same logic, different naming convention
public async Task<IActionResult> OnGetAsync(int id) { ... }
public async Task<IActionResult> OnPostAsync(int id) { ... }
```

#### [BindProperty] — Replacing [Bind(...)]

MVC used `[Bind("Title,AuthorId,...")]` on an action parameter to whitelist safe form fields (over-posting protection). Razor Pages uses `[BindProperty]` on a property — only properties marked with it are bound from a POST:

```csharp
// MVC — whitelist on the parameter
public IActionResult Create([Bind("Title,AuthorId,CategoryId")] BookFormViewModel vm) { }

// Razor Pages — opt-in on the property (all other properties on PageModel are never bound)
[BindProperty]
public BookFormViewModel Book { get; set; } = new();
```

In the view, field names now reflect the property path on the PageModel:

```html
<!-- MVC view: @model BookFormViewModel — asp-for maps to the model directly -->
<input asp-for="Title" />           <!-- generates name="Title" -->

<!-- Razor Pages view: model IS the PageModel — use the full property path -->
<input asp-for="Book.Title" />      <!-- generates name="Book.Title" -->
```

#### Named POST Handlers

One page can handle multiple POST actions via named handlers:

```csharp
// Handler method named OnPost + "Delete" + Async
public async Task<IActionResult> OnPostDeleteAsync(int id) { ... }
```

```html
<!-- Form targets the named handler via asp-page-handler -->
<form asp-page-handler="Delete" asp-route-id="@book.Id" method="post">
    <button type="submit">Delete</button>
</form>
<!-- Posts to: /Books?handler=Delete&id=5 -->
```

#### Automatic Anti-Forgery

In MVC, every POST action needed `[ValidateAntiForgeryToken]`. Razor Pages validates the anti-forgery token automatically on all POST handlers — no attribute needed.

#### Post-Redirect-Get (PRG)

The same PRG pattern applies — use `RedirectToPage` instead of `RedirectToAction`:

```csharp
// MVC
TempData["SuccessMessage"] = "Book added!";
return RedirectToAction(nameof(Index));

// Razor Pages
TempData["SuccessMessage"] = "Book added!";
return RedirectToPage("./Index");  // relative to the current page's folder
```

#### Tag Helpers

Navigation tag helpers change from `asp-controller`/`asp-action` to `asp-page`:

```html
<!-- MVC -->
<a asp-controller="Books" asp-action="Edit" asp-route-id="@book.Id">Edit</a>

<!-- Razor Pages -->
<a asp-page="./Edit" asp-route-id="@book.Id">Edit</a>
```

Forms post back to the current page without any attributes:

```html
<!-- Razor Pages form — posts to the current page URL automatically -->
<!-- Anti-forgery token is injected by the tag helper automatically -->
<form method="post">
    <input asp-for="Book.Title" class="form-control" />
    <button type="submit">Save</button>
</form>
```

#### Page() vs View()

```csharp
return Page();    // Razor Pages — renders the .cshtml paired with this PageModel
return View(vm);  // MVC — renders a named view and passes a separate ViewModel
```

---

### 5. REST API Controllers

**Start here:** [`ApiControllers/BooksApiController.cs`](ApiControllers/BooksApiController.cs)

API controllers return JSON, not HTML. They differ from Razor Pages in several ways:

```csharp
[ApiController]           // ← key difference
[Route("api/books")]
public class BooksApiController : ControllerBase  // ControllerBase, not PageModel
```

**What `[ApiController]` gives you for free:**
- Automatic `400 Bad Request` when `ModelState` is invalid (no manual check needed)
- `[FromBody]` inferred for complex POST/PUT parameters
- RFC 7807 Problem Details JSON error format

#### HTTP Verb → Status Code Conventions

| Operation | Verb | Success code | Notes |
|-----------|------|-------------|-------|
| Read all | `GET` | `200 Ok` | |
| Read one | `GET` | `200 Ok` or `404 NotFound` | |
| Create | `POST` | `201 Created` | + `Location` header |
| Update | `PUT` | `204 NoContent` | No body needed |
| Delete | `DELETE` | `204 NoContent` | |
| Invalid input | Any | `400 BadRequest` | |

#### Model Binding Sources

```csharp
[HttpGet("{id:int}")]        // {id:int} = route constraint (only integers match)
public IActionResult Get(
    int id,                              // [FromRoute] inferred
    [FromQuery] string? sort,            // /api/books?sort=title
    [FromBody] CreateBookRequest req)    // JSON request body
```

---

### 6. Razor Syntax & Tag Helpers

**Start here:** [`Pages/Books/Create.cshtml`](Pages/Books/Create.cshtml), [`Pages/Shared/_Layout.cshtml`](Pages/Shared/_Layout.cshtml)

Razor is a template syntax that mixes C# and HTML. Code blocks use `@{ }`, expressions use `@`.

#### Tag Helpers

Tag Helpers replace raw HTML attributes with intelligent, model-aware syntax:

```html
<!-- Generates correct href from the page path -->
<a asp-page="/Books/Edit" asp-route-id="@book.Id">Edit</a>

<!-- Generates <form method="post"> + CSRF token automatically -->
<form method="post">

<!-- Generates <input type="text" name="Book.Title" data-val="..."> -->
<input asp-for="Book.Title" class="form-control" />

<!-- Renders [Display(Name="Book Title")] from the ViewModel annotation -->
<label asp-for="Book.Title" class="form-label"></label>

<!-- Shows validation error message for Book.Title -->
<span asp-validation-for="Book.Title" class="text-danger"></span>

<!-- Populates <select> options from a SelectList -->
<select asp-for="Book.AuthorId" asp-items="Model.Book.Authors"></select>
```

#### XSS Safety

Razor HTML-encodes all output by default. `@book.Title` renders `&lt;script&gt;` as text, not executable code. Use `@Html.Raw()` only when you explicitly trust the content.

#### Layout Inheritance

`_Layout.cshtml` is the page template. `@RenderBody()` is where each page's content is inserted. Pages can push content to named sections:

```html
<!-- In _Layout.cshtml -->
@await RenderSectionAsync("Scripts", required: false)

<!-- In a page -->
@section Scripts {
    <script src="~/lib/jquery-validation/jquery.validate.min.js"></script>
}
```

---

### 7. Design Patterns

#### Guard Clauses
**File:** [`Patterns/GuardClauses/Guard.cs`](Patterns/GuardClauses/Guard.cs)
**Used in:** [`Services/LibraryService.cs`](Services/LibraryService.cs)

Replace deeply nested `if` blocks with early-exit checks at the top of a method. The happy path stays at the base indentation level.

```csharp
// Without guard clauses — happy path buried 3 levels deep
if (book != null) {
    if (!string.IsNullOrEmpty(book.Title)) {
        if (book.AuthorId > 0) { /* actual work */ }
    }
}

// With guard clauses — happy path is immediately visible
Guard.AgainstNull(book, nameof(book));
Guard.AgainstNullOrEmpty(book.Title, nameof(book.Title));
Guard.AgainstNonPositive(book.AuthorId, nameof(book.AuthorId));
// actual work here, at top level
```

---

#### Strategy Pattern
**Files:** [`Patterns/Strategy/`](Patterns/Strategy/)
**Used in:** [`Pages/Books/Index.cshtml.cs`](Pages/Books/Index.cshtml.cs) — `?sort=` query param

Define a family of algorithms, encapsulate each one, and make them interchangeable. The caller doesn't care *how* sorting works — just *that* it does.

```csharp
// Without Strategy — grows with every new sort option
string sortBy switch { "title" => ..., "author" => ..., "year" => ... };

// With Strategy — add SortByPageCount by adding one class, changing nothing else
public interface IBookSortStrategy
{
    IEnumerable<Book> Sort(IEnumerable<Book> books);
}
```

Try it: `/Books?sort=author`, `/Books?sort=year`, `/Books?sort=price`

---

#### Builder Pattern
**Files:** [`Patterns/Builder/BookSearchQueryBuilder.cs`](Patterns/Builder/BookSearchQueryBuilder.cs)
**Used in:** [`Pages/Books/Index.cshtml.cs`](Pages/Books/Index.cshtml.cs), [`ApiControllers/BooksApiController.cs`](ApiControllers/BooksApiController.cs)

Construct complex objects step by step using a fluent interface. Avoids unreadable constructors with many optional parameters.

```csharp
// Without Builder — which argument is which?
new BookSearchQuery("dune", null, 3, 1960, 2000, 14.99m, true)

// With Builder — self-documenting, each filter is optional
var query = new BookSearchQueryBuilder()
    .WithTitle("dune")
    .InCategory(3)
    .PublishedBetween(1960, 2000)
    .AvailableOnly()
    .Build();
```

Try it: `/api/books/search?title=dune&yearFrom=1960`

---

#### Decorator Pattern
**File:** [`Patterns/Decorator/CachedBookRepository.cs`](Patterns/Decorator/CachedBookRepository.cs)
**Used in:** [`Program.cs`](Program.cs) — wraps `BookRepository` transparently

Add behavior (caching) to an object without modifying it and without subclassing. The rest of the app is unaware of the wrapper.

```csharp
// CachedBookRepository wraps BookRepository — same interface, transparent to callers
public class CachedBookRepository : IBookRepository
{
    public async Task<IEnumerable<Book>> GetAllWithDetailsAsync()
    {
        if (_cache.TryGetValue("books:all", out var cached)) return cached!; // cache hit
        var books = await _inner.GetAllWithDetailsAsync();                   // cache miss
        _cache.Set("books:all", books, TimeSpan.FromMinutes(5));
        return books;
    }
}

// DI wiring — callers requesting IBookRepository get the cached version
builder.Services.AddScoped<BookRepository>();                     // concrete
builder.Services.AddScoped<IBookRepository>(sp =>                 // interface → decorator
    new CachedBookRepository(sp.GetRequiredService<BookRepository>(), ...));
```

Visit `/Books` twice — the second request is served from cache (no SQL logged to console).

---

#### Factory Pattern
**Files:** [`Patterns/Factory/`](Patterns/Factory/)
**Used in:** [`ApiControllers/BooksApiController.cs`](ApiControllers/BooksApiController.cs) — `/api/books/export`

Centralize object creation logic. The caller requests a type by key; the factory decides which concrete class to return.

```csharp
// Without Factory — every place that exports must contain this switch
if (format == "csv") formatter = new CsvFormatter();
else if (format == "json") formatter = new JsonFormatter();

// With Factory — adding XML export = one new class + one registration in Program.cs
IBookReportFormatter formatter = _factory.GetFormatter(format);
string output = formatter.Format(books);
return File(bytes, formatter.ContentType, $"export.{formatter.FileExtension}");
```

Try it: `/api/books/export?format=csv` · `/api/books/export?format=json`

---

### 8. Logging

**Start here:** [`Middleware/RequestLoggingMiddleware.cs`](Middleware/RequestLoggingMiddleware.cs), [`Services/LibraryService.cs`](Services/LibraryService.cs)

`ILogger<T>` is built into ASP.NET Core — inject it anywhere, no extra registration needed.

```csharp
public class LibraryService
{
    private readonly ILogger<LibraryService> _logger;

    public LibraryService(ILogger<LibraryService> logger) => _logger = logger;

    public async Task DeleteAuthorAsync(int id)
    {
        // Structured logging: {AuthorId} becomes a searchable field in log aggregators
        _logger.LogWarning("Refused to delete Author {AuthorId} — still has books", id);
    }
}
```

Log levels in order: `Trace` < `Debug` < `Information` < `Warning` < `Error` < `Critical`

Configure which levels appear in `appsettings.json`. The project enables EF Core SQL logging at `Information` so you can see every query in the console while developing.

---

## API Reference

| Method | URL | Description |
|--------|-----|-------------|
| `GET` | `/api/books` | All books |
| `GET` | `/api/books/{id}` | One book by ID |
| `GET` | `/api/books/search` | Search (Builder pattern) — params: `title`, `authorLastName`, `categoryId`, `yearFrom`, `yearTo`, `maxPrice`, `availableOnly` |
| `POST` | `/api/books` | Create a book (JSON body) |
| `PUT` | `/api/books/{id}` | Update a book |
| `DELETE` | `/api/books/{id}` | Delete a book |
| `GET` | `/api/books/export?format=csv\|json` | Export (Factory pattern) |
| `GET` | `/api/authors` | All authors |
| `GET` | `/api/authors/{id}` | Author with their books |
| `DELETE` | `/api/authors/{id}` | Delete (fails if author has books) |
| `GET` | `/swagger` | Interactive API explorer |

---

## Concept → File Map

| Concept | File(s) |
|---------|---------|
| DI registration | [`Program.cs`](Program.cs) |
| Singleton lifetime | [`Services/StatisticsService.cs`](Services/StatisticsService.cs) |
| Scoped lifetime | [`Services/LibraryService.cs`](Services/LibraryService.cs), [`Repositories/BookRepository.cs`](Repositories/BookRepository.cs) |
| Transient lifetime | [`Services/OperationIdService.cs`](Services/OperationIdService.cs) |
| Live DI demo | [`Pages/DiDemo.cshtml.cs`](Pages/DiDemo.cshtml.cs) → `/DiDemo` |
| DbContext | [`Data/LibraryDbContext.cs`](Data/LibraryDbContext.cs) |
| Eager loading | [`Repositories/BookRepository.cs`](Repositories/BookRepository.cs) |
| Fluent API | [`Data/LibraryDbContext.cs`](Data/LibraryDbContext.cs) |
| Migrations | [`Migrations/`](Migrations/) |
| Middleware pipeline | [`Program.cs`](Program.cs) |
| Before/after pipeline | [`Middleware/RequestLoggingMiddleware.cs`](Middleware/RequestLoggingMiddleware.cs) |
| Global error handling | [`Middleware/GlobalExceptionMiddleware.cs`](Middleware/GlobalExceptionMiddleware.cs) |
| Razor Pages routing | [`Pages/Books/Index.cshtml`](Pages/Books/Index.cshtml) |
| PageModel / OnGet / OnPost | [`Pages/Books/Create.cshtml.cs`](Pages/Books/Create.cshtml.cs) |
| [BindProperty] | [`Pages/Books/Create.cshtml.cs`](Pages/Books/Create.cshtml.cs) |
| Named POST handlers | [`Pages/Books/Index.cshtml.cs`](Pages/Books/Index.cshtml.cs) → `OnPostDeleteAsync` |
| PRG pattern | [`Pages/Books/Create.cshtml.cs`](Pages/Books/Create.cshtml.cs) → `OnPostAsync` |
| Model validation | [`ViewModels/BookViewModel.cs`](ViewModels/BookViewModel.cs) |
| Tag Helpers (asp-page) | [`Pages/Books/Create.cshtml`](Pages/Books/Create.cshtml) |
| REST API | [`ApiControllers/BooksApiController.cs`](ApiControllers/BooksApiController.cs) |
| Guard Clauses | [`Patterns/GuardClauses/Guard.cs`](Patterns/GuardClauses/Guard.cs) |
| Strategy Pattern | [`Patterns/Strategy/`](Patterns/Strategy/) |
| Builder Pattern | [`Patterns/Builder/`](Patterns/Builder/) |
| Decorator Pattern | [`Patterns/Decorator/CachedBookRepository.cs`](Patterns/Decorator/CachedBookRepository.cs) |
| Factory Pattern | [`Patterns/Factory/`](Patterns/Factory/) |

---

## Suggested Learning Path

1. **Read `Program.cs` top-to-bottom** — it explains the entire application in one file
2. **`Models/Book.cs`** — understand the domain and Data Annotations
3. **`Data/LibraryDbContext.cs`** — understand how EF Core maps models to tables
4. **`Repositories/BookRepository.cs`** — see LINQ, `.Include()`, `AsNoTracking()`
5. **`Services/LibraryService.cs`** — see business rules and why services exist
6. **`Services/StatisticsService.cs`** — understand the Singleton lifetime
7. **`Middleware/RequestLoggingMiddleware.cs`** — understand the pipeline model
8. **`Pages/Books/Index.cshtml.cs`** — see the full Razor Pages CRUD cycle (list + delete)
9. **`Pages/Books/Create.cshtml.cs` + `Create.cshtml`** — see `[BindProperty]`, `OnGet/OnPost`, and the PRG pattern
10. **`ApiControllers/BooksApiController.cs`** — compare REST API controllers to Razor Pages
11. **`Patterns/` directory** — explore each pattern with its before/after examples
12. **Visit `/DiDemo`** in the browser — observe DI lifetimes live
13. **Run the app with the console open** — watch EF Core print SQL for every query
