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
        if (KeyzAllowUtilitesDefOf.KAU_DesignatorAllow.KeyDownEvent)
        {
            AllowAll(map);
            Event.current.Use();
        }
        if (KeyzAllowUtilitesDefOf.KAU_DesignatorForbid.KeyDownEvent)
        {
            AllowAll(map, true);
            Event.current.Use();
        }
    }

    public static IEnumerable<CompForbiddable> GetCompsForbidden(Map map, Def ofDef = null)
    {
        IEnumerable<ThingWithComps> things;
        things = ofDef == null ? map.listerThings.AllThings.OfType<ThingWithComps>() : map.listerThings.AllThings.OfType<ThingWithComps>().Where(t => t.def == ofDef);
        things = things.Where(t => t.HasComp<CompForbiddable>()).Where(t => !map.fogGrid.IsFogged(t.Position));
        things = things.Where(t => t.def is { EverHaulable: true });
        return things.Select(t => t.GetComp<CompForbiddable>());
    }

    public static void AllowAll(Map map, bool forbid = false, Def ofDef = null)
    {
        int countOfForbiddables = 0;
        foreach (CompForbiddable compForbiddable in GetCompsForbidden(map, ofDef))
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
