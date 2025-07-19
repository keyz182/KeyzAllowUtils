using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Settings : ModSettings
{
    public int MaxSelect = 300;
    public bool DisableHaulUrgently = false;
    public bool DisableAllowShortcuts = false;
    public bool EnableDangerousOpts = false;

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.Label("KeyzAllowUtilities_Settings_MaxSelect".Translate(MaxSelect));
        options.IntAdjuster(ref MaxSelect,  10, 0);
        options.Gap();

        options.CheckboxLabeled("KAU_ToggleHaulUrgently".Translate(), ref DisableHaulUrgently);
        options.Gap();

        options.CheckboxLabeled("KAU_ToggleDisableAllowShortcuts".Translate(), ref DisableAllowShortcuts);
        options.Gap();

        options.CheckboxLabeled("KAU_ToggleDangerousOptimisations".Translate(), ref EnableDangerousOpts, tooltip:"KAU_ToggleDangerousOptimisationsToolTip".Translate());
        options.End();

        if (DisableHaulUrgently && !Find.Maps.NullOrEmpty())
        {
            foreach (Map map in Find.Maps)
            {
                map.designationManager.RemoveAllDesignationsOfDef(KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);
            }
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref MaxSelect, "MaxSelect", 300);
        Scribe_Values.Look(ref DisableHaulUrgently, "DisableHaulUrgently", false);
        Scribe_Values.Look(ref DisableAllowShortcuts, "DisableAllowShortcuts", false);
        Scribe_Values.Look(ref EnableDangerousOpts, "EnableDangerousOpts", false);
    }
}
