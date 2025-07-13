using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[HarmonyPatch(typeof(Thing))]
[StaticConstructorOnStartup]
public static class Thing_Patches
{
    public static readonly Texture2D KUA_MultiSelect = ContentFinder<Texture2D>.Get("UI/KUA_MultiSelect");

    [HarmonyPatch(nameof(Thing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Thing __instance, ref IEnumerable<Gizmo> __result)
    {
        List<Gizmo> gizmos = __result.ToList();

        Command_Action command_Action = new()
        {
            icon = KUA_MultiSelect, defaultLabel = "KUA_MultiSelect".Translate(), defaultDesc = "KUA_MultiSelectDesc".Translate(), action = () =>
            {
                List<FloatMenuOption> items =
                [
                    new FloatMenuOption("KUA_SelectOnScreen".Translate(), () =>
                    {
                        FilterUtils.SelectOnScreen(__instance);
                    }),

                    new FloatMenuOption("KUA_SelectOnMap".Translate(), () =>
                    {
                        __instance.Map.SelectOnMap(__instance);
                    })

                ];

                Find.WindowStack.Add(new FloatMenu(items));
            }
        };
        gizmos.Add(command_Action);
        __result = gizmos;
    }

}
