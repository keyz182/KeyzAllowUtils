using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Windows;
using Verse;
using Verse.Steam;
using Input = UnityEngine.Input;

namespace KeyzAllowUtilities;

public class KeyHandler(Map map) : MapComponent(map)
{
    public static Designator_SelectSimilar selectSimilar =>
        DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators.FirstOrDefault(d => d is Designator_SelectSimilar) as Designator_SelectSimilar;


    public override void FinalizeInit()
    {
        foreach (Pawn mech in map.mapPawns.SpawnedColonyMechs)
        {
            // get haul urgently picked up
            if (mech.RaceProps.mechEnabledWorkTypes.Contains(KeyzAllowUtilitesDefOf.KAU_UrgentHaul))
            {
                mech.workSettings.Notify_UseWorkPrioritiesChanged();
                if (mech.workSettings.GetPriority(KeyzAllowUtilitesDefOf.KAU_UrgentHaul) <= 0)
                {
                    mech.workSettings.SetPriority(KeyzAllowUtilitesDefOf.KAU_UrgentHaul, 1);
                }
            }
        }
    }

    public override void MapComponentOnGUI()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        if(KeyzAllowUtilitiesMod.settings.DisableAllShortcuts) return;

        if (KeyzAllowUtilitesDefOf.KAU_Allow.KeyDownEvent)
        {
            AllowAll(map);
            Event.current.Use();
        }else if (KeyzAllowUtilitesDefOf.KAU_Forbid.KeyDownEvent)
        {
            AllowAll(map, true);
            Event.current.Use();
        }

        if (KeyzAllowUtilitesDefOf.KAU_SelectSimilar.KeyDownEvent)
        {
            if (selectSimilar != null)
            {
                Find.DesignatorManager.Select(selectSimilar);
            }
            Event.current.Use();
        }else if (KeyzAllowUtilitesDefOf.KAU_HarvestFullyGrown.KeyDownEvent)
        {
            CutFullyGrownOnMap(map);
            Event.current.Use();
        }

        if(KeyzAllowUtilitiesMod.settings.DisableAllowShortcuts) return;

        if (KeyzAllowUtilitesDefOf.KAU_Allow.KeyDownEvent)
        {
            AllowAll(map);
            Event.current.Use();
        }else if (KeyzAllowUtilitesDefOf.KAU_Forbid.KeyDownEvent)
        {
            AllowAll(map, true);
            Event.current.Use();
        }

        // else if (KeyzAllowUtilitesDefOf.KAU_HaulUrgently.KeyDownEvent)
        // {
        //     foreach (Thing thing in Find.Selector.SelectedObjects.OfType<Thing>().Where(t=>t.def.EverHaulable && !map.designationManager.HasMapDesignationOn(t)))
        //     {
        //         map.designationManager.AddDesignation(new Designation(thing, KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation));
        //     }
        // }
    }

    public static void CutFullyGrownOnMap(Map map)
    {
        foreach (Plant plant in map.listerThings.AllThings.OnlySelectableThings().NotFogged().OfType<Plant>().Where(p=>Mathf.Approximately(p.Growth, 1f)))
        {
            plant.Map.designationManager.RemoveAllDesignationsOn(plant);
            plant.Map.designationManager.AddDesignation(new Designation((LocalTargetInfo) plant, DesignationDefOf.CutPlant));
        }
    }

    public static void AllowAll(Map map, bool forbid = false, Def ofDef = null)
    {
        int countOfForbiddables = 0;
        foreach (CompForbiddable compForbiddable in map.ForbiddableThings(ofDef))
        {
            if (compForbiddable.Forbidden != forbid) // Dont' count anything already set to what we want
            {
                compForbiddable.Forbidden = forbid;
                countOfForbiddables += compForbiddable.parent.stackCount;
            }
        }
        if(!forbid)
            Messages.Message("KUA_Allowed".Translate(countOfForbiddables), MessageTypeDefOf.NeutralEvent);
        else
            Messages.Message("KUA_Forbidden".Translate(countOfForbiddables), MessageTypeDefOf.NeutralEvent);
    }
}
