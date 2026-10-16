using System;
using HarmonyLib;
using PickUpAndHaul;
using Verse;

namespace KeyzAllowUtilities.PUAH;

public class PUAHMod : Mod
{
    public PUAHMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from PUAHMod");
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("keyz182.rimworld.KeyzAllowUtilities.PUAH.main");
        harmony.PatchAll(typeof(PUAHMod).Assembly);

        try
        {
            WorkGiver_HaulToInventory hauler = (WorkGiver_HaulToInventory) Activator.CreateInstance(typeof(WorkGiver_HaulToInventory));
            WorkGiver_HaulUrgently.JobOnThingDelegate = (pawn, thing, forced) => hauler.ShouldSkip(pawn, forced) ? null : hauler.JobOnThing(pawn, thing, forced);
            ModLog.Debug("PUAH compat applied");
        }
        catch (Exception e)
        {
            ModLog.Error("Failed to add PUAH compat", e);
        }
    }
}
