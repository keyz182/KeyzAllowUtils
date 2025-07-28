using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Thing))]
[StaticConstructorOnStartup]
public static class Thing_Patches
{
    public static readonly Texture2D KUA_MultiSelectIcon = ContentFinder<Texture2D>.Get("UI/KUA_MultiSelect");

    public static readonly Texture2D KUA_ToggleHaulUrgentlyIcon = ContentFinder<Texture2D>.Get("UI/KUA_ToggleHaulUrgently");
    public static readonly Texture2D KUA_ToggleHaulUrgentlyDisableIcon = ContentFinder<Texture2D>.Get("UI/KUA_ToggleHaulUrgentlyDisable");

    public static Lazy<Designator_SelectSimilar> SelectDesignator = new(()=>DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_SelectSimilar>().FirstOrDefault());

    public static Lazy<string> KUA_MultiSelect = new(() => "KUA_MultiSelect".Translate());
    public static Lazy<string> KUA_MultiSelectDesc = new(() => "KUA_MultiSelectDesc".Translate());
    public static Lazy<string> KUA_SelectOnScreen = new(() => "KUA_SelectOnScreen".Translate());
    public static Lazy<string> KUA_SelectOnMap = new(() => "KUA_SelectOnMap".Translate());
    public static Lazy<string> KUA_SelectInRect = new(() => "KUA_SelectInRect".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgently = new(() => "KUA_ToggleHaulUrgently".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyDesc = new(() => "KUA_ToggleHaulUrgentlyDesc".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyDisable = new(() => "KUA_ToggleHaulUrgentlyDisable".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyDisableDesc = new(() => "KUA_ToggleHaulUrgentlyDisableDesc".Translate());

    public static Lazy<string> KUA_ToggleHaulUrgentlyOnScreen = new(() => "KUA_ToggleHaulUrgentlyOnScreen".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyOnMap = new(() => "KUA_ToggleHaulUrgentlyOnMap".Translate());


    [HarmonyPatch(nameof(Thing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Thing __instance, ref IEnumerable<Gizmo> __result)
    {
        List<Gizmo> gizmos = __result.ToList();
        Map currentMap = __instance.MapOrHolderMap();
        if(currentMap == null) return;

        Command_Action command_Action = new()
        {
            icon = KUA_MultiSelectIcon,
            defaultLabel = KUA_MultiSelect.Value,
            defaultDesc = KUA_MultiSelectDesc.Value,
            action = () =>
            {
                if (Event.current == null || Event.current.button == 0)
                {
                    if(SelectDesignator != null)
                        Find.DesignatorManager.Select(SelectDesignator.Value);
                }
                else
                {
                    List<FloatMenuOption> items = [];

                    if (Event.current.shift || !__instance.def.MadeFromStuff)
                    {
                        items.Add(new FloatMenuOption(KUA_SelectOnScreen.Value, () =>
                        {
                            FilterUtils.SelectOnScreen(__instance);
                        }));
                        items.Add(new FloatMenuOption(KUA_SelectOnMap.Value, () =>
                        {
                            currentMap.SelectOnMap(__instance);
                        }));
                        items.Add(new FloatMenuOption(KUA_SelectInRect.Value, () =>
                        {
                            if (SelectDesignator != null)
                                Find.DesignatorManager.Select(SelectDesignator.Value);
                        }));
                    }
                    else
                    {
                        items.Add(new FloatMenuOption("KUA_SelectOnScreenWithStuff".Translate(__instance.Stuff.LabelAsStuff), () =>
                        {
                            FilterUtils.SelectOnScreen(__instance, __instance.Stuff);
                        }));
                        items.Add(new FloatMenuOption("KUA_SelectOnMapWithStuff".Translate(__instance.Stuff.LabelAsStuff), () =>
                        {
                            currentMap.SelectOnMap(__instance, __instance.Stuff);
                        }));
                        items.Add(new FloatMenuOption(KUA_SelectInRect.Value, () =>
                        {
                            if (SelectDesignator != null)
                                Find.DesignatorManager.Select(SelectDesignator.Value);
                        }));
                    }

                    Find.WindowStack.Add(new FloatMenu(items));
                }
            }
        };
        gizmos.Add(command_Action);


        if (!KeyzAllowUtilitiesMod.settings.DisableHaulUrgently && __instance is not Pawn && __instance.def.EverHaulable)
        {
            Designation des = __instance.MapOrHolderMap()?.designationManager?.DesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);

            if (des == null)
            {
                gizmos.Add( new Command_Action
                {
                    icon = KUA_ToggleHaulUrgentlyIcon,
                    defaultLabel = KUA_ToggleHaulUrgently.Value,
                    defaultDesc = KUA_ToggleHaulUrgentlyDesc.Value,
                    hotKey = KeyzAllowUtilitiesMod.settings.DisableAllShortcuts ? null : KeyzAllowUtilitesDefOf.KAU_HaulUrgently,
                    action = () =>
                    {
                        if (Event.current == null || Event.current.button == 0)
                        {
                            if (!__instance.IsInValidBestStorage() && !currentMap.designationManager.HasMapDesignationOn(__instance))
                                currentMap.designationManager.AddDesignation(new Designation(__instance, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
                        }
                        else
                        {
                            HaulUrgentlyRightClick(__instance);
                        }
                    }
                });
            }
            else
            {
                gizmos.Add( new Command_Action
                {
                    icon = KUA_ToggleHaulUrgentlyDisableIcon,
                    defaultLabel = KUA_ToggleHaulUrgentlyDisable.Value,
                    defaultDesc = KUA_ToggleHaulUrgentlyDisableDesc.Value,
                    hotKey = KeyzAllowUtilitiesMod.settings.DisableAllShortcuts ? null : KeyzAllowUtilitesDefOf.KAU_HaulUrgently,
                    action = () =>
                    {
                        if (Event.current == null || Event.current.button == 0)
                        {
                            currentMap.designationManager.RemoveDesignation(des);
                        }
                        else
                        {
                            HaulUrgentlyRightClick(__instance);
                        }
                    }
                });
            }
        }

        __result = gizmos;
    }

    public static void HaulUrgentlyRightClick(Thing __instance)
    {
        List<FloatMenuOption> items = [];

        items.Add(new FloatMenuOption(KUA_ToggleHaulUrgentlyOnScreen.Value, () =>
        {
            FilterUtils.SelectAnyOnScreen(__instance.MapOrHolderMap(), __instance.Position, filter:Filter);
            foreach (Thing thing in Find.Selector.SelectedObjects.OfType<Thing>())
            {
                if (!thing.IsInValidBestStorage() && !thing.MapOrHolderMap().designationManager.HasMapDesignationOn(thing))
                    thing.MapOrHolderMap().designationManager.AddDesignation(new Designation(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
            }
            Find.Selector.ClearSelection();
        }));
        items.Add(new FloatMenuOption(KUA_ToggleHaulUrgentlyOnMap.Value, () =>
        {
            __instance.MapOrHolderMap().SelectAnyOnMap(__instance.Position, filter:Filter);
            foreach (Thing thing in Find.Selector.SelectedObjects.OfType<Thing>())
            {
                if (!thing.IsInValidBestStorage() && !thing.MapOrHolderMap().designationManager.HasMapDesignationOn(thing))
                    thing.MapOrHolderMap().designationManager.AddDesignation(new Designation(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
            }
            Find.Selector.ClearSelection();
        }));

        Find.WindowStack.Add(new FloatMenu(items));

        return;

        bool Filter(Thing thing)
        {
            return !thing.def.designateHaulable && thing.def.EverHaulable && thing is not Building;
        }
    }
}
