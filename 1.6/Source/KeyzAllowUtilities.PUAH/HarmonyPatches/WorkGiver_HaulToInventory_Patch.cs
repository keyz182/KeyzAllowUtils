using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PickUpAndHaul;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities.PUAH.HarmonyPatches;

[HarmonyPatch(typeof(WorkGiver_HaulToInventory))]
public static class WorkGiver_HaulToInventory_Patch
{

    [HarmonyPatch(nameof(WorkGiver_HaulToInventory.GoodThingToHaul))]
    [HarmonyPostfix]
    public static void HasJobOnThing(Thing t, Pawn pawn, ref bool __result)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableNoHauling) return;
        Designation des = pawn.Map.designationManager.DesignationOn(t, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);
        if (des == null) return;
        __result = false;
    }

    [HarmonyPatch(nameof(WorkGiver_HaulToInventory.HasJobOnThing))]
    [HarmonyPostfix]
    public static void HasJobOnThing(Pawn pawn, Thing thing, ref bool __result)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableNoHauling) return;
        Designation des = pawn.Map.designationManager.DesignationOn(thing, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);
        if (des == null) return;
        __result = false;
    }

    [HarmonyPatch(nameof(WorkGiver_HaulToInventory.PotentialWorkThingsGlobal))]
    [HarmonyPostfix]
    public static void PotentialWorkThingsGlobal_Patch(Pawn pawn, ref IEnumerable<Thing> __result) {
        if (KeyzAllowUtilitiesMod.settings.DisableNoHauling) return;

        __result = __result.Where(t=>pawn.Map.designationManager.DesignationOn(t, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation) == null);
    }
}
