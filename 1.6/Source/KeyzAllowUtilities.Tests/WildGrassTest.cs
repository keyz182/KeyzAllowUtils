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
    [TestSubject(typeof(Plant_Patches))]
    public class WildGrassTest
    {
        // Same construction strategy as DesignatorClearTest: ThingDef..ctor() reaches Unity's
        // ShaderDatabase static initializer, so skip constructors entirely.
        private static int _defNameCounter;

        private static ThingDef MakePlantDef(string defName, bool sowable)
        {
            var plantProps = (PlantProperties)RuntimeHelpers.GetUninitializedObject(typeof(PlantProperties));
            plantProps.sowTags = sowable ? new List<string> { "Ground" } : null;

            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = defName;
            def.defNameHash = Interlocked.Increment(ref _defNameCounter);
            def.plant = plantProps;
            return def;
        }

        private static Plant MakePlant(ThingDef def) => new Plant { def = def };

        private static void SetHarvestWildGrass(bool value) =>
            KeyzAllowUtilitiesMod.settings = new Settings { HarvestWildGrass = value };

        [Theory]
        [InlineData("Plant_Grass")]
        [InlineData("Plant_TallGrass")]
        [InlineData("MO_WildGrass")]
        public void IsWildGrass_UnsowableGrass_ReturnsTrue(string defName)
        {
            Assert.True(Plant_Patches.IsWildGrass(MakePlantDef(defName, sowable: false)));
        }

        [Fact]
        public void IsWildGrass_SowableHaygrass_ReturnsFalse()
        {
            Assert.False(Plant_Patches.IsWildGrass(MakePlantDef("Plant_Haygrass", sowable: true)));
        }

        [Fact]
        public void IsWildGrass_UnsowableNonGrass_ReturnsFalse()
        {
            Assert.False(Plant_Patches.IsWildGrass(MakePlantDef("Plant_TreeOak", sowable: false)));
        }

        [Fact]
        public void IsWildGrass_NullDefOrNotAPlant_ReturnsFalse()
        {
            Assert.False(Plant_Patches.IsWildGrass(null));
            var notPlant = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            notPlant.defName = "Grass_Rug";
            Assert.False(Plant_Patches.IsWildGrass(notPlant));
        }

        [Fact]
        public void SkipAsWildGrass_SettingOff_SkipsWildGrass()
        {
            SetHarvestWildGrass(false);
            Assert.True(Plant_Patches.SkipAsWildGrass(MakePlant(MakePlantDef("Plant_Grass", sowable: false))));
        }

        [Fact]
        public void SkipAsWildGrass_SettingOn_KeepsWildGrass()
        {
            SetHarvestWildGrass(true);
            Assert.False(Plant_Patches.SkipAsWildGrass(MakePlant(MakePlantDef("Plant_Grass", sowable: false))));
        }

        [Fact]
        public void SkipAsWildGrass_SettingOff_KeepsSownCrop()
        {
            SetHarvestWildGrass(false);
            Assert.False(Plant_Patches.SkipAsWildGrass(MakePlant(MakePlantDef("Plant_Haygrass", sowable: true))));
        }
    }
}
