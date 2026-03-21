using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(DesignationCategoryDef))]
public static class DesignationCategoryDef_Patch
{
    [HarmonyPatch(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
    [HarmonyPostfix]
    public static void ResolvedAllowedDesignators_Patch(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
    {
        if(!KeyzAllowUtilitiesMod.settings.DisableHaulUrgently) return;

        List<Designator> list = __result.ToList();
        list.RemoveAll(d => d is Designator_HaulUrgently);
        __result = list;
    }
}
