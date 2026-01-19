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
    public static readonly Texture2D KUA_CutGrown = ContentFinder<Texture2D>.Get("UI/KUA_CutGrown");

    public static void DesignateFullyGrownOnScreen(IEnumerable<Plant> things, Map map, DesignationDef designation, bool checkIfHarvestable = true)
    {
        IEnumerable<Thing> plants = map.ThingsOnScreen((thing => thing.def.category == ThingCategory.Plant && ThingSelectionUtility.SelectableByMapClick(thing) )).OfDefs(things.Select(t=>t.def).Distinct());

        foreach (Plant plant in plants.OnlySelectableThings().NotFogged().OfType<Plant>().NearestTo(map.GetCenterOfScreenOnMap()))
        {
            if (Mathf.Approximately(plant.Growth, 1f) && (!checkIfHarvestable || plant.HarvestableNow))
            {
                plant.Map.designationManager.RemoveAllDesignationsOn(plant);
                plant.Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) plant, designation));
            }
        }
    }

    public static void DesignateFullyGrownOnMap(this Map map, IEnumerable<Plant> things, DesignationDef designation, bool checkIfHarvestable = true)
    {
        foreach (Plant plant in map.listerThings.AllThings.OfDefs(things.Select(t=>t.def).Distinct()).OnlySelectableThings().NotFogged().OfType<Plant>())
        {
            if (Mathf.Approximately(plant.Growth, 1f) && (!checkIfHarvestable || plant.HarvestableNow))
            {
                plant.Map.designationManager.RemoveAllDesignationsOn(plant);
                plant.Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) plant, designation));
            }
        }
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

    public static Designator_PlantsHarvest harvestPlants =>
        DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators.FirstOrDefault(d => d is Designator_PlantsHarvest) as Designator_PlantsHarvest;
    public static Designator_PlantsCut cutPlants =>
        DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators.FirstOrDefault(d => d is Designator_PlantsCut) as Designator_PlantsCut;

    [HarmonyPatch(nameof(Plant.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Patch(Plant __instance, ref IEnumerable<Gizmo> __result)
    {
        List<Gizmo> gizmos = __result.ToList();

        if(KeyzAllowUtilitiesMod.settings.DisableHarvest) return;
        if(!__instance.def.plant.Harvestable) return;

        if (!__instance.def.plant.IsTree)
        {
            Command_Action harvestGrownCommand = new()
            {
                icon = KUA_HarvestGrown,
                defaultLabel = "KUA_HarvestGrown".Translate(),
                defaultDesc = "KUA_HarvestGrownDesc".Translate(),
                action = () =>
                {
                    if (Event.current.shift)
                    {
                        Find.DesignatorManager.Select(harvestPlants);
                        return;
                    }
                    List<FloatMenuOption> items =
                    [
                        new("KUA_HarvestOnScreen".Translate(), () =>
                        {
                            if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                            {
                                DesignateFullyGrownOnScreen(things.OfType<Plant>(), __instance.Map, DesignationDefOf.HarvestPlant);
                            }

                            DesignateFullyGrownOnScreen([__instance], __instance.Map, DesignationDefOf.HarvestPlant);
                        }),

                        new("KUA_HarvestOnMap".Translate(), () =>
                        {
                            if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                            {
                                __instance.Map.DesignateFullyGrownOnMap(things.OfType<Plant>(), DesignationDefOf.HarvestPlant);
                            }

                            __instance.Map.DesignateFullyGrownOnMap([__instance], DesignationDefOf.HarvestPlant);
                        })
                    ];

                    Find.WindowStack.Add(new FloatMenu(items));
                }
            };
            gizmos.Add(harvestGrownCommand);
        }else
        {
            Command_Action cutGrownCommand = new()
            {
                icon = KUA_CutGrown,
                defaultLabel = "KUA_CutGrown".Translate(),
                defaultDesc = "KUA_CutGrownDesc".Translate(),
                action = () =>
                {
                    if (Event.current.shift)
                    {
                        Find.DesignatorManager.Select(harvestPlants);
                        return;
                    }
                    List<FloatMenuOption> items =
                    [
                        new("KUA_CutGrownOnScreen".Translate(), () =>
                        {
                            if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                            {
                                DesignateFullyGrownOnScreen(things.OfType<Plant>(), __instance.Map, DesignationDefOf.CutPlant, false);
                            }

                            DesignateFullyGrownOnScreen([__instance], __instance.Map, DesignationDefOf.CutPlant, false);
                        }),

                        new("KUA_CutGrownOnMap".Translate(), () =>
                        {
                            if (TryGetSelectedOfCategory(ThingCategory.Plant, out List<Thing> things))
                            {
                                __instance.Map.DesignateFullyGrownOnMap(things.OfType<Plant>(), DesignationDefOf.CutPlant, false);
                            }

                            __instance.Map.DesignateFullyGrownOnMap([__instance], DesignationDefOf.CutPlant, false);
                        })
                    ];

                    Find.WindowStack.Add(new FloatMenu(items));
                }
            };
            gizmos.Add(cutGrownCommand);
        }

        __result = gizmos;
    }

}
