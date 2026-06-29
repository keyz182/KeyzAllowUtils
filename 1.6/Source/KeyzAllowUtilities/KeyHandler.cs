using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class KeyHandler(Map map) : MapComponent(map)
{
    public static Lazy<Designator_SelectSimilar> SelectSimilar = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_SelectSimilar>().FirstOrDefault());

    public static Lazy<Designator_FinishOff> FinishOff = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_FinishOff>().FirstOrDefault());

    public static Lazy<Designator_HaulUrgently> HaulUrgently = new(() => DefDatabase<DesignationCategoryDef>.GetNamed("Orders").AllResolvedDesignators
        .OfType<Designator_HaulUrgently>().FirstOrDefault());


    public override void FinalizeInit()
    {
        foreach (Pawn mech in map.mapPawns.SpawnedColonyMechs)
        {
            // get haul urgently picked up
            if (KeyzAllowUtilitesDefOf.KAU_UrgentHaul != null && mech.RaceProps.mechEnabledWorkTypes.Contains(KeyzAllowUtilitesDefOf.KAU_UrgentHaul))
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

        if (!KeyzAllowUtilitiesMod.settings.DisableAllowShortcuts && KeyzAllowUtilitesDefOf.KAU_Allow.KeyDownEvent)
        {
            bool excludeCorpses = KeyzAllowUtilitiesMod.settings.ExcludeCorpsesFromAllowAll && !Event.current.shift;
            AllowAll(map, excludeCorpses: excludeCorpses);
            Event.current.Use();
        }else if (!KeyzAllowUtilitiesMod.settings.DisableAllowShortcuts && KeyzAllowUtilitesDefOf.KAU_Forbid.KeyDownEvent)
        {
            AllowAll(map, true);
            Event.current.Use();
        }else if (!KeyzAllowUtilitiesMod.settings.DisableSelection && KeyzAllowUtilitesDefOf.KAU_SelectSimilar.KeyDownEvent)
        {
            if (SelectSimilar.Value != null)
            {
                Find.DesignatorManager.Select(SelectSimilar.Value);
            }
            Event.current.Use();
        }else if (!KeyzAllowUtilitiesMod.settings.DisableFinishOff && KeyzAllowUtilitesDefOf.KAU_FinishOff.KeyDownEvent)
        {
            if (FinishOff.Value != null)
            {
                Find.DesignatorManager.Select(FinishOff.Value);
            }
            Event.current.Use();
        }else if (!KeyzAllowUtilitiesMod.settings.DisableHaulUrgently && KeyzAllowUtilitesDefOf.KAU_HaulUrgentlySelection.KeyDownEvent)
        {
            if (HaulUrgently.Value != null)
            {
                List<Thing> selected = Find.Selector.SelectedObjects.OfType<Thing>().ToList();
                if (selected.Count == 0)
                {
                    Find.DesignatorManager.Select(HaulUrgently.Value);
                    Event.current.Use();
                }
                else
                {
                    List<Thing> haulables = selected.Where(t => HaulUrgently.Value.CanDesignateThing(t).Accepted).ToList();
                    if (haulables.Count > 0)
                    {
                        foreach (Thing thing in haulables)
                            HaulUrgently.Value.DesignateThing(thing);
                        Event.current.Use();
                    }
                    // else: only non-haulables selected — don't consume O so it can pass through (e.g. Build Copy)
                }
            }
            else
            {
                Event.current.Use();
            }
        }
    }

    public static void AllowAll(Map map, bool forbid = false, Def ofDef = null, bool excludeCorpses = false)
    {
        List<ThingWithComps> selectedForbiddables = Find.Selector.SelectedObjects
            .OfType<ThingWithComps>()
            .Where(t => t.HasComp<CompForbiddable>() && !map.fogGrid.IsFogged(t.Position))
            .ToList();

        IEnumerable<CompForbiddable> targets = selectedForbiddables.Count > 0
            ? selectedForbiddables.Select(t => t.GetComp<CompForbiddable>())
            : map.ForbiddableThings(ofDef, excludeCorpses);

        int countOfForbiddables = 0;
        foreach (CompForbiddable compForbiddable in targets)
        {
            if (compForbiddable.Forbidden != forbid) // Dont' count anything already set to what we want
            {
                compForbiddable.Forbidden = forbid;
                countOfForbiddables += compForbiddable.parent.stackCount;
            }
        }
        if (!forbid)
        {
            string messageKey = excludeCorpses ? "KUA_AllowedExcludingCorpses" : "KUA_Allowed";
            Messages.Message(messageKey.Translate(countOfForbiddables), MessageTypeDefOf.NeutralEvent);
        }
        else
        {
            Messages.Message("KUA_Forbidden".Translate(countOfForbiddables), MessageTypeDefOf.NeutralEvent);
        }
    }
}
