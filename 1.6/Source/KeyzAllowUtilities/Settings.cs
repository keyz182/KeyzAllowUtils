using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Settings : ModSettings
{
    public int MaxSelect = 300;
    public bool DisableHaulUrgently = false;
    public bool DisableNoHauling = false;
    public bool DisableClaimAll = false;
    public bool DisableFinishOff = false;
    public bool DisableStripMine = false;
    public bool AllowFinishOffOnFriendly = false;
    public bool DisableAllowShortcuts = false;
    public bool DisableAllShortcuts = false;
    public bool DisableMeleeRequirementForFinishOff = false;
    public bool DisableHarvest = false;
    public bool DisableHarvestAll = false;
    public bool DisableCut = false;
    public bool DisableSelection = false;
    public bool DisableFertileZone = false;
    public bool ExcludeCorpsesFromAllowAll = true;
    public bool DisableSelectStored = false;
    public float PlantGrownLevel = 1f;

    private float ScrollViewHeight = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public void DoWindowContents(Rect wrect)
    {
        bool prevHaulUrgently = DisableHaulUrgently;
        bool prevFinishOff = DisableFinishOff;
        bool prevHarvest = DisableHarvest;
        bool prevCut = DisableCut;

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
            options.Label($"Version: {KeyzAllowUtilitiesMod.Version}");
            options.Gap();

            options.Label("KeyzAllowUtilities_Settings_MaxSelect".Translate(MaxSelect));
            options.IntAdjuster(ref MaxSelect, 10, 0);
            options.CheckboxLabeled("KAU_ToggleHaulUrgently".Translate(), ref DisableHaulUrgently);
            options.CheckboxLabeled("KAU_ToggleNoHauling".Translate(), ref DisableNoHauling);
            options.CheckboxLabeled("KAU_DisableClaimAll".Translate(), ref DisableClaimAll);
            options.CheckboxLabeled("KAU_ToggleFinishOff".Translate(), ref DisableFinishOff);
            options.CheckboxLabeled("KAU_ToggleStripMine".Translate(), ref DisableStripMine);
            options.CheckboxLabeled("KAU_ToggleHarvest".Translate(), ref DisableHarvest);
            options.CheckboxLabeled("KAU_ToggleHarvestAll".Translate(), ref DisableHarvestAll);
            options.CheckboxLabeled("KAU_ToggleCut".Translate(), ref DisableCut);
            options.CheckboxLabeled("KAU_ToggleSelection".Translate(), ref DisableSelection);
            options.CheckboxLabeled("KAU_ToggleFertileZone".Translate(), ref DisableFertileZone);
            options.CheckboxLabeled("KAU_ToggleAllowFinishOffOnFriendly".Translate(), ref AllowFinishOffOnFriendly);
            options.CheckboxLabeled("KAU_ToggleDisableAllowShortcuts".Translate(), ref DisableAllowShortcuts);
            options.CheckboxLabeled("KAU_ExcludeCorpsesFromAllowAll".Translate(), ref ExcludeCorpsesFromAllowAll);
            options.CheckboxLabeled("KAU_DisableSelectStored".Translate(), ref DisableSelectStored);
            options.CheckboxLabeled("KAU_ToggleDisableAllShortcuts".Translate(), ref DisableAllShortcuts);
            options.CheckboxLabeled("KAU_DisableMeleeRequirementForFinishOff".Translate(), ref DisableMeleeRequirementForFinishOff);
            PlantGrownLevel = options.SliderLabeled("KAU_PlantGrownLevel".Translate(PlantGrownLevel*100), PlantGrownLevel, 0f, 1f);

            options.Gap();
            options.GapLine();

            if (prevHaulUrgently != DisableHaulUrgently
                || prevFinishOff != DisableFinishOff
                || prevHarvest != DisableHarvest
                || prevCut != DisableCut)
            {
                ValidateDesignators();
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

            if (DisableHarvest && !Find.Maps.NullOrEmpty())
            {
                foreach (Map map in Find.Maps)
                {
                    map.designationManager.RemoveAllDesignationsOfDef(DesignationDefOf.HarvestPlant);
                }
            }

            if (DisableCut && !Find.Maps.NullOrEmpty())
            {
                foreach (Map map in Find.Maps)
                {
                    map.designationManager.RemoveAllDesignationsOfDef(DesignationDefOf.CutPlant);
                }
            }
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref MaxSelect, "MaxSelect", 300);
        Scribe_Values.Look(ref DisableHaulUrgently, "DisableHaulUrgently", false);
        Scribe_Values.Look(ref DisableNoHauling, "DisableNoHauling", false);
        Scribe_Values.Look(ref DisableClaimAll, "DisableClaimAll", false);
        Scribe_Values.Look(ref DisableFinishOff, "DisableFinishOff", false);
        Scribe_Values.Look(ref DisableStripMine, "DisableStripMine", false);
        Scribe_Values.Look(ref DisableHarvest, "DisableHarvest", false);
        Scribe_Values.Look(ref DisableHarvestAll, "DisableHarvestAll", false);
        Scribe_Values.Look(ref DisableCut, "DisableCut", false);
        Scribe_Values.Look(ref DisableSelection, "DisableSelection", false);
        Scribe_Values.Look(ref DisableFertileZone, "DisableFertileZone", false);
        Scribe_Values.Look(ref ExcludeCorpsesFromAllowAll, "ExcludeCorpsesFromAllowAll", true);
        Scribe_Values.Look(ref DisableSelectStored, "DisableSelectStored", false);
        Scribe_Values.Look(ref AllowFinishOffOnFriendly, "AllowFinishOffOnFriendly", false);
        Scribe_Values.Look(ref DisableAllowShortcuts, "DisableAllowShortcuts", false);
        Scribe_Values.Look(ref DisableAllShortcuts, "DisableAllShortcuts", false);
        Scribe_Values.Look(ref DisableMeleeRequirementForFinishOff, "DisableMeleeRequirementForFinishOff", false);
        Scribe_Values.Look(ref PlantGrownLevel, "PlantGrownLevel", 1f);

        ValidateDesignators();
    }
}
