using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class KeyHandler(Map map) : MapComponent(map)
{
    public override void MapComponentOnGUI()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;
        if (KeyzAllowUtilitesDefOf.KAU_Allow.KeyDownEvent)
        {
            AllowAll(map);
            Event.current.Use();
        }
        if (KeyzAllowUtilitesDefOf.KAU_Forbid.KeyDownEvent)
        {
            AllowAll(map, true);
            Event.current.Use();
        }
        if (KeyzAllowUtilitesDefOf.KAU_SelectSimilar.KeyDownEvent)
        {
            CutFullyGrownOnMap(map);
            Event.current.Use();
        }

        if (KeyzAllowUtilitesDefOf.KAU_HarvestFullyGrown.KeyDownEvent)
        {

        }
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
            compForbiddable.Forbidden = forbid;
            countOfForbiddables+= compForbiddable.parent.stackCount;
        }
        if(!forbid)
            Messages.Message("KUA_Allowed".Translate(countOfForbiddables), MessageTypeDefOf.NeutralEvent);
        else
            Messages.Message("KUA_Forbidden".Translate(countOfForbiddables), MessageTypeDefOf.NeutralEvent);
    }
}
