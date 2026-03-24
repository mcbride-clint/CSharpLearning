namespace BookLibrary.Middleware;

// =============================================================================
// MIDDLEWARE: GlobalExceptionMiddleware
// =============================================================================
// WHAT IS MIDDLEWARE?
// Middleware is software assembled into the request pipeline. Each component:
//   1. Receives an HttpContext (the current request + response)
//   2. Can do work BEFORE passing to the next component
//   3. Calls 'await _next(context)' to pass control down the pipeline
//   4. Can do work AFTER the downstream components complete
//
// Think of the pipeline as Russian nesting dolls. The outermost middleware
// wraps all the others. Execution flows:
//   Request:  Outer → Middle → Inner → Controller
//   Response: Controller → Inner → Middle → Outer
//
// THIS MIDDLEWARE handles ALL unhandled exceptions from anywhere downstream.
// It must be registered FIRST in Program.cs so it wraps the entire pipeline.
//
// WITHOUT THIS:
//   An unhandled exception in a controller would bubble up to the Kestrel
//   web server, which would return a 500 with a generic error page or JSON.
//   You'd have no control over the error format or logging.
//
// WITH THIS:
//   Every exception is caught in ONE place. We can:
//   - Log it consistently with structured logging
//   - Return a friendly JSON response for API requests
//   - Return a redirect/error page for MVC requests
//   - Never expose internal stack traces to end users
//
// REGISTRATION ORDER IN PROGRAM.CS:
//   app.UseMiddleware<GlobalExceptionMiddleware>(); // MUST BE FIRST
//   app.UseMiddleware<RequestLoggingMiddleware>();
//   app.UseRouting();
//   app.MapControllers();
// =============================================================================

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    // Middleware constructors are called ONCE at startup (middleware instances are singletons).
    // Therefore: only inject Singleton services here.
    // ILogger<T> is safe — it's effectively a Singleton factory.
    //
    // To inject Scoped services (like DbContext), add them as parameters to InvokeAsync
    // instead — ASP.NET Core will resolve them from the request scope there.
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pass control to the next middleware in the pipeline.
            // If ANYTHING downstream throws an unhandled exception, it bubbles up here.
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the full exception — message, stack trace, and any inner exceptions.
            // Structured logging: {Method} and {Path} become searchable fields in log
            // aggregation tools like Seq, Datadog, or Azure Monitor.
            _logger.LogError(ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Set the status code BEFORE writing the response body.
        // Once you start writing the body, the status code is locked in.
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // API requests: return JSON. Convention: if the path starts with /api,
        // the caller is probably a frontend, mobile app, or Swagger client
        // that expects JSON, not HTML.
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json";

            // In production, NEVER include the real error message or stack trace —
            // it reveals implementation details that attackers can exploit.
            // In development, it's useful to see the actual exception.
            var isDevelopment = context.RequestServices
                .GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false;

            var response = isDevelopment
                ? new { error = ex.Message, type = ex.GetType().Name }
                : new { error = "An unexpected error occurred. Please try again later.", type = "InternalServerError" };

            await context.Response.WriteAsJsonAsync(response);
        }
        else
        {
            // MVC requests: redirect to the error page.
            // The user sees a friendly error page, not a raw exception.
            context.Response.Redirect("/Home/Error");
        }
    }
}
