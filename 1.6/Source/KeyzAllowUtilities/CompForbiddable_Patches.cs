using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

[HarmonyPatch(typeof(CompForbiddable))]
public static class CompForbiddable_Patches
{
    [HarmonyPatch(nameof(CompForbiddable.CompGetGizmosExtra))]
    [HarmonyPostfix]
    public static void CompGetGizmosExtra_Patch(CompForbiddable __instance, ref IEnumerable<Gizmo> __result)
    {
        List<Gizmo> gizmos = new List<Gizmo>(__result);
        for (int i = 0; i < gizmos.Count; i++)
        {
            if (gizmos[i] is Command_Toggle command_Toggle && command_Toggle.hotKey == KeyBindingDefOf.Command_ItemForbid && command_Toggle.icon == TexCommand.ForbidOff)
            {
                Command_Toggle_WithContext newCmd = new();
                newCmd.hotKey = command_Toggle.hotKey;
                newCmd.icon = command_Toggle.icon;
                newCmd.isActive = command_Toggle.isActive;
                newCmd.defaultLabel = command_Toggle.defaultLabel;
                newCmd.activateIfAmbiguous =  command_Toggle.activateIfAmbiguous;
                newCmd.defaultDesc =  command_Toggle.defaultDesc;
                newCmd.tutorTag = command_Toggle.tutorTag;
                newCmd.toggleAction = command_Toggle.toggleAction;
                newCmd.RightClickAction = () =>
                {
                    if(__instance.parent.Map == null) return;

                    FloatMenuOption AllowAll = new FloatMenuOption("KUA_AllowAll".Translate(), () =>
                    {
                        KeyHandler.AllowAll(__instance.parent.Map, false, __instance.parent.def);
                    });

                    FloatMenuOption ForbidAll = new FloatMenuOption("KUA_ForbidAll".Translate(), () =>
                    {
                        KeyHandler.AllowAll(__instance.parent.Map, true, __instance.parent.def);
                    });

                    FloatMenu menu = new FloatMenu([AllowAll, ForbidAll]);

                    Find.WindowStack.Add(menu);
                };
                gizmos[i] = newCmd;
            }
        }

        __result = gizmos;
    }

}
