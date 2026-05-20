using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Thing))]
[StaticConstructorOnStartup]
public static class Thing_Patches
{
    public static readonly Texture2D KUA_MultiSelectIcon = ContentFinder<Texture2D>.Get("UI/KUA_MultiSelect");

    public static readonly Texture2D KUA_ToggleNoHaulIcon = ContentFinder<Texture2D>.Get("UI/KUA_ToggleNoHaul");
    public static readonly Texture2D KUA_ClaimAllDoorsIcon = ContentFinder<Texture2D>.Get("UI/KUA_ClaimAllDoors");

    public static readonly Texture2D KUA_ToggleHaulUrgentlyIcon = ContentFinder<Texture2D>.Get("UI/KUA_ToggleHaulUrgently");
    public static readonly Texture2D KUA_ToggleHaulUrgentlyDisableIcon = ContentFinder<Texture2D>.Get("UI/KUA_ToggleHaulUrgentlyDisable");

    public static Lazy<Designator_SelectSimilar> SelectDesignator = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_SelectSimilar>().FirstOrDefault());

    public static Lazy<string> KUA_MultiSelect = new(() => "KUA_MultiSelect".Translate());
    public static Lazy<string> KUA_MultiSelectDesc = new(() => "KUA_MultiSelectDesc".Translate());
    public static Lazy<string> KUA_SelectOnScreen = new(() => "KUA_SelectOnScreen".Translate());
    public static Lazy<string> KUA_SelectOnMap = new(() => "KUA_SelectOnMap".Translate());
    public static Lazy<string> KUA_SelectInRect = new(() => "KUA_SelectInRect".Translate());

    public static Lazy<string> KUA_ToggleNoHaulUrgently = new(() => "KUA_ToggleNoHaulUrgently".Translate());
    public static Lazy<string> KUA_ToggleNoHaulUrgentlyDesc = new(() => "KUA_ToggleNoHaulUrgentlyDesc".Translate());

    public static Lazy<string> KUA_ToggleNoHaulUrgentlyDisable = new(() => "KUA_ToggleNoHaulUrgentlyDisable".Translate());
    public static Lazy<string> KUA_ToggleNoHaulUrgentlyDisableDesc = new(() => "KUA_ToggleNoHaulUrgentlyDisableDesc".Translate());

    public static Lazy<string> KUA_ClaimAllDoors = new(() => "KUA_ClaimAllDoors".Translate());
    public static Lazy<string> KUA_ClaimAllDoorsDesc = new(() => "KUA_ClaimAllDoorsDesc".Translate());

    public static Lazy<string> KUA_ToggleHaulUrgently = new(() => "KUA_ToggleHaulUrgently".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyDesc = new(() => "KUA_ToggleHaulUrgentlyDesc".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyDisable = new(() => "KUA_ToggleHaulUrgentlyDisable".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyDisableDesc = new(() => "KUA_ToggleHaulUrgentlyDisableDesc".Translate());

    public static Lazy<string> KUA_ToggleHaulUrgentlyOnScreen = new(() => "KUA_ToggleHaulUrgentlyOnScreen".Translate());
    public static Lazy<string> KUA_ToggleHaulUrgentlyOnMap = new(() => "KUA_ToggleHaulUrgentlyOnMap".Translate());

    public static Lazy<Designator_HaulUrgently> HaulUrgently = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_HaulUrgently>().FirstOrDefault());

    [HarmonyPatch(nameof(Thing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Thing __instance, ref IEnumerable<Gizmo> __result)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableSelection
            && KeyzAllowUtilitiesMod.settings.DisableHaulUrgently
            && KeyzAllowUtilitiesMod.settings.DisableNoHauling
            && KeyzAllowUtilitiesMod.settings.DisableClaimAll)
            return;

        Map currentMap = __instance.MapOrHolderMap();
        if (currentMap == null)
            return;

        List<Gizmo> gizmos = __result.ToList();

        if (!KeyzAllowUtilitiesMod.settings.DisableSelection
            && !(KeyzAllowUtilitiesMod.settings.DisableSelectOnScreen
                 && KeyzAllowUtilitiesMod.settings.DisableSelectOnMap
                 && KeyzAllowUtilitiesMod.settings.DisableSelectInRect))
        {
            Command_Action command_Action = new()
            {
                icon = KUA_MultiSelectIcon,
                defaultLabel = KUA_MultiSelect.Value,
                defaultDesc = KUA_MultiSelectDesc.Value,
                action = () =>
                {
                    var settings = KeyzAllowUtilitiesMod.settings;
                    bool checkStuff = Event.current is not { shift: true }
                                      && __instance.def.MadeFromStuff;

                    List<FloatMenuOption> items = BuildSelectMenuItems(
                        __instance, currentMap, checkStuff, settings);

                    if (items.Count == 0)
                        return;

                    if (Event.current == null || Event.current.button == 0)
                    {
                        if (items.Count == 1)
                        {
                            items[0].action();
                        }
                        else if (!settings.DisableSelectInRect)
                        {
                            if (SelectDesignator.Value != null)
                                Find.DesignatorManager.Select(SelectDesignator.Value);
                        }
                        else
                        {
                            Find.WindowStack.Add(new FloatMenu(items));
                        }
                    }
                    else
                    {
                        Find.WindowStack.Add(new FloatMenu(items));
                    }
                }
            };
            gizmos.Add(command_Action);
        }

        if (!KeyzAllowUtilitiesMod.settings.DisableHaulUrgently && __instance is not Pawn && __instance.def.EverHaulable)
        {
            Designation des = __instance.MapOrHolderMap()?.designationManager?.DesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);

            if (des == null)
            {
                gizmos.Add(new Command_Action
                {
                    icon = KUA_ToggleHaulUrgentlyIcon,
                    defaultLabel = KUA_ToggleHaulUrgently.Value,
                    defaultDesc = KUA_ToggleHaulUrgentlyDesc.Value,
                    hotKey = KeyzAllowUtilitiesMod.settings.DisableAllShortcuts ? null : KeyzAllowUtilitesDefOf.KAU_HaulUrgently,
                    action = () =>
                    {
                        if (Event.current.shift)
                        {
                            Find.DesignatorManager.Select(HaulUrgently.Value);
                            return;
                        }
                        if (Event.current == null || Event.current.button == 0)
                        {
                            if (!__instance.IsInValidBestStorage() && currentMap.designationManager.DesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation) == null)
                            {
                                // Mutual exclusion: Haul Urgently wins over NoHaul
                                currentMap.designationManager.TryRemoveDesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);
                                HaulUrgently.Value.DesignateThing(__instance);
                            }
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
                gizmos.Add(new Command_Action
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

        if (!KeyzAllowUtilitiesMod.settings.DisableNoHauling && __instance is not Pawn && __instance.def.EverHaulable && __instance.Spawned)
        {
            Designation des = __instance.MapOrHolderMap()?.designationManager?.DesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);

            if (des == null)
            {
                gizmos.Add(new Command_Action
                {
                    icon = KUA_ToggleNoHaulIcon,
                    defaultLabel = KUA_ToggleNoHaulUrgently.Value,
                    defaultDesc = KUA_ToggleNoHaulUrgentlyDesc.Value,
                    action = () =>
                    {
                        // Mutual exclusion: NoHaul wins over Haul Urgently
                        currentMap.designationManager.TryRemoveDesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
                        currentMap.designationManager.AddDesignation(new Designation(__instance, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation));
                        // Cancel any in-flight haul jobs on this thing so the gizmo feels responsive
                        CancelHaulJobsTargeting(currentMap, __instance);
                    }
                });
            }
            else
            {
                gizmos.Add(new Command_Action
                {
                    icon = KUA_ToggleNoHaulIcon,
                    defaultLabel = KUA_ToggleNoHaulUrgentlyDisable.Value,
                    defaultDesc = KUA_ToggleNoHaulUrgentlyDisableDesc.Value,
                    action = () =>
                    {
                        currentMap.designationManager.RemoveDesignation(des);
                    }
                });
            }
        }

        if (!KeyzAllowUtilitiesMod.settings.DisableSelectStored && __instance is ISlotGroupParent slotGroupParent)
        {
            gizmos.Add(FilterUtils.MakeSelectStoredGizmo(slotGroupParent));
        }

        if (!KeyzAllowUtilitiesMod.settings.DisableClaimAll && __instance is Building_Door door)
        {
            // Only show the "Claim all doors" button when the selected door is unclaimed
            if (door.ClaimableBy(Faction.OfPlayer))
            {
                gizmos.Add(new Command_Action
                {
                    icon = KUA_ClaimAllDoorsIcon,
                    defaultLabel = KUA_ClaimAllDoors.Value,
                    defaultDesc = KUA_ClaimAllDoorsDesc.Value,
                    action = () =>
                    {
                        List<Building_Door> claimableDoors = __instance.Map.listerBuildings.allBuildingsNonColonist.OfType<Building_Door>().Where(d => d.ClaimableBy(Faction.OfPlayer)).ToList();

                        foreach (Building_Door claimableDoor in claimableDoors)
                        {
                            claimableDoor.SetFaction(Faction.OfPlayer);
                            foreach (IntVec3 cell in claimableDoor.OccupiedRect())
                                FleckMaker.ThrowMetaPuffs(new TargetInfo(cell, claimableDoor.Map));
                        }

                        Messages.Message("KUA_ClaimedDoors".Translate(claimableDoors.Count), MessageTypeDefOf.PositiveEvent);
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
            FilterUtils.SelectAnyOnScreen(__instance.MapOrHolderMap(), __instance.Position, Filter);
            int n = 0;
            foreach (Thing thing in Find.Selector.SelectedObjects.OfType<Thing>())
            {
                if (!thing.IsInValidBestStorage() && !thing.MapOrHolderMap().designationManager.HasMapDesignationOn(thing))
                {
                    thing.MapOrHolderMap().designationManager.AddDesignation(new Designation(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
                    // See Designator_HaulUrgently.DesignateThing — issue #24: do not add vanilla
                    // DesignationDefOf.Haul. ListerHaulables already tracks free-standing haulables.
                    n++;
                }
            }
            Find.Selector.ClearSelection();
            Plant_Patches.ReportDesignated(n);
        }));
        items.Add(new FloatMenuOption(KUA_ToggleHaulUrgentlyOnMap.Value, () =>
        {
            __instance.MapOrHolderMap().SelectAnyOnMap(__instance.Position, Filter);
            int n = 0;
            foreach (Thing thing in Find.Selector.SelectedObjects.OfType<Thing>())
            {
                if (!thing.IsInValidBestStorage() && !thing.MapOrHolderMap().designationManager.HasMapDesignationOn(thing))
                {
                    thing.MapOrHolderMap().designationManager.AddDesignation(new Designation(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
                    // See Designator_HaulUrgently.DesignateThing — issue #24: do not add vanilla
                    // DesignationDefOf.Haul. ListerHaulables already tracks free-standing haulables.
                    n++;
                }
            }
            Find.Selector.ClearSelection();
            Plant_Patches.ReportDesignated(n);
        }));

        Find.WindowStack.Add(new FloatMenu(items));

        return;

        bool Filter(Thing thing)
        {
            return !thing.def.designateHaulable && thing.def.EverHaulable && thing is not Building;
        }
    }

    private static List<FloatMenuOption> BuildSelectMenuItems(
        Thing thing, Map map, bool checkStuff, Settings settings)
    {
        List<FloatMenuOption> items = [];
        IEnumerable<Thing> selected = Find.Selector.SelectedObjects.OfType<Thing>();

        if (!settings.DisableSelectOnScreen)
        {
            string label = checkStuff
                ? "KUA_SelectOnScreenWithStuff".Translate(thing.Stuff.LabelAsStuff)
                : KUA_SelectOnScreen.Value;
            items.Add(new FloatMenuOption(label, () =>
                FilterUtils.SelectOnScreen(thing, checkStuff, selected)));
        }

        if (!settings.DisableSelectOnMap)
        {
            string label = checkStuff
                ? "KUA_SelectOnMapWithStuff".Translate(thing.Stuff.LabelAsStuff)
                : KUA_SelectOnMap.Value;
            items.Add(new FloatMenuOption(label, () =>
                map.SelectOnMap(thing, checkStuff, selected)));
        }

        if (!settings.DisableSelectInRect)
        {
            items.Add(new FloatMenuOption(KUA_SelectInRect.Value, () =>
            {
                if (SelectDesignator.Value != null)
                    Find.DesignatorManager.Select(SelectDesignator.Value);
            }));
        }

        return items;
    }

    /// <summary>
    /// Cancel any in-flight haul jobs targeting <paramref name="thing"/> on <paramref name="map"/>.
    /// Called when the user toggles "Do Not Haul" on a thing — without this, a pawn that has
    /// already been assigned a HaulToCell/HaulToContainer job will finish hauling the item to
    /// storage before the new designation takes effect, which feels broken to the player.
    /// </summary>
    private static void CancelHaulJobsTargeting(Map map, Thing thing)
    {
        if (map?.mapPawns == null) return;
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            Job job = pawn.CurJob;
            if (job == null) continue;
            if (job.def != JobDefOf.HaulToCell && job.def != JobDefOf.HaulToContainer) continue;

            bool targets = job.targetA.Thing == thing;
            if (!targets && job.targetQueueA != null)
            {
                foreach (LocalTargetInfo t in job.targetQueueA)
                {
                    if (t.Thing == thing) { targets = true; break; }
                }
            }
            if (targets)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
