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
    private record struct FilterCondition
    (
        bool isMinified,
        ThingDef def, // If `isMinified` is `true`, this is the def of the inner thing.
        bool? wornByCorpse,
        bool? biocoded,
        RotStage? rotStage,
        ThingDef stuff
    )
    {
        public static FilterCondition Of(Thing thing, bool checkStuff)
        {
            bool isMinified;

            if (thing is MinifiedThing minifiedThing)
            {
                thing = minifiedThing.InnerThing;
                isMinified = true;
            }
            else
            {
                isMinified = false;
            }

            return new FilterCondition
            (
                isMinified,
                thing.def,
                (thing as Apparel)?.WornByCorpse,
                thing.TryGetComp<CompBiocodable>()?.Biocoded,
                thing.TryGetComp<CompRottable>()?.Stage,
                checkStuff ? thing.Stuff : null
            );
        }

        private readonly Func<Thing, bool> Finalize(Func<Thing, bool> baseFilter)
        {
            return isMinified ? target => target is MinifiedThing minifiedTarget && baseFilter(minifiedTarget.InnerThing) : baseFilter;
        }

        private readonly bool BaseFilter(Thing thing)
        {
            return thing.def == def;
        }

        public readonly Func<Thing, bool> MakeFilter()
        {
            FilterCondition condition = this;
            Func<Thing, bool> baseFilter = condition.BaseFilter;

            if (wornByCorpse is { } expectedWornByCorpse)
            {
                Func<Thing, bool> outerBaseFilter = baseFilter;

                baseFilter = target => outerBaseFilter(target) && (target as Apparel)?.WornByCorpse == expectedWornByCorpse;
            }

            if (biocoded is { } expectedBiocoded)
            {
                Func<Thing, bool> outerBaseFilter = baseFilter;

                baseFilter = target => outerBaseFilter(target) && target.TryGetComp<CompBiocodable>()?.Biocoded == expectedBiocoded;
            }

            if (rotStage is { } expectedRotStage)
            {
                Func<Thing, bool> outerBaseFilter = baseFilter;

                baseFilter = target => outerBaseFilter(target) && target.TryGetComp<CompRottable>()?.Stage == expectedRotStage;
            }

            // ReSharper disable once InvertIf
            if (stuff is { } expectedStuff)
            {
                Func<Thing, bool> outerBaseFilter = baseFilter;

                baseFilter = target => outerBaseFilter(target) && target.Stuff == expectedStuff;
            }

            return Finalize(baseFilter);
        }
    }

    public static Func<Thing, bool> MakeFilter(IEnumerable<Thing> things, bool checkStuff)
    {
        List<Func<Thing, bool>> funcs = things
            .Select(thing => FilterCondition.Of(thing, checkStuff))
            .Distinct()
            .Select(condition => condition.MakeFilter())
            .ToList();
        if (funcs.Count == 0) return _ => false;
        return thing => funcs.Any(f => f(thing));
    }

    private static readonly Lazy<MethodInfo> _getMapRect = new(() => AccessTools.Method(typeof(ThingSelectionUtility), "GetMapRect"));
    private static CellRect GetMapRect(Rect rect)
    {
        return (CellRect) _getMapRect.Value.Invoke(null, [rect]);
    }

    public static void SelectOnScreen(Thing thing, bool checkStuff, IEnumerable<Thing> selected)
    {
        IEnumerable<Thing> things = thing.MapOrHolderMap().ThingsOnScreen(MakeFilter(selected, checkStuff));

        foreach (Thing outerThing in things.NotFogged().NearestTo(thing.Map.GetCenterOfScreenOnMap()).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            Find.Selector.Select(outerThing, forceDesignatorDeselect: false);
        }
    }

    public static IEnumerable<Thing> Selectables(this Map map, Func<Thing, bool> filter = null)
    {
        IEnumerable<Thing> result = map.listerThings.AllThings.Where(ThingSelectionUtility.SelectableByMapClick);

        return filter == null ? result : result.Where(filter);
    }

    public static void SelectAnyOnScreen(Map map, IntVec3 pos, Func<Thing, bool> filter)
    {
        IEnumerable<Thing> things = map.ThingsOnScreen(filter);

        foreach (Thing outerThing in things.NotFogged().NearestTo(pos).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            Find.Selector.Select(outerThing, forceDesignatorDeselect: false);
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
            return list.Where(t => t.def != null && t.def == def);
        }

        public IEnumerable<T> OfDefs(IEnumerable<Def> defs)
        {
            HashSet<Def> defSet = defs as HashSet<Def> ?? defs.ToHashSet();
            return list.Where(t => t.def != null && defSet.Contains(t.def));
        }

        public IEnumerable<T> OnlySelectableThings()
        {
            return list.Where(ThingSelectionUtility.SelectableByMapClick);
        }
    }

    extension(Thing outerThing)
    {
        public Map MapOrHolderMap()
        {
            if (outerThing.Map != null)
                return outerThing.Map;
            return outerThing.ParentHolder is not Thing t ? null : t.Map;
        }
    }

    extension(Map map)
    {
        public void SelectAnyOnMap(IntVec3 pos, Func<Thing, bool> filter)
        {
            IEnumerable<Thing> matchingThings = map.Selectables(filter);

            foreach (Thing mapThing in matchingThings.NearestTo(pos).NotFogged().Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
            {
                Find.Selector.Select(mapThing, forceDesignatorDeselect: false);
            }
        }

        public void SelectOnMap(Thing thing, bool checkStuff, IEnumerable<Thing> selected)
        {
            IEnumerable<Thing> things = map.Selectables().Where(MakeFilter(selected, checkStuff));

            foreach (Thing mapthing in things.NearestTo(thing).NotFogged().Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
            {
                Find.Selector.Select(mapthing, forceDesignatorDeselect: false);
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
            return cells.SelectMany(map.thingGrid.ThingsAt);
        }

        public IEnumerable<Thing> GetThingsInRadius<T>(IntVec3 center, int radius)
            where T : Thing
        {
            return GenRadial.RadialCellsAround(center, radius, true).SelectMany(map.thingGrid.ThingsAt).OfType<T>().OrderBy(t => t.Position.DistanceTo(center));
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

        public IEnumerable<Thing> ThingsOnScreen(Func<Thing, bool> filter)
        {
            Rect rect = new(0.0f, 0.0f, UI.screenWidth, UI.screenHeight);
            CellRect mapRect = GetMapRect(rect);
            IEnumerable<IntVec3> cells = mapRect.ExpandedBy(1).Cells.Where(cell => cell.InBounds(onMap));
            IEnumerable<Thing> things = cells.SelectMany(onMap.thingGrid.ThingsAt).Where(ThingSelectionUtility.SelectableByMapClick);

            return things.Where(filter);
        }
    }
}
