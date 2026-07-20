using KeyzAllowUtilities.HarmonyPatches;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_HarvestGrown : Designator_Plants, IClearsOwnDesignationsOnly
{
    public DesignationDef ClearDesignation => DesignationDefOf.HarvestPlant;

    public bool AffectsForClear(LocalTargetInfo target) => RemoveAllDesignationsAffects(target);

    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableHarvest;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableHarvest;
    protected override DesignationDef Designation => DesignationDefOf.HarvestPlant;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_HarvestGrown()
    {
        defaultLabel = "KUA_HarvestGrown".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_HarvestGrown");
        defaultDesc = "KUA_HarvestGrownDesc".Translate();
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_CutPlants;
        hotKey = KeyzAllowUtilitesDefOf.KAU_HarvestFullyGrown;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (t.def.plant == null)
            return "KAU_NotAPlant".Translate();

        if (t is not Plant plant)
        {
            return "KAU_NotAPlant".Translate();
        }

        if (!plant.def.plant.Harvestable) return "KAU_NotHarvestable".Translate();

        if (plant.def.plant.harvestTag == "Wood" || plant.def.plant.harvestedThingDef == null) return "KAU_NoHarvestableThing".Translate();

        if(Map.designationManager.AllDesignationsOn(plant).Any(des=>des.def == DesignationDefOf.HarvestPlant)) return "KAU_AlreadyDesignated".Translate();

        if (!Plant_Patches.IsFullyGrown(plant))
        {
            return "KAU_NotFullyGrown".Translate();
        }

        if (t.TryGetComp(out CompPlantPreventCutting comp) && comp.PreventCutting)
            return "MessageMustPlantCuttingForbidden".Translate();

        return true;
    }


    // Scoped to what this designator can actually place (CanDesignateThing rejects "Wood" and
    // plants with no harvested product), so clearing here does not wipe tree-chop designations.
    protected override bool RemoveAllDesignationsAffects(LocalTargetInfo target)
    {
        return Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable(target);
    }

    public override void DesignateThing(Thing t)
    {
        Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) t, DesignationDefOf.HarvestPlant));
        t.SetForbidden(false, false);
    }
}
