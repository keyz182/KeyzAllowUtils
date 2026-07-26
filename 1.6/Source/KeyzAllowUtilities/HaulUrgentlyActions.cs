using System.Collections.Generic;
using System.Linq;
using KeyzAllowUtilities.HarmonyPatches;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

/// <summary>
/// Named, RimWorld-Multiplayer-serializable entry points for the HaulUrgently gizmo and its
/// bulk "on screen"/"on map" float-menu options. Extracted out of
/// <see cref="HarmonyPatches.Thing_Patches"/> so the optional Compatibility/rwmt.Multiplayer
/// assembly can register them with <c>MP.RegisterSyncMethod</c> without having to reach into
/// anonymous lambdas (MP matches sync targets by declared name, and named static methods with
/// primitive/serializable parameters are what its API expects).
///
/// The three "synced" methods below are pure functions of their arguments: they read no
/// <c>Event.current</c>, <c>Find.Selector</c>, <c>UI.screen*</c>, or <c>Find.CurrentMap</c>.
/// Multiplayer replays them on every client from the serialized arguments alone, so all state
/// they act on must arrive as an argument, and each must be re-validated on entry — the world
/// may have moved on between the initiating client's click and a replaying client's turn.
///
/// With Multiplayer absent (or the compat assembly not loaded) these are simply called directly;
/// nothing here depends on Multiplayer being present.
/// </summary>
public static class HaulUrgentlyActions
{
    /// <summary>
    /// Synced. Designates a single thing haul-urgently, matching
    /// <see cref="Designator_HaulUrgently.DesignateThing"/>'s effect. Clears any NoHaul
    /// designation first — Haul Urgently wins the mutual exclusion between the two.
    /// </summary>
    public static void DesignateHaulUrgently(Thing thing)
    {
        if (thing == null || thing.Destroyed) return;
        Map map = thing.MapOrHolderMap();
        if (map == null) return;
        if (thing.IsInValidBestStorage()) return;
        if (map.designationManager.DesignationOn(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation) != null) return;

        map.designationManager.TryRemoveDesignationOn(thing, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);
        map.designationManager.AddDesignation(new Designation(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
        thing.SetForbidden(false, false);
    }

    /// <summary>
    /// Synced. Cancels a haul-urgently designation on a thing. Takes the <see cref="Thing"/>
    /// itself, not the <see cref="Designation"/>, and re-resolves it on entry — the designation
    /// instance from the initiating client's click is not what a replaying client holds.
    /// </summary>
    public static void CancelHaulUrgently(Thing thing)
    {
        if (thing == null || thing.Destroyed) return;
        Map map = thing.MapOrHolderMap();
        if (map == null) return;

        Designation des = map.designationManager.DesignationOn(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
        if (des == null) return;

        map.designationManager.RemoveDesignation(des);
    }

    /// <summary>
    /// Synced. Bulk-designates a resolved set of things haul-urgently — the target of the
    /// "on screen" / "on map" float-menu options. <paramref name="things"/> must already be
    /// resolved by the initiating client (see <see cref="ResolveTargetsOnScreen"/> /
    /// <see cref="ResolveTargetsOnMap"/>) and transmitted as-is: re-deriving the set on a
    /// replaying client would use that client's own camera/selection state and produce a
    /// different set.
    /// </summary>
    public static void DesignateHaulUrgentlyBulk(Map map, List<Thing> things)
    {
        if (map == null || things == null) return;

        int n = 0;
        foreach (Thing t in things)
        {
            if (t == null || t.Destroyed) continue; // MP deserializes a destroyed Thing as null
            Map m = t.MapOrHolderMap();
            if (m != map) continue;
            if (t.IsInValidBestStorage()) continue;
            if (m.designationManager.HasMapDesignationOn(t)) continue;

            m.designationManager.TryRemoveDesignationOn(t, KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation);
            m.designationManager.AddDesignation(new Designation(t, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
            n++;
        }

        Plant_Patches.ReportDesignated(n);
    }

    /// <summary>
    /// NOT synced — resolves the "on screen" bulk target set on the initiating client only.
    /// Mirrors <see cref="FilterUtils.SelectAnyOnScreen"/> but returns the list instead of
    /// driving <c>Find.Selector</c>, so the resolved set can be transmitted as a synced
    /// argument rather than re-derived from each client's own camera position.
    /// </summary>
    public static List<Thing> ResolveTargetsOnScreen(Thing origin)
    {
        Map map = origin.MapOrHolderMap();
        if (map == null) return [];

        return map.ThingsOnScreen(HaulUrgentlyFilter)
            .NotFogged()
            .NearestTo(origin.Position)
            .Take(KeyzAllowUtilitiesMod.settings.MaxSelect)
            .ToList();
    }

    /// <summary>
    /// NOT synced — resolves the "on map" bulk target set on the initiating client only.
    /// Mirrors <see cref="FilterUtils.SelectAnyOnMap"/> but returns the list instead of driving
    /// <c>Find.Selector</c>.
    /// </summary>
    public static List<Thing> ResolveTargetsOnMap(Thing origin)
    {
        Map map = origin.MapOrHolderMap();
        if (map == null) return [];

        return map.Selectables(HaulUrgentlyFilter)
            .NearestTo(origin.Position)
            .NotFogged()
            .Take(KeyzAllowUtilitiesMod.settings.MaxSelect)
            .ToList();
    }

    /// <summary>The bulk-select filter: haulable things not already covered by the single-item designator.</summary>
    public static bool HaulUrgentlyFilter(Thing thing) =>
        !thing.def.designateHaulable && thing.def.EverHaulable && thing is not Building;
}
