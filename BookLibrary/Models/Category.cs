using System.ComponentModel.DataAnnotations;

namespace BookLibrary.Models;

// =============================================================================
// DOMAIN MODEL: Category
// =============================================================================
// A simple lookup / reference entity. Categories rarely change and are shared
// across many books. This is a classic "one-to-many" parent entity.
//
// Lookup entities like this are often seeded via EF Core's HasData() method
// in OnModelCreating — see LibraryDbContext.cs for the seed data.
// =============================================================================

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // A description is optional — note the nullable '?' type
    [MaxLength(500)]
    public string? Description { get; set; }

    // Navigation property: the collection of Books in this Category.
    // EF Core infers the one-to-many: one Category → many Books.
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
