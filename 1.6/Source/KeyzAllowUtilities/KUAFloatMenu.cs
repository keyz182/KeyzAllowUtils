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
        return base.TargetPawnValid(pawn, context) && pawn.Downed;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(
        Pawn clickedPawn,
        FloatMenuContext context)
    {
        if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly))
        {
            yield return new FloatMenuOption(
                "KAU_NoKill".Translate((NamedArgument) clickedPawn.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
        }

        foreach (Pawn pawn in context.ValidSelectedPawns)
        {
            int melee = pawn.skills.GetSkill(SkillDefOf.Melee).Level;
            if (!KeyzAllowUtilitiesMod.settings.DisableMeleeRequirementForFinishOff && melee < 5)
            {
                yield return new FloatMenuOption(
                    "KAU_NoKill".Translate((NamedArgument) clickedPawn.Label) + ": " + "KAU_MeleeTooLow".Translate(melee).CapitalizeFirst(), null);
            }
            else
            {
                Verb verb = pawn.meleeVerbs?.TryGetMeleeVerb(clickedPawn);
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
