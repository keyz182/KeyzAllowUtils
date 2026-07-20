using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_HarvestAllWood : Designator_PlantsHarvestWood
{
    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableHarvestAll;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableHarvestAll;

    protected override DesignationDef Designation => DesignationDefOf.HarvestPlant;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_HarvestAllWood()
    {
        defaultLabel = "KUA_HarvestAllWood".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_HarvestGrownWood");
        defaultDesc = "KUA_HarvestAllWoodDesc".Translate();
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_CutPlants;
        hotKey = KeyzAllowUtilitesDefOf.KAU_HarvestAllWood;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        AcceptanceReport report = base.CanDesignateThing(t);
        if (!report.Accepted) return report;
        if (t is not Plant) return false;
        return true;
    }

    public override void DesignateThing(Thing t)
    {
        base.DesignateThing(t);
        t.SetForbidden(false, false);
    }
}
