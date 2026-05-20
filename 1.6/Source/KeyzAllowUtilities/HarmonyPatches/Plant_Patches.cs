using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Plant))]
[StaticConstructorOnStartup]
public static class Plant_Patches
{
    public static readonly Texture2D KUA_HarvestGrown = ContentFinder<Texture2D>.Get("UI/KUA_HarvestGrown");
    public static readonly Texture2D KUA_HarvestGrownWood = ContentFinder<Texture2D>.Get("UI/KUA_HarvestGrownWood");
    public static readonly Texture2D KUA_CutGrown = ContentFinder<Texture2D>.Get("UI/KUA_CutGrown");

    public static Lazy<string> KUA_HarvestGrown_Label = new(() => "KUA_HarvestGrown".Translate());
    public static Lazy<string> KUA_HarvestGrownDesc_Label = new(() => "KUA_HarvestGrownDesc".Translate());
    public static Lazy<string> KUA_CutGrown_Label = new(() => "KUA_CutGrown".Translate());
    public static Lazy<string> KUA_CutGrownDesc_Label = new(() => "KUA_CutGrownDesc".Translate());
    public static Lazy<string> KUA_HarvestAll_Label = new(() => "KUA_HarvestAll".Translate());
    public static Lazy<string> KUA_HarvestAllDesc_Label = new(() => "KUA_HarvestAllDesc".Translate());
    public static Lazy<string> KUA_HarvestAllWood_Label = new(() => "KUA_HarvestAllWood".Translate());
    public static Lazy<string> KUA_HarvestAllWoodDesc_Label = new(() => "KUA_HarvestAllWoodDesc".Translate());
    public static Lazy<string> KUA_HarvestOnScreen_Label = new(() => "KUA_HarvestOnScreen".Translate());
    public static Lazy<string> KUA_HarvestOnMap_Label = new(() => "KUA_HarvestOnMap".Translate());
    public static Lazy<string> KUA_CutGrownOnScreen_Label = new(() => "KUA_CutGrownOnScreen".Translate());
    public static Lazy<string> KUA_CutGrownOnMap_Label = new(() => "KUA_CutGrownOnMap".Translate());
    public static Lazy<string> KUA_HarvestAllOnScreen_Label = new(() => "KUA_HarvestAllOnScreen".Translate());
    public static Lazy<string> KUA_HarvestAllOnMap_Label = new(() => "KUA_HarvestAllOnMap".Translate());
    public static Lazy<string> KUA_HarvestAllWoodOnScreen_Label = new(() => "KUA_HarvestAllWoodOnScreen".Translate());
    public static Lazy<string> KUA_HarvestAllWoodOnMap_Label = new(() => "KUA_HarvestAllWoodOnMap".Translate());

    public static bool IsFullyGrown(Plant plant)
    {
        float level = KeyzAllowUtilitiesMod.settings.PlantGrownLevel;
        if (level >= 1f)
            return plant.LifeStage == PlantLifeStage.Mature;
        return plant.Growth >= level;
    }

    public static int DesignateFullyGrownOnScreen(IEnumerable<Plant> things, Map map, DesignationDef designation, bool checkIfHarvestable = true)
    {
        IEnumerable<Plant> plants = map.SelectablePlantsOnScreen()
            .OfDefs(things.ToDefSet())
            .OnlySelectableThings()
            .NotFogged()
            .NearestTo(map.GetCenterOfScreenOnMap());
        return ApplyPlantDesignation(plants, p => IsFullyGrown(p) && (!checkIfHarvestable || p.HarvestableNow), designation);
    }

    public static int DesignateFullyGrownOnMap(this Map map, IEnumerable<Plant> things, DesignationDef designation, bool checkIfHarvestable = true)
    {
        IEnumerable<Plant> plants = map.listerThings.AllThings
            .OfDefs(things.ToDefSet())
            .OnlySelectableThings()
            .NotFogged()
            .OfType<Plant>();
        return ApplyPlantDesignation(plants, p => IsFullyGrown(p) && (!checkIfHarvestable || p.HarvestableNow), designation);
    }

