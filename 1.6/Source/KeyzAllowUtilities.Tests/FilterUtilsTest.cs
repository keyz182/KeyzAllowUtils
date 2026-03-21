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
    }
}
