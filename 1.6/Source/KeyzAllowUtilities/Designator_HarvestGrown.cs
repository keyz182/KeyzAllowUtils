using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_HarvestGrown : Designator_PlantsHarvestWood
{
    public static Lazy<FieldInfo> CanDesignateStumpsNow => new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(Designator_HarvestGrown), nameof(CanDesignateStumpsNow)));
    protected override DesignationDef Designation => DesignationDefOf.CutPlant;

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
    }

    public static bool Harvestable(Plant plant)
    {
        return Mathf.Approximately(plant.Growth, 1f) && plant.HarvestableNow;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        AcceptanceReport acceptanceReport = base.CanDesignateThing(t);
        if (!acceptanceReport.Accepted)
            return acceptanceReport;
        Plant plant = (Plant) t;

        if (!Harvestable(plant)) return false;

        if (t.TryGetComp(out CompPlantPreventCutting comp) && comp.PreventCutting)
            return "MessageMustPlantCuttingForbidden".Translate();

        if (!plant.HarvestableNow || plant.def.plant.harvestTag != "Standard")
        {
            return t.def.plant.isStump && !(bool)CanDesignateStumpsNow.Value.GetValue(this) ? false : (AcceptanceReport) true;
        }

        return true;
    }


    protected override bool RemoveAllDesignationsAffects(LocalTargetInfo target)
    {
        return target.Thing.def.plant.harvestTag == "Standard" || target.Thing.def.plant.IsTree;
    }

    public override void DesignateThing(Thing t)
    {
        PossiblyWarnPlayerImportantPlantDesignateCut(t);
        if (ModsConfig.IdeologyActive && t.def.plant.IsTree && t.def.plant.treeLoversCareIfChopped)
            PossiblyWarnPlayerOnDesignatingTreeCut();

        Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) t, DesignationDefOf.HarvestPlant));
        t.SetForbidden(false, false);
    }
}
