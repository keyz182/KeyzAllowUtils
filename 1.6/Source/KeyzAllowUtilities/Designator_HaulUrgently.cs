using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Designator_HaulUrgently : Designator
{
    protected override DesignationDef Designation => KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public static readonly Material DragHighlightThingMat = MaterialPool.MatFrom("UI/KUA_HaulHighlight", ShaderDatabase.MetaOverlay);

    public Designator_HaulUrgently()
    {
        defaultLabel = "KUA_HaulUrgently".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_ToggleHaulUrgently");
        defaultDesc = "DesignatorHaulThingsDesc".Translate();
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_Haul;
        hotKey = KeyBindingDefOf.Misc12;
    }

    public static Thing GetFirstUgentHaulable(IntVec3 c, Map map)
    {
        List<Thing> thingList = map.thingGrid.ThingsListAt(c);
        return Enumerable.FirstOrDefault(thingList, t => (t.def.designateHaulable || t.def.EverHaulable) && !t.IsInValidBestStorage());
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!c.InBounds(Map) || c.Fogged(Map))
            return false;
        Thing firstHaulable = GetFirstUgentHaulable(c, Map);
        if (firstHaulable == null)
            return "MessageMustDesignateHaulable".Translate();
        AcceptanceReport acceptanceReport = CanDesignateThing(firstHaulable);
        return !acceptanceReport.Accepted ? acceptanceReport : true;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        Thing haulable = GetFirstUgentHaulable(c,Map);
        if (haulable != null)
            DesignateThing(haulable);
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (!t.def.designateHaulable && !t.def.EverHaulable)
            return false;
        if (t.IsInValidBestStorage())
            return "MessageAlreadyInStorage".Translate();
        if (Map.designationManager.DesignationOn(t, Designation) != null)
            return false;
        return true;
    }

    public override void DesignateThing(Thing t)
    {
        Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) t, Designation));
        t.SetForbidden(false, false);
    }

    public override void SelectedUpdate() => GenUI.RenderMouseoverBracket();

    private static HashSet<Thing> seenThings = new();
    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        seenThings.Clear();
        foreach (IntVec3 dragCell in dragCells)
        {
            if (Map.designationManager.HasMapDesignationAt(dragCell))
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
