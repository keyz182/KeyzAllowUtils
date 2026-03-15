using System.Collections.Generic;
using System.Linq;
using KeyzAllowUtilities.HarmonyPatches;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_CutGrown : Designator_PlantsCut
{
    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableCut;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableCut;
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
        hotKey = KeyzAllowUtilitesDefOf.KAU_CutFullyGrown;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if(!base.CanDesignateThing(t)) return false;
        if (t is not Plant plant) return false;
        return !Plant_Patches.IsFullyGrown(plant);
    }

    public override void DesignateThing(Thing t)
    {
        base.DesignateThing(t);
        t.SetForbidden(false, false);
    }
}
