#if NET10_0_OR_GREATER
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities.HarmonyPatches;

/// <summary>
/// Runs after vanilla's auto-attack check for a waiting drafted pawn. If vanilla found nothing to
/// shoot and party hunt is on, the pawn shoots the nearest hunt-marked animal in range. The
/// conditions mirror vanilla's own ranged "fire at will" branch, so the pawn never moves.
/// </summary>
[HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
public static class JobDriver_Wait_PartyHunt_Patch
{
    [HarmonyPostfix]
    public static void Postfix(JobDriver_Wait __instance)
    {
        Pawn pawn = __instance.pawn;
        Job job = __instance.job;
        if (job == null || job.def != JobDefOf.Wait_Combat || !job.canUseRangedWeapon) return;
        if (!PartyHunt.IsEnabled(pawn) || !PartyHunt.CanUse(pawn)) return;
        if (!pawn.drafter.FireAtWill || pawn.Downed || pawn.stances.FullBodyBusy) return;
        // Vanilla already started an attack (warmup is a busy stance).
        if (pawn.stances.curStance is Stance_Busy) return;

        Pawn target = PartyHunt.FindTarget(pawn, pawn.CurrentEffectiveVerb);
        if (target != null) pawn.TryStartAttack(target);
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public static class Pawn_PartyHunt_Gizmo_Patch
{
    private static readonly System.Lazy<string> Label = new(() => "KAU_PartyHunt".Translate());
    private static readonly System.Lazy<string> Desc = new(() => "KAU_PartyHuntDesc".Translate());

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
    {
        if (!PartyHunt.CanUse(__instance)) return;
        __result = Append(__result, __instance);
    }

    private static IEnumerable<Gizmo> Append(IEnumerable<Gizmo> gizmos, Pawn pawn)
    {
        foreach (Gizmo g in gizmos) yield return g;
        yield return new Command_Toggle
        {
            icon = PartyHunt.Icon,
            defaultLabel = Label.Value,
            defaultDesc = Desc.Value,
            isActive = () => PartyHunt.IsEnabled(pawn),
            toggleAction = () => PartyHunt.SetEnabled(pawn, !PartyHunt.IsEnabled(pawn)),
        };
    }
}
#endif
