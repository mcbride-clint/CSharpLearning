using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookLibrary.Models;

// =============================================================================
// DOMAIN MODEL: Book
// =============================================================================
// This is the central entity of our application. It demonstrates:
//   - Foreign keys (AuthorId, CategoryId)
//   - Navigation properties (Author, Category)
//   - EF Core conventions vs explicit configuration
//   - Column type mapping
//   - Multiple annotation types
// =============================================================================

public class Book
{
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    // ISBN has a specific format — [StringLength] works like [MaxLength] but
    // also sets a minimum length, useful for format validation.
    [StringLength(13, MinimumLength = 10)]
    public string? ISBN { get; set; }

    // [Range] adds both a database check constraint AND model validation.
    [Range(1, int.MaxValue, ErrorMessage = "Published year must be a positive number.")]
    public int PublishedYear { get; set; }

    [Range(1, 10000, ErrorMessage = "Page count must be between 1 and 10,000.")]
    public int PageCount { get; set; }

    // DECIMAL IN SQLITE:
    // SQLite has no native DECIMAL type — it stores numbers as TEXT, INTEGER, or REAL.
    // We use [Column(TypeName = "TEXT")] to tell EF Core to serialize the decimal
    // as a text string, which preserves precision. In SQL Server or PostgreSQL you
    // would use "decimal(18,2)" instead.
    [Column(TypeName = "TEXT")]
    [Range(0, 9999.99, ErrorMessage = "Price must be between 0 and 9999.99.")]
    public decimal Price { get; set; }

    // A simple boolean flag — maps to INTEGER (0/1) in SQLite.
    public bool IsAvailable { get; set; } = true;

    // Optional longer description
    [MaxLength(2000)]
    public string? Description { get; set; }

    // =========================================================================
    // FOREIGN KEY + NAVIGATION PROPERTY PAIR
    // =========================================================================
    // EF Core CONVENTION: a property named "[NavigationPropertyName]Id" is
    // automatically treated as the Foreign Key for that navigation property.
    //
    // So "AuthorId" + "Author" together tell EF Core:
    //   - Add an AuthorId column (FK) to the Books table
    //   - That FK references the Authors table's Id column
    //   - Set up a one-to-many relationship (one Author → many Books)
    //
    // You could replace this convention with an explicit [ForeignKey("AuthorId")]
    // attribute on the Author property, or configure it in Fluent API. The
    // convention approach shown here is the most common and concise.
    // =========================================================================

    // [Required] on the FK property means the book MUST have an author.
    // If you omit [Required], EF Core will make the FK nullable (optional relationship).
    [Required]
    public int AuthorId { get; set; }

    // The navigation property — lets you write: book.Author.FullName
    // This is NOT a column; it's loaded by EF Core via .Include() or lazy loading.
    public Author Author { get; set; } = null!;
    // 'null!' suppresses the nullable warning — EF Core will always populate
    // this before you access it (when queried with .Include()), so it's safe.

    [Required]
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;
}
