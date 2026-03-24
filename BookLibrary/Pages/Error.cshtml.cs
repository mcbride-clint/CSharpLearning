using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages;

// =============================================================================
// RAZOR PAGES ERROR PAGE — PageModel
// =============================================================================
// [ResponseCache]: prevents browsers from caching error pages — each visit
// should show the current error, not a stale cached one.
//
// [IgnoreAntiforgeryToken]: the error page may be reached from the global
// exception middleware via HttpContext.Features, which doesn't have a valid
// antiforgery context. This attribute tells Razor Pages not to validate the
// token for this page.
// =============================================================================
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        // Activity.Current?.Id: the OpenTelemetry/DiagnosticSource trace ID for
        // the current request, if distributed tracing is active.
        // HttpContext.TraceIdentifier: ASP.NET Core's own request identifier —
        // always available as a fallback.
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
