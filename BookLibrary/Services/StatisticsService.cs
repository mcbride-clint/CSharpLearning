using BookLibrary.Interfaces;

namespace BookLibrary.Services;

// =============================================================================
// SERVICE: StatisticsService — SINGLETON LIFETIME DEMONSTRATION
// =============================================================================
// SINGLETON: One instance is created when the app starts and reused for every
// request, for the entire lifetime of the application.
//
// WHEN TO USE SINGLETON:
//   - The service is stateless (pure computation, no stored data), OR
//   - The service manages shared, application-wide state that is THREAD-SAFE
//   - Examples: configuration readers, in-memory caches, connection pools,
//               request counters (like this one), logging infrastructure
//
// WHEN NOT TO USE SINGLETON:
//   - When the service holds per-request state (use Scoped instead)
//   - When the service wraps a non-thread-safe resource (e.g., DbContext)
//
// THREAD SAFETY:
//   Because Singletons are shared across all concurrent requests (multiple
//   threads), any shared mutable state MUST be thread-safe.
//
//   Here we use System.Threading.Interlocked.Increment() which is an atomic
//   operation — it reads and increments the counter in a single, uninterruptible
//   CPU instruction. This is faster and simpler than using lock(){}.
//
// CAPTIVE DEPENDENCY — THE CLASSIC SINGLETON MISTAKE:
//   NEVER inject a Scoped service into a Singleton's constructor.
//   Example of what NOT to do:
//     public StatisticsService(LibraryDbContext db) // ← WRONG! DbContext is Scoped
//
//   If you did this, the DI container would throw:
//     InvalidOperationException: Cannot consume scoped service 'LibraryDbContext'
//     from singleton 'StatisticsService'.
//
//   ASP.NET Core detects this automatically in Development mode and throws at
//   startup — a great safety net. In production (where this check is disabled
//   for performance), a captive dependency causes the Scoped service to outlive
//   its intended lifetime, leading to stale data and connection leaks.
//
//   SAFE ALTERNATIVE: inject IServiceScopeFactory and create a scope manually
//   if a Singleton genuinely needs occasional Scoped access.
// =============================================================================

public class StatisticsService : IStatisticsService
{
    // 'long' rather than 'int' because Interlocked.Increment works on long/int.
    // 'volatile' ensures all threads see the latest written value (prevents caching).
    private long _totalRequestsServed = 0;

    // DateTime.UtcNow at construction time = when the app started.
    // This is set ONCE in the constructor (Singleton is constructed once at startup).
    private readonly DateTime _startedAt = DateTime.UtcNow;

    // StatisticsService has no dependencies to inject — it manages pure in-memory state.
    // This is why it's a good candidate for Singleton.

    public void RecordRequest()
    {
        // Interlocked.Increment: atomically increments the value.
        // This is thread-safe — multiple threads can call this simultaneously
        // and each increment will be counted exactly once.
        //
        // If we used _totalRequestsServed++ instead, two threads could read the
        // same value, both increment it, and one increment would be lost (race condition).
        Interlocked.Increment(ref _totalRequestsServed);
    }

    public AppStatistics GetStats()
    {
        // Reading _totalRequestsServed is safe here because long reads are
        // atomic on 64-bit platforms. On 32-bit you'd use Interlocked.Read().
        return new AppStatistics(
            TotalRequestsServed: _totalRequestsServed,
            UptimeSeconds: (long)(DateTime.UtcNow - _startedAt).TotalSeconds,
            StartedAt: _startedAt
        );
    }
}
