using System.Collections.Generic;
using System.Linq;
using KeyzAllowUtilities.HarmonyPatches;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_CutBlighted : Designator_PlantsCut, IClearsOwnDesignationsOnly
{
    public DesignationDef ClearDesignation => DesignationDefOf.CutPlant;

    // Deliberately the inherited Designator_PlantsCut predicate rather than "is blighted":
    // blight clears once a plant is cut or cured, and scoping to it would strand existing
    // cut designations with no designator able to clear them.
    public bool AffectsForClear(LocalTargetInfo target) => RemoveAllDesignationsAffects(target);

    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableCut;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableCut;
    protected override DesignationDef Designation => DesignationDefOf.CutPlant;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_CutBlighted()
    {
        defaultLabel = "KUA_CutBlighted".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KUA_CutBlighted");
        defaultDesc = "KUA_CutBlightedDesc".Translate();
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_CutPlants;
        hotKey = KeyzAllowUtilitesDefOf.KAU_CutBlighted;
    }

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (t.def.plant == null || t is not Plant plant)
            return false;
        if (Map.designationManager.DesignationOn(t, designationDef) != null)
            return false;

        return plant.Blighted;
    }

    public override void DesignateThing(Thing t)
    {
        base.DesignateThing(t);
        t.SetForbidden(false, false);
    }
}
