using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookLibrary.ViewModels;

// =============================================================================
// VIEWMODEL: BookViewModel
// =============================================================================
// WHY VIEWMODELS? Why not pass Book (the domain model) directly to views?
//
//   1. OVER-POSTING ATTACK: If you bind a domain model directly from a form,
//      a malicious user can POST hidden fields (e.g., "IsAdmin=true") and
//      set properties you didn't intend. ViewModels only expose what the form needs.
//
//   2. SENSITIVE DATA LEAKAGE: Domain models may have fields you never want
//      displayed (e.g., internal flags, audit timestamps). ViewModels let you
//      control exactly what goes to the view.
//
//   3. SHAPE MISMATCH: Views often need data from multiple entities plus UI
//      helpers like SelectList. Domain models don't carry SelectLists.
//      ViewModels are shaped for the VIEW, not the database.
//
//   4. VALIDATION RULES DIFFER: A Book entity might allow null Description
//      at the database level, but your create form might require it. Separate
//      validation on the ViewModel without modifying the domain model.
//
// VIEWMODEL vs DTO:
//   ViewModels → shaped for Razor views (may contain SelectList, display strings)
//   DTOs       → shaped for API consumers (serialised to/from JSON)
//   Domain     → shaped for the database (EF Core entities)
// =============================================================================

/// <summary>Used for displaying a book in list/details views.</summary>
public class BookViewModel
{
    public int Id { get; set; }

    // [Display] controls the label shown in Razor views with asp-for.
    // Without it, Razor uses the property name: "Title" → "Title".
    // With it, you can write "Book Title" as the label without renaming the property.
    [Display(Name = "Book Title")]
    public string Title { get; set; } = string.Empty;

    public string? ISBN { get; set; }

    [Display(Name = "Published Year")]
    public int PublishedYear { get; set; }

    [Display(Name = "Pages")]
    public int PageCount { get; set; }

    // [DataType] hints to the view how to render/format the value.
    // DataType.Currency renders the value with a currency symbol in display templates.
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Available")]
    public bool IsAvailable { get; set; }

    public string? Description { get; set; }

    // These are DISPLAY strings — computed from the related entities.
    // The view shows "Herbert, Frank" instead of AuthorId = 1.
    [Display(Name = "Author")]
    public string AuthorName { get; set; } = string.Empty;

    [Display(Name = "Category")]
    public string CategoryName { get; set; } = string.Empty;
}

/// <summary>Used for the Create/Edit book forms — includes dropdown data.</summary>
public class BookFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(300)]
    [Display(Name = "Book Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN must be 10–13 characters.")]
    public string? ISBN { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Please enter a valid year.")]
    [Display(Name = "Published Year")]
    public int PublishedYear { get; set; } = DateTime.Now.Year;

    [Required]
    [Range(1, 10000)]
    [Display(Name = "Page Count")]
    public int PageCount { get; set; }

    [Required]
    [Range(0, 9999.99)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Currently Available")]
    public bool IsAvailable { get; set; } = true;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please select an author.")]
    [Display(Name = "Author")]
    public int AuthorId { get; set; }

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    // SelectList: provides the data for <select> dropdowns in Razor views.
    // Used with: <select asp-for="AuthorId" asp-items="Model.Authors">
    // These are NOT bound from the form — they're only populated on GET requests.
    // On POST, only Id/AuthorId/CategoryId/etc are needed.
    public IEnumerable<SelectListItem> Authors { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();
}
