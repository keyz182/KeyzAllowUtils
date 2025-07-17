using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class JobDriver_FinishOff : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, false, false);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        AddFailCondition(JobHasFailed);
        yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);
        Thing skullMote = null;

        yield return new Toil
        {
            initAction = delegate
            {
                Pawn victim = job.targetA.Thing as Pawn;
                skullMote = TryMakeSkullMote(victim, 0.25f);
                KeyzAllowUtilitesDefOf.KAU_WeaponGlint.Spawn().Trigger(pawn, job.targetA.Thing, -1);
            },
            defaultDuration = 60,
            defaultCompleteMode = ToilCompleteMode.Delay
        };

        yield return new Toil
        {
            initAction = delegate
            {
                if (job.targetA.Thing is not Pawn victim || job.verbToUse == null) return;

                job.verbToUse.TryStartCastOn(victim, false, true, false, false);

                if (!victim.HostileTo(Faction.OfPlayer))
                {
                    ThoughtUtility.GiveThoughtsForPawnExecuted(victim, pawn, PawnExecutionKind.GenericBrutal);
                }

                if (victim.RaceProps is { intelligence: Intelligence.Animal })
                {
                    pawn.records.Increment(RecordDefOf.AnimalsSlaughtered);
                }

                if (victim.IsPrisonerOfColony)
                {
                    TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, pawn, victim);
                }

                DoExecution(pawn, victim);

                if (skullMote is { Destroyed: false })
                {
                    skullMote.Destroy(DestroyMode.Vanish);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }

    private void DoExecution(Pawn slayer, Pawn victim)
    {
        int bloodAmount = Mathf.Max(GenMath.RoundRandom(victim.BodySize * 8f), 1);
        for (int i = 0; i < bloodAmount; i++)
        {
            victim.health.DropBloodFilth();
        }

        BodyPartRecord bodyPartRecord = victim.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).FirstOrDefault();
        int damageAmount = ((bodyPartRecord != null) ? Mathf.Clamp((int) victim.health.hediffSet.GetPartHealth(bodyPartRecord) - 1, 1, 20) : 20);
        DamageInfo damageInfo = new(DamageDefOf.ExecutionCut, damageAmount, -1f, -1f, slayer, bodyPartRecord, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true,
            true, QualityCategory.Normal, true);
        victim.TakeDamage(damageInfo);

        if (!victim.Dead)
        {
            victim.Kill(damageInfo, null);
        }
    }

    private Thing TryMakeSkullMote(Pawn victim, float chance)
    {
        if (victim?.RaceProps is not { intelligence: Intelligence.Humanlike }) return null;

        if (!Rand.Chance(chance))return null;

        ThingDef mote_ThoughtBad = ThingDefOf.Mote_ThoughtBad;
        MoteBubble moteBubble = (MoteBubble) ThingMaker.MakeThing(mote_ThoughtBad, null);
        moteBubble.SetupMoteBubble(ThoughtDefOf.WitnessedDeathAlly.Icon, null, null);
        moteBubble.Attach(victim);
        return GenSpawn.Spawn(moteBubble, victim.Position, victim.Map, WipeMode.Vanish);
    }

    private bool JobHasFailed()
    {
        Pawn victim = TargetThingA as Pawn;
        return victim is not { Spawned: true } || !victim.HostileTo(pawn.Faction) || victim.Dead || !victim.Downed;// || !HugsLibUtility.HasDesignation(pawn, AllowToolDefOf.FinishOffDesignation);
    }

    private const int PrepareSwingDuration = 60;
    private const float VictimSkullMoteChance = 0.25f;
    private const float OpportunityTargetMaxRange = 8f;
}
