using System.Linq;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

/// <summary>
/// Shift-click on a gizmo picks up the matching drag designator instead of acting on the
/// selection, like Allow Tool. 1.7 only; on 1.6 this always returns false.
/// </summary>
public static class ShiftSelect
{
    /// <summary>
    /// If Shift is held and <paramref name="designator"/> exists, select it and return true so
    /// the caller skips its normal action.
    /// </summary>
    public static bool TrySelect(Designator designator)
    {
#if NET10_0_OR_GREATER
        if (designator == null || Event.current is not { shift: true }) return false;
        Find.DesignatorManager.Select(designator);
        return true;
#else
        return false;
#endif
    }

    /// <summary>The Orders-category instance of a designator type, or null.</summary>
    public static T OrdersDesignator<T>() where T : Designator =>
        DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("Orders")?.AllResolvedDesignators.OfType<T>().FirstOrDefault();
}
