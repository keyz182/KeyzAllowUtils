using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_CutGrown : Designator_PlantsCut
{
    protected override DesignationDef Designation => DesignationDefOf.CutPlant;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_CutGrown()
    {
        defaultLabel = "KUA_CutGrown".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_CutGrown");
        defaultDesc = "KUA_CutGrownDesc".Translate();
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
        return base.CanDesignateThing(t) && Harvestable((Plant) t);
    }

    public override void DesignateThing(Thing t)
    {
        Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) t, DesignationDefOf.CutPlant));
        t.SetForbidden(false, false);
    }
}
