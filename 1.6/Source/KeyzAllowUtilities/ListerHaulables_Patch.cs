using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

[HarmonyPatch(typeof(ListerHaulables))]
public static class ListerHaulables_Patch
{
    public static Dictionary<Map, ListerUrgentHaulables> Listers = new();

    public static Lazy<FieldInfo> Map = new(() => AccessTools.Field(typeof(ListerHaulables), "map"));
    public static ListerUrgentHaulables GetListerForMap(Map map)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableHaulUrgently) return null;
        if (map == null) return null;
        if (Listers.TryGetValue(map, out ListerUrgentHaulables forMap)) return forMap;

        ListerUrgentHaulables lister = new(map);
        Listers[map] = lister;
        return lister;
    }

    public static ListerUrgentHaulables GetListerForLister(ListerHaulables __instance)
    {
        Map map = Map.Value.GetValue(__instance) as Map;
        if (map == null) return null;
        return GetListerForMap(map);
    }

    [HarmonyPatch(nameof(ListerHaulables.HaulDesignationAdded))]
    [HarmonyPostfix]
    public static void HaulDesignationAdded_Patch(ListerHaulables __instance, Thing t)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.HaulDesignationAdded(t);
    }

    [HarmonyPatch(nameof(ListerHaulables.HaulDesignationRemoved))]
    [HarmonyPostfix]
    public static void HaulDesignationRemoved(ListerHaulables __instance, Thing t)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.HaulDesignationRemoved(t);
    }

    [HarmonyPatch(nameof(ListerHaulables.Notify_Spawned))]
    [HarmonyPostfix]
    public static void Notify_Spawned(ListerHaulables __instance, Thing t)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.Notify_Spawned(t);
    }

    [HarmonyPatch(nameof(ListerHaulables.Notify_DeSpawned))]
    [HarmonyPostfix]
    public static void Notify_DeSpawned(ListerHaulables __instance, Thing t)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.Notify_DeSpawned(t);
    }

    [HarmonyPatch(nameof(ListerHaulables.Notify_Unforbidden))]
    [HarmonyPostfix]
    public static void Notify_Unforbidden(ListerHaulables __instance, Thing t)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.Notify_Unforbidden(t);
    }

    [HarmonyPatch(nameof(ListerHaulables.Notify_Forbidden))]
    [HarmonyPostfix]
    public static void Notify_Forbidden(ListerHaulables __instance, Thing t)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.Notify_Forbidden(t);
    }

    [HarmonyPatch(nameof(ListerHaulables.Notify_AddedThing))]
    [HarmonyPostfix]
    public static void Notify_AddedThing(ListerHaulables __instance, Thing t)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.Notify_AddedThing(t);
    }

    [HarmonyPatch(nameof(ListerHaulables.Notify_SlotGroupChanged))]
    [HarmonyPostfix]
    public static void Notify_SlotGroupChanged(ListerHaulables __instance, SlotGroup sg)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.Notify_SlotGroupChanged(sg);
    }

    [HarmonyPatch(nameof(ListerHaulables.Notify_HaulSourceChanged))]
    [HarmonyPostfix]
    public static void Notify_HaulSourceChanged(ListerHaulables __instance, IHaulSource holder)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.Notify_HaulSourceChanged(holder);
    }

    [HarmonyPatch(nameof(ListerHaulables.ListerHaulablesTick))]
    [HarmonyPostfix]
    public static void ListerHaulablesTick(ListerHaulables __instance)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.ListerUrgentHaulablesTick();
    }

    [HarmonyPatch(nameof(ListerHaulables.RecalcAllInCell))]
    [HarmonyPostfix]
    public static void RecalcAllInCell(ListerHaulables __instance, IntVec3 c)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.RecalcAllInCell(c);
    }

    [HarmonyPatch(nameof(ListerHaulables.RecalcAllInCells))]
    [HarmonyPostfix]
    public static void RecalcAllInCells(ListerHaulables __instance, IEnumerable<IntVec3> cells)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.RecalcAllInCells(cells);
    }

    [HarmonyPatch(nameof(ListerHaulables.RecalculateAllInHaulSources))]
    [HarmonyPostfix]
    public static void RecalculateAllInHaulSources(ListerHaulables __instance, IList<IHaulSource> sources)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.RecalculateAllInHaulSources(sources);
    }

    [HarmonyPatch(nameof(ListerHaulables.RecalculateAllInHaulSource))]
    [HarmonyPostfix]
    public static void RecalculateAllInHaulSource(ListerHaulables __instance, IHaulSource source)
    {
        ListerUrgentHaulables lister = GetListerForLister(__instance);
        lister?.RecalculateAllInHaulSource(source);
    }

}
