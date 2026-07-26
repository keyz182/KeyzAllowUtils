using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

public class WorkGiver_HaulUrgently: WorkGiver_Scanner
{
    public delegate Job TryGetJobOnThing(Pawn pawn, Thing t, bool forced);
    public static TryGetJobOnThing JobOnThingDelegate = HaulAIUtility.HaulToStorageJob;


    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    /// <summary>Upper bound on candidates handed to the downstream closest-reachable search.</summary>
    private const int CandidateCap = 64;

    /// <summary>
    /// Reusable scratch buffer, cleared at the top of every <see cref="PotentialWorkThingsGlobal"/>
    /// call — an allocation cache, not a pool: it carries no state between calls. Capacity is
    /// fixed at <see cref="CandidateCap"/> and never exceeded, so it never regrows.
    /// </summary>
    private readonly List<Thing> candidateBuffer = new(CandidateCap);

    /// <summary>
    /// The CandidateCap nearest urgent-haul targets on the pawn's own map, nearest first. A pure
    /// function of (pawn.Position, map designation state): no Rand, no cross-call state, no
    /// dependence on designation enumeration order — so it is identical on every Multiplayer
    /// client (see issue: HaulUrgently could desync when multiple items were designated, because
    /// the previous Rand-based sampling made a different number of Rand calls depending on the
    /// size of a stale, unsaved, cross-map-shared search pool).
    /// Returns List&lt;Thing&gt; deliberately: GenClosest.ClosestThing_Global takes an indexed-IList
    /// fast path and GenClosest.EarlyOutSearch takes the ICollection.Count fast path — a HashSet
    /// would miss both.
    /// </summary>
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        Map map = pawn.Map;
        if (map == null) return null; // null is vanilla's "nothing" — the base method returns null

        List<Thing> buffer = candidateBuffer;
        buffer.Clear();

        IntVec3 root = pawn.Position;

        foreach (Designation des in map.designationManager
                     .SpawnedDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation))
        {
            Thing thing = des.target.Thing;
            // Spawned/Map checks are required, not defensive: ClosestThing_Global.Process demands
            // Spawned, and a despawned Thing's Position is stale.
            if (thing == null || !thing.Spawned || thing.Map != map) continue;
            InsertCapped(buffer, thing, root);
        }

        return buffer;
    }

    /// <summary>
    /// Keeps <paramref name="buffer"/> sorted ascending by <see cref="CompareCandidates(IntVec3,Thing,Thing)"/>
    /// and no longer than <see cref="CandidateCap"/>. Delegates to the Thing-independent
    /// <see cref="InsertCappedByKey{T}"/> so the bounded top-K algorithm itself is unit-testable
    /// without needing a positioned Thing (Thing's position setter is private).
    /// </summary>
    private static void InsertCapped(List<Thing> buffer, Thing candidate, IntVec3 root) =>
        InsertCappedByKey(buffer, candidate, CandidateCap, (a, b) => CompareCandidates(root, a, b));

    /// <summary>
    /// Generic bounded top-K insertion: keeps <paramref name="buffer"/> sorted ascending by
    /// <paramref name="compare"/> and no longer than <paramref name="cap"/>, evicting the current
    /// worst element once full. O(1) reject for a candidate worse than the worst one kept, since
    /// the buffer is already sorted. <paramref name="compare"/> must be a total order (see
    /// <see cref="CompareCandidates(IntVec3,IntVec3,int,IntVec3,int)"/>) or the binary search below
    /// is not valid.
    /// </summary>
    internal static void InsertCappedByKey<T>(List<T> buffer, T candidate, int cap, Comparison<T> compare)
    {
        int count = buffer.Count;
        if (count == cap && compare(candidate, buffer[count - 1]) >= 0)
            return;

        int lo = 0, hi = count;
        while (lo < hi)
        {
            int mid = (int)(((uint)lo + (uint)hi) >> 1);
            if (compare(candidate, buffer[mid]) < 0) hi = mid; else lo = mid + 1;
        }

        if (count == cap) buffer.RemoveAt(count - 1);
        buffer.Insert(lo, candidate);
    }

    private static int CompareCandidates(IntVec3 root, Thing a, Thing b) =>
        CompareCandidates(root, a.Position, a.thingIDNumber, b.Position, b.thingIDNumber);

    /// <summary>
    /// TOTAL order on haul candidates: exact integer squared distance from <paramref name="root"/>,
    /// then thingIDNumber. Never returns 0 for distinct things, so the sorted result is unique and
    /// sort stability is irrelevant. thingIDNumber is saved in the game file and identical on
    /// every Multiplayer client. Kept primitive-only and pure so it is unit-testable — Thing's
    /// position setter is private, so a bare test host cannot position a real Thing.
    /// </summary>
    internal static int CompareCandidates(IntVec3 root, IntVec3 aPos, int aId, IntVec3 bPos, int bId)
    {
        int da = (root - aPos).LengthHorizontalSquared;
        int db = (root - bPos).LengthHorizontalSquared;
        if (da != db) return da < db ? -1 : 1;
        if (aId != bId) return aId < bId ? -1 : 1;
        return 0;
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        // AnySpawnedDesignationOfDef is a plain loop; SpawnedDesignationsOfDef(...).Any() built a
        // yield-return state machine on every pawn's every scan.
        return KeyzAllowUtilitiesMod.settings.DisableHaulUrgently
               || pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)
               || base.ShouldSkip(pawn, forced)
               || !pawn.Map.designationManager
                       .AnySpawnedDesignationOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
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
