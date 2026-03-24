using BookLibrary.Interfaces;

namespace BookLibrary.Services;

// =============================================================================
// SERVICE: OperationIdService — TRANSIENT LIFETIME DEMONSTRATION
// =============================================================================
// TRANSIENT: A brand-new instance is created EVERY SINGLE TIME this service
// is requested from the DI container. This happens even if two classes request
// it within the same HTTP request.
//
// DEMONSTRATION:
//   Visit /Home/DiDemo to see live proof:
//   - HomeController receives OperationIdService instance A (GUID: xxxxxxxx)
//   - LibraryService receives OperationIdService instance B (GUID: yyyyyyyy)
//   - Both GUIDs are DIFFERENT — proving a new instance per injection
//
//   If this were SCOPED: both would receive the same instance within one request.
//   If this were SINGLETON: all requests ever would receive the same instance.
//
// WHEN TO USE TRANSIENT:
//   - Lightweight, stateless services (simple calculators, formatters, mappers)
//   - When state should NEVER be shared — not even within one request
//   - When the cost of creating a new instance is negligible
//
// WHEN NOT TO USE TRANSIENT:
//   - Services with expensive initialization (database connections, HTTP clients)
//   - Services that hold resources that should be reused within a request
//
// MEMORY CONSIDERATION:
//   If a Transient service implements IDisposable, ASP.NET Core's DI container
//   will NOT dispose it until the scope ends (the end of the request). Many
//   Transient instances with unmanaged resources can cause memory issues.
//   For IDisposable Transients, prefer Scoped or manage disposal manually.
// =============================================================================

public class OperationIdService : IOperationIdService
{
    // Guid.NewGuid() generates a cryptographically random, universally unique ID.
    // This is evaluated ONCE when the constructor runs.
    // Because the constructor runs on EVERY injection for a Transient service,
    // each instance gets its own unique GUID.
    public Guid OperationId { get; } = Guid.NewGuid();
}
