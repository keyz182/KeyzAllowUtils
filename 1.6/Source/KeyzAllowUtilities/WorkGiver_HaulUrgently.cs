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

        var thingsOut = searchPool.TakeRandom(toTake).ToHashSet();
        searchPool.RemoveAll(thingsOut.Contains);

        return thingsOut;
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return base.ShouldSkip(pawn, forced) || KeyzAllowUtilitiesMod.settings.DisableHaulUrgently || !pawn.Map.designationManager.SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation).Any() || pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t.MapHeld?.designationManager.DesignationOn(t, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation) == null) return null;
        if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced)) return null;
        return TryMakeGenebankJob(pawn, t) ?? JobOnThingDelegate(pawn, t, forced);
    }

    private static Job TryMakeGenebankJob(Pawn pawn, Thing t)
    {
        if (!ModsConfig.BiotechActive || t is not Genepack genepack)
            return null;

        if (!genepack.AutoLoad)
            return null;

        Thing genebank = FindGeneBank(pawn, genepack);
        if (genebank == null)
            return null;

        var job = JobMaker.MakeJob(JobDefOf.CarryGenepackToContainer, genepack, genebank, genebank.InteractionCell);
        job.count = 1;
        return job;
    }

    private static Thing FindGeneBank(Pawn pawn, Genepack genepack)
    {
        if (genepack.targetContainer != null)
        {
            if (genepack.targetContainer.Map == pawn.Map)
            {
                var targetComp = genepack.targetContainer.TryGetComp<CompGenepackContainer>();
                if (targetComp != null && !targetComp.Full)
                    return genepack.targetContainer;
            }
            return null;
        }

        return GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForGroup(ThingRequestGroup.GenepackHolder),
            PathEndMode.Touch,
            TraverseParms.For(pawn),
            validator: candidate =>
            {
                if (candidate.IsForbidden(pawn))
                    return false;
                if (!pawn.CanReserve(candidate))
                    return false;
                var comp = candidate.TryGetComp<CompGenepackContainer>();
                return comp is { Full: false, autoLoad: true };
            });
    }
}
