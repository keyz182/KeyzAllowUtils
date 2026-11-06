#if NET10_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

/// <summary>
/// Remove floor, limited to one floor type. Picking the tool lists the removable floor types on
/// the map; the player either drags to mark only that type, or marks every cell of it at once.
/// Built for insect sludge spread over the map, without touching the colony's own floors.
/// 1.7 only.
/// </summary>
public class Designator_RemoveFloorMatching : Designator_RemoveFloor
{
    private TerrainDef target;

    public Designator_RemoveFloorMatching()
    {
        defaultLabel = "KAU_RemoveFloorMatching".Translate();
        defaultDesc = "KAU_RemoveFloorMatchingDesc".Translate();
        hotKey = null; // vanilla Remove floor already owns Misc1
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        AcceptanceReport report = base.CanDesignateCell(c);
        if (!report.Accepted) return report;
        if (target != null && Map.terrainGrid.TerrainAt(c) != target) return false;
        return report;
    }

    public override void ProcessInput(Event ev)
    {
        if (!CheckCanInteract()) return;

        Map map = Find.CurrentMap;
        List<IGrouping<TerrainDef, IntVec3>> floors = RemovableCells(map)
            .GroupBy(c => map.terrainGrid.TerrainAt(c))
            .OrderByDescending(g => g.Count())
            .ToList();

        if (floors.Count == 0)
        {
            Messages.Message("KAU_NoRemovableFloors".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        List<FloatMenuOption> options = new();
        foreach (IGrouping<TerrainDef, IntVec3> group in floors)
        {
            TerrainDef def = group.Key;
            List<IntVec3> cells = group.ToList();
            string name = def.LabelCap + " (" + cells.Count + ")";
            options.Add(new FloatMenuOption("KAU_RemoveFloorDrag".Translate(name), () =>
            {
                target = def;
                base.ProcessInput(ev);
            }));
            options.Add(new FloatMenuOption("KAU_RemoveFloorAllOnMap".Translate(name), () =>
            {
                target = def;
                int n = 0;
                foreach (IntVec3 c in cells)
                {
                    if (!CanDesignateCell(c).Accepted) continue;
                    DesignateSingleCell(c);
                    n++;
                }
                Messages.Message("KUA_DesignatedCount".Translate(n), MessageTypeDefOf.NeutralEvent, historical: false);
            }));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static IEnumerable<IntVec3> RemovableCells(Map map)
    {
        foreach (IntVec3 c in map.AllCells)
        {
            if (c.Fogged(map) || !map.terrainGrid.CanRemoveTopLayerAt(c)) continue;
            yield return c;
        }
    }
}
#endif
