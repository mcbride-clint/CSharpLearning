using System.Diagnostics;
using BookLibrary.Interfaces;

namespace BookLibrary.Middleware;

// =============================================================================
// MIDDLEWARE: RequestLoggingMiddleware
// =============================================================================
// This middleware demonstrates:
//   1. The before/after pipeline pattern (code runs before AND after _next)
//   2. How to inject Scoped services into Singleton-lifetime middleware
//   3. Request timing using Stopwatch
//   4. Structured logging with ILogger
//
// THE PIPELINE MODEL (Russian Dolls):
//
//   ┌─────────────────────────────────────────┐
//   │  GlobalExceptionMiddleware              │  ← outermost
//   │  ┌───────────────────────────────────┐  │
//   │  │  RequestLoggingMiddleware         │  │
//   │  │  ┌─────────────────────────────┐  │  │
//   │  │  │  UseStaticFiles            │  │  │
//   │  │  │  ┌─────────────────────┐   │  │  │
//   │  │  │  │  UseRouting         │   │  │  │
//   │  │  │  │  MapControllers     │   │  │  │
//   │  │  │  └─────────────────────┘   │  │  │
//   │  │  └─────────────────────────────┘  │  │
//   │  └───────────────────────────────────┘  │
//   └─────────────────────────────────────────┘
//
// INJECTING SCOPED SERVICES INTO MIDDLEWARE:
//   Middleware instances are created once (Singleton lifecycle).
//   You CANNOT inject Scoped services in the constructor — they would be
//   "captured" and kept alive beyond their intended lifetime (captive dependency).
//
//   SOLUTION: Declare Scoped services as parameters on InvokeAsync.
//   ASP.NET Core automatically resolves them from the current request's scope
//   each time InvokeAsync is called, so they have the correct Scoped lifetime.
//
//   Singleton  → inject in constructor ✓
//   Scoped     → inject in InvokeAsync parameter ✓
//   Transient  → inject in InvokeAsync parameter ✓ (or constructor for stateless ones)
// =============================================================================

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // ILogger is safe in constructor — it's a factory that doesn't hold request state.
    // IStatisticsService is Singleton — also safe in constructor.
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // IStatisticsService is injected as a CONSTRUCTOR parameter above, but we could
    // also inject it here in InvokeAsync. Either works for Singletons.
    // We demonstrate the InvokeAsync injection pattern explicitly for clarity.
    public async Task InvokeAsync(HttpContext context, IStatisticsService statistics)
    {
        // ---- BEFORE: runs before the rest of the pipeline ----

        // Stopwatch starts BEFORE calling _next — measures total pipeline time.
        var stopwatch = Stopwatch.StartNew();

        // Increment the application-wide request counter (Singleton state).
        statistics.RecordRequest();

        _logger.LogInformation(
            "[REQ] {Method} {Path}{Query}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        // ---- PASS CONTROL DOWNSTREAM ----
        // Everything between here and the log below runs in the inner middleware/controller.
        await _next(context);

        // ---- AFTER: runs after the response has been generated ----
        // At this point, the controller has run and the response body is ready.
        // We measure total time including controller execution, view rendering, etc.
        stopwatch.Stop();

        _logger.LogInformation(
            "[RES] {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
