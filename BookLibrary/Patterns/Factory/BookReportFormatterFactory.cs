namespace BookLibrary.Patterns.Factory;

// =============================================================================
// FACTORY PATTERN — Step 3: The Factory
// =============================================================================
// The factory's job is simple: given a format key ("csv", "json"), return the
// correct IBookReportFormatter. The factory OWNS the creation/selection logic.
//
// WHY NOT JUST USE A STATIC METHOD?
//   A static factory can't be injected or mocked. By making it a class registered
//   in DI, callers depend on BookReportFormatterFactory (or an interface for it)
//   rather than a static call — keeping code testable.
//
// DI TRICK — IEnumerable injection:
//   When you register multiple implementations of the same interface:
//     builder.Services.AddSingleton<IBookReportFormatter, CsvBookReportFormatter>();
//     builder.Services.AddSingleton<IBookReportFormatter, JsonBookReportFormatter>();
//
//   And a class requests IEnumerable<IBookReportFormatter>:
//     public BookReportFormatterFactory(IEnumerable<IBookReportFormatter> formatters)
//
//   ASP.NET Core's DI container automatically injects ALL registered
//   IBookReportFormatter implementations. This is called "open collection injection".
//   Adding a new formatter only requires registering it — the factory receives it
//   automatically without any code change.
// =============================================================================

public class BookReportFormatterFactory
{
    private readonly IReadOnlyDictionary<string, IBookReportFormatter> _formatters;

    public BookReportFormatterFactory(IEnumerable<IBookReportFormatter> formatters)
    {
        // Index by FileExtension for O(1) lookup.
        // Case-insensitive: "CSV" and "csv" both work.
        _formatters = formatters.ToDictionary(
            f => f.FileExtension,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the formatter for the given <paramref name="format"/> key.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when format is unrecognised.</exception>
    public IBookReportFormatter GetFormatter(string format)
    {
        if (_formatters.TryGetValue(format, out var formatter))
            return formatter;

        var supported = string.Join(", ", _formatters.Keys);
        throw new NotSupportedException(
            $"Report format '{format}' is not supported. Supported formats: {supported}");
    }

    /// <summary>Returns all supported format keys — useful for API documentation.</summary>
    public IEnumerable<string> SupportedFormats => _formatters.Keys;
}
