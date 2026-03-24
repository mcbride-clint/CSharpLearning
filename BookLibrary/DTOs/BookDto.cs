namespace BookLibrary.DTOs;

// =============================================================================
// DATA TRANSFER OBJECTS (DTOs)
// =============================================================================
// DTOs are shapes designed for crossing API boundaries (serialised to/from JSON).
//
// KEY DIFFERENCES from domain models:
//   - DTOs are flat: they expand navigation properties (Author.FullName instead
//     of AuthorId) so the API consumer doesn't need to make extra calls.
//   - DTOs are versioned independently from domain models: you can add/rename
//     a domain property without breaking API consumers if you control the mapping.
//   - DTOs prevent exposing internal entity IDs or database-specific types.
//
// MAPPING: We use manual mapping here for simplicity. In larger projects,
// AutoMapper or Mapster libraries can automate this mapping.
// =============================================================================

/// <summary>API response shape for a book (read operations).</summary>
public record BookDto(
    int Id,
    string Title,
    string? ISBN,
    int PublishedYear,
    int PageCount,
    decimal Price,
    bool IsAvailable,
    string? Description,
    string AuthorName,       // flattened — no need to join client-side
    int AuthorId,
    string CategoryName,
    int CategoryId
);

/// <summary>API request body for creating a book.</summary>
public record CreateBookRequest(
    string Title,
    string? ISBN,
    int PublishedYear,
    int PageCount,
    decimal Price,
    bool IsAvailable,
    string? Description,
    int AuthorId,
    int CategoryId
);

/// <summary>API request body for updating a book (all fields optional for partial update).</summary>
public record UpdateBookRequest(
    string? Title,
    string? ISBN,
    int? PublishedYear,
    int? PageCount,
    decimal? Price,
    bool? IsAvailable,
    string? Description,
    int? AuthorId,
    int? CategoryId
);
