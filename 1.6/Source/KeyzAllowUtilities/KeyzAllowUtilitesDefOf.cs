using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

[DefOf]
public static class KeyzAllowUtilitesDefOf
{
    // Designators
    public static readonly DesignationDef KAU_NoHaulDesignation;
    public static readonly DesignationDef KAU_HaulUrgentlyDesignation;
    public static readonly DesignationDef KAU_FinishOffDesignation;
    public static readonly DesignationDef KAU_StripFinishOffDesignation;
    public static readonly DesignationDef KAU_SelectSimilarDesignation;

    // Keybinds
    public static readonly KeyBindingDef KAU_Allow;
    public static readonly KeyBindingDef KAU_Forbid;
    public static readonly KeyBindingDef KAU_SelectSimilar;
    public static readonly KeyBindingDef KAU_HarvestFullyGrown;
    public static readonly KeyBindingDef KAU_HarvestFullyGrownWood;
    public static readonly KeyBindingDef KAU_CutFullyGrown;
    public static readonly KeyBindingDef KAU_HaulUrgently;
    public static readonly KeyBindingDef KAU_StripMine;
    public static readonly KeyBindingDef KAU_FinishOff;
    public static readonly KeyBindingDef KAU_FertileGrowArea;
    public static readonly KeyBindingDef KAU_FertileGrowAreaExpand;
    public static readonly KeyBindingDef KAU_SelectSimilarDesignator;
    public static readonly KeyBindingDef KAU_MapEditMode;

    // Jobs
    public static readonly JobDef KAU_FinishOffPawn;
    public static readonly JobDef KAU_StripFinishOffPawn;

    // Effecters
    public static readonly EffecterDef KAU_WeaponGlint;

    // Work types
    public static readonly WorkTypeDef KAU_FinishingOff;
    public static readonly WorkTypeDef KAU_UrgentHaul;

    static KeyzAllowUtilitesDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(KeyzAllowUtilitesDefOf));
}
