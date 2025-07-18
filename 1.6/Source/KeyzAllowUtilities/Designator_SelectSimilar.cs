using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_SelectSimilar : Designator
{
    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public static readonly Material DragHighlightThingMat = MaterialPool.MatFrom("UI/KUA_HaulHighlight", ShaderDatabase.MetaOverlay);

    public Designator_SelectSimilar()
    {
        defaultLabel = "KUA_SelectSimilar".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_MultiSelect");
        defaultDesc = "KUA_SelectSimilarDesc".Translate();
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_Haul;
        hotKey = KeyBindingDefOf.Misc12;
    }

    public List<Thing> SelectableThingsInCell(IntVec3 c)
    {
        if (!c.InBounds(Map) || c.Fogged(Map))
            return [];

        IEnumerable<Thing> selected = Find.Selector.SelectedObjects.OfType<Thing>();
        List<Thing> thingsInCell = Map.thingGrid.ThingsListAt(c).Where(t=>t.def.selectable).Where(t => selected.Any(s => t.def == s.def)).ToList();

        if (!Event.current.shift)
        {
            thingsInCell = thingsInCell.Where(t => selected.Any(s => s.Stuff == null || s.Stuff == t.Stuff)).ToList();
        }

        return thingsInCell;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        List<Thing> thingsInCell = SelectableThingsInCell(c);

        if (thingsInCell.Count <= 0)
            return "No Selectables";

        return true;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        List<Thing> thingsInCell = SelectableThingsInCell(c);
        foreach (Thing thing in thingsInCell)
        {
            Find.Selector.Select(thing);
        }
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        return t.def.selectable;
    }

    public override void DesignateThing(Thing t)
    {
        Find.Selector.Select(t);
    }

    public override void SelectedUpdate() => GenUI.RenderMouseoverBracket();

    private static HashSet<Thing> seenThings = new();
    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        seenThings.Clear();
        foreach (IntVec3 dragCell in dragCells)
        {
            if (Map.thingGrid.ThingsListAt(dragCell).Any(t=>Find.Selector.IsSelected(t)))
            {
                Graphics.DrawMesh(MeshPool.plane10, dragCell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays.AltitudeFor()), Quaternion.identity, DragHighlightThingMat, 0);
                if (Map.designationManager.DesignationAt(dragCell, DesignationDefOf.Mine) != null)
                    continue;
            }
            List<Thing> thingList = dragCell.GetThingList(Map);
            foreach (Thing t in thingList)
            {
                if (!seenThings.Contains(t) && CanDesignateThing(t).Accepted)
                {
                    Vector3 drawPos = t.DrawPos with
                    {
                        y = AltitudeLayer.MetaOverlays.AltitudeFor()
                    };
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, DragHighlightThingMat, 0);
                    seenThings.Add(t);
                }
            }
        }
        seenThings.Clear();
    }
}
