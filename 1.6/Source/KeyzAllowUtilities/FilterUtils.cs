using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public static class FilterUtils
{
    public static IEnumerable<Thing> NotFogged(this IEnumerable<Thing> list)
    {
        return list.Where(t => !t.Map.fogGrid.IsFogged(t.Position));
    }

    public static IEnumerable<Thing> NearestTo(this IEnumerable<Thing> things, LocalTargetInfo target)
    {
        return things.OrderBy(t => t.Position.DistanceToSquared(target.Cell));
    }

    public static IEnumerable<Thing> OfDef(this IEnumerable<Thing> things, Def def)
    {
        return things.Where(t=> t.def != null && t.def == def);
    }

    public static IEnumerable<Thing> OfDefs(this IEnumerable<Thing> things, IEnumerable<Def> defs)
    {
        return things.Where(t=> t.def != null && defs.Contains(t.def));
    }

    public static IEnumerable<Thing> OnMap(this IEnumerable<Thing> things, Map map)
    {
        return things.Where(t=> map.listerThings.AllThings.Contains(t));
    }

    public static IEnumerable<Thing> OnlySelectableThings(this IEnumerable<Thing> things)
    {
        return things.Where(ThingSelectionUtility.SelectableByMapClick);
    }

    public static void SelectThisOrInnerThing(this Thing outerThing, Def def)
    {
        Thing innerThing = outerThing.GetInnerIfMinified();
        if (innerThing.def == def )
            Find.Selector.Select(innerThing);
    }

    public static void SelectOnScreen(Thing thing, ThingDef stuff = null)
    {
        bool Filter(Thing t)
        {
            bool allow = ThingSelectionUtility.SelectableByMapClick(t) && t.def == thing.def;

            if (stuff != null && t.Stuff != stuff) allow = false;

            return allow;
        }

        IEnumerable<Thing> things = thing.Map.ThingsOnScreen(Filter);

        foreach (Thing outerThing in things.NotFogged().NearestTo(thing).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            outerThing.SelectThisOrInnerThing(thing.def);
        }
    }

    public static void SelectOnMap(this Map map, Thing thing, ThingDef stuff = null)
    {
        IEnumerable<Thing> things = map.listerThings.AllThings.Where(t => t.def == thing.def);

        if (stuff != null)
            things = things.Where(t => t.Stuff == stuff);

        foreach (Thing mapthing in things.NearestTo(thing).NotFogged().Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            Find.Selector.Select(mapthing);
        }
    }

    public static IEnumerable<CompForbiddable> ForbiddableThings(this Map map, Def ofDef = null)
    {
        IEnumerable<ThingWithComps> things = ofDef == null ? map.listerThings.AllThings.OfType<ThingWithComps>() : map.listerThings.AllThings.OfType<ThingWithComps>().Where(t => t.def == ofDef);
        things = things.Where(t => t.HasComp<CompForbiddable>()).Where(t => !map.fogGrid.IsFogged(t.Position));
        things = things.Where(t => t.def is { EverHaulable: true });
        return things.Select(t => t.GetComp<CompForbiddable>());
    }

    public static IEnumerable<Thing> GetThingsInRadius(this Map map, IntVec3 center, int radius)
    {
        IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(center, radius, true);
        return cells.SelectMany(c => map.thingGrid.ThingsAt(c));
    }

    public static IEnumerable<Thing> GetThingsInRadius<T>(this Map map, IntVec3 center, int radius)
        where T : Thing
    {
        return GenRadial.RadialCellsAround(center, radius, true).SelectMany(c => map.thingGrid.ThingsAt(c)).OfType<T>();
    }

    public static void SelectAllOnMapFromHotKey(this Map onMap)
    {
        if (Find.Selector.NumSelected > 0)
        {
            List<ThingDef> uniqueDefs = Find.Selector.SelectedObjects.OfType<Thing>().Select(t => t.def).Distinct().ToList();

            IEnumerable<Thing> matchingThings = onMap.listerThings.AllThings.Where(t => uniqueDefs.Contains(t.def));

            foreach (Thing thing in matchingThings.NotFogged().NearestTo(onMap.Center).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
            {
                Find.Selector.Select(thing);
            }
        }
    }

    public static Lazy<MethodInfo> _GetMapRect = new(() => AccessTools.Method(typeof(ThingSelectionUtility), "GetMapRect"));
    public static CellRect GetMapRect(Rect rect)
    {
        return (CellRect) _GetMapRect.Value.Invoke(null, [rect]);
    }

    public static IEnumerable<Thing> ThingsOnScreen(this Map onMap, Predicate<Thing> filter)
    {
        Rect rect = new(0.0f, 0.0f, UI.screenWidth, UI.screenHeight);
        CellRect mapRect = GetMapRect(rect);
        List<IntVec3> cells = mapRect.ExpandedBy(1).Cells.Where(cell => cell.InBounds(onMap)).ToList();
        List<Thing> things = cells.SelectMany(cell => onMap.thingGrid.ThingsAt(cell)).ToList();

        foreach (Thing thing in things)
        {
            if (filter(thing)) yield return thing;
        }
    }
}
