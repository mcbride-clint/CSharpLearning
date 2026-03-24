using System.Diagnostics;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Controllers;

// =============================================================================
// MVC CONTROLLER: HomeController
// =============================================================================
// An MVC Controller inherits from 'Controller' (not 'ControllerBase').
// The extra 'Controller' base class adds:
//   - View() — render a Razor view and return HTML
//   - ViewBag / ViewData — pass data from controller to view
//   - TempData — pass data that survives ONE redirect
//   - PartialView() — render a partial view
//
// ROUTING (Conventional):
//   MVC controllers use the pattern: /{controller}/{action}/{id?}
//   HomeController.Index() maps to /Home/Index (or just "/" via the default route).
//   The default route is configured in Program.cs:
//     app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
//
// DI IN CONTROLLERS:
//   The DI container creates controllers and injects constructor dependencies.
//   You never call 'new HomeController(...)' yourself.
// =============================================================================

public class HomeController : Controller
{
    private readonly IStatisticsService _statistics;
    private readonly IOperationIdService _operationId;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IStatisticsService statistics,        // Singleton — shared across all requests
        IOperationIdService operationId,      // Transient — unique to this injection
        ILogger<HomeController> logger)
    {
        _statistics = statistics;
        _operationId = operationId;
        _logger = logger;
    }

    // GET /
    // GET /Home
    // GET /Home/Index
    public IActionResult Index()
    {
        var stats = _statistics.GetStats();

        // ViewBag is dynamic — easy to use but no compile-time type checking.
        // Typos in property names cause runtime errors, not compile errors.
        // For larger amounts of data, use a strongly-typed ViewModel instead.
        ViewBag.TotalRequests = stats.TotalRequestsServed;
        ViewBag.UptimeSeconds = stats.UptimeSeconds;
        ViewBag.StartedAt = stats.StartedAt;
        ViewBag.ControllerOperationId = _operationId.OperationId;

        return View(); // renders Views/Home/Index.cshtml
    }

    // ==========================================================================
    // DI LIFETIME DEMONSTRATION — GET /Home/DiDemo
    // ==========================================================================
    // Visit this route and check the console output for the logged GUIDs.
    public IActionResult DiDemo([FromServices] IOperationIdService secondOperationId)
    {
        // [FromServices] resolves a service from DI for this action method only.
        // Useful when a dependency is only needed in one action, not the whole controller.

        // _operationId: injected in the constructor (Transient)
        // secondOperationId: injected via [FromServices] here (Transient)
        //
        // Because IOperationIdService is TRANSIENT, these two GUIDs will be DIFFERENT.
        // They are two separate instances created at two different injection points.
        _logger.LogInformation(
            "[DiDemo] Constructor OperationId: {CtorId} | [FromServices] OperationId: {SvcId} | Same? {Same}",
            _operationId.OperationId,
            secondOperationId.OperationId,
            _operationId.OperationId == secondOperationId.OperationId);

        ViewBag.CtorOperationId    = _operationId.OperationId;
        ViewBag.ActionOperationId  = secondOperationId.OperationId;
        ViewBag.AreSame            = _operationId.OperationId == secondOperationId.OperationId;
        ViewBag.Stats              = _statistics.GetStats();

        return View();
    }

    // GET /Home/Error
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
