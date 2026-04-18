using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Zone_Stockpile))]
public static class Zone_Stockpile_Patch
{
    [HarmonyPatch(nameof(Zone_Stockpile.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Zone_Stockpile __instance, ref IEnumerable<Gizmo> __result)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableSelectStored)
            return;

        __result = AppendSelectStored(__result, __instance);
    }

    private static IEnumerable<Gizmo> AppendSelectStored(IEnumerable<Gizmo> gizmos, Zone_Stockpile stockpile)
    {
        foreach (Gizmo g in gizmos)
        {
            yield return g;
        }

        yield return FilterUtils.MakeSelectStoredGizmo(stockpile);
    }
}
