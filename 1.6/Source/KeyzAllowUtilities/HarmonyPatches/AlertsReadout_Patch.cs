using HarmonyLib;
using RimWorld;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(AlertsReadout))]
public static class AlertsReadout_Patch
{
    [HarmonyPatch(nameof(AlertsReadout.AlertsReadoutOnGUI))]
    [HarmonyPrefix]
    public static bool AlertsReadoutOnGUI()
    {
        if (GameComp.Instance != null && GameComp.Instance.EditModeActive)
        {
            return false;
        }
        return true;
    }

}
