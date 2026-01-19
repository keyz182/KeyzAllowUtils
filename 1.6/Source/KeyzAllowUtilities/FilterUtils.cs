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
    public static Lazy<MethodInfo> _GetMapRect = new(() => AccessTools.Method(typeof(ThingSelectionUtility), "GetMapRect"));
    public static CellRect GetMapRect(Rect rect)
    {
        return (CellRect) _GetMapRect.Value.Invoke(null, [rect]);
    }

    public static void SelectOnScreen(Thing thing, ThingDef stuff = null, Predicate<Thing> filter = null)
    {
        IEnumerable<Thing> things = thing.MapOrHolderMap().ThingsOnScreen(Filter);

        foreach (Thing outerThing in things.NotFogged().NearestTo(thing.Map.GetCenterOfScreenOnMap()).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            outerThing.SelectThisOrInnerThing(thing.def);
        }

        return;

        bool Filter(Thing t)
        {
            bool allow = ThingSelectionUtility.SelectableByMapClick(t) && t.def == thing.def;

            if (stuff != null && t.Stuff != stuff) allow = false;

            return filter?.Invoke(t) ?? allow;
        }
    }

    public static IEnumerable<Thing> Selectables(this Map map, Predicate<Thing> filter = null)
    {
        return map.listerThings.AllThings.Where(ThingSelectionUtility.SelectableByMapClick).Where(t=>filter?.Invoke(t) ?? true);
    }

    public static void SelectAnyOnScreen(Map map, IntVec3 pos, ThingDef stuff = null, Predicate<Thing> filter = null)
    {
        IEnumerable<Thing> things = map.ThingsOnScreen(Filter);

        foreach (Thing outerThing in things.NotFogged().NearestTo(pos).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            outerThing.SelectThisOrInnerThing();
        }

        return;

        bool Filter(Thing t)
        {
            bool allow = ThingSelectionUtility.SelectableByMapClick(t);

            if (stuff != null && t.Stuff != stuff) allow = false;

            return filter?.Invoke(t) ?? allow;
        }
    }

    extension<T>(IEnumerable<T> list) where T : Thing
    {
        public IEnumerable<T> NearestTo(LocalTargetInfo target)
        {
            return list.OrderBy(t => t.Position.DistanceToSquared(target.Cell));
        }

        public IEnumerable<T> NotFogged()
        {
            return list.Where(t => !t.MapOrHolderMap().fogGrid.IsFogged(t.Position));
        }

        public IEnumerable<T> OfDef(Def def)
        {
            return list.Where(t=> t.def != null && t.def == def);
        }

        public IEnumerable<T> OfDefs(IEnumerable<Def> defs)
        {
            return list.Where(t=> t.def != null && defs.Contains(t.def));
        }

        public IEnumerable<T> OnMap(Map map)
        {
            return list.Where(t=> map.Selectables().Contains(t));
        }

        public IEnumerable<T> OnlySelectableThings()
        {
            return list.Where(ThingSelectionUtility.SelectableByMapClick);
        }
    }

    extension(Thing outerThing)
    {
        public void SelectThisOrInnerThing(Def def = null)
        {
            Thing innerThing = outerThing.GetInnerIfMinified();
            if (def == null || innerThing.def == def )
                Find.Selector.Select(innerThing);
        }

        public Map MapOrHolderMap()
        {
            if (outerThing.Map != null) return outerThing.Map;
            return outerThing.ParentHolder is not Thing t ? null : t.Map;
        }
    }

    extension(Map map)
    {
        public void SelectAnyOnMap(IntVec3 pos, Predicate<Thing> filter = null)
        {
            IEnumerable<Thing> matchingThings = map.Selectables(filter);

            foreach (Thing mapThing in matchingThings.NearestTo(pos).NotFogged().Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
            {
                Find.Selector.Select(mapThing);
            }
        }

        public void SelectMultiOnMap(List<object> things, Predicate<Thing> filter = null)
        {
            IEnumerable<ThingDef> uniqueThings = things.OfType<Thing>().Select(t => t.def).Distinct();

            IEnumerable<Thing> matchingThings = map.Selectables(filter).Where(t => uniqueThings.Contains(t.def));

            foreach (Thing mapThing in matchingThings.NearestTo(map.Center).NotFogged().Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
            {
                Find.Selector.Select(mapThing);
            }
        }

        public void SelectMultiOnMapByStuff(List<object> things)
        {
            var pairs = things.OfType<Thing>().Select(t => new { t.def, t.Stuff }).Distinct();

            IEnumerable<Thing> matchingThings = map.Selectables().Where(t => pairs.Contains(new { t.def, t.Stuff }));

            foreach (Thing mapThing in matchingThings.NearestTo(map.Center).NotFogged().Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
            {
                Find.Selector.Select(mapThing);
            }
        }

        public void SelectOnMap(Thing thing, ThingDef stuff = null)
        {
            IEnumerable<Thing> things = map.Selectables().Where(t => t.def == thing.def);

            if (stuff != null)
                things = things.Where(t => t.Stuff == stuff);

            foreach (Thing mapthing in things.NearestTo(thing).NotFogged().Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
            {
                Find.Selector.Select(mapthing);
            }
        }

        public IEnumerable<CompForbiddable> ForbiddableThings(Def ofDef = null)
        {
            IEnumerable<ThingWithComps> things = ofDef == null ? map.listerThings.AllThings.OfType<ThingWithComps>() : map.listerThings.AllThings.OfType<ThingWithComps>().Where(t => t.def == ofDef);
            things = things.Where(t => t.HasComp<CompForbiddable>()).Where(t => !map.fogGrid.IsFogged(t.Position));
            things = things.Where(t => t.def is { EverHaulable: true });
            return things.Select(t => t.GetComp<CompForbiddable>());
        }

        public IEnumerable<Thing> GetThingsInRadius(IntVec3 center, int radius)
        {
            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(center, radius, true);
            return cells.SelectMany(c => map.thingGrid.ThingsAt(c));
        }

        public IEnumerable<Thing> GetThingsInRadius<T>(IntVec3 center, int radius)
            where T : Thing
        {
            return GenRadial.RadialCellsAround(center, radius, true).SelectMany(c => map.thingGrid.ThingsAt(c)).OfType<T>().OrderBy(t=>t.Position.DistanceTo(center));
        }
    }

    extension(Map onMap)
    {
        public IntVec3 GetMapCenter()
        {
            return onMap.Center;
        }

        public IntVec3 GetCenterOfScreenOnMap()
        {
            Rect rect = new(0.0f, 0.0f, UI.screenWidth, UI.screenHeight);
            CellRect mapRect = GetMapRect(rect);
            return mapRect.CenterCell;
        }

        public IEnumerable<Thing> ThingsOnScreen(Predicate<Thing> filter)
        {
            Rect rect = new(0.0f, 0.0f, UI.screenWidth, UI.screenHeight);
            CellRect mapRect = GetMapRect(rect);
            List<IntVec3> cells = mapRect.ExpandedBy(1).Cells.Where(cell => cell.InBounds(onMap)).ToList();
            List<Thing> things = cells.SelectMany(cell => onMap.thingGrid.ThingsAt(cell)).Where(ThingSelectionUtility.SelectableByMapClick).ToList();

            foreach (Thing thing in things)
            {
                if (filter(thing)) yield return thing;
            }
        }
    }
}
