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
    private readonly List<Thing> similarTo = [];
    private Func<Thing, bool> filterIgnoreStuff = null;
    private Func<Thing, bool> filterWithStuff = null;

    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableSelection;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableSelection;
    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

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
        // If `similarTo` is empty (nothing pre-selected), seed it from the thing under the cursor.
        if (similarTo.Empty())
        {
            Thing hovered = MouseOverThing();
            if (hovered != null && !similarTo.Contains(hovered))
            {
                similarTo.Add(hovered);
            }
        }

        // `similarTo` will only be non-empty between `Selected()` and `Deselected()` calls. So this emptiness check
        // ensures the cached filter being consistent with the `similarTo`.
        if (similarTo.Empty())
        {
            return _ => false;
        }

        bool ignoreStuff = Event.current?.shift ?? false;
        ref Func<Thing, bool> selectedFilter = ref (ignoreStuff ? ref filterIgnoreStuff : ref filterWithStuff);

        selectedFilter ??= FilterUtils.MakeFilter(similarTo, checkStuff: !ignoreStuff);

        return selectedFilter;
    }

    private static Thing MouseOverThing()
    {
        var mouseCell = UI.MouseCell();

        if (!mouseCell.InBounds(Find.CurrentMap))
            return null;

        foreach (Thing t in Find.CurrentMap.thingGrid.ThingsListAt(mouseCell))
        {
            if (t.def.selectable && !t.IsForbidden(Faction.OfPlayer))
                return t;
        }

        return null;
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

    public override void Selected()
    {
        similarTo.AddRange(Find.Selector.SelectedObjects.OfType<Thing>());
    }

    public override void Deselected()
    {
        similarTo.Clear();
        filterIgnoreStuff = null;
        filterWithStuff = null;
    }

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

                    Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, DesignatorUtility.DragHighlightThingMat, 0);
                }
            }
        }
        drawnPos.Clear();
    }
}
