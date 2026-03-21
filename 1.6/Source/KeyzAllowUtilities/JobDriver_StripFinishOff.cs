using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

public class JobDriver_StripFinishOff : JobDriver_FinishOff
{
    public void DoStrip(Pawn slayer, Pawn victim)
    {
        if (victim is IStrippable strippable)
        {
            strippable.Strip(true);
        }
    }

    public override void DoExecution(Pawn slayer, Pawn victim)
    {
        DoStrip(slayer, victim);
        base.DoExecution(slayer, victim);
        slayer.records.Increment(RecordDefOf.BodiesStripped);
    }

    private const int PrepareSwingDuration = 120;
    private const float VictimSkullMoteChance = 0.25f;
    private const float OpportunityTargetMaxRange = 8f;
}
