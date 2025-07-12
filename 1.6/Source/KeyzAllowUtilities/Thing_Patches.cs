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

    public static IEnumerable<Thing> NearestTo(this IEnumerable<Thing> things, Thing thing)
    {
        return things.OrderBy(t => t.Position.DistanceToSquared(thing.Position));
    }

    public static void SelectOnScreen(Thing thing, Map map)
    {
        Rect rect = new Rect(0.0f, 0.0f, (float) UI.screenWidth, (float) UI.screenHeight);
        IEnumerable<Thing> things = ThingSelectionUtility.MultiSelectableThingsInScreenRectDistinct(rect);

        foreach (Thing outerThing in things.Where(t=>t.def == thing.def).NearestTo(thing).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            Thing innerThing = outerThing.GetInnerIfMinified();
            if (innerThing.def == thing.def )
                Find.Selector.Select(innerThing);
        }
    }

    public static void SelectOnMap(Thing thing, Map map)
    {
        foreach (Thing mapthing in map.listerThings.AllThings.Where(t=>t.def == thing.def).NearestTo(thing).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            Find.Selector.Select(mapthing);
        }
    }

    [HarmonyPatch(nameof(Thing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Thing __instance, ref IEnumerable<Gizmo> __result)
    {
        List<Gizmo> gizmos = __result.ToList();

        Command_Action command_Action = new Command_Action();
        command_Action.icon = KUA_MultiSelect;
        command_Action.defaultLabel = "KUA_MultiSelect".Translate();
        command_Action.defaultDesc = "KUA_MultiSelectDesc".Translate();
        command_Action.action = () =>
        {
            var items = new List<FloatMenuOption>();

            items.Add(new FloatMenuOption("KUA_SelectOnScreen", () =>
            {
                SelectOnScreen(__instance, __instance.Map);
            }));

            items.Add(new FloatMenuOption("KUA_SelectOnMap", () =>
            {
                SelectOnMap(__instance, __instance.Map);
            }));

            Find.WindowStack.Add(new FloatMenu(items));
        };
        gizmos.Add(command_Action);
        __result = gizmos;
    }

}
