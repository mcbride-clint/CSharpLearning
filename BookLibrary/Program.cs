// =============================================================================
// PROGRAM.CS — THE APPLICATION ENTRY POINT AND COMPOSITION ROOT
// =============================================================================
// This file has two jobs:
//
//   PHASE 1: SERVICE REGISTRATION (builder phase)
//     "Here are all the components my application uses and how they're created."
//     The DI container reads these registrations and creates objects on demand.
//
//   PHASE 2: MIDDLEWARE PIPELINE (app phase)
//     "In what order should HTTP requests flow through the application?"
//     ORDER IS CRITICAL — middleware executes top-to-bottom on request,
//     bottom-to-top on response (the Russian doll model).
//
// TOP-LEVEL STATEMENTS (C# 9+):
//   There is no explicit 'class Program { static void Main(...) }' here.
//   C# generates it for you. The code at the top level IS Main().
// =============================================================================

using BookLibrary.Data;
using BookLibrary.Interfaces;
using BookLibrary.Middleware;
using BookLibrary.Patterns.Decorator;
using BookLibrary.Patterns.Factory;
using BookLibrary.Patterns.Strategy;
using BookLibrary.Repositories;
using BookLibrary.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// WebApplication.CreateBuilder:
//   - Configures appsettings.json + appsettings.{Environment}.json
//   - Sets up the built-in logging system
//   - Configures Kestrel as the web server
//   - Enables environment variables and command-line args as config sources

// =============================================================================
// SECTION 1: RAZOR PAGES + API CONTROLLERS
// =============================================================================
// AddRazorPages registers all the infrastructure for:
//   - Razor Pages (PageModel, OnGet/OnPost handlers, @page directive)
//   - Razor view rendering, Tag Helpers, Antiforgery (auto on all POSTs)
//   - Data Annotations validation
//   - ILogger<T> factory (any class can inject ILogger<T> for free)
//
// AddControllers registers MVC controller infrastructure WITHOUT Razor Views.
//   Used here for the REST API controllers in ApiControllers/ only.
//   API controllers use [ApiController] + attribute routing and don't render views.
//
// Razor Pages vs MVC Controllers:
//   MVC:          AddControllersWithViews() + MapControllerRoute("default", ...)
//   Razor Pages:  AddRazorPages()           + MapRazorPages()
//   API only:     AddControllers()          + MapControllers()
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // API controllers only (ApiControllers/)

// AddEndpointsApiExplorer + AddSwaggerGen: enables Swagger/OpenAPI documentation.
// Swagger generates an interactive UI at /swagger where you can test API endpoints.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Book Library API", Version = "v1" });
});

// =============================================================================
// SECTION 2: EF CORE + SQLITE
// =============================================================================
// AddDbContext<T>:
//   - Registers LibraryDbContext with the DI container
//   - LIFETIME: SCOPED by default (one instance per HTTP request)
//   - Reads the connection string from appsettings.json
//
// UseSqlite: configures EF Core to use SQLite as the database provider.
// Swap this for .UseSqlServer(...) or .UseNpgsql(...) to change databases —
// no other code needs to change. This is the Repository pattern's payoff.
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LibraryDb")));

// =============================================================================
// SECTION 3: IN-MEMORY CACHE (needed by the Decorator pattern)
// =============================================================================
// AddMemoryCache: registers IMemoryCache as a Singleton.
// IMemoryCache stores data in the process's memory — fast, but lost on restart.
// Used by CachedBookRepository (Decorator pattern).
builder.Services.AddMemoryCache();

// =============================================================================
// SECTION 4: DEPENDENCY INJECTION — THE THREE LIFETIMES
// =============================================================================
//
// ┌─────────────────┬──────────────────────────────────────────────────────────┐
// │ Lifetime        │ When to use                                              │
// ├─────────────────┼──────────────────────────────────────────────────────────┤
// │ SINGLETON       │ Stateless OR thread-safe shared state.                   │
// │ (app lifetime)  │ One instance shared by ALL requests, ALL threads.        │
// │                 │ Examples: config readers, caches, statistics services    │
// ├─────────────────┼──────────────────────────────────────────────────────────┤
// │ SCOPED          │ Per-HTTP-request state. Most common lifetime.            │
// │ (request scope) │ One instance per request; disposed at end of request.   │
// │                 │ Examples: DbContext, repositories, business services     │
// ├─────────────────┼──────────────────────────────────────────────────────────┤
// │ TRANSIENT       │ Lightweight, stateless. New instance every injection.    │
// │ (per injection) │ Even within the same request, each injection is new.    │
// │                 │ Examples: formatters, mappers, unique-ID generators      │
// └─────────────────┴──────────────────────────────────────────────────────────┘
//
// CAPTIVE DEPENDENCY RULE:
//   Never inject a shorter-lived service into a longer-lived one.
//   WRONG: Singleton → Scoped (Scoped captured, outlives its scope)
//   WRONG: Singleton → Transient (Transient becomes effectively Singleton)
//   OK:    Scoped    → Singleton (shorter injects into longer — safe)
//   OK:    Transient → Scoped   (shorter injects into longer — safe)
//   ASP.NET Core detects Singleton→Scoped violations at startup in Development.

// ---- SINGLETON services ----
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
// StatisticsService is thread-safe (uses Interlocked) — Singleton is correct.

// ---- SCOPED services ----
// BookRepository: register the CONCRETE type (not interface) so the Decorator can wrap it.
builder.Services.AddScoped<BookRepository>();
// CachedBookRepository (Decorator): wraps BookRepository with IMemoryCache.
// We register CachedBookRepository as IBookRepository — callers get the cached version.
builder.Services.AddScoped<IBookRepository>(sp =>
    new CachedBookRepository(
        sp.GetRequiredService<BookRepository>(),   // the real repository
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));
// Any class requesting IBookRepository now transparently gets the cached version.

builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ILibraryService, LibraryService>();

// ---- TRANSIENT services ----
builder.Services.AddTransient<IOperationIdService, OperationIdService>();
// New Guid generated on every injection — proves Transient creates new instances.
// Visit /Home/DiDemo to see this in action.

// =============================================================================
// SECTION 5: STRATEGY PATTERN — registering all sort strategies
// =============================================================================
// Register each sort strategy under the SAME interface.
// When BookSorter requests IEnumerable<IBookSortStrategy>, DI injects all four.
// This is "open collection injection" — add a new strategy by adding one line here.
builder.Services.AddSingleton<IBookSortStrategy, SortByTitle>();
builder.Services.AddSingleton<IBookSortStrategy, SortByAuthor>();
builder.Services.AddSingleton<IBookSortStrategy, SortByYear>();
builder.Services.AddSingleton<IBookSortStrategy, SortByPrice>();
builder.Services.AddSingleton<BookSorter>();
// BookSorter is Singleton — it holds only read-only strategy references (all Singletons).

// =============================================================================
// SECTION 6: FACTORY PATTERN — registering all report formatters
// =============================================================================
builder.Services.AddSingleton<IBookReportFormatter, CsvBookReportFormatter>();
builder.Services.AddSingleton<IBookReportFormatter, JsonBookReportFormatter>();
builder.Services.AddSingleton<BookReportFormatterFactory>();
// BookReportFormatterFactory receives IEnumerable<IBookReportFormatter> via DI —
// all registered formatters are automatically injected. Add XML by adding one line above.

// =============================================================================
// BUILD THE APP
// =============================================================================
var app = builder.Build();

// =============================================================================
// MIDDLEWARE PIPELINE — ORDER IS CRITICAL
// =============================================================================
// Requests flow TOP-TO-BOTTOM through this list.
// Responses flow BOTTOM-TO-TOP (reverse order through each middleware's "after" code).
//
// Visual representation (Russian dolls):
//
//  ┌─ GlobalExceptionMiddleware ──────────────────────────────┐
//  │  ┌─ RequestLoggingMiddleware ──────────────────────────┐ │
//  │  │  ┌─ UseHttpsRedirection ─────────────────────────┐ │ │
//  │  │  │  ┌─ UseStaticFiles ──────────────────────────┐ │ │ │
//  │  │  │  │  ┌─ UseRouting ────────────────────────┐  │ │ │ │
//  │  │  │  │  │  MapControllers / MapControllerRoute │  │ │ │ │
//  │  │  │  │  └────────────────────────────────────────┘  │ │ │ │
//  │  │  │  └────────────────────────────────────────────────┘ │ │ │
//  │  │  └──────────────────────────────────────────────────────┘ │ │
//  │  └────────────────────────────────────────────────────────────┘ │
//  └──────────────────────────────────────────────────────────────────┘

// 1. EXCEPTION HANDLER — must be OUTERMOST to catch errors from all downstream middleware.
//    If this were registered AFTER UseRouting, routing exceptions would not be caught.
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. HTTPS REDIRECT — redirect HTTP → HTTPS before serving anything.
//    In development, this is often skipped for convenience.
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();

// 3. STATIC FILES — serves CSS, JS, images from wwwroot/ directly.
//    Requests that match a static file never reach controllers.
//    Register BEFORE RequestLoggingMiddleware so we don't log static file hits.
app.UseStaticFiles();

// 4. REQUEST LOGGING — comes after static files so CSS/JS hits aren't logged.
//    Logs method, path, status code, and elapsed time for every dynamic request.
app.UseMiddleware<RequestLoggingMiddleware>();

// 5. ROUTING — matches the incoming URL to a controller/action/endpoint.
//    MUST come BEFORE UseAuthentication and UseAuthorization.
app.UseRouting();

// 6. AUTHENTICATION + AUTHORIZATION (commented out — not configured in this project).
//    When you add auth, these must appear AFTER UseRouting and BEFORE MapControllers.
// app.UseAuthentication();
// app.UseAuthorization();

// 7. SWAGGER UI — only in Development (don't expose API docs in production).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Book Library API v1");
        options.RoutePrefix = "swagger"; // accessible at /swagger
    });
}

// 8. MAP ENDPOINTS:
//    MapRazorPages: routes file-system-based Razor Pages (Pages/ folder).
//      Pages/Index.cshtml        → /
//      Pages/Books/Index.cshtml  → /Books or /Books/Index
//      Pages/Books/Edit.cshtml   → /Books/Edit/{id} (from @page "{id:int}")
//
//    MapControllers: routes API controllers via [Route("api/[controller]")] attributes.
//      No conventional route table needed — each API endpoint declares its own route.
app.MapRazorPages();
app.MapControllers(); // API controllers (ApiControllers/)

// =============================================================================
// SECTION 7: DATABASE INITIALIZATION
// =============================================================================
// Apply EF Core migrations and seed data on startup.
// We need a DI scope because LibraryDbContext is Scoped — it can't be resolved
// directly from the root ServiceProvider (which has no scope context).
//
// CreateScope(): creates a temporary scope so we can resolve Scoped services
// outside of an HTTP request context (e.g., at application startup).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

    // EnsureCreated: creates the database and applies the model if it doesn't exist.
    // For development this is fine. In production, prefer db.Database.Migrate()
    // which applies pending migrations incrementally.
    //
    // NOTE: EnsureCreated does NOT run migrations — use it OR migrations, not both.
    // For this learning project, EnsureCreated is simpler. Run 'dotnet ef database update'
    // separately if you want migration support.
    db.Database.EnsureCreated();
}

app.Run();
