using System.ComponentModel.DataAnnotations;

namespace BookLibrary.ViewModels;

public class CategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Display(Name = "Books in Category")]
    public int BookCount { get; set; }
}

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
