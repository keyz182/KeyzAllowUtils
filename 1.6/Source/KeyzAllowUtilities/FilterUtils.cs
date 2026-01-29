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

        public readonly Func<Thing, bool> MakeFilter()
        {
            Func<Func<Thing, bool>, Func<Thing, bool>> finalize = isMinified
                ? (baseFilter => target => target is MinifiedThing minifiedTarget && baseFilter(minifiedTarget.InnerThing))
                : (baseFilter => baseFilter);

            ThingDef def = this.def;
            Func<Thing, bool> baseFilter = target => target.def == def;

            if (wornByCorpse is bool expectedWornByCorpse)
            {
                Func<Thing, bool> baseFilter2 = baseFilter;

                baseFilter = target => baseFilter2(target) && (target as Apparel)?.WornByCorpse == expectedWornByCorpse;
            }

            if (biocoded is bool expectedBiocoded)
            {
                Func<Thing, bool> baseFilter2 = baseFilter;

                baseFilter = target => baseFilter2(target) && target.TryGetComp<CompBiocodable>()?.Biocoded == expectedBiocoded;
            }

            if (rotStage is RotStage expectedRotStage)
            {
                Func<Thing, bool> baseFilter2 = baseFilter;

                baseFilter = target => baseFilter2(target) && target.TryGetComp<CompRottable>()?.Stage == expectedRotStage;
            }

            if (stuff is ThingDef expectedStuff)
            {
                Func<Thing, bool> baseFilter2 = baseFilter;

                baseFilter = target => baseFilter2(target) && target.Stuff == expectedStuff;
            }

            return finalize(baseFilter);
        }
    }

    public static Func<Thing, bool> MakeFilter(IEnumerable<Thing> things, bool checkStuff)
    {
        return things.Select(thing => FilterCondition.Of(thing, checkStuff))
            .Distinct()
            .Select(condition => condition.MakeFilter())
            .Aggregate((Thing _) => false, (lhs, rhs) => thing => lhs(thing) || rhs(thing));
    }

    public static Lazy<MethodInfo> _GetMapRect = new(() => AccessTools.Method(typeof(ThingSelectionUtility), "GetMapRect"));
    public static CellRect GetMapRect(Rect rect)
    {
        return (CellRect) _GetMapRect.Value.Invoke(null, [rect]);
    }

    public static void SelectOnScreen(Thing thing, bool checkStuff, IEnumerable<Thing> selected)
    {
        IEnumerable<Thing> things = thing.MapOrHolderMap().ThingsOnScreen(MakeFilter(selected, checkStuff));

        foreach (Thing outerThing in things.NotFogged().NearestTo(thing.Map.GetCenterOfScreenOnMap()).Take(KeyzAllowUtilitiesMod.settings.MaxSelect))
        {
            Find.Selector.Select(outerThing);
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
            Find.Selector.Select(outerThing);
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
            return list.Where(t => t.def != null && defs.Contains(t.def));
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
                Find.Selector.Select(mapThing);
            }
        }

        public void SelectOnMap(Thing thing, bool checkStuff, IEnumerable<Thing> selected)
        {
            IEnumerable<Thing> things = map.Selectables().Where(MakeFilter(selected, checkStuff));

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
