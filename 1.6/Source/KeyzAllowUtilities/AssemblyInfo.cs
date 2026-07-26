using System.Runtime.CompilerServices;

// Exposes internal members (e.g. WorkGiver_HaulUrgently's ordering comparer) to the xunit test
// project so deterministic-ordering logic can be unit-tested without making it public API.
[assembly: InternalsVisibleTo("KeyzAllowUtilities.Tests")]
