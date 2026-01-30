using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace KeyzAllowUtilities;

public static class Listing_Standard_Utils
{
    public static void IntAdjusterWithDisplay(this Listing_Standard listing, ref int val, int countChange, int min = 0)
    {
        Rect rect = listing.GetRect(18f) with { width = 24f };
        if (Widgets.ButtonText(rect, $"<size=10%>-{countChange}</size>"))
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            val -= countChange * GenUI.CurrentAdjustmentMultiplier();
            if (val < min)
                val = min;
        }
        rect.x += rect.width + 2f;
        if (Widgets.ButtonText(rect, $"<size=10%>+{countChange}</size>"))
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            val += countChange * GenUI.CurrentAdjustmentMultiplier();
            if (val < min)
                val = min;
        }

        rect.x += rect.width + 2f;
        rect.y -= 2f;

        Widgets.Label(rect, $@"<size=10%>{val}</size>");
        listing.Gap(listing.verticalSpacing);
    }
}
