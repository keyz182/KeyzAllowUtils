using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_FinishOff : Designator
{
    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableFinishOff;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableFinishOff;

    protected override DesignationDef Designation => KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public static readonly Material DragHighlightThingMat = MaterialPool.MatFrom("UI/KUA_FinishOffHighlight", ShaderDatabase.MetaOverlay);

    public Designator_FinishOff()
    {
        defaultLabel = "KUA_ToggleFinishOff".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_ToggleFinishOff");
        defaultDesc = "KUA_ToggleFinishOffDesc".Translate();
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_Haul;
    }

    public List<Thing> SelectableThingsInCell(IntVec3 c)
    {
        if (!c.InBounds(Map) || c.Fogged(Map))
            return [];

        return Map.thingGrid.ThingsListAt(c).Where(t=>CanDesignateThing(t)).ToList();
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
            DesignateThing(thing);
        }
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if(t is not Pawn { Downed: true, Dead: false } pawn || pawn.Faction == Faction.OfPlayer || (pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayer)) return false;

        return KeyzAllowUtilitiesMod.settings.AllowFinishOffOnFriendly || pawn.Faction.HostileTo(Faction.OfPlayer) || pawn.IsAnimal;
    }

    public override void DesignateThing(Thing t)
    {
        DesignationDef def = Event.current.shift
            ? KeyzAllowUtilitesDefOf.KAU_StripFinishOffDesignation
            : Designation;
        if (Map.designationManager.DesignationOn(t, def) == null)
        {
            Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) t, def));
        }
        t.SetForbidden(false, false);
    }

    public override void SelectedUpdate() => GenUI.RenderMouseoverBracket();

    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        var seenThings = new HashSet<Thing>();
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
    }
}
