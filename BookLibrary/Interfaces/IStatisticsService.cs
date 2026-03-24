namespace BookLibrary.Interfaces;

// =============================================================================
// INTERFACE: IStatisticsService
// =============================================================================
// This service is intentionally SIMPLE — its job is purely to demonstrate
// the SINGLETON lifetime. A singleton is created once and shared across all
// requests for the app's entire lifetime. See StatisticsService.cs for the
// implementation and the Singleton vs Scoped vs Transient comparison.
// =============================================================================

/// <summary>Tracks application-level statistics accumulated since startup.</summary>
public interface IStatisticsService
{
    /// <summary>Increments the total request counter. Called by RequestLoggingMiddleware.</summary>
    void RecordRequest();

    /// <summary>Returns a snapshot of current application statistics.</summary>
    AppStatistics GetStats();
}

/// <summary>Snapshot of app-level statistics at a point in time.</summary>
public record AppStatistics(
    long TotalRequestsServed,
    long UptimeSeconds,
    DateTime StartedAt
);
