using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_SelectSimilar : Designator
{
    private Func<Thing, bool> filter = null;
    private Func<Thing, bool> filterWithStuff = null;

    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableSelection;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableSelection;
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
        hotKey = KeyzAllowUtilitesDefOf.KAU_SelectSimilarDesignator;
    }

    private Func<Thing, bool> GetFilter()
    {
        if (Event.current?.shift ?? false)
        {
            filterWithStuff ??= FilterUtils.MakeFilter(Find.Selector.SelectedObjects.OfType<Thing>(), false);

            return filterWithStuff;
        }
        else
        {
            filter ??= FilterUtils.MakeFilter(Find.Selector.SelectedObjects.OfType<Thing>(), true);

            return filter;
        }
    }

    public IEnumerable<Thing> SelectableThingsInCell(IntVec3 c)
    {
        if (!c.InBounds(Map) || c.Fogged(Map))
        {
            return [];
        }

        IEnumerable<Thing> thingsInCell = Map.thingGrid.ThingsListAt(c);
        IEnumerable<Thing> thingsInCellInStuff = thingsInCell.Where(t => t is not MinifiedThing).SelectMany(t => t.TryGetInnerInteractableThingOwner() ?? Enumerable.Empty<Thing>());

        return thingsInCell.Concat(thingsInCellInStuff).Where(GetFilter());
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        return SelectableThingsInCell(c).Any() ? true : "No Selectables";
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        foreach (Thing thing in SelectableThingsInCell(c))
        {
            Find.Selector.Select(thing, forceDesignatorDeselect: false);
        }
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        return GetFilter()(t);
    }

    public override void DesignateThing(Thing t)
    {
        Find.Selector.Select(t, forceDesignatorDeselect: false);
    }

    public override void SelectedUpdate() => GenUI.RenderMouseoverBracket();

    private static HashSet<Vector2> drawnPos = new();

    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        drawnPos.Clear();
        foreach (IntVec3 dragCell in dragCells)
        {
            foreach (Thing t in SelectableThingsInCell(dragCell))
            {
                if (t.DrawPosHeld is Vector3 drawPosHeld && drawnPos.Add(new Vector2(drawPosHeld.x, drawPosHeld.z)))
                {
                    Vector3 drawPos = new(drawPosHeld.x, AltitudeLayer.MetaOverlays.AltitudeFor(), drawPosHeld.z);

                    Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, DragHighlightThingMat, 0);
                }
            }
        }
        drawnPos.Clear();
    }

    protected override void FinalizeDesignationSucceeded()
    {
        filter = null;
        filterWithStuff = null;
        base.FinalizeDesignationSucceeded();
    }

    protected override void FinalizeDesignationFailed()
    {
        filter = null;
        filterWithStuff = null;
        base.FinalizeDesignationFailed();
    }
}
