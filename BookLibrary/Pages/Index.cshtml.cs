using BookLibrary.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages;

// =============================================================================
// RAZOR PAGES: IndexModel (was HomeController.Index)
// =============================================================================
// KEY DIFFERENCES FROM MVC:
//
//   MVC Controller:
//     public class HomeController : Controller
//     {
//         public IActionResult Index() { ... return View(); }
//     }
//
//   Razor Page:
//     public class IndexModel : PageModel       ← inherits PageModel, not Controller
//     {
//         public void OnGet() { ... }           ← handler named after HTTP verb
//     }
//
// The PageModel is the "controller" and "view model" combined into one class.
// The corresponding .cshtml file is the "view" — it lives alongside the .cs file.
//
// ROUTING:
//   MVC: /{controller}/{action} → /Home/Index (mapped via MapControllerRoute)
//   Razor Pages: /folder/filename → /Index → / (the app root)
//   The route comes from the file's location in the Pages/ folder, not a route table.
// =============================================================================
public class IndexModel : PageModel
{
    private readonly IStatisticsService _statistics;
    private readonly IOperationIdService _operationId;
    private readonly ILogger<IndexModel> _logger;

    // Constructor injection works exactly the same as in MVC controllers.
    // The DI container creates IndexModel and injects these automatically.
    public IndexModel(
        IStatisticsService statistics,
        IOperationIdService operationId,
        ILogger<IndexModel> logger)
    {
        _statistics = statistics;
        _operationId = operationId;
        _logger = logger;
    }

    // RAZOR PAGES: public properties on PageModel replace ViewBag.
    // The page (.cshtml) accesses these as Model.TotalRequests — strongly typed,
    // no dynamic dispatch, no risk of runtime typos.
    public long TotalRequests { get; private set; }
    public long UptimeSeconds { get; private set; }
    public DateTime StartedAt { get; private set; }
    public Guid ControllerOperationId { get; private set; }

    // OnGet() replaces a GET action method. Razor Pages uses naming conventions:
    //   OnGet / OnGetAsync    → handles HTTP GET
    //   OnPost / OnPostAsync  → handles HTTP POST
    //   OnPostDelete / OnPostDeleteAsync → handles POST with ?handler=Delete
    public void OnGet()
    {
        var stats = _statistics.GetStats();

        TotalRequests       = stats.TotalRequestsServed;
        UptimeSeconds       = stats.UptimeSeconds;
        StartedAt           = stats.StartedAt;
        ControllerOperationId = _operationId.OperationId;
    }
}
