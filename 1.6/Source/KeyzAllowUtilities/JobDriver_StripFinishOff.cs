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
        if (RecordDefOf.BodiesStripped != null)
        {
            slayer.records.Increment(RecordDefOf.BodiesStripped);
        }
    }

}
