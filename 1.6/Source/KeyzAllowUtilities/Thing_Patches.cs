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

    public static readonly Texture2D KUA_ToggleHaulUrgently = ContentFinder<Texture2D>.Get("UI/KUA_ToggleHaulUrgently");
    public static readonly Texture2D KUA_ToggleHaulUrgentlyDisable = ContentFinder<Texture2D>.Get("UI/KUA_ToggleHaulUrgentlyDisable");


    [HarmonyPatch(nameof(Thing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Thing __instance, ref IEnumerable<Gizmo> __result)
    {
        List<Gizmo> gizmos = __result.ToList();

        Command_Action command_Action = new()
        {
            icon = KUA_MultiSelect, defaultLabel = "KUA_MultiSelect".Translate(), defaultDesc = "KUA_MultiSelectDesc".Translate(), action = () =>
            {
                List<FloatMenuOption> items = [];

                if (Event.current.shift || !__instance.def.MadeFromStuff)
                {
                    items.Add(new FloatMenuOption("KUA_SelectOnScreen".Translate(), () =>
                    {
                        FilterUtils.SelectOnScreen(__instance);
                    }));
                    items.Add(new FloatMenuOption("KUA_SelectOnMap".Translate(), () =>
                    {
                        __instance.Map.SelectOnMap(__instance);
                    }));
                }
                else
                {
                    items.Add(new FloatMenuOption("KUA_SelectOnScreenWithStuff".Translate(__instance.Stuff.LabelAsStuff), () =>
                    {
                        FilterUtils.SelectOnScreen(__instance, __instance.Stuff);
                    }));
                    items.Add(new FloatMenuOption("KUA_SelectOnMapWithStuff".Translate(__instance.Stuff.LabelAsStuff), () =>
                    {
                        __instance.Map.SelectOnMap(__instance, __instance.Stuff);
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(items));
            }
        };
        gizmos.Add(command_Action);


        if (__instance is not Pawn && __instance.def.EverHaulable)
        {
            Designation des = __instance?.Map?.designationManager?.DesignationOn(__instance, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation);

            if (des == null)
            {
                gizmos.Add( new Command_Action()
                {
                    icon = KUA_ToggleHaulUrgently, defaultLabel = "KUA_ToggleHaulUrgently".Translate(), defaultDesc = "KUA_ToggleHaulUrgentlyDesc".Translate(), action = () =>
                    {
                        __instance?.Map.designationManager.AddDesignation(new Designation(__instance, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
                    }
                });
            }
            else
            {
                gizmos.Add( new Command_Action()
                {
                    icon = KUA_ToggleHaulUrgentlyDisable, defaultLabel = "KUA_ToggleHaulUrgentlyDisable".Translate(), defaultDesc = "KUA_ToggleHaulUrgentlyDisableDesc".Translate(), action = () =>
                    {
                        __instance?.Map.designationManager.RemoveDesignation(des);
                    }
                });
            }
        }

        __result = gizmos;
    }

}
