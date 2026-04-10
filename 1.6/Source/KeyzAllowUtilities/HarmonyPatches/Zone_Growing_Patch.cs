using System.Collections.Generic;
using System.Linq;
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

        List<Gizmo> gizmos = __result.ToList();
        Gizmo othergizmo = gizmos.FirstOrDefault(g => g is Designator_ZoneAdd_Growing_Expand);
        if (othergizmo == null)
        {
            gizmos.Add(_fertileExpand);
        }
        else
        {
            gizmos.Insert(gizmos.IndexOf(othergizmo) + 1, _fertileExpand);
        }

        __result = gizmos;
    }

}
