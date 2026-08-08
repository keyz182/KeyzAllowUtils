using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class WorkGiver_FinishOff : WorkGiver_Scanner
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false) => base.ShouldSkip(pawn, forced) || KeyzAllowUtilitiesMod.settings.DisableFinishOff;

    public static Designation GetOwnDesignation(Pawn target) =>
        target.Map.designationManager.DesignationOn(target, KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation) ??
        target.Map.designationManager.DesignationOn(target, KeyzAllowUtilitesDefOf.KAU_StripFinishOffDesignation);

    public static bool IsValidTarget(Pawn target, Pawn worker) => IsValidTarget(target, worker, out _);

    public static bool IsValidTarget(Pawn target, Pawn worker, out Designation designation)
    {
        designation = GetOwnDesignation(target);
        if (designation == null) return false;

        if (!target.Spawned || target.Dead || !target.Downed)
        {
            target.Map.designationManager.RemoveDesignation(designation);
            designation = null;
            return false;
        }

        // A pawn already working this target (or with no worker specified, e.g. a UI-only check)
        // must not be treated as unavailable — only genuinely-blocked targets (reserved by someone
        // else) are skipped, and skipping never deletes the order.
        return worker == null || worker.CanReserve(target);
    }

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Pawn target in pawn.Map.mapPawns.AllPawnsSpawned)
        {
            if (IsValidTarget(target, pawn))
            {
                yield return target;
            }
        }
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn target)
        {
            JobFailReason.Is("KAU_NotAPawn".Translate());
            return null;
        }

        if (!IsValidTarget(target, pawn, out Designation des))
        {
            JobFailReason.Is("KAU_NotFriendlyOrDowned".Translate(), null);
            return null;
        }

        if (!KeyzAllowUtilitiesMod.settings.DisableMeleeRequirementForFinishOff && pawn.skills.GetSkill(SkillDefOf.Melee).Level < 5)
        {
            JobFailReason.Is("Melee too low", null);
            return null;
        }

        Pawn_MeleeVerbs meleeVerbs = pawn.meleeVerbs;
        Verb verb = meleeVerbs?.TryGetMeleeVerb(target);
        if (verb == null)return null;

        JobDef jobDef = des.def == KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation ? KeyzAllowUtilitesDefOf.KAU_FinishOffPawn : KeyzAllowUtilitesDefOf.KAU_StripFinishOffPawn;

        Job job = JobMaker.MakeJob(jobDef, target);
        job.verbToUse = verb;
        job.killIncappedTarget = true;
        return job;
    }
}
