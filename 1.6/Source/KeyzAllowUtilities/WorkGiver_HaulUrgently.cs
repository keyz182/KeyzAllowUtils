using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class WorkGiver_HaulUrgently: WorkGiver_Scanner
{
    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.designationManager.SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation).Where(d => d.target.Thing != null).Select(d => d.target.Thing);
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return !pawn.Map.designationManager.SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation).Any();
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced)) return null;
        Job job = HaulAIUtility.HaulToStorageJob(pawn, t, forced);

        return job;
    }
}
