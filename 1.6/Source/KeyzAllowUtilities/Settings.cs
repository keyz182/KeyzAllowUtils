using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Settings : ModSettings
{
    public int MaxSelect = 300;
    public void DoWindowContents(Rect wrect)
    {
        var options = new Listing_Standard();
        options.Begin(wrect);

        options.Label("KeyzAllowUtilities_Settings_MaxSelect".Translate(MaxSelect));
        options.IntAdjuster(ref MaxSelect,  10, 0);
        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref MaxSelect, "MaxSelect", 300);
    }
}
