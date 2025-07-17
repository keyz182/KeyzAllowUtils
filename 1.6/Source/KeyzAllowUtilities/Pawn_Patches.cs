using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[HarmonyPatch(typeof(Pawn))]
[StaticConstructorOnStartup]
public static class Pawn_Patches
{
    public static readonly Texture2D KUA_ToggleFinishOff = ContentFinder<Texture2D>.Get("UI/KUA_ToggleFinishOff");
    public static readonly Texture2D KUA_ToggleFinishOffDisable = ContentFinder<Texture2D>.Get("UI/KUA_ToggleFinishOffDisable");

    [HarmonyPatch(nameof(Pawn.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Pawn __instance, ref IEnumerable<Gizmo> __result)
    {
        if(!__instance.Downed || __instance.Dead) return;

        List<Gizmo> gizmos = __result.ToList();
        Designation des = __instance.Map.designationManager.DesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation);

        if (des == null)
        {
            gizmos.Add( new Command_Action()
            {
                icon = KUA_ToggleFinishOff, defaultLabel = "KUA_ToggleFinishOff".Translate(), defaultDesc = "KUA_ToggleFinishOffDesc".Translate(), action = () =>
                {
                    __instance.Map.designationManager.AddDesignation(new Designation(__instance, KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation));
                }
            });
        }
        else
        {
            gizmos.Add( new Command_Action()
            {
                icon = KUA_ToggleFinishOffDisable, defaultLabel = "KUA_ToggleFinishOffDisable".Translate(), defaultDesc = "KUA_ToggleFinishOffDisableDesc".Translate(), action = () =>
                {
                    __instance.Map.designationManager.RemoveDesignation(des);
                }
            });
        }
        __result = gizmos;
    }
}
