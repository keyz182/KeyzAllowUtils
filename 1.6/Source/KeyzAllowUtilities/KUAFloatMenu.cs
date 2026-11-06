using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class KUAFloatMenu: FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => true;

    public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
    {
        return base.TargetPawnValid(pawn, context) && pawn.Downed && pawn.Faction != Faction.OfPlayer && (pawn.guest == null || pawn.guest.HostFaction != Faction.OfPlayer);
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(
        Pawn clickedPawn,
        FloatMenuContext context)
    {
        if(KeyzAllowUtilitiesMod.settings.DisableFinishOff) yield break;

        if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly))
        {
            yield return new FloatMenuOption(
                "KAU_NoKill".Translate((NamedArgument) clickedPawn.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
        }

        foreach (Pawn pawn in context.ValidSelectedPawns)
        {
            if (!KeyzAllowUtilitiesMod.settings.AllowFinishOffOnFriendly && !(clickedPawn.Faction?.HostileTo(Faction.OfPlayer) ?? true) && !clickedPawn.IsAnimal) continue;

            Verb verb = FinishOffVerb.Choose(pawn, clickedPawn, out bool ranged);
            string tooLow = FinishOffVerb.SkillTooLowReason(pawn, ranged);

            if (tooLow != null)
            {
                yield return new FloatMenuOption(
                    "KAU_NoKill".Translate((NamedArgument) clickedPawn.Label) + ": " + tooLow.CapitalizeFirst(), null);
            }
            else
            {
                if (verb == null)
                {
                    yield return new FloatMenuOption(
                        "KAU_NoKill".Translate((NamedArgument) clickedPawn.Label) + ": " + "KAU_Unable".Translate().CapitalizeFirst(), null);
                }
                else
                {
                    yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                        "KAU_Kill".Translate((NamedArgument) clickedPawn.Label, pawn.Label),
                        () =>
                        {
                            Job job = JobMaker.MakeJob(KeyzAllowUtilitesDefOf.KAU_FinishOffPawn, clickedPawn);
                            job.count = 1;
                            job.verbToUse = verb;
                            pawn.jobs.TryTakeOrderedJob(job);
                        }), pawn, clickedPawn);

                    if ((clickedPawn.apparel?.AnyApparel ?? false) || (clickedPawn.equipment?.HasAnything() ?? false))
                    {
                        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                            "KAU_StripKill".Translate((NamedArgument) clickedPawn.Label, pawn.Label),
                            () =>
                            {
                                Job job = JobMaker.MakeJob(KeyzAllowUtilitesDefOf.KAU_StripFinishOffPawn, clickedPawn);
                                job.count = 1;
                                job.verbToUse = verb;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }), pawn, clickedPawn);
                    }
                }
            }
        }
    }
}
