using System.Text;
using System.Text.Json;
using BookLibrary.Models;

namespace BookLibrary.Patterns.Factory;

// =============================================================================
// FACTORY PATTERN — Step 2: Concrete Products
// =============================================================================
// Each class implements IBookReportFormatter and produces one output format.
// They are REGISTERED as IBookReportFormatter in DI (multiple implementations
// of the same interface), then collected and indexed by the factory.
// =============================================================================

/// <summary>Formats books as comma-separated values (CSV) suitable for Excel.</summary>
public class CsvBookReportFormatter : IBookReportFormatter
{
    public string ContentType  => "text/csv";
    public string FileExtension => "csv";

    public string Format(IEnumerable<Book> books)
    {
        var sb = new StringBuilder();

        // CSV header row
        sb.AppendLine("Id,Title,Author,Category,ISBN,PublishedYear,PageCount,Price,Available");

        foreach (var book in books)
        {
            // Wrap fields in quotes to handle commas in titles/author names.
            // Real-world CSV should also escape embedded quotes — see RFC 4180.
            sb.AppendLine(string.Join(",",
                book.Id,
                $"\"{book.Title}\"",
                $"\"{book.Author?.FullName ?? "Unknown"}\"",
                $"\"{book.Category?.Name ?? "Unknown"}\"",
                book.ISBN ?? "",
                book.PublishedYear,
                book.PageCount,
                book.Price,
                book.IsAvailable));
        }

        return sb.ToString();
    }
}

/// <summary>Formats books as JSON (pretty-printed for readability).</summary>
public class JsonBookReportFormatter : IBookReportFormatter
{
    public string ContentType   => "application/json";
    public string FileExtension => "json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true  // pretty-print with newlines and indentation
    };

    public string Format(IEnumerable<Book> books)
    {
        // Project to an anonymous type to control exactly what appears in the JSON.
        // This avoids circular reference issues (Book → Author → Books → Book...)
        // that would occur if we serialized the full entity graph.
        var output = books.Select(b => new
        {
            b.Id,
            b.Title,
            Author    = b.Author?.FullName ?? "Unknown",
            Category  = b.Category?.Name ?? "Unknown",
            b.ISBN,
            b.PublishedYear,
            b.PageCount,
            b.Price,
            b.IsAvailable
        });

        return JsonSerializer.Serialize(output, Options);
    }
}
