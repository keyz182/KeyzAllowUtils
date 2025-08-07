using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public static class DebugActions
{
    public static Lazy<FieldInfo> _allCavePlants = new(()=>AccessTools.Field(typeof(WildPlantSpawner), "allCavePlants"));
    public static List<ThingDef> allCavePlants => _allCavePlants.Value.GetValue(null) as List<ThingDef>;

    public static Lazy<FieldInfo> _distanceSqToNearbyClusters = new(()=>AccessTools.Field(typeof(WildPlantSpawner), "distanceSqToNearbyClusters"));
    public static Dictionary<ThingDef, float> distanceSqToNearbyClusters => _distanceSqToNearbyClusters.Value.GetValue(null) as Dictionary<ThingDef, float>;

    public static void CavePlantsForSpot(IntVec3 c, Map map, List<ThingDef> outPlants)
    {
        foreach (ThingDef t in allCavePlants)
        {
            if (t.CanEverPlantAt(c, map, false))
                outPlants.Add(t);
        }
    }

    public static Lazy<MethodInfo> EnoughLowerOrderPlantsNearby = new(()=>AccessTools.Method(typeof(WildPlantSpawner), "EnoughLowerOrderPlantsNearby"));
    public static Lazy<MethodInfo> PlantChoiceWeight = new(()=>AccessTools.Method(typeof(WildPlantSpawner), "PlantChoiceWeight"));

    public static void PlantsForSpot(IntVec3 c, Map map, List<ThingDef> outPlants, float plantDensity)
    {
        List<ThingDef> allWildPlants = map.Biome.AllWildPlants;
        foreach (ThingDef plantDef in allWildPlants)
        {
            if (plantDef.CanEverPlantAt(c, map, false))
            {
                if (Mathf.Approximately(plantDef.plant.wildOrder, map.Biome.LowestWildAndCavePlantOrder))
                {
                    float num = 7f;
                    if (plantDef.plant.GrowsInClusters)
                        num = Math.Max(num, plantDef.plant.wildClusterRadius * 1.5f);
                    bool flag = EnoughLowerOrderPlantsNearby.Value.Invoke(map.wildPlantSpawner, [c, plantDensity, num, plantDef]) as bool? ?? false;
                    if (!flag)
                        continue;
                }
                outPlants.Add(plantDef);
            }
        }
    }

    private static List<KeyValuePair<ThingDef, float>> tmpPossiblePlantsWithWeight = new();

    [DebugAction(
        "Map",
        "Grow plants to maturity in area (rect)",
        false,
        false,
        false,
        false,
        false,
        0,
        false,
        actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap,
        displayPriority = 100)]
    private static void GrowPlantsToMaturity()
    {
        DebugToolsGeneral.GenericRectTool("Grow", rect =>
        {
            foreach (IntVec3 c in rect.Cells)
            {
                Plant plant = c.GetPlant(Find.CurrentMap);
                if (plant?.def.plant == null)
                    return;
                plant.Growth = 1f;
                Find.CurrentMap.mapDrawer.SectionAt(c).RegenerateAllLayers();
            }
        });
    }

    [DebugAction("Map", "Replant area (rect)", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100)]
    private static void ReplantArea()
    {
        DebugToolsGeneral.GenericRectTool("Replant", rect =>
        {
            float currentPlantDensity = 1;
            float numDesiredPlants = rect.Cells.Count();

            foreach (IntVec3 c in rect.Cells)
            {
                // if (!Rand.Chance(1f / 1000f))
                RoofDef roof = c.GetRoof(Find.CurrentMap);
                List<ThingDef> potentialPlants = [];
                if (roof is { isNatural: true })
                {
                    CavePlantsForSpot(c, Find.CurrentMap, potentialPlants);
                }
                else
                {
                    PlantsForSpot(c, Find.CurrentMap, potentialPlants, currentPlantDensity);
                }

                if(!potentialPlants.Any()) continue;

                foreach (ThingDef t in potentialPlants)
                {
                    float num = PlantChoiceWeight.Value.Invoke(Find.CurrentMap.wildPlantSpawner, [t, c, distanceSqToNearbyClusters, numDesiredPlants, currentPlantDensity]) as float? ?? 0;
                    tmpPossiblePlantsWithWeight.Add(new KeyValuePair<ThingDef, float>(t, num));
                }

                if (!tmpPossiblePlantsWithWeight.TryRandomElementByWeight(x => x.Value, out KeyValuePair<ThingDef, float> result))
                    continue;

                Plant newThing = (Plant) ThingMaker.MakeThing(result.Key);

                newThing.Growth = Mathf.Clamp01(WildPlantSpawner.InitialGrowthRandomRange.RandomInRange);
                if (newThing.def.plant.LimitedLifespan)
                    newThing.Age = Rand.Range(0, Mathf.Max(newThing.def.plant.LifespanTicks - 50, 0));

                GenSpawn.Spawn(newThing, c, Find.CurrentMap);
            }
        });
    }

    [DebugAction("Map", "Grow plants of type (rect)", false, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100)]
    private static List<DebugActionNode> GrowPlantsOfType()
    {
        List<DebugActionNode> debugActionNodeList = new();
        IEnumerable<ThingDef> plants = DefDatabase<ThingDef>.AllDefsListForReading.Where(x =>
            x.category == ThingCategory.Plant);
        foreach (ThingDef plant in plants)
        {
            debugActionNodeList.Add(new DebugActionNode(plant.defName)
            {
                action = () => DebugToolsGeneral.GenericRectTool(plant.defName, rect =>
                {
                    foreach (IntVec3 c in rect)
                    {
                        if(GenRadial.RadialCellsAround(c, plant.size.Magnitude / 2, true).Any(cell=>Find.CurrentMap.thingGrid.CellContains(c, plant) || !plant.CanEverPlantAt(cell, Find.CurrentMap, true))) continue;

                        Plant newThing = (Plant) ThingMaker.MakeThing(plant);
                        newThing.Growth = Mathf.Clamp01(WildPlantSpawner.InitialGrowthRandomRange.RandomInRange);
                        if (newThing.def.plant.LimitedLifespan)
                            newThing.Age = Rand.Range(0, Mathf.Max(newThing.def.plant.LifespanTicks - 50, 0));
                        GenSpawn.Spawn(newThing, c, Find.CurrentMap);
                    }
                })
            });
        }
        return debugActionNodeList;
    }

    [DebugAction("Map", "Clear items in area (rect)", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100)]
    private static void ClearItemsArea()
    {
        DebugToolsGeneral.GenericRectTool("Clear Items", rect =>
        {
            foreach (IntVec3 c in rect.Cells)
            {
                foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(c))
                {
                    if (thing.def.building == null && thing.def.plant == null && thing is not Pawn)
                    {
                        thing.Destroy();
                    }
                }
            }
        });
    }

    [DebugAction("Map", "Clear plants in area (rect)", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100)]
    private static void ClearPlantsArea()
    {
        DebugToolsGeneral.GenericRectTool("Clear Plants", rect =>
        {
            foreach (IntVec3 c in rect.Cells)
            {
                foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(c))
                {
                    if (thing.def.plant != null)
                    {
                        thing.Destroy();
                    }
                }
            }
        });
    }
}
