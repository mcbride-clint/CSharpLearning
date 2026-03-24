using BookLibrary.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages;

// =============================================================================
// RAZOR PAGES: DiDemoModel (was HomeController.DiDemo)
// =============================================================================
// DEMONSTRATING TWO INJECTION POINTS FOR TRANSIENT SERVICES:
//
//   MVC used [FromServices] on the action parameter:
//     public IActionResult DiDemo([FromServices] IOperationIdService secondId) { ... }
//
//   Razor Pages uses [FromServices] on the OnGet handler parameter:
//     public void OnGet([FromServices] IOperationIdService secondId) { ... }
//
//   Both work identically. [FromServices] resolves a service from DI for
//   that specific method call — useful when a dependency is only needed in
//   one handler, not the whole page lifecycle.
// =============================================================================
public class DiDemoModel : PageModel
{
    private readonly IStatisticsService _statistics;
    private readonly IOperationIdService _operationId;
    private readonly ILogger<DiDemoModel> _logger;

    public DiDemoModel(
        IStatisticsService statistics,
        IOperationIdService operationId,
        ILogger<DiDemoModel> logger)
    {
        _statistics = statistics;
        _operationId = operationId;
        _logger = logger;
    }

    public Guid CtorOperationId { get; private set; }
    public Guid ActionOperationId { get; private set; }
    public bool AreSame { get; private set; }
    public BookLibrary.Interfaces.AppStatistics? Stats { get; private set; }

    public void OnGet([FromServices] IOperationIdService secondOperationId)
    {
        // _operationId: injected into the constructor (Transient)
        // secondOperationId: injected via [FromServices] into this handler (Transient)
        //
        // Because IOperationIdService is TRANSIENT, these two GUIDs will be DIFFERENT.
        // Transient = new instance on every injection point, even within one request.
        _logger.LogInformation(
            "[DiDemo] Constructor OperationId: {CtorId} | [FromServices] OperationId: {SvcId} | Same? {Same}",
            _operationId.OperationId,
            secondOperationId.OperationId,
            _operationId.OperationId == secondOperationId.OperationId);

        CtorOperationId   = _operationId.OperationId;
        ActionOperationId = secondOperationId.OperationId;
        AreSame           = _operationId.OperationId == secondOperationId.OperationId;
        Stats             = _statistics.GetStats();
    }
}
