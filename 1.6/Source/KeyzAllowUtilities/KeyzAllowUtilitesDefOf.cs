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

    static KeyzAllowUtilitesDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(KeyzAllowUtilitesDefOf));
}
