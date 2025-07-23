using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Designator_ZoneAdd_GrowingFertile : Designator_ZoneAdd_Growing
{
    public static float FertileSoilMinLevel = 1.4f;

    public Designator_ZoneAdd_GrowingFertile()
    {
        zoneTypeToPlace = typeof (Zone_Growing);
        defaultLabel = "KAU_GrowingZone".Translate();
        defaultDesc = "KAU_DesignatorGrowingZoneDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing");
        tutorTag = "ZoneAdd_Growing";
        hotKey = KeyBindingDefOf.Misc11;
        soundSucceeded = SoundDefOf.Designate_ZoneAdd_Growing;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!base.CanDesignateCell(c).Accepted)
            return false;

        return c.GetFertility(Map) < (double) FertileSoilMinLevel ? false : (AcceptanceReport) true;
    }
}
