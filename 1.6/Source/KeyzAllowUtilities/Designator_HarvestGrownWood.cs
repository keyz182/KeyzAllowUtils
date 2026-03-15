using System;
using System.Reflection;
using HarmonyLib;
using KeyzAllowUtilities.HarmonyPatches;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_HarvestGrownWood : Designator_PlantsHarvestWood
{
    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableHarvest;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableHarvest;

    protected override DesignationDef Designation => DesignationDefOf.CutPlant;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_HarvestGrownWood()
    {
        defaultLabel = "KUA_HarvestGrownWood".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_HarvestGrownWood");
        defaultDesc = "KUA_HarvestGrownWoodDesc".Translate();
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_CutPlants;
        hotKey = KeyzAllowUtilitesDefOf.KAU_HarvestFullyGrownWood;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        AcceptanceReport report = base.CanDesignateThing(t);
        if (!report.Accepted) return report;

        if (t is not Plant tree) return false;
        return Plant_Patches.IsFullyGrown(tree);
    }

    public override void DesignateThing(Thing t)
    {
        base.DesignateThing(t);
        t.SetForbidden(false, false);
    }
}
