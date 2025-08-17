using HarmonyLib;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(HaulAIUtility))]
public static class HaulAIUtility_Patch
{

    [HarmonyPatch(nameof(HaulAIUtility.HaulToStorageJob))]
    [HarmonyPrefix]
    public static bool HaulToStorageJob(Pawn p, Thing t, bool forced, ref Job __result)
    {
        __result = null;
        if (KeyzAllowUtilitiesMod.settings.DisableNoHauling) return true;
        Designation des = p.Map.designationManager.DesignationOn(t, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);
        if (des == null) return true;
        return false;
    }
}
