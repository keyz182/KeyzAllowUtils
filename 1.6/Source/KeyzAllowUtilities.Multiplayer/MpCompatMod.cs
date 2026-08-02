using System;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace KeyzAllowUtilities.Multiplayer;

/// <summary>
/// Registers HaulUrgently's synced actions with RimWorld Multiplayer, and un-syncs Select
/// Similar (see MpSelectSimilarUnpatch). Only loaded when the Multiplayer mod is active (see
/// loadFolders.xml's IfModActive gate) — the main KeyzAllowUtilities assembly has no reference
/// to, or dependency on, the Multiplayer API.
/// </summary>
public class MpCompatMod : Mod
{
    public MpCompatMod(ModContentPack content) : base(content)
    {
        // MP.enabled is only set once Multiplayer.API.MP's static constructor has resolved the
        // MultiplayerAPIBridge from the Multiplayer assembly; calling the API without it throws
        // UninitializedAPI. All mod assemblies (including Multiplayer's) load before any Mod is
        // constructed, so this check is reliable here regardless of load order.
        if (!MP.enabled)
        {
            ModLog.Warn("Multiplayer present but its API bridge is unavailable — HaulUrgently sync not registered");
            return;
        }

        try
        {
            // MinTime(0) is explicit: ISyncMethod's default throttling would otherwise silently
            // drop rapid repeat clicks on the same gizmo.
            MP.RegisterSyncMethod(typeof(HaulUrgentlyActions), nameof(HaulUrgentlyActions.DesignateHaulUrgently))
                .CancelIfAnyArgNull().MinTime(0);
            MP.RegisterSyncMethod(typeof(HaulUrgentlyActions), nameof(HaulUrgentlyActions.CancelHaulUrgently))
                .CancelIfAnyArgNull().MinTime(0);
            MP.RegisterSyncMethod(typeof(HaulUrgentlyActions), nameof(HaulUrgentlyActions.DesignateHaulUrgentlyBulk))
                .CancelIfAnyArgNull().MinTime(0);

            // A client joining mid-session must not have their local settings silently delete
            // the host's shared designations on load — see Settings.ValidateDesignators.
            Settings.SuppressDesignationPurge = () => MP.IsInMultiplayer;

            // Select Similar only ever changes this client's local selection (Find.Selector) —
            // it must never be command-synced, so MpSelectSimilarUnpatch removes Multiplayer's
            // generic designator sync from it. PatchAll registers that class's Harmony patches
            // (including its Selected() timing net); Ensure() also runs eagerly from its own
            // static constructor. See MpSelectSimilarUnpatch and Settings.DesignateSuccessFeedbackSuppressed.
            new Harmony("keyz182.rimworld.KeyzAllowUtilities.mp").PatchAll(Assembly.GetExecutingAssembly());
            Settings.DesignateSuccessFeedbackSuppressed = () => MP.IsInMultiplayer;

            ModLog.Log("Multiplayer compatibility registered (HaulUrgently sync, Select Similar unpatch)");
        }
        catch (Exception e)
        {
            ModLog.Error("Multiplayer sync registration failed", e);
        }
    }
}
