#if NET10_0_OR_GREATER
using System;
using HarmonyLib;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

/// <summary>
/// Vanilla builds the Hunt, Tame, Mine, Deconstruct, Strip, Cancel (and similar) buttons on a
/// selected thing through Designator.CreateReverseDesignationGizmo. Shift-click on any of them
/// now picks up that designator for dragging; a normal click is unchanged. 1.7 only.
/// </summary>
[HarmonyPatch(typeof(Designator), nameof(Designator.CreateReverseDesignationGizmo))]
public static class ReverseDesignator_ShiftSelect_Patch
{
    private static readonly Lazy<string> ShiftTip = new(() => "KAU_ShiftClickTip".Translate());

    [HarmonyPostfix]
    public static void Postfix(Designator __instance, Command_Action __result)
    {
        if (__result?.action == null) return;
        Action original = __result.action;
        __result.action = () =>
        {
            if (ShiftSelect.TrySelect(__instance)) return;
            original();
        };
        __result.defaultDesc = __result.defaultDesc + "\n\n" + ShiftTip.Value;
    }
}
#endif
