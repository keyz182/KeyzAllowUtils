using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class WorkGiver_HaulUrgently: WorkGiver_Scanner
{
    public delegate Job TryGetJobOnThing(Pawn pawn, Thing t, bool forced);
    public static TryGetJobOnThing JobOnThingDelegate = HaulAIUtility.HaulToStorageJob;


    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public List<Thing> searchPool = [];


    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        if (searchPool.NullOrEmpty())
        {
            searchPool = pawn.Map.designationManager.SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation).Where(d => d.target.Thing != null)
                .Select(d => d.target.Thing).ToList();
        }

        int toTake = Math.Min(Math.Max(10, searchPool.Count / 10), searchPool.Count);

        IEnumerable<Thing> thingsOut = searchPool.TakeRandom(toTake);
        searchPool.RemoveAll(t => thingsOut.Contains(t));

        return pawn.Map.designationManager.SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation).Where(d => d.target.Thing != null).Select(d => d.target.Thing);
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return KeyzAllowUtilitiesMod.settings.DisableHaulUrgently || !pawn.Map.designationManager.SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation).Any();
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced)) return null;
        Job job = JobOnThingDelegate(pawn, t, forced);

        return job;
    }
}
