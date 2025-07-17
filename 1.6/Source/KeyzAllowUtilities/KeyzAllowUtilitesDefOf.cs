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
    public static readonly JobDef KAU_FinishOffPawn;
    public static readonly DesignationDef KAU_HaulUrgentlyDesignation;
    public static readonly DesignationDef KAU_FinishOffDesignation;
    public static readonly EffecterDef KAU_WeaponGlint;

    static KeyzAllowUtilitesDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(KeyzAllowUtilitesDefOf));
}
