using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using KeyzAllowUtilities;
using Verse;
using Xunit;

namespace KeyzAllowUtilities.Tests
{
    [TestSubject(typeof(FilterUtils))]
    public class FilterUtilsTest
    {
        [Fact]
        public void NotFogged_WhenListIsEmpty_ReturnsEmpty()
        {
            var things = new List<Thing>();

            var result = things.NotFogged().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void NotFogged_ReturnsIEnumerableOfThings()
        {
            // NotFogged() is an extension on IEnumerable<T> where T : Thing.
            // It filters by fog grid on each thing's map via MapOrHolderMap().
            // Testing the fog-grid predicate requires a running game instance;
            // this test verifies the extension is reachable and returns a valid sequence.
            var things = new List<Thing>();

            IEnumerable<Thing> result = things.NotFogged();

            Assert.NotNull(result);
        }

        [Fact]
        public void ToDefSet_WhenListIsEmpty_ReturnsEmptyHashSet()
        {
            var things = new List<Thing>();

            HashSet<Def> result = things.ToDefSet();

            Assert.Empty(result);
        }

        [Fact]
        public void ToDefSet_ReturnsHashSetOfNonNullDefs()
        {
            // ToDefSet() materialises Thing.def values into a HashSet<Def>, skipping nulls.
            // Constructing real ThingDefs requires a running game; this test verifies
            // behaviour on a list containing a Thing with no def (null).
            var thingWithNullDef = new Thing(); // def is null by default

            var result = new List<Thing> { thingWithNullDef }.ToDefSet();

            Assert.Empty(result);
        }

        // ── OfDef ──────────────────────────────────────────────────────────────

        [Fact]
        public void OfDef_WhenListIsEmpty_ReturnsEmpty()
        {
            var things = new List<Thing>();

            var result = things.OfDef(new ThingDef()).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void OfDef_WhenThingHasNullDef_IsExcluded()
        {
            var thingWithNullDef = new Thing(); // def is null by default

            var result = new List<Thing> { thingWithNullDef }.OfDef(new ThingDef()).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void OfDef_WhenDefMatches_IsIncluded()
        {
            var def = new ThingDef();
            var thing = new Thing { def = def };

            var result = new List<Thing> { thing }.OfDef(def).ToList();

            Assert.Single(result);
            Assert.Same(thing, result[0]);
        }

        [Fact]
        public void OfDef_WhenDefDoesNotMatch_IsExcluded()
        {
            var def = new ThingDef();
            var otherDef = new ThingDef();
            var thing = new Thing { def = def };

            var result = new List<Thing> { thing }.OfDef(otherDef).ToList();

            Assert.Empty(result);
        }

        // ── OfDefs ─────────────────────────────────────────────────────────────

        [Fact]
        public void OfDefs_WhenListIsEmpty_ReturnsEmpty()
        {
            var things = new List<Thing>();
            var defs = new List<Def> { new ThingDef() };

            var result = things.OfDefs(defs).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void OfDefs_WhenThingHasNullDef_IsExcluded()
        {
            var thingWithNullDef = new Thing(); // def is null by default
            var defs = new List<Def> { new ThingDef() };

            var result = new List<Thing> { thingWithNullDef }.OfDefs(defs).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void OfDefs_WhenDefInSet_IsIncluded()
        {
            var def = new ThingDef();
            var thing = new Thing { def = def };
            var defs = new List<Def> { def };

            var result = new List<Thing> { thing }.OfDefs(defs).ToList();

            Assert.Single(result);
            Assert.Same(thing, result[0]);
        }

        [Fact]
        public void OfDefs_WhenDefNotInSet_IsExcluded()
        {
            var def = new ThingDef();
            var otherDef = new ThingDef();
            var thing = new Thing { def = def };
            var defs = new List<Def> { otherDef };

            var result = new List<Thing> { thing }.OfDefs(defs).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void OfDefs_WhenDefsIsAlreadyHashSet_FiltersBehaviorIdentically()
        {
            // OfDefs uses `defs as HashSet<Def> ?? defs.ToHashSet()` to avoid re-allocation.
            // Passing a HashSet<Def> directly exercises the fast-path; result must be the same.
            var def = new ThingDef();
            var thing = new Thing { def = def };
            var defSet = new HashSet<Def> { def };

            var result = new List<Thing> { thing }.OfDefs(defSet).ToList();

            Assert.Single(result);
            Assert.Same(thing, result[0]);
        }

        // ── NearestTo ──────────────────────────────────────────────────────────

        // NearestTo sorts by IntVec3.DistanceToSquared — pure integer math.
        // Thing.Position setter skips map-registration when the thing is not spawned,
        // so we can assign positions freely on unspawned Things.

        [Fact]
        public void NearestTo_WhenListIsEmpty_ReturnsEmpty()
        {
            var things = new List<Thing>();

            var result = things.NearestTo(new IntVec3(0, 0, 0)).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void NearestTo_OrdersByDistanceAscending()
        {
            var origin = new IntVec3(0, 0, 0);
            var near = new Thing();
            near.Position = new IntVec3(1, 0, 0);   // dist² = 1
            var mid = new Thing();
            mid.Position = new IntVec3(3, 0, 0);    // dist² = 9
            var far = new Thing();
            far.Position = new IntVec3(5, 0, 0);    // dist² = 25

            // Intentionally pass in reverse order to confirm sorting is applied
            var result = new List<Thing> { far, mid, near }.NearestTo(origin).ToList();

            Assert.Equal(3, result.Count);
            Assert.Same(near, result[0]);
            Assert.Same(mid, result[1]);
            Assert.Same(far, result[2]);
        }

        [Fact]
        public void NearestTo_ThingsAtSameDistance_AllAppear()
        {
            var origin = new IntVec3(0, 0, 0);
            var a = new Thing();
            a.Position = new IntVec3(2, 0, 0);  // dist² = 4
            var b = new Thing();
            b.Position = new IntVec3(-2, 0, 0); // dist² = 4

            var result = new List<Thing> { a, b }.NearestTo(origin).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(a, result);
            Assert.Contains(b, result);
        }

        // ── MapOrHolderMap ─────────────────────────────────────────────────────

        [Fact]
        public void MapOrHolderMap_WhenThingHasNoMapAndNoHolder_ReturnsNull()
        {
            // MapOrHolderMap first checks thing.Map; if null, falls back to
            // (thing.ParentHolder as Thing)?.Map. A freshly constructed Thing has
            // both null, so the result is null.
            // The holder-fallback branch requires a spawned IThingHolder, which needs
            // map infrastructure — that path is a known untestable gap.
            var thing = new Thing();

            var result = thing.MapOrHolderMap();

            Assert.Null(result);
        }
    }
}
