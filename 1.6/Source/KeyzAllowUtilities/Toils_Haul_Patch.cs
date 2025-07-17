using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

[HarmonyPatch(typeof(Toils_Haul))]
[HarmonyPatch("PlaceHauledThingInCell")]
public static class Toils_Haul_Patch
{
    [HarmonyPostfix]
    public static void ClearHaulUrgently(Toil __result)
    {
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
