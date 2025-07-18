﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class WorkGiver_FinishOff : WorkGiver_Scanner
{

    public static bool IsValidTarget(Pawn target, Pawn worker)
    {
        Designation des = target.Map.designationManager.DesignationOn(target, KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation) ?? target.Map.designationManager.DesignationOn(target, KeyzAllowUtilitesDefOf.KAU_StripFinishOffDesignation);
        if (des == null) return false;

        if (target.Downed && !target.Dead && !target.Map.reservationManager.IsReserved(target))
        {
            return true;
        }

        target.Map.designationManager.RemoveDesignation(des);
        return false;
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
            JobFailReason.Is("Not a pawn");
            return null;
        }

        if (!IsValidTarget(target, pawn))
        {
            JobFailReason.Is("Not friendly or downed", null);
            return null;
        }

        if (pawn.skills.GetSkill(SkillDefOf.Melee).Level < 5)
        {
            JobFailReason.Is("Melee too low", null);
            return null;
        }

        Pawn_MeleeVerbs meleeVerbs = pawn.meleeVerbs;
        Verb verb = meleeVerbs?.TryGetMeleeVerb(target);
        if (verb == null)return null;

        Designation des = target.Map.designationManager.DesignationOn(target, KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation) ?? target.Map.designationManager.DesignationOn(target, KeyzAllowUtilitesDefOf.KAU_StripFinishOffDesignation);

        JobDef jobDef = des.def == KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation ? KeyzAllowUtilitesDefOf.KAU_FinishOffPawn : KeyzAllowUtilitesDefOf.KAU_StripFinishOffPawn;

        Job job = JobMaker.MakeJob(jobDef, target);
        job.verbToUse = verb;
        job.killIncappedTarget = true;
        return job;
    }
}
