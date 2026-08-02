using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_SelectSimilar : Designator
{
    private readonly List<Thing> similarTo = [];
    private Func<Thing, bool> filterIgnoreStuff = null;
    private Func<Thing, bool> filterWithStuff = null;

    // True when the tool was activated with nothing selected, so each drag anchors its own
    // filter from the cursor. False when the player had a pre-selection, where the original
    // behaviour — one filter for the whole activation — is what they asked for.
    private bool seededFromCursor;

    // Support for Settings.DesignateSuccessFeedbackSuppressed (see that field for why): track
    // whether a drag is in progress and whether it has selected anything yet, so the drag's
    // feedback sound plays at most once — after the whole drag, not once per matching cell.
    private bool inMultiCellDesignation;
    private bool anySelectedThisDrag;

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
        bool selectedAny = false;
        foreach (Thing thing in SelectableThingsInCell(c))
        {
            Find.Selector.Select(thing, forceDesignatorDeselect: false);
            selectedAny = true;
        }

        if (!selectedAny)
        {
            return;
        }

        if (inMultiCellDesignation)
        {
            // Recorded for DesignateMultiCell below to act on once, after the whole drag.
            anySelectedThisDrag = true;
        }
        else if (Settings.DesignateSuccessFeedbackSuppressed())
        {
            // Single click: this is the only DesignateSingleCell call for the action, so it's
            // safe to play the feedback sound directly here. See Settings.DesignateSuccessFeedbackSuppressed.
            soundSucceeded?.PlayOneShotOnCamera();
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
        seededFromCursor = similarTo.Empty();
    }

    public override void Deselected()
    {
        ClearFilterCache();
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        inMultiCellDesignation = true;
        anySelectedThisDrag = false;

        base.DesignateMultiCell(cells);

        inMultiCellDesignation = false;
        if (anySelectedThisDrag && Settings.DesignateSuccessFeedbackSuppressed())
        {
            // See Settings.DesignateSuccessFeedbackSuppressed: base.DesignateMultiCell already
            // called Finalize(true), but Multiplayer's global patch on it skips the sound outside
            // a synced command. Play it once here for the whole drag, not per matching cell.
            soundSucceeded?.PlayOneShotOnCamera();
        }

        // The tool stays selected after a drag, so without this the next drag would reuse the
        // first drag's anchor and match the wrong things. Base resolves the filter for every
        // cell before returning, so clearing here cannot affect the drag just completed.
        if (seededFromCursor)
        {
            ClearFilterCache();
        }
    }

    private void ClearFilterCache()
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
