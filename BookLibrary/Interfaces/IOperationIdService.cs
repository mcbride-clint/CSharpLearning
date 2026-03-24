namespace BookLibrary.Interfaces;

// =============================================================================
// INTERFACE: IOperationIdService
// =============================================================================
// This interface exists purely to DEMONSTRATE the TRANSIENT lifetime.
//
// TRANSIENT: A brand-new instance is created every single time this service
// is resolved from the DI container — even within the same HTTP request,
// and even if the same class requests it twice.
//
// HOW TO OBSERVE THE DIFFERENCE:
//   1. Inject IOperationIdService into HomeController
//   2. Also inject IOperationIdService into LibraryService
//   3. Both receive DIFFERENT GUIDs in the same request — proving Transient
//      creates a new instance on every injection.
//
// If this were SCOPED, both would receive the same GUID within one request.
// If this were SINGLETON, all requests across the entire application lifetime
// would receive the same GUID.
//
// See HomeController.cs → DiDemo action for the live demonstration.
// =============================================================================

public interface IOperationIdService
{
    /// <summary>
    /// A unique identifier generated once when this instance was constructed.
    /// With Transient lifetime, every injection gets a different GUID.
    /// </summary>
    Guid OperationId { get; }
}
