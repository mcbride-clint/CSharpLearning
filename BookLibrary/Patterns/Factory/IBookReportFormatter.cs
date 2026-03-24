using BookLibrary.Models;

namespace BookLibrary.Patterns.Factory;

// =============================================================================
// FACTORY PATTERN — Step 1: The Product Interface
// =============================================================================
// INTENT: Centralize object creation logic. The caller requests an object by
// type (e.g., "csv"); the factory decides which concrete class to instantiate.
//
// PROBLEM WITHOUT FACTORY:
//   Every controller that generates reports needs its own if/switch:
//
//   if (format == "csv")
//       formatter = new CsvBookReportFormatter();
//   else if (format == "json")
//       formatter = new JsonBookReportFormatter();
//   // Adding XML requires changing EVERY controller that has this if/switch
//
// SOLUTION WITH FACTORY:
//   IBookReportFormatter f = _factory.GetFormatter(format);
//   string output = f.Format(books);
//   return File(Encoding.UTF8.GetBytes(output), f.ContentType, $"books.{f.FileExtension}");
//
//   Adding XML: add XmlBookReportFormatter class + register it in Program.cs.
//   No controller code changes. No switch modifications. Just one new class.
//
// DI INTEGRATION (in Program.cs):
//   builder.Services.AddSingleton<IBookReportFormatter, CsvBookReportFormatter>();
//   builder.Services.AddSingleton<IBookReportFormatter, JsonBookReportFormatter>();
//   builder.Services.AddSingleton<BookReportFormatterFactory>();
//
//   The factory receives IEnumerable<IBookReportFormatter> from DI —
//   meaning ALL registered formatters are injected automatically.
//
// DEMONSTRATED AT: GET /api/books/export?format=csv  and  ?format=json
// =============================================================================

public interface IBookReportFormatter
{
    /// <summary>Formats the book list as a string (CSV, JSON, XML, etc.).</summary>
    string Format(IEnumerable<Book> books);

    /// <summary>MIME type for the HTTP Content-Type header (e.g., "text/csv").</summary>
    string ContentType { get; }

    /// <summary>File extension used for the download filename (e.g., "csv").</summary>
    string FileExtension { get; }
}
