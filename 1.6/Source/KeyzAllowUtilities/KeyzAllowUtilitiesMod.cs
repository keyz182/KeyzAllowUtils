using System;
using System.Reflection;
using Verse;
using UnityEngine;
using HarmonyLib;
using Verse.AI;

namespace KeyzAllowUtilities;

public class KeyzAllowUtilitiesMod : Mod
{
    public static Settings settings;

    public KeyzAllowUtilitiesMod(ModContentPack content) : base(content)
    {
        ModLog.Log("Loading KeyzAllowUtilities");
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("keyz182.rimworld.KeyzAllowUtilities.main");
        harmony.PatchAll();
        TryIntegratePUAH();
    }

    private static void TryIntegratePUAH()
    {
        if (ModLister.GetActiveModWithIdentifier("Mehni.PickUpAndHaul") == null) return;
        try
        {
            var puahType = AccessTools.TypeByName("PickUpAndHaul.WorkGiver_HaulToInventory");
            if (puahType == null) return;
            var method = AccessTools.Method(puahType, "JobOnThing")
                      ?? AccessTools.Method(puahType, "TryGetJobOnThing");
            if (method == null) return;
            WorkGiver_HaulUrgently.JobOnThingDelegate =
                (WorkGiver_HaulUrgently.TryGetJobOnThing)Delegate.CreateDelegate(
                    typeof(WorkGiver_HaulUrgently.TryGetJobOnThing), method);
            ModLog.Log("PUAH detected — urgent haul will use PUAH multi-haul job");
        }
        catch (Exception e)
        {
            ModLog.Warning($"Failed to integrate with PUAH: {e.Message}");
        }
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "KeyzAllowUtilities_SettingsCategory".Translate();
    }
}
