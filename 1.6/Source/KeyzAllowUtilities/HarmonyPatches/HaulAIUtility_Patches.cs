using HarmonyLib;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities.HarmonyPatches;

/// <summary>
/// Consumer side of the "Do Not Haul" feature.
///
/// The gizmo in <see cref="Thing_Patches"/> toggles <c>KAU_NoHaulDesignation</c> on a Thing,
/// but on its own that designation does nothing — vanilla haul WorkGivers do not look at it.
/// This patch is what actually blocks hauling.
///
/// We patch <see cref="HaulAIUtility.PawnCanAutomaticallyHaulFast"/> rather than the slow
/// variant because it is the lowest common denominator: <c>PawnCanAutomaticallyHaul</c> calls
/// Fast internally, the mod's own <c>WorkGiver_HaulUrgently</c> calls Fast directly, and
/// Pick Up And Haul's <c>WorkGiver_HaulToInventory</c> calls Fast in three places
/// (<c>HasJobOnThing</c>, <c>JobOnThing</c>, and its inner Validator). One postfix covers all
/// haul WorkGivers we care about.
///
/// Recipe / toolbench ingredient pickup goes through <c>WorkGiver_DoBill</c>, which does NOT
/// route through <see cref="HaulAIUtility"/>, so resources marked Do Not Haul will still be
/// picked up for crafting recipes — matching the feature's documented contract:
/// "This does not prevent items being hauled to work — e.g. resources to a toolbench."
/// </summary>
[HarmonyPatch(typeof(HaulAIUtility))]
public static class HaulAIUtility_Patches
{
    [HarmonyPatch(nameof(HaulAIUtility.PawnCanAutomaticallyHaulFast))]
    [HarmonyPostfix]
    public static void PawnCanAutomaticallyHaulFast_Postfix(Pawn p, Thing t, bool forced, ref bool __result)
    {
        // Feature respects the same kill switch as the gizmo. Defensive null-coalesce mirrors
        // existing patches in the project — settings can briefly be null at startup.
        if (KeyzAllowUtilitiesMod.settings?.DisableNoHauling ?? true) return;

        // Nothing to do if the caller already decided no.
        if (!__result) return;

        // MapHeld covers items inside containers / pawn inventory; gizmo gates on Spawned so
        // designations are only ever added against spawned items, but be defensive on read.
        DesignationManager dm = t?.MapHeld?.designationManager;
        if (dm == null) return;

        // Performance: SpawnedDesignationsOfDef hits a per-def index in vanilla — this is O(1)
        // when no NoHaul designations exist on the map (the common case for most colonies).
        // PawnCanAutomaticallyHaulFast is on a hot path (every haulable × every pawn during
        // work-giver scans), so the empty-map fast path matters.
        foreach (Designation d in dm.SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_NoHaulDesignation))
        {
            if (d.target.Thing == t)
            {
                __result = false;
                return;
            }
        }
    }
}
