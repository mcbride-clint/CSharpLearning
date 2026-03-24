using System.ComponentModel.DataAnnotations;

namespace BookLibrary.Models;

// =============================================================================
// DOMAIN MODEL: Author
// =============================================================================
// A domain model represents a real-world concept in your application. EF Core
// maps this C# class to a database table called "Authors" automatically
// (by convention — it pluralises the class name).
//
// DATA ANNOTATIONS vs FLUENT API:
// You can configure EF Core in two ways:
//   1. Data Annotations (attributes on properties) — shown here
//   2. Fluent API (code in DbContext.OnModelCreating) — shown in LibraryDbContext
//
// Both achieve the same result. Annotations are convenient for simple rules;
// Fluent API is more powerful and keeps your models "clean" of framework concerns.
// Pick ONE style and be consistent.
// =============================================================================

public class Author
{
    // EF Core convention: a property named "Id" or "[TypeName]Id" is automatically
    // treated as the Primary Key and given an IDENTITY/AUTOINCREMENT constraint.
    // You never set this yourself — the database generates it on INSERT.
    public int Id { get; set; }

    // [Required] tells EF Core: NOT NULL in the database.
    // It also tells ASP.NET Core's model binder to fail validation if this is missing.
    // [MaxLength] maps to VARCHAR(100) — prevents unbounded data and adds a DB constraint.
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    // The '?' makes this a NULLABLE REFERENCE TYPE — it can legitimately be null.
    // EF Core maps this to a nullable column (no NOT NULL constraint).
    // Nullable reference types (enabled in .csproj via <Nullable>enable</Nullable>)
    // help catch null-reference bugs at compile time rather than at runtime.
    [MaxLength(2000)]
    public string? Biography { get; set; }

    public DateTime BirthDate { get; set; }

    // NAVIGATION PROPERTY: A virtual collection of all Books written by this Author.
    // EF Core uses this to build the relationship: one Author → many Books.
    // The 'virtual' keyword is optional but enables lazy loading (not used here).
    //
    // IMPORTANT: This property is NOT a column. EF Core uses it purely for
    // relationship mapping and .Include() eager-loading queries.
    public ICollection<Book> Books { get; set; } = new List<Book>();

    // A computed property — not stored in the database (EF Core ignores properties
    // without a setter by default). Useful for display purposes.
    public string FullName => $"{FirstName} {LastName}";
}
