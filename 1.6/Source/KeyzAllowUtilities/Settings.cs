using System;
using System.Collections.Generic;
using System.Linq;
using KeyzAllowUtilities.Utilities;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Settings : ModSettings
{
    public int MaxSelect = 300;
    public bool DisableHaulUrgently = false;
    public bool DisableNoHauling = false;
    public bool DisableFinishOff = false;
    public bool DisableStripMine = false;
    public bool AllowFinishOffOnFriendly = false;
    public bool DisableAllowShortcuts = false;
    public bool DisableAllShortcuts = false;
    public bool DisableMeleeRequirementForFinishOff = false;
    public bool DisableHarvest = false;
    public bool DisableCut = false;
    public bool DisableSelection = false;
    public bool DisableFertileZone = false;

    public void RemoveThingDefFromDenyList(ThingDef buildable)
    {
        _defSelectionDenyList.Remove(buildable.defName);
        _cachedDenyList?.Clear();
    }

    public void AddThingDefToDenyList(ThingDef buildable)
    {
        _defSelectionDenyList.Add(buildable.defName);
        _cachedDenyList?.Clear();
    }

    protected List<string> _defSelectionDenyList = [];

    private List<ThingDef> _cachedDenyList;
    public List<ThingDef> DefSelectionDenyList
    {
        get
        {
            if (_cachedDenyList.NullOrEmpty())
            {
                if (_defSelectionDenyList.NullOrEmpty())
                {
                    _defSelectionDenyList = [];
                }
                _cachedDenyList = _defSelectionDenyList
                    .Select(s => DefDatabase<ThingDef>.GetNamed(s))
                    .Where(d => d is not null)
                    .ToList();
            }

            return _cachedDenyList;
        }
    }

    public bool IsAllowed(Thing thing) => !DefSelectionDenyList.Contains(thing.def);

    private float ScrollViewHeight = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public void DoWindowContents(Rect wrect)
    {
        Rect contentScrollContainerRect = new(
            wrect.xMin,
            wrect.yMin,
            wrect.width - 16,
            Mathf.Max(ScrollViewHeight, wrect.height)
        );

        scrollPosition = GUI.BeginScrollView(wrect, scrollPosition, contentScrollContainerRect);

        Listing_Standard options = new() { maxOneColumn = true };
        try
        {
            options.Begin(contentScrollContainerRect);

            options.GapLine();
            GameFont orig = Text.Font;
            Text.Font = GameFont.Medium;
            options.Label("General Settings");
            Text.Font = orig;
            options.Gap();

            options.Label("KeyzAllowUtilities_Settings_MaxSelect".Translate(MaxSelect));
            options.IntAdjuster(ref MaxSelect, 10, 0);
            options.CheckboxLabeled("KAU_ToggleHaulUrgently".Translate(), ref DisableHaulUrgently);
            options.CheckboxLabeled("KAU_ToggleNoHauling".Translate(), ref DisableNoHauling);
            options.CheckboxLabeled("KAU_ToggleFinishOff".Translate(), ref DisableFinishOff);
            options.CheckboxLabeled("KAU_ToggleStripMine".Translate(), ref DisableStripMine);
            options.CheckboxLabeled("KAU_ToggleHarvest".Translate(), ref DisableHarvest);
            options.CheckboxLabeled("KAU_ToggleCut".Translate(), ref DisableCut);
            options.CheckboxLabeled("KAU_ToggleSelection".Translate(), ref DisableSelection);
            options.CheckboxLabeled("KAU_ToggleFertileZone".Translate(), ref DisableFertileZone);
            options.CheckboxLabeled("KAU_ToggleAllowFinishOffOnFriendly".Translate(), ref AllowFinishOffOnFriendly);
            options.CheckboxLabeled("KAU_ToggleDisableAllowShortcuts".Translate(), ref DisableAllowShortcuts);
            options.CheckboxLabeled("KAU_ToggleDisableAllShortcuts".Translate(), ref DisableAllShortcuts);
            options.CheckboxLabeled("KAU_DisableMeleeRequirementForFinishOff".Translate(), ref DisableMeleeRequirementForFinishOff);

            options.Gap();
            options.GapLine();
            Text.Font = GameFont.Medium;
            options.Label("Denylist");
            Text.Font = orig;
            options.Label("ThingDefs in this list will be ignored by this mods tools.");
            options.Gap();
            if (options.ButtonText("Add ThingDef to denylist"))
            {
                Dialog_ThingDefFinder finder = new();
                Find.WindowStack.Add(finder);
            }

            options.Gap();
            foreach (ThingDef deniedThing in DefSelectionDenyList.Where(t => t != null).ToList()) // Handle mods removed and such
            {
                bool remove;
                if (deniedThing.uiIcon != null)
                {
                    remove = options.ButtonTextLabeledPct($"{deniedThing.LabelCap} ({deniedThing.defName})", $"Remove", 0.9f,
                        TextAnchor.MiddleLeft, labelIcon: deniedThing.uiIcon);
                }
                else
                {
                    remove = options.ButtonText($"{deniedThing.LabelCap} ({deniedThing.defName})");
                }
                if (remove)
                {
                    RemoveThingDefFromDenyList(deniedThing);
                }
            }
        }
        catch (Exception e)
        {
            ModLog.Error("Error rendering settings menu", e);
        }
        finally
        {
            ScrollViewHeight = options.CurHeight;
            options.End();
            GUI.EndScrollView();

        }


    }

    public void ValidateDesignators()
    {
        if (Scribe.mode == LoadSaveMode.Inactive || Scribe.mode == LoadSaveMode.Saving)
        {
            KeyzAllowUtilitesDefOf.KAU_UrgentHaul?.Toggle(!DisableHaulUrgently);
            KeyzAllowUtilitesDefOf.KAU_FinishingOff?.Toggle(!DisableFinishOff);

            if (DisableHaulUrgently && !Find.Maps.NullOrEmpty())
            {
                foreach (Map map in Find.Maps)
                {
                    map.designationManager.RemoveAllDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
                }
            }

            if (DisableFinishOff && !Find.Maps.NullOrEmpty())
            {
                foreach (Map map in Find.Maps)
                {
                    map.designationManager.RemoveAllDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_FinishOffDesignation);
                }
            }
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref MaxSelect, "MaxSelect", 300);
        Scribe_Values.Look(ref DisableHaulUrgently, "DisableHaulUrgently", false);
        Scribe_Values.Look(ref DisableNoHauling, "DisableNoHauling", false);
        Scribe_Values.Look(ref DisableFinishOff, "DisableFinishOff", false);
        Scribe_Values.Look(ref DisableStripMine, "DisableStripMine", false);
        Scribe_Values.Look(ref DisableHarvest, "DisableHarvest", false);
        Scribe_Values.Look(ref DisableCut, "DisableCut", false);
        Scribe_Values.Look(ref DisableSelection, "DisableSelection", false);
        Scribe_Values.Look(ref DisableFertileZone, "DisableFertileZone", false);
        Scribe_Values.Look(ref AllowFinishOffOnFriendly, "AllowFinishOffOnFriendly", false);
        Scribe_Values.Look(ref DisableAllowShortcuts, "DisableAllowShortcuts", false);
        Scribe_Values.Look(ref DisableAllShortcuts, "DisableAllShortcuts", false);
        Scribe_Values.Look(ref DisableMeleeRequirementForFinishOff, "DisableMeleeRequirementForFinishOff", false);

        Scribe_Collections.Look(ref _defSelectionDenyList, "DefSelectionDenyList", LookMode.Value);
        _cachedDenyList?.Clear();

        ValidateDesignators();
    }
}
