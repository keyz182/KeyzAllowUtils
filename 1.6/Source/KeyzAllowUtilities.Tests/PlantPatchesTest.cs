using JetBrains.Annotations;
using KeyzAllowUtilities;
using KeyzAllowUtilities.HarmonyPatches;
using RimWorld;
using Xunit;

namespace KeyzAllowUtilities.Tests
{
    [TestSubject(typeof(Plant_Patches))]
    public class PlantPatchesTest
    {
        // Plant_Patches.IsFullyGrown reads KeyzAllowUtilitiesMod.settings.PlantGrownLevel
        // (a static field) and checks Plant.Growth / Plant.LifeStage.
        // Both Plant.Growth and the settings field can be set without game infrastructure.

        private static Plant MakePlant(float growth)
        {
            var plant = new Plant();
            plant.Growth = growth;
            return plant;
        }

        private static void SetLevel(float level) =>
            KeyzAllowUtilitiesMod.settings = new Settings { PlantGrownLevel = level };

        // ── Level == 1f: delegates to LifeStage ───────────────────────────────

        [Fact]
        public void IsFullyGrown_WhenLevelIs1_AndGrowthIs1_ReturnsTrue()
        {
            SetLevel(1f);
            var plant = MakePlant(1f); // LifeStage == Mature

            Assert.True(Plant_Patches.IsFullyGrown(plant));
        }

        [Fact]
        public void IsFullyGrown_WhenLevelIs1_AndGrowthBelow1_ReturnsFalse()
        {
            SetLevel(1f);
            var plant = MakePlant(0.5f); // LifeStage != Mature

            Assert.False(Plant_Patches.IsFullyGrown(plant));
        }

        // ── Level < 1f: delegates to Growth >= level (float comparison) ───────

        [Fact]
        public void IsFullyGrown_WhenLevelBelow1_AndGrowthExceedsLevel_ReturnsTrue()
        {
            SetLevel(0.75f);
            var plant = MakePlant(0.9f);

            Assert.True(Plant_Patches.IsFullyGrown(plant));
        }

        [Fact]
        public void IsFullyGrown_WhenLevelBelow1_AndGrowthBelowLevel_ReturnsFalse()
        {
            SetLevel(0.75f);
            var plant = MakePlant(0.5f);

            Assert.False(Plant_Patches.IsFullyGrown(plant));
        }

        [Fact]
        public void IsFullyGrown_WhenGrowthEqualsLevel_ReturnsTrue()
        {
            // Growth >= level — equality is inclusive
            SetLevel(0.5f);
            var plant = MakePlant(0.5f);

            Assert.True(Plant_Patches.IsFullyGrown(plant));
        }
    }
}
