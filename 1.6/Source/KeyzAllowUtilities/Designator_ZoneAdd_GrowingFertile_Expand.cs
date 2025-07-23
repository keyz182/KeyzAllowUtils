using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

public class Designator_ZoneAdd_GrowingFertile_Expand : Designator_ZoneAdd_GrowingFertile
{
    protected override bool ShowRightClickHideOptions => false;

    public Designator_ZoneAdd_GrowingFertile_Expand()
    {
        defaultLabel = "KAU_DesignatorZoneExpand".Translate();
        hotKey = KeyBindingDefOf.Misc7;
    }
}
