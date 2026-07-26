using System;
using System.Collections.Generic;
using System.Linq;
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

    public static Lazy<string> KUA_SelectRotting = new(() => "KUA_SelectRotting".Translate());
    public static Lazy<string> KUA_SelectRottingDesc = new(() => "KUA_SelectRottingDesc".Translate());
    public static Lazy<string> KUA_SelectRottingOnScreen = new(() => "KUA_SelectRottingOnScreen".Translate());
    public static Lazy<string> KUA_SelectRottingOnMap = new(() => "KUA_SelectRottingOnMap".Translate());

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

        if (!KeyzAllowUtilitiesMod.settings.DisableSelection
            && !KeyzAllowUtilitiesMod.settings.DisableSelectRotting
            && __instance is Corpse
            && IsRotting(__instance))
        {
            gizmos.Add(new Command_Action
            {
                icon = KUA_MultiSelectIcon,
                defaultLabel = KUA_SelectRotting.Value,
                defaultDesc = KUA_SelectRottingDesc.Value,
                action = () =>
                {
                    if (Event.current == null || Event.current.button == 0)
                    {
                        FilterUtils.SelectAnyOnScreen(currentMap, __instance.Position, IsRotting);
                    }
                    else
                    {
                        SelectRottingRightClick(__instance);
                    }
                }
            });
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
                        // Event.current is read here, before any synced call, and never inside
                        // HaulUrgentlyActions — Multiplayer replays synced methods with
                        // Event.current null or different, so branch selection must happen on
                        // the initiating client only.
                        bool shift = Event.current is { shift: true };
                        int button = Event.current?.button ?? 0;
                        if (shift)
                        {
                            Find.DesignatorManager.Select(HaulUrgently.Value);
                            return;
                        }
                        if (button == 0)
                        {
                            HaulUrgentlyActions.DesignateHaulUrgently(__instance);
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
                        int button = Event.current?.button ?? 0;
                        if (button == 0)
                        {
                            HaulUrgentlyActions.CancelHaulUrgently(__instance);
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
        // The target set is resolved here, on the clicking client, from that client's own
        // camera/selection state, then transmitted to HaulUrgentlyActions.DesignateHaulUrgentlyBulk
        // as a plain List<Thing> — Multiplayer has no way to re-derive a camera-dependent set
        // identically on every client, so the resolved set itself is the synced argument.
        List<FloatMenuOption> items =
        [
            new FloatMenuOption(KUA_ToggleHaulUrgentlyOnScreen.Value, () =>
                HaulUrgentlyActions.DesignateHaulUrgentlyBulk(
                    __instance.MapOrHolderMap(), HaulUrgentlyActions.ResolveTargetsOnScreen(__instance))),
            new FloatMenuOption(KUA_ToggleHaulUrgentlyOnMap.Value, () =>
                HaulUrgentlyActions.DesignateHaulUrgentlyBulk(
                    __instance.MapOrHolderMap(), HaulUrgentlyActions.ResolveTargetsOnMap(__instance))),
        ];

        Find.WindowStack.Add(new FloatMenu(items));
    }

    /// <summary>
    /// True when <paramref name="thing"/> is a corpse that has begun to rot (Rotting or Dessicated).
    /// See issue #26 — used to bulk-select decayed corpses for destruction while leaving
    /// fresh ones to be hauled away.
    /// </summary>
    private static bool IsRotting(Thing thing)
    {
        return thing is Corpse
               && thing.TryGetComp<CompRottable>() is { } rot
               && rot.Stage != RotStage.Fresh;
    }

    public static void SelectRottingRightClick(Thing __instance)
    {
        List<FloatMenuOption> items =
        [
            new(KUA_SelectRottingOnScreen.Value, () =>
                FilterUtils.SelectAnyOnScreen(__instance.MapOrHolderMap(), __instance.Position, IsRotting)),
            new(KUA_SelectRottingOnMap.Value, () =>
                __instance.MapOrHolderMap().SelectAnyOnMap(__instance.Position, IsRotting))
        ];

        Find.WindowStack.Add(new FloatMenu(items));
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
