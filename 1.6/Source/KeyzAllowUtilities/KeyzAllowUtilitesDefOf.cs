using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

[DefOf]
public static class KeyzAllowUtilitesDefOf
{
    public static readonly KeyBindingDef KAU_DesignatorAllow;
    public static readonly KeyBindingDef KAU_DesignatorForbid;

    static KeyzAllowUtilitesDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(KeyzAllowUtilitesDefOf));
}
