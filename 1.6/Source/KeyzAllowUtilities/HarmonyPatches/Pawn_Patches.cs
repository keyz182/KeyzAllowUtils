using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Pawn))]
[StaticConstructorOnStartup]
public static class Pawn_Patches
{
    public static readonly Texture2D KUA_ToggleFinishOff = ContentFinder<Texture2D>.Get("UI/KUA_ToggleFinishOff");
    public static readonly Texture2D KUA_ToggleFinishOffDisable = ContentFinder<Texture2D>.Get("UI/KUA_ToggleFinishOffDisable");

    public static Lazy<string> KUA_ToggleFinishOff_Label = new(() => "KUA_ToggleFinishOff".Translate());
    public static Lazy<string> KUA_ToggleFinishOffDesc_Label = new(() => "KUA_ToggleFinishOffDesc".Translate());
    public static Lazy<string> KUA_ToggleFinishOffDisable_Label = new(() => "KUA_ToggleFinishOffDisable".Translate());
    public static Lazy<string> KUA_ToggleFinishOffDisableDesc_Label = new(() => "KUA_ToggleFinishOffDisableDesc".Translate());

    public static Lazy<Designator_FinishOff> FinishOff = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_FinishOff>().FirstOrDefault());

    [HarmonyPatch(nameof(Pawn.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Pawn __instance, ref IEnumerable<Gizmo> __result)
    {
        if(KeyzAllowUtilitiesMod.settings.DisableFinishOff || !__instance.Downed || __instance.Dead || __instance.MapOrHolderMap() == null) return;
        if(__instance.Faction == Faction.OfPlayer || (__instance.guest != null && __instance.guest.HostFaction == Faction.OfPlayer)) return;

        bool hideGizmo = !KeyzAllowUtilitiesMod.settings.AllowFinishOffOnFriendly && !(__instance.Faction?.HostileTo(Faction.OfPlayer) ?? true) && !__instance.IsAnimal;

        List<Gizmo> gizmos = __result.ToList();
        Designation des = __instance.MapOrHolderMap().designationManager.DesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation);

        if (__instance.Faction is not { IsPlayer: true } && des == null)
        {
            if (!hideGizmo)
            {
                gizmos.Add(new Command_Action()
                {
                    icon = KUA_ToggleFinishOff,
                    defaultLabel = KUA_ToggleFinishOff_Label.Value,
                    defaultDesc = KUA_ToggleFinishOffDesc_Label.Value,
                    action = () =>
                    {
                        if (Event.current.shift)
                        {
                            Find.DesignatorManager.Select(FinishOff.Value);
                            return;
                        }
                        __instance.MapOrHolderMap().designationManager.AddDesignation(new Designation(__instance, KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation));
                    }
                });
            }
        }
        else
        {
            gizmos.Add( new Command_Action()
            {
                icon = KUA_ToggleFinishOffDisable, defaultLabel = KUA_ToggleFinishOffDisable_Label.Value, defaultDesc = KUA_ToggleFinishOffDisableDesc_Label.Value, action = () =>
                {
                    if (Event.current.shift)
                    {
                        Find.DesignatorManager.Select(FinishOff.Value);
                        return;
                    }
                    __instance.MapOrHolderMap().designationManager.RemoveDesignation(des);
                }
            });
        }
        __result = gizmos;
    }

    // [HarmonyPatch(nameof(Pawn.GetDisabledWorkTypes))]
    // [HarmonyPostfix]
    // public static void GetDisabledWorkTypes(Pawn __instance, List<WorkTypeDef> __result)
    // {
    //     SkillRecord melee = __instance?.skills?.GetSkill(SkillDefOf.Melee);
    //     if (melee == null || melee.levelInt < 5)
    //     {
    //         __result.Add(KeyzAllowUtilitesDefOf.KAU_FinishingOff);
    //     }
    // }
}
