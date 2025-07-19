using System;
using PickUpAndHaul;
using Verse;

namespace KeyzAllowUtilities.PUAHUnofficial;

public class PUAHMod : Mod
{
    public PUAHMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from PUAHMod");
        try
        {
            WorkGiver_HaulToInventory hauler = (WorkGiver_HaulToInventory) Activator.CreateInstance(typeof(WorkGiver_HaulToInventory));
            WorkGiver_HaulUrgently.JobOnThingDelegate = (pawn, thing, forced) => hauler.ShouldSkip(pawn, forced) ? null : hauler.JobOnThing(pawn, thing, forced);
            ModLog.Debug("PUAH unofficial compat applied");
        }
        catch (Exception e)
        {
            ModLog.Error("Failed to add PUAH unofficial compat", e);
        }
    }
}
