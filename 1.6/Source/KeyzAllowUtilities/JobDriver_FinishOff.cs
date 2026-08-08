using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class JobDriver_FinishOff : JobDriver
{
    // The right-click float menu (KUAFloatMenu) orders this job without ever placing a
    // designation, so the designation check below only applies when one existed at job start —
    // otherwise a float-menu-ordered kill would fail its first tick.
    private bool _hadDesignationAtStart;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        _hadDesignationAtStart = job.targetA.Thing is Pawn victim && WorkGiver_FinishOff.GetOwnDesignation(victim) != null;
        return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, false, false);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        AddFailCondition(JobHasFailed);
        yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);
        Thing skullMote = null;
        Effecter weaponGlint = null;

        yield return new Toil
        {
            initAction = delegate
            {
                Pawn victim = job.targetA.Thing as Pawn;
                try
                {
                    skullMote = TryMakeSkullMote(victim, 0.25f);
                    weaponGlint = KeyzAllowUtilitesDefOf.KAU_WeaponGlint.Spawn();
                    weaponGlint.Trigger(pawn, job.targetA.Thing, -1);
                }
                catch (Exception e)
                {
                    ModLog.Error($"{pawn} failed to play finish-off cosmetics on {victim}", e);
                }
            },
            defaultDuration = 60,
            defaultCompleteMode = ToilCompleteMode.Delay
        };

        yield return new Toil
        {
            initAction = delegate
            {
                weaponGlint?.Cleanup();

                if (job.targetA.Thing is not Pawn victim || victim.Destroyed || victim.Dead)
                {
                    ModLog.Warn($"{pawn} could not finish off {job.targetA.Thing}: target missing, destroyed, or already dead");
                    return;
                }

                try
                {
                    Verb verb = job.verbToUse ?? pawn.meleeVerbs?.TryGetMeleeVerb(victim);
                    verb?.TryStartCastOn(victim, false, true, false, false);

                    // Only give execution thoughts for victims where GiveThoughtsForPawnExecuted
                    // produces semantically correct output: prisoners, guests, and player-faction
                    // colonists. Hostile enemies fall through to the ExecutedColonist branch
                    // internally, which would incorrectly give a "witnessed settler execution" mood.
                    if (victim.IsPrisoner || victim.HostFaction != null || victim.Faction == Faction.OfPlayer)
                    {
                        ThoughtUtility.GiveThoughtsForPawnExecuted(victim, pawn, PawnExecutionKind.GenericBrutal);
                    }

                    if (victim.RaceProps is { intelligence: Intelligence.Animal } && RecordDefOf.AnimalsSlaughtered != null)
                    {
                        pawn.records.Increment(RecordDefOf.AnimalsSlaughtered);
                    }

                    if (victim.IsPrisonerOfColony)
                    {
                        TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, pawn, victim);
                    }
                }
                catch (Exception e)
                {
                    ModLog.Error($"{pawn} hit an error finishing off {victim} — killing anyway", e);
                }

                // The kill must never be skippable: everything above is cosmetic/flavor and must
                // not be able to leave the victim alive if it throws or bails.
                DoExecution(pawn, victim);

                if (skullMote is { Destroyed: false })
                {
                    skullMote.Destroy(DestroyMode.Vanish);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }

    public virtual void DoExecution(Pawn slayer, Pawn victim)
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

        slayer.Notify_KilledPawn(victim);
    }

    public Thing TryMakeSkullMote(Pawn victim, float chance)
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
        return victim is not { Spawned: true } || victim.Dead || !victim.Downed
               || (_hadDesignationAtStart && WorkGiver_FinishOff.GetOwnDesignation(victim) == null);
    }

}
