namespace BookLibrary.Patterns.GuardClauses;

// =============================================================================
// GUARD CLAUSES — Early Return / Early Throw Pattern
// =============================================================================
// INTENT: Eliminate deeply nested if-blocks by checking preconditions at the
// top of a method and failing fast. The "happy path" stays at the base
// indentation level, making it easy to read.
//
// WITHOUT GUARD CLAUSES (nested ifs):
// ─────────────────────────────────────────────────────────────────────────────
//   public async Task<Book> CreateBookAsync(Book book)
//   {
//       if (book != null)
//       {
//           if (!string.IsNullOrEmpty(book.Title))
//           {
//               if (book.AuthorId > 0)
//               {
//                   if (book.CategoryId > 0)
//                   {
//                       // actual work buried 4 levels deep
//                       await _books.AddAsync(book);
//                       return book;
//                   }
//                   else throw new ArgumentException("CategoryId required");
//               }
//               else throw new ArgumentException("AuthorId required");
//           }
//           else throw new ArgumentException("Title required");
//       }
//       else throw new ArgumentNullException(nameof(book));
//   }
//
// WITH GUARD CLAUSES (early throws):
// ─────────────────────────────────────────────────────────────────────────────
//   public async Task<Book> CreateBookAsync(Book book)
//   {
//       Guard.AgainstNull(book, nameof(book));
//       Guard.AgainstNullOrEmpty(book.Title, nameof(book.Title));
//       Guard.AgainstNonPositive(book.AuthorId, nameof(book.AuthorId));
//       Guard.AgainstNonPositive(book.CategoryId, nameof(book.CategoryId));
//
//       // happy path — immediately visible, no nesting
//       await _books.AddAsync(book);
//       return book;
//   }
//
// ADVANTAGES:
//   - Cyclomatic complexity drops (fewer branches = easier to test)
//   - The happy path is obvious — readers can skip the guards
//   - Each guard is one clear line documenting a precondition
//   - Used in: LibraryService.cs (every method)
//
// WHEN NOT TO USE:
//   - Don't use for business logic (e.g., "author has books") — that belongs
//     in an if-block with a descriptive comment in the service method
//   - Don't over-guard: public APIs need guards; internal helpers often don't
// =============================================================================

public static class Guard
{
    /// <summary>Throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is null.</summary>
    public static void AgainstNull<T>(T value, string paramName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(paramName, $"'{paramName}' must not be null.");
    }

    /// <summary>Throws <see cref="ArgumentException"/> if <paramref name="value"/> is null, empty, or whitespace.</summary>
    public static void AgainstNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"'{paramName}' must not be null or empty.", paramName);
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero or negative.</summary>
    public static void AgainstNonPositive(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value,
                $"'{paramName}' must be a positive integer (got {value}).");
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
    public static void AgainstNegative(decimal value, string paramName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, value,
                $"'{paramName}' must not be negative (got {value}).");
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is outside [min, max].</summary>
    public static void AgainstOutOfRange<T>(T value, T min, T max, string paramName)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value,
                $"'{paramName}' must be between {min} and {max} (got {value}).");
    }
}
