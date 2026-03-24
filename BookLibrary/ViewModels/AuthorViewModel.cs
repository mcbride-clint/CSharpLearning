using System.ComponentModel.DataAnnotations;

namespace BookLibrary.ViewModels;

public class AuthorViewModel
{
    public int Id { get; set; }

    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Birth Date")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    public string? Biography { get; set; }

    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}";

    [Display(Name = "Number of Books")]
    public int BookCount { get; set; }
}

public class AuthorFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Birth Date")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; } = DateTime.Now.AddYears(-30);

    [MaxLength(2000)]
    public string? Biography { get; set; }
}
