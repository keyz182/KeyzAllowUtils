using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using KeyzAllowUtilities.HarmonyPatches;
using RimWorld;
using Verse;
using Xunit;

namespace KeyzAllowUtilities.Tests
{
    [TestSubject(typeof(Designator_RightClickFloatMenuOptions_Patch))]
    public class DesignatorClearTest
    {
        // Same construction strategy as MakeFilterTest: ThingDef..ctor() reaches Unity's
        // ShaderDatabase static initializer, which crashes outside the Unity runtime.
        // GetUninitializedObject skips all constructors, and unique defNames keep distinct
        // defs from colliding via Def.GetHashCode().
        private static int _defNameCounter;

        private static T MakeDef<T>() where T : Def
        {
            var def = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
            var n = Interlocked.Increment(ref _defNameCounter);
            def.defName = $"TestDef_{n}";
            def.defNameHash = n;
            return def;
        }

        /// <summary>
        /// A plant whose harvestTag / harvested product decide whether the non-wood harvest
        /// designators consider it theirs to clear.
        /// </summary>
        private static Thing MakePlantThing(string harvestTag, bool hasHarvestedThing)
        {
            var plantProps = (PlantProperties)RuntimeHelpers.GetUninitializedObject(typeof(PlantProperties));
            plantProps.harvestTag = harvestTag;
            plantProps.harvestedThingDef = hasHarvestedThing ? MakeDef<ThingDef>() : null;

            var def = MakeDef<ThingDef>();
            def.plant = plantProps;

            return new Thing { def = def };
        }

        private static Designation MakeDesignation(Thing t, DesignationDef def)
        {
            return new Designation(t, def);
        }

        [Fact]
        public void AffectedOnly_WhenDesignationsNull_ReturnsEmpty()
        {
            var result = Designator_RightClickFloatMenuOptions_Patch.AffectedOnly(null, _ => true);

            Assert.Empty(result);
        }

        [Fact]
        public void AffectedOnly_KeepsOnlyTargetsMatchingPredicate()
        {
            var def = MakeDef<DesignationDef>();
            var crop = MakePlantThing("Standard", hasHarvestedThing: true);
            var tree = MakePlantThing("Wood", hasHarvestedThing: true);

            var designations = new List<Designation>
            {
                MakeDesignation(crop, def),
                MakeDesignation(tree, def)
            };

            var result = Designator_RightClickFloatMenuOptions_Patch.AffectedOnly(
                designations, Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable);

            Assert.Single(result);
            Assert.Same(crop, result[0].target.Thing);
        }

        [Fact]
        public void AffectedOnly_CountMatchesTheSetThatWouldBeRemoved()
        {
            // This is the invariant the original bug violated: vanilla filtered the displayed
            // count by RemoveAllDesignationsAffects but removed every designation of the def.
            // One materialised list drives both, so they cannot diverge.
            var def = MakeDef<DesignationDef>();
            var designations = new List<Designation>
            {
                MakeDesignation(MakePlantThing("Standard", hasHarvestedThing: true), def),
                MakeDesignation(MakePlantThing("Wood", hasHarvestedThing: true), def),
                MakeDesignation(MakePlantThing("Standard", hasHarvestedThing: true), def),
                MakeDesignation(MakePlantThing("Wood", hasHarvestedThing: true), def)
            };

            var affected = Designator_RightClickFloatMenuOptions_Patch.AffectedOnly(
                designations, Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable);

            Assert.Equal(2, affected.Count);
            foreach (var designation in affected)
            {
                Assert.Equal("Standard", designation.target.Thing.def.plant.harvestTag);
            }
        }

        [Fact]
        public void AffectedOnly_WhenNothingMatches_ReturnsEmpty()
        {
            var def = MakeDef<DesignationDef>();
            var designations = new List<Designation>
            {
                MakeDesignation(MakePlantThing("Wood", hasHarvestedThing: true), def)
            };

            var result = Designator_RightClickFloatMenuOptions_Patch.AffectedOnly(
                designations, Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable);

            Assert.Empty(result);
        }

        [Fact]
        public void IsNonWoodHarvestable_WhenHarvestTagIsWood_ReturnsFalse()
        {
            var tree = MakePlantThing("Wood", hasHarvestedThing: true);

            Assert.False(Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable(tree));
        }

        [Fact]
        public void IsNonWoodHarvestable_WhenNoHarvestedThingDef_ReturnsFalse()
        {
            var barren = MakePlantThing("Standard", hasHarvestedThing: false);

            Assert.False(Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable(barren));
        }

        [Fact]
        public void IsNonWoodHarvestable_WhenStandardPlantWithProduct_ReturnsTrue()
        {
            var crop = MakePlantThing("Standard", hasHarvestedThing: true);

            Assert.True(Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable(crop));
        }

        [Fact]
        public void IsNonWoodHarvestable_WhenThingIsNotAPlant_ReturnsFalse()
        {
            var notAPlant = new Thing { def = MakeDef<ThingDef>() };

            Assert.False(Designator_RightClickFloatMenuOptions_Patch.IsNonWoodHarvestable(notAPlant));
        }
    }
}
