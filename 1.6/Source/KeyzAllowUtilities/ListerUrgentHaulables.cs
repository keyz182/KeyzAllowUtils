using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class ListerUrgentHaulables
{
    public Map map;
    // public HashSet<Thing> haulables = new();
    // Use concurrent dict in place of a concurrent hash set https://stackoverflow.com/a/18923091
    private ConcurrentDictionary<Thing, byte> haulables = new();
    public const int CellsPerTick = 4;
    public const int HaulSourcesPerTick = 4;
    public static int groupCycleIndex = 0;
    public static readonly List<int> cellCycleIndices = new();

    public static bool UnsafeOptimisationsEnabled => KeyzAllowUtilitiesMod.settings.EnableDangerousOpts;

    public bool KeepWorking = true;
    public Thread WorkThread;

    public ListerUrgentHaulables(Map map)
    {
        this.map = map;
        WorkThread = new Thread(ProcessingThread);
    }

    ~ListerUrgentHaulables()
    {
        KeepWorking = false;
        WorkThread?.Join();
    }

    ConcurrentQueue<IntVec3> cellsToRecalculate = new ConcurrentQueue<IntVec3>();
    ConcurrentQueue<IHaulSource> haulSourcesToRecalculate = new ConcurrentQueue<IHaulSource>();

    public void ProcessingThread()
    {
        while (KeepWorking && UnsafeOptimisationsEnabled)
        {
            try
            {
                if (cellsToRecalculate.TryDequeue(out IntVec3 cell))
                {
                    RecalcAllInCell(cell);
                }

                if (haulSourcesToRecalculate.TryDequeue(out IHaulSource haulSource))
                {
                    RecalculateAllInHaulSource(haulSource);
                }
            }
            catch (Exception e)
            {
                ModLog.Error("Unknown error processing urgent haulables", e);
            }

            Thread.Sleep(10);
        }
    }

    public ICollection<Thing> ThingsPotentiallyNeedingHauling()
    {
        return haulables.Keys;
    }

    public void Notify_Spawned(Thing t) => CheckAdd(t);

    public void Notify_DeSpawned(Thing t) => TryRemove(t);

    public void HaulDesignationAdded(Thing t) => CheckAdd(t);

    public void HaulDesignationRemoved(Thing t) => TryRemove(t);

    public void Notify_Unforbidden(Thing t) => CheckAdd(t);

    public void Notify_Forbidden(Thing t) => TryRemove(t);

    public void Notify_AddedThing(Thing t) => CheckAdd(t);

    public void Notify_SlotGroupChanged(SlotGroup sg)
    {
        List<IntVec3> cellsList = sg.CellsList;
        if (cellsList == null)
            return;
        sg.RemoveHaulDesignationOnStoredThings();
        foreach (IntVec3 t in cellsList)
            if (UnsafeOptimisationsEnabled) cellsToRecalculate.Enqueue(t);
            else RecalcAllInCell(t);
    }

    public void Notify_HaulSourceChanged(IHaulSource holder)
    {
        foreach (Thing directlyHeldThing in holder.GetDirectlyHeldThings())
            Check(directlyHeldThing);
    }

    public void ListerUrgentHaulablesTick()
    {
        ++groupCycleIndex;
        if (groupCycleIndex >= Int32.MaxValue)
            groupCycleIndex = 0;
        CellsCheckTick(map.haulDestinationManager.AllGroupsListForReading);
        HaulSourcesCheckTick(map.haulDestinationManager.AllHaulSourcesListForReading);
    }

    public void CellsCheckTick(List<SlotGroup> sgList)
    {
        if (sgList.Count == 0)
            return;
        int index1 = groupCycleIndex % sgList.Count;
        SlotGroup sg = sgList[groupCycleIndex % sgList.Count];
        if (sg.CellsList.Count == 0)
            return;
        while (cellCycleIndices.Count <= index1)
            cellCycleIndices.Add(0);
        if (cellCycleIndices[index1] >= 2147473647)
            cellCycleIndices[index1] = 0;
        for (int index2 = 0; index2 < 4; ++index2)
        {
            cellCycleIndices[index1]++;
            if (UnsafeOptimisationsEnabled) cellsToRecalculate.Enqueue(sg.CellsList[cellCycleIndices[index1] % sg.CellsList.Count]);
            else RecalcAllInCell(sg.CellsList[cellCycleIndices[index1] % sg.CellsList.Count]);
        }
    }

    public void HaulSourcesCheckTick(List<IHaulSource> haulList)
    {
        if (haulList.Count == 0)
            return;
        int num1 = Mathf.CeilToInt(haulList.Count / 4f);
        int num2 = groupCycleIndex % num1;
        for (int index1 = 0; index1 < 4 && index1 < haulList.Count; ++index1)
        {
            int index2 = num2 + index1;
            IHaulSource haul = haulList[index2];
            if (haul.GetDirectlyHeldThings().Count != 0)
                if (UnsafeOptimisationsEnabled) haulSourcesToRecalculate.Enqueue(haul);
                else RecalculateAllInHaulSource(haul);
        }
    }

    public void RecalcAllInCell(IntVec3 c)
    {
        List<Thing> thingList = c.GetThingList(map);
        if (UnsafeOptimisationsEnabled)
        {
            GenThreading.ParallelForEach(thingList, Check);
        }
        else
        {
            foreach (Thing t in thingList)
                Check(t);
        }
    }

    public void RecalcAllInCells(IEnumerable<IntVec3> cells)
    {
        foreach (IntVec3 cell in cells)
            RecalcAllInCell(cell);
    }

    public void RecalculateAllInHaulSources(IList<IHaulSource> sources)
    {
        if (UnsafeOptimisationsEnabled)
        {
            GenThreading.ParallelForEach(sources.ToList(), (IHaulSource source) =>
            {
                foreach (Thing directlyHeldThing in source.GetDirectlyHeldThings())
                    Check(directlyHeldThing);
            });
        }
        else
        {
            foreach (IHaulSource source in sources)
            {
                foreach (Thing directlyHeldThing in source.GetDirectlyHeldThings())
                    Check(directlyHeldThing);
            }
        }
    }

    public void RecalculateAllInHaulSource(IHaulSource source)
    {
        if (UnsafeOptimisationsEnabled)
        {
            GenThreading.ParallelForEach(source.GetDirectlyHeldThings().ToList(), Check);
        }
        else
        {
            foreach (Thing directlyHeldThing in source.GetDirectlyHeldThings())
                Check(directlyHeldThing);
        }
    }

    public void Check(Thing t)
    {
        if (ShouldBeHaulable(t))
            haulables.TryAdd(t, 1);
        else
            haulables.TryRemove(t, out byte _);
    }

    public bool ShouldBeHaulable(Thing t)
    {
        return !t.IsForbidden(Faction.OfPlayer) &&
               (t.def.alwaysHaulable || t.def.EverHaulable && (map.designationManager.DesignationOn(t, DesignationDefOf.Haul) != null || t.IsInAnyStorage())) &&
               !t.IsInValidBestStorage() && (!(t.ParentHolder is IHaulSource parentHolder) || parentHolder.HaulSourceEnabled);
    }

    public void CheckAdd(Thing t)
    {
        if (!ShouldBeHaulable(t))
            return;
        haulables.TryAdd(t,1);
    }

    public void TryRemove(Thing t)
    {
        if (t.def.category != ThingCategory.Item || !haulables.ContainsKey(t))
            return;
        haulables.TryRemove(t, out byte _);
    }
}
