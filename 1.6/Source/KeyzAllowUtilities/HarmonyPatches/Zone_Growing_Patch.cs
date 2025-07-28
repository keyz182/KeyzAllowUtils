using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Zone_Growing))]
public static class Zone_Growing_Patch
{
    [HarmonyPatch(nameof(Zone_Growing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(ref IEnumerable<Gizmo> __result)
    {
        List<Gizmo> gizmos = __result.ToList();
        Gizmo othergizmo = gizmos.FirstOrDefault(g => g is Designator_ZoneAdd_Growing_Expand);
        Gizmo fertileExpand = DesignatorUtility.FindAllowedDesignator<Designator_ZoneAdd_GrowingFertile_Expand>();
        if(fertileExpand == null) return;
        if (othergizmo == null)
        {
            gizmos.Add(fertileExpand);
        }
        else
        {
            gizmos.Insert(gizmos.IndexOf(othergizmo)+1, fertileExpand);
        }

        __result = gizmos;
    }

}
