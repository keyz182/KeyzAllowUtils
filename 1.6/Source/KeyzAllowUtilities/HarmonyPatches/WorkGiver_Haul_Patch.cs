using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(WorkGiver_Haul))]
public class WorkGiver_Haul_Patch
{
    [HarmonyPatch(nameof(WorkGiver_Haul.JobOnThing))]
    [HarmonyPostfix]
    public static void JobOnThing(Pawn pawn, Thing t, ref Job __result)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableNoHauling) return;
        Designation des = pawn.Map.designationManager.DesignationOn(t, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);
        if (des == null) return;
        __result = null;
    }
}
