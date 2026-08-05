using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Settings : ModSettings
{
    /// <summary>
    /// Set by the optional Compatibility/rwmt.Multiplayer assembly to <c>() => MP.IsInMultiplayer</c>.
    /// ValidateDesignators runs client-locally on every load (host or join), driven by this
    /// client's settings; without this guard, a client joining with e.g. DisableHaulUrgently set
    /// would silently delete the whole session's shared designations out from under everyone
    /// else. Mirrors the existing WorkGiver_HaulUrgently.JobOnThingDelegate hook pattern so this
    /// assembly stays free of any Multiplayer API dependency.
    /// </summary>
    public static Func<bool> SuppressDesignationPurge = () => false;

    /// <summary>
    /// Set by the optional Compatibility/rwmt.Multiplayer assembly to <c>() => MP.IsInMultiplayer</c>.
    /// Select Similar is deliberately left un-synced by Multiplayer (see MpSelectSimilarUnpatch in
    /// that assembly), since it only changes this client's local selection. But Multiplayer's
    /// generic Designator.Finalize patch only plays soundSucceeded while a synced command is
    /// executing, so once un-synced that success feedback is silently skipped. This hook lets
    /// Designator_SelectSimilar play it directly instead, without this assembly depending on the
    /// Multiplayer API. Mirrors the SuppressDesignationPurge hook above.
    /// </summary>
    public static Func<bool> DesignateSuccessFeedbackSuppressed = () => false;

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
    public bool DisableSelectOnScreen = false;
    public bool DisableSelectOnMap = false;
    public bool DisableSelectInRect = false;
    public bool DisableSelectRotting = false;
    public bool DisableFertileZone = false;
    public bool ExcludeCorpsesFromAllowAll = true;
    public bool DisableSelectStored = false;
    public float PlantGrownLevel = 1f;

    // Diagnostic-only. Off by default. When true, MpSelectSimilarUnpatch logs one line per
    // Designate* invocation intercepted, and Designator_SelectSimilar.DesignateMultiCell logs
    // when a drag reaches the override. Intended for one-drag capture while diagnosing a
    // Multiplayer sync report — noisy under sustained use.
    public bool MpDebugLogging = false;

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

            GameFont orig = Text.Font;
            options.Label($"Version: {KeyzAllowUtilitiesMod.Version}");

            // Selection & Allowing
            options.GapLine();
            Text.Font = GameFont.Small;
            options.Label("KAU_Section_Selection".Translate());
            Text.Font = orig;
            options.Label("KeyzAllowUtilities_Settings_MaxSelect".Translate(MaxSelect));
            options.IntAdjuster(ref MaxSelect, 10, 0);
            options.CheckboxLabeled("KAU_ToggleSelection".Translate(), ref DisableSelection);
            if (!DisableSelection)
            {
                options.Indent();
                options.ColumnWidth -= Listing.ColumnSpacing;
                options.CheckboxLabeled("KAU_DisableSelectOnScreen".Translate(), ref DisableSelectOnScreen);
                options.CheckboxLabeled("KAU_DisableSelectOnMap".Translate(), ref DisableSelectOnMap);
                options.CheckboxLabeled("KAU_DisableSelectInRect".Translate(), ref DisableSelectInRect);
                options.CheckboxLabeled("KAU_DisableSelectRotting".Translate(), ref DisableSelectRotting);
                options.ColumnWidth += Listing.ColumnSpacing;
                options.Outdent();
            }
            options.CheckboxLabeled("KAU_DisableSelectStored".Translate(), ref DisableSelectStored);
            options.CheckboxLabeled("KAU_DisableClaimAll".Translate(), ref DisableClaimAll);
            options.CheckboxLabeled("KAU_ExcludeCorpsesFromAllowAll".Translate(), ref ExcludeCorpsesFromAllowAll);

            // Hauling
            options.GapLine();
            Text.Font = GameFont.Small;
            options.Label("KAU_Section_Hauling".Translate());
            Text.Font = orig;
            options.CheckboxLabeled("KAU_ToggleHaulUrgently".Translate(), ref DisableHaulUrgently);
            options.CheckboxLabeled("KAU_ToggleNoHauling".Translate(), ref DisableNoHauling);

            // Plants
            options.GapLine();
            Text.Font = GameFont.Small;
            options.Label("KAU_Section_Plants".Translate());
            Text.Font = orig;
            options.CheckboxLabeled("KAU_ToggleHarvest".Translate(), ref DisableHarvest);
            options.CheckboxLabeled("KAU_ToggleHarvestAll".Translate(), ref DisableHarvestAll);
            options.CheckboxLabeled("KAU_ToggleCut".Translate(), ref DisableCut);
            options.CheckboxLabeled("KAU_ToggleFertileZone".Translate(), ref DisableFertileZone);
            PlantGrownLevel = options.SliderLabeled("KAU_PlantGrownLevel".Translate(PlantGrownLevel*100), PlantGrownLevel, 0f, 1f);

            // Combat
            options.GapLine();
            Text.Font = GameFont.Small;
            options.Label("KAU_Section_Combat".Translate());
            Text.Font = orig;
            options.CheckboxLabeled("KAU_ToggleFinishOff".Translate(), ref DisableFinishOff);
            options.CheckboxLabeled("KAU_ToggleAllowFinishOffOnFriendly".Translate(), ref AllowFinishOffOnFriendly);
            options.CheckboxLabeled("KAU_DisableMeleeRequirementForFinishOff".Translate(), ref DisableMeleeRequirementForFinishOff);

            // Mining
            options.GapLine();
            Text.Font = GameFont.Small;
            options.Label("KAU_Section_Mining".Translate());
            Text.Font = orig;
            options.CheckboxLabeled("KAU_ToggleStripMine".Translate(), ref DisableStripMine);

            // Shortcuts
            options.GapLine();
            Text.Font = GameFont.Small;
            options.Label("KAU_Section_Shortcuts".Translate());
            Text.Font = orig;
            options.CheckboxLabeled("KAU_ToggleDisableAllowShortcuts".Translate(), ref DisableAllowShortcuts);
            options.CheckboxLabeled("KAU_ToggleDisableAllShortcuts".Translate(), ref DisableAllShortcuts);

            // Diagnostics
            options.GapLine();
            Text.Font = GameFont.Small;
            options.Label("KAU_Section_Diagnostics".Translate());
            Text.Font = orig;
            options.CheckboxLabeled("KAU_MpDebugLogging".Translate(), ref MpDebugLogging, "KAU_MpDebugLogging_Tip".Translate());

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

            // Suppressed under Multiplayer: this runs per-client on every load, so an unguarded
            // purge would let one client's local settings delete designations shared by everyone.
            if (SuppressDesignationPurge()) return;

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
        Scribe_Values.Look(ref DisableSelectOnScreen, "DisableSelectOnScreen", false);
        Scribe_Values.Look(ref DisableSelectOnMap, "DisableSelectOnMap", false);
        Scribe_Values.Look(ref DisableSelectInRect, "DisableSelectInRect", false);
        Scribe_Values.Look(ref DisableSelectRotting, "DisableSelectRotting", false);
        Scribe_Values.Look(ref DisableFertileZone, "DisableFertileZone", false);
        Scribe_Values.Look(ref ExcludeCorpsesFromAllowAll, "ExcludeCorpsesFromAllowAll", true);
        Scribe_Values.Look(ref DisableSelectStored, "DisableSelectStored", false);
        Scribe_Values.Look(ref AllowFinishOffOnFriendly, "AllowFinishOffOnFriendly", false);
        Scribe_Values.Look(ref DisableAllowShortcuts, "DisableAllowShortcuts", false);
        Scribe_Values.Look(ref DisableAllShortcuts, "DisableAllShortcuts", false);
        Scribe_Values.Look(ref DisableMeleeRequirementForFinishOff, "DisableMeleeRequirementForFinishOff", false);
        Scribe_Values.Look(ref PlantGrownLevel, "PlantGrownLevel", 1f);
        Scribe_Values.Look(ref MpDebugLogging, "MpDebugLogging", false);

        ValidateDesignators();
    }
}
