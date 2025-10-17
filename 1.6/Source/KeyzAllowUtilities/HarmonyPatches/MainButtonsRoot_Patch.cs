using HarmonyLib;
using RimWorld;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(MainButtonsRoot))]
public static class MainButtonsRoot_Patch
{
    [HarmonyPatch(nameof(MainButtonsRoot.MainButtonsOnGUI))]
    [HarmonyPrefix]
    public static bool MainButtonsOnGUI_BeforeMainTabs()
    {
        if (GameComp.Instance != null && GameComp.Instance.EditModeActive)
        {
            return false;
        }
        return true;
    }
}
