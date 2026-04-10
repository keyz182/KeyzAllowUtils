using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class Designator_ZoneAdd_GrowingFertile : Designator_ZoneAdd_Growing
{
    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableFertileZone;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableFertileZone;

    public static float FertileSoilMinLevel = 1.4f;

    public Designator_ZoneAdd_GrowingFertile()
    {
        zoneTypeToPlace = typeof (Zone_Growing);
        defaultLabel = "KAU_GrowingZone".Translate();
        defaultDesc = "KAU_DesignatorGrowingZoneDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing");
        tutorTag = "ZoneAdd_Growing";
        hotKey = KeyzAllowUtilitesDefOf.KAU_FertileGrowArea;
        soundSucceeded = SoundDefOf.Designate_ZoneAdd_Growing;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        // Skip Designator_ZoneAdd_Growing.CanDesignateCell — it calls
        // BuildCopyCommandUtility.FindAllowedDesignator per cell, which is
        // extremely expensive with many mods. Our fertility threshold (≥1.4)
        // is strictly stricter than vanilla's, so the parent check is redundant.
        // Inline Designator_ZoneAdd.CanDesignateCell instead.
        // Cache Map (Designator.Map resolves Find.CurrentMap each call).
        Map map = Map;

        if (!c.InBounds(map))
            return false;

        Zone zone = map.zoneManager.ZoneAt(c);
        if (zone != null && zone.GetType() != zoneTypeToPlace)
            return false;

        // Check fertility first — cheaper than IsZoneableCell (which iterates
        // thingGrid) and fails more often on non-fertile terrain.
        if (c.GetFertility(map) < (double)FertileSoilMinLevel)
            return false;

        return Designator_ZoneAdd.IsZoneableCell(c, map);
    }
}
