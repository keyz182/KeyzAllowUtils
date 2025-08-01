using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace KeyzAllowUtilities;

public static class Listing_Standard_Utils
{
    public static void IntAdjusterWithDisplay(this Listing_Standard listing, ref int val, int countChange, int min = 0)
    {
        Rect rect = listing.GetRect(24f) with { width = 42f };
        if (Widgets.ButtonText(rect, "-" + countChange))
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            val -= countChange * GenUI.CurrentAdjustmentMultiplier();
            if (val < min)
                val = min;
        }
        rect.x += rect.width + 2f;
        if (Widgets.ButtonText(rect, "+" + countChange))
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            val += countChange * GenUI.CurrentAdjustmentMultiplier();
            if (val < min)
                val = min;
        }

        rect.x += 44f;

        Widgets.Label(rect, $@"{val}");
        listing.Gap(listing.verticalSpacing);
    }
}