    public static int DesignateAnyOnScreen(IEnumerable<Plant> things, Map map, DesignationDef designation, bool checkIfHarvestable = true)
    {
        IEnumerable<Plant> plants = map.SelectablePlantsOnScreen()
            .OfDefs(things.ToDefSet())
            .OnlySelectableThings()
            .NotFogged()
            .NearestTo(map.GetCenterOfScreenOnMap());
        return ApplyPlantDesignation(plants, p => !checkIfHarvestable || p.HarvestableNow, designation);
    }

    public static int DesignateAnyOnMap(this Map map, IEnumerable<Plant> things, DesignationDef designation, bool checkIfHarvestable = true)
    {
        IEnumerable<Plant> plants = map.listerThings.AllThings
            .OfDefs(things.ToDefSet())
            .OnlySelectableThings()
            .NotFogged()
            .OfType<Plant>();
        return ApplyPlantDesignation(plants, p => !checkIfHarvestable || p.HarvestableNow, designation);
    }

    private static int ApplyPlantDesignation(IEnumerable<Plant> plants, Func<Plant, bool> predicate, DesignationDef designation)
    {
        int count = 0;
        foreach (Plant plant in plants)
        {
            if (predicate(plant))
            {
                plant.Map.designationManager.RemoveAllDesignationsOn(plant);
                plant.Map.designationManager.AddDesignation(new Designation((LocalTargetInfo)plant, designation));
                count++;
            }
        }
        return count;
    }

    public static void ReportDesignated(int count)
    {
        if (count <= 0) return;
        Messages.Message("KUA_DesignatedCount".Translate(count), MessageTypeDefOf.NeutralEvent, historical: false);
    }

    public static bool TryGetSelectedOfCategory(ThingCategory category, out List<Thing> things)
    {
        things = [];
        if (Find.Selector.NumSelected <= 0) return false;

        things = Find.Selector.SelectedObjects.OfType<Thing>().Where(t => t.def?.category == category).ToList();
        if (things.Count > 0) return true;

        things = [];
        return false;
    }

