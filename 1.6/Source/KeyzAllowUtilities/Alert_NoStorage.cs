#if NET10_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

/// <summary>
/// Lists loose, unforbidden items on player home maps that no enabled storage is allowed to
/// accept. Checks storage filters only, not free space. Found by the game automatically as an
/// Alert subclass. 1.7 only.
/// </summary>
public class Alert_NoStorage : Alert
{
    private const int RecheckIntervalTicks = 250;

    private readonly List<Thing> culprits = new();
    private int lastCheckTick = -RecheckIntervalTicks;

    public Alert_NoStorage()
    {
        defaultPriority = AlertPriority.Medium;
    }

    public override string GetLabel() => "KAU_NoStorageAlert".Translate(culprits.Count);

    public override TaggedString GetExplanation()
    {
        IEnumerable<string> lines = culprits
            .GroupBy(t => t.def)
            .OrderByDescending(g => g.Sum(t => t.stackCount))
            .Select(g => "  - " + g.Key.LabelCap + " x" + g.Sum(t => t.stackCount));
        return "KAU_NoStorageAlertDesc".Translate() + "\n\n" + string.Join("\n", lines);
    }

    public override AlertReport GetReport()
    {
        if (KeyzAllowUtilitiesMod.settings?.DisableNoStorageAlert ?? true) return false;

        int now = Find.TickManager.TicksGame;
        if (now - lastCheckTick >= RecheckIntervalTicks)
        {
            lastCheckTick = now;
            FindCulprits();
        }
        return AlertReport.CulpritsAre(culprits);
    }

    private void FindCulprits()
    {
        culprits.Clear();
        foreach (Map map in Find.Maps)
        {
            if (!map.IsPlayerHome) continue;
            List<IHaulDestination> destinations = map.haulDestinationManager.AllHaulDestinationsListForReading;
            foreach (Thing t in map.listerHaulables.ThingsPotentiallyNeedingHauling())
            {
                if (!t.Spawned || t.IsForbidden(Faction.OfPlayer)) continue;
                if (!HasAcceptingStorage(t, destinations)) culprits.Add(t);
            }
        }
    }

    private static bool HasAcceptingStorage(Thing t, List<IHaulDestination> destinations)
    {
        for (int i = 0; i < destinations.Count; i++)
        {
            IHaulDestination d = destinations[i];
            if (d.HaulDestinationEnabled && d.Accepts(t)) return true;
        }
        return false;
    }
}
#endif
