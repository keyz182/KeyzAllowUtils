using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

[HarmonyPatch(typeof(Toils_Haul))]
public static class Toils_Haul_Patch
{
    [HarmonyPatch(nameof(Toils_Haul.PlaceHauledThingInCell))]
    [HarmonyPostfix]
    public static void PlaceHauledThingInCell(Toil __result)
    {
        if(KeyzAllowUtilitiesMod.settings.DisableHaulUrgently) return;
        Action originalInitAction = __result.initAction;
        __result.initAction = delegate
        {
            Thing carriedThing = __result.actor.carryTracker.CarriedThing;
            bool flag = carriedThing != null;
            if (flag)
            {
                __result.actor.Map.designationManager.TryRemoveDesignationOn(carriedThing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
            }
            originalInitAction();
        };
    }

    [HarmonyPatch(nameof(Toils_Haul.PlaceCarriedThingInCellFacing))]
    [HarmonyPostfix]
    public static void PlaceCarriedThingInCellFacing(Toil __result)
    {
        if(KeyzAllowUtilitiesMod.settings.DisableHaulUrgently) return;
        Action originalInitAction = __result.initAction;
        __result.initAction = delegate
        {
            Thing carriedThing = __result.actor.carryTracker.CarriedThing;
            bool flag = carriedThing != null;
            if (flag)
            {
                __result.actor.Map.designationManager.TryRemoveDesignationOn(carriedThing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
            }
            originalInitAction();
        };
    }

    [HarmonyPatch(nameof(Toils_Haul.DepositHauledThingInContainer))]
    [HarmonyPostfix]
    public static void DepositHauledThingInContainer(Toil __result)
    {
        if(KeyzAllowUtilitiesMod.settings.DisableHaulUrgently) return;
        Action originalInitAction = __result.initAction;
        __result.initAction = delegate
        {
            Thing carriedThing = __result.actor.carryTracker.CarriedThing;
            bool flag = carriedThing != null;
            if (flag)
            {
                __result.actor.Map.designationManager.TryRemoveDesignationOn(carriedThing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
            }
            originalInitAction();
        };
    }
}
