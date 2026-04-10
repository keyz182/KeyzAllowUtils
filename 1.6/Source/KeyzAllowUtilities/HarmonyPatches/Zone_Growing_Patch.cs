using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Zone_Growing))]
public static class Zone_Growing_Patch
{
    private static Designator_ZoneAdd_GrowingFertile_Expand _fertileExpand;
    private static Game _cachedForGame;

    [HarmonyPatch(nameof(Zone_Growing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(ref IEnumerable<Gizmo> __result)
    {
        if (_cachedForGame != Current.Game)
        {
            _fertileExpand = DesignatorUtility.FindAllowedDesignator<Designator_ZoneAdd_GrowingFertile_Expand>();
            _cachedForGame = Current.Game;
        }
        if (_fertileExpand == null) return;

        __result = InsertFertileExpand(__result, _fertileExpand);
    }

    private static IEnumerable<Gizmo> InsertFertileExpand(IEnumerable<Gizmo> gizmos, Gizmo fertileExpand)
    {
        bool inserted = false;
        foreach (Gizmo g in gizmos)
        {
            yield return g;
            if (!inserted && g is Designator_ZoneAdd_Growing_Expand)
            {
                yield return fertileExpand;
                inserted = true;
            }
        }
        if (!inserted)
        {
            yield return fertileExpand;
        }
    }
}
