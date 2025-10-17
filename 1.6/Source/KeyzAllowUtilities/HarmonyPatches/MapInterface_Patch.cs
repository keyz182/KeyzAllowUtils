using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(MapInterface))]
public static class MapInterface_Patch
{
    [HarmonyPatch(nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs))]
    [HarmonyPrefix]
    public static bool MapInterfaceOnGUI_BeforeMainTabs(MapInterface __instance)
    {
        if (Find.CurrentMap == null)
            return true;

        if (GameComp.Instance != null && GameComp.Instance.EditModeActive)
        {
            GameComp.Instance.MapInterface_EditModeOnGUI(__instance);
            return false;
        }
        return true;
    }

}
