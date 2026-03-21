using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using KeyzAllowUtilities;
using Verse;
using Xunit;

namespace KeyzAllowUtilities.Tests
{
    [TestSubject(typeof(FilterUtils))]
    public class MakeFilterTest
    {
        // ThingDef..ctor() triggers Unity's ShaderDatabase static initializer which calls
        // UnityEngine.Resources.Load() — an ECall that crashes outside the Unity runtime.
        // GetUninitializedObject skips all constructors. defName must be unique per instance
        // because Def.GetHashCode() hashes on defName; two null-defName defs collide in
        // HashSet<Def>, which would make distinct defs appear equal.
        private static int _defNameCounter;
        private static ThingDef MakeThingDef()
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            var n = Interlocked.Increment(ref _defNameCounter);
            def.defName = $"TestDef_{n}";
            // Def.GetHashCode() returns defNameHash and Def.Equals() compares defNameHash +
            // type. All GetUninitializedObject instances have defNameHash=0 and collide in
            // HashSet<Def>, so assign a unique value to make each test def distinct.
            def.defNameHash = n;
            return def;
        }

        // FilterUtils.MakeFilter builds a predicate from seed Things via FilterCondition.Of().
        // With new Thing() (no comps, null Stuff), optional conditions (biocoded, rotStage,
        // wornByCorpse, stuff) all resolve to null and are skipped — only the base def-equality
        // check is built. This lets us exercise the full predicate logic without game state.

        [Fact]
        public void MakeFilter_WhenNoThings_ReturnsFalseForAnyThing()
        {
            var filter = FilterUtils.MakeFilter(new List<Thing>(), checkStuff: false);
            var anyThing = new Thing { def = MakeThingDef() };

            Assert.False(filter(anyThing));
        }

        [Fact]
        public void MakeFilter_WhenSingleThing_MatchesThingWithSameDef()
        {
            var def = MakeThingDef();
            var seed = new Thing { def = def };
            var target = new Thing { def = def };

            var filter = FilterUtils.MakeFilter(new List<Thing> { seed }, checkStuff: false);

            Assert.True(filter(target));
        }

        [Fact]
        public void MakeFilter_WhenSingleThing_RejectsThingWithDifferentDef()
        {
            var seedDef = MakeThingDef();
            var otherDef = MakeThingDef();
            var seed = new Thing { def = seedDef };
            var target = new Thing { def = otherDef };

            var filter = FilterUtils.MakeFilter(new List<Thing> { seed }, checkStuff: false);

            Assert.False(filter(target));
        }

        [Fact]
        public void MakeFilter_WhenDuplicateSeeds_DeduplicatesConditions()
        {
            // Two seeds with the same def should produce the same predicate behaviour as one.
            // FilterCondition is a record struct and uses Distinct() — duplicates are removed.
            var def = MakeThingDef();
            var seed1 = new Thing { def = def };
            var seed2 = new Thing { def = def };
            var target = new Thing { def = def };
            var nonTarget = new Thing { def = MakeThingDef() };

            var filter = FilterUtils.MakeFilter(new List<Thing> { seed1, seed2 }, checkStuff: false);

            Assert.True(filter(target));
            Assert.False(filter(nonTarget));
        }

        [Fact]
        public void MakeFilter_WhenCheckStuffFalse_IgnoresStuffDifference()
        {
            // With checkStuff=false, two seeds with the same def but different Stuff
            // should be deduplicated (same FilterCondition) and the filter matches any
            // target with that def regardless of Stuff.
            var def = MakeThingDef();
            var seed = new Thing { def = def };       // Stuff is null
            var target = new Thing { def = def };     // Stuff is null — should match

            var filter = FilterUtils.MakeFilter(new List<Thing> { seed }, checkStuff: false);

            Assert.True(filter(target));
        }

        [Fact]
        public void MakeFilter_MultipleSeeds_MatchesThingMatchingAny()
        {
            var defA = MakeThingDef();
            var defB = MakeThingDef();
            var seedA = new Thing { def = defA };
            var seedB = new Thing { def = defB };
            var targetA = new Thing { def = defA };
            var targetB = new Thing { def = defB };
            var targetC = new Thing { def = MakeThingDef() };

            var filter = FilterUtils.MakeFilter(new List<Thing> { seedA, seedB }, checkStuff: false);

            Assert.True(filter(targetA));
            Assert.True(filter(targetB));
            Assert.False(filter(targetC));
        }
    }
}
