using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

[DefOf]
public static class KeyzAllowUtilitesDefOf
{
    public static readonly KeyBindingDef KAU_Allow;
    public static readonly KeyBindingDef KAU_Forbid;
    public static readonly KeyBindingDef KAU_SelectSimilar;
    public static readonly KeyBindingDef KAU_HarvestFullyGrown;
    public static readonly KeyBindingDef KAU_HaulUrgently;
    public static readonly KeyBindingDef KAU_FinishOff;
    public static readonly JobDef KAU_FinishOffPawn;
    public static readonly JobDef KAU_StripFinishOffPawn;
    public static readonly DesignationDef KAU_HaulUrgentlyDesignation;
    public static readonly DesignationDef KAU_FinishOffDesignation;
    public static readonly DesignationDef KAU_StripFinishOffDesignation;
    public static readonly DesignationDef KAU_SelectSimilarDesignation;
    public static readonly EffecterDef KAU_WeaponGlint;
    public static readonly WorkTypeDef KAU_FinishingOff;
    public static readonly WorkTypeDef KAU_UrgentHaul;

    static KeyzAllowUtilitesDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(KeyzAllowUtilitesDefOf));
}
