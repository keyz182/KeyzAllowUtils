using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

/// <summary>
/// Implemented by mod designators whose "Clear all Designators" option must only clear the
/// designations that designator could itself have placed.
///
/// Vanilla's <see cref="Designator.RemoveAllDesignationsAffects"/> is protected, so the patch
/// below cannot reach it; this interface re-exposes it (plus the designation def to enumerate)
/// without reflection.
/// </summary>
public interface IClearsOwnDesignationsOnly
{
    /// <summary>The designation def whose entries this designator's clear-all should consider.</summary>
    DesignationDef ClearDesignation { get; }

    /// <summary>Mirrors the designator's own <c>RemoveAllDesignationsAffects</c>.</summary>
    bool AffectsForClear(LocalTargetInfo target);
}

/// <summary>
/// Corrects RimWorld's "Clear all Designators (N)" option for this mod's plant designators.
///
/// Vanilla counts and removes inconsistently: the displayed count is filtered by
/// <c>RemoveAllDesignationsAffects</c>, but the removal delegate deletes every designation of
/// that def. Because Chop Wood and Harvest both use <see cref="DesignationDefOf.HarvestPlant"/>
/// and are distinguished only by that predicate, clearing from a chop designator wipes crop
/// harvest designations too. This postfix swaps in an option where the count and the removal
/// use the same predicate.
///
/// Scoped deliberately to designators implementing <see cref="IClearsOwnDesignationsOnly"/> —
/// vanilla and other mods' designators are left untouched.
/// </summary>
[HarmonyPatch(typeof(Designator), nameof(Designator.RightClickFloatMenuOptions), MethodType.Getter)]
public static class Designator_RightClickFloatMenuOptions_Patch
{
    private static readonly Lazy<string> RemoveAllDesignationsLabel =
        new(() => "RemoveAllDesignations".Translate());

    [HarmonyPostfix]
    public static void Postfix(Designator __instance, ref IEnumerable<FloatMenuOption> __result)
    {
        if (__instance is not IClearsOwnDesignationsOnly clearer) return;
        if (__instance.Map?.designationManager == null || clearer.ClearDesignation == null) return;

        __result = Rebuild(__instance, clearer, __result);
    }

    private static IEnumerable<FloatMenuOption> Rebuild(
        Designator designator, IClearsOwnDesignationsOnly clearer, IEnumerable<FloatMenuOption> original)
    {
        // Vanilla labels both variants of its clear-all option with this prefix
        // ("... (N)" and "... (none)"), so a prefix match identifies the one to replace.
        string prefix = RemoveAllDesignationsLabel.Value;

        foreach (FloatMenuOption option in original)
        {
            if (option.Label == null || !option.Label.StartsWith(prefix))
            {
                yield return option;
            }
        }

        // designationsByDef is a DefMap, which is pre-sized for every def — the indexer always
        // returns a list, so there is no missing-key case to handle.
        DesignationManager manager = designator.Map.designationManager;
        List<Designation> affected =
            AffectedOnly(manager.designationsByDef[clearer.ClearDesignation], clearer.AffectsForClear);

        if (affected.Count == 0)
        {
            yield return new FloatMenuOption($"{prefix} ({"NoneLower".Translate()})", null);
            yield break;
        }

        yield return new FloatMenuOption($"{prefix} ({affected.Count})", () =>
        {
            foreach (Designation designation in affected)
            {
                manager.RemoveDesignation(designation);
            }
        });
    }

    /// <summary>
    /// Matches the scope of the non-wood harvest designators (Harvest Grown / Harvest All):
    /// plants that yield a harvestable product and are not chopped for wood. Used as their
    /// <c>RemoveAllDesignationsAffects</c> so clearing from them leaves tree-chop designations
    /// alone, mirroring the tree-only predicate the wood designators inherit.
    /// </summary>
    public static bool IsNonWoodHarvestable(LocalTargetInfo target)
    {
        return target.Thing?.def?.plant is { } plant
               && plant.harvestTag != "Wood"
               && plant.harvestedThingDef != null;
    }

    /// <summary>
    /// Snapshots the designations the predicate accepts. Materialised into a list so the
    /// count shown to the player and the set removed when they click are the same, and so
    /// removal does not mutate a collection it is iterating.
    /// </summary>
    public static List<Designation> AffectedOnly(
        IEnumerable<Designation> designations, Func<LocalTargetInfo, bool> affects)
    {
        List<Designation> result = [];
        if (designations == null) return result;

        foreach (Designation designation in designations)
        {
            if (affects(designation.target))
            {
                result.Add(designation);
            }
        }

        return result;
    }
}