    public static Lazy<Designator_PlantsHarvest> HarvestPlants = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_PlantsHarvest>().FirstOrDefault());
    public static Lazy<Designator_PlantsCut> CutPlants = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_PlantsCut>().FirstOrDefault());

    [HarmonyPatch(nameof(Plant.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Plant __instance, ref IEnumerable<Gizmo> __result)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableHarvest && KeyzAllowUtilitiesMod.settings.DisableHarvestAll) return;
        if (!__instance.def.plant.Harvestable) return;

        List<Gizmo> gizmos = __result.ToList();

        if (!KeyzAllowUtilitiesMod.settings.DisableHarvest)
        {
            if (!__instance.def.plant.IsTree)
            {
                Command_Action harvestGrownCommand = new()
                {
                    icon = KUA_HarvestGrown,
                    defaultLabel = KUA_HarvestGrown_Label.Value,
                    defaultDesc = KUA_HarvestGrownDesc_Label.Value,
                    action = () =>
                    {
                        if (Event.current.shift)
                        {
                            Find.DesignatorManager.Select(HarvestPlants.Value);
                            return;
                        }
                        List<FloatMenuOption> items =
                        [
                            new(KUA_HarvestOnScreen_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += DesignateFullyGrownOnScreen(things.OfType<Plant>(), __instance.Map, DesignationDefOf.HarvestPlant);
                                }

                                n += DesignateFullyGrownOnScreen([__instance], __instance.Map, DesignationDefOf.HarvestPlant);
                                ReportDesignated(n);
                            }),

                            new(KUA_HarvestOnMap_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += __instance.Map.DesignateFullyGrownOnMap(things.OfType<Plant>(), DesignationDefOf.HarvestPlant);
                                }

                                n += __instance.Map.DesignateFullyGrownOnMap([__instance], DesignationDefOf.HarvestPlant);
                                ReportDesignated(n);
                            })
                        ];

                        Find.WindowStack.Add(new FloatMenu(items));
                    }
                };
                gizmos.Add(harvestGrownCommand);
            }
            else
            {
                Command_Action cutGrownCommand = new()
                {
                    icon = KUA_CutGrown,
                    defaultLabel = KUA_CutGrown_Label.Value,
                    defaultDesc = KUA_CutGrownDesc_Label.Value,
                    action = () =>
                    {
                        if (Event.current.shift)
                        {
                            Find.DesignatorManager.Select(CutPlants.Value);
                            return;
                        }
                        List<FloatMenuOption> items =
                        [
                            new(KUA_CutGrownOnScreen_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += DesignateFullyGrownOnScreen(things.OfType<Plant>(), __instance.Map, DesignationDefOf.CutPlant, false);
                                }

                                n += DesignateFullyGrownOnScreen([__instance], __instance.Map, DesignationDefOf.CutPlant, false);
                                ReportDesignated(n);
                            }),

                            new(KUA_CutGrownOnMap_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += __instance.Map.DesignateFullyGrownOnMap(things.OfType<Plant>(), DesignationDefOf.CutPlant, false);
                                }

                                n += __instance.Map.DesignateFullyGrownOnMap([__instance], DesignationDefOf.CutPlant, false);
                                ReportDesignated(n);
                            })
                        ];

                        Find.WindowStack.Add(new FloatMenu(items));
                    }
                };
                gizmos.Add(cutGrownCommand);
            }
        }

        if (!KeyzAllowUtilitiesMod.settings.DisableHarvestAll)
        {
            if (!__instance.def.plant.IsTree)
            {
                Command_Action harvestAllCommand = new()
                {
                    icon = KUA_HarvestGrown,
                    defaultLabel = KUA_HarvestAll_Label.Value,
                    defaultDesc = KUA_HarvestAllDesc_Label.Value,
                    action = () =>
                    {
                        List<FloatMenuOption> items =
                        [
                            new(KUA_HarvestAllOnScreen_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += DesignateAnyOnScreen(things.OfType<Plant>(), __instance.Map, DesignationDefOf.HarvestPlant, false);
                                }

                                n += DesignateAnyOnScreen([__instance], __instance.Map, DesignationDefOf.HarvestPlant, false);
                                ReportDesignated(n);
                            }),

                            new(KUA_HarvestAllOnMap_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += __instance.Map.DesignateAnyOnMap(things.OfType<Plant>(), DesignationDefOf.HarvestPlant, false);
                                }

                                n += __instance.Map.DesignateAnyOnMap([__instance], DesignationDefOf.HarvestPlant, false);
                                ReportDesignated(n);
                            })
                        ];

                        Find.WindowStack.Add(new FloatMenu(items));
                    }
                };
                gizmos.Add(harvestAllCommand);
            }
            else
            {
                Command_Action harvestAllWoodCommand = new()
                {
                    icon = KUA_HarvestGrownWood,
                    defaultLabel = KUA_HarvestAllWood_Label.Value,
                    defaultDesc = KUA_HarvestAllWoodDesc_Label.Value,
                    action = () =>
                    {
                        List<FloatMenuOption> items =
                        [
                            new(KUA_HarvestAllWoodOnScreen_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += DesignateAnyOnScreen(things.OfType<Plant>(), __instance.Map, DesignationDefOf.CutPlant, false);
                                }

                                n += DesignateAnyOnScreen([__instance], __instance.Map, DesignationDefOf.CutPlant, false);
                                ReportDesignated(n);
                            }),

                            new(KUA_HarvestAllWoodOnMap_Label.Value, () =>
                            {
                                int n = 0;
                                if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                                {
                                    n += __instance.Map.DesignateAnyOnMap(things.OfType<Plant>(), DesignationDefOf.CutPlant, false);
                                }

                                n += __instance.Map.DesignateAnyOnMap([__instance], DesignationDefOf.CutPlant, false);
                                ReportDesignated(n);
                            })
                        ];

                        Find.WindowStack.Add(new FloatMenu(items));
                    }
                };
                gizmos.Add(harvestAllWoodCommand);
            }
        }

        __result = gizmos;
    }

}
