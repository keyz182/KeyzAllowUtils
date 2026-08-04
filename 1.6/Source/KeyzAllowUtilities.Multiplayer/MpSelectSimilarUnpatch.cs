using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace KeyzAllowUtilities.Multiplayer;

/// <summary>
/// Select Similar is a UI-local designator — its Designate* methods only call
/// Find.Selector.Select, they write no Designation and mutate no game state. Multiplayer's
/// generic designation sync (Multiplayer.Client.DesignatorPatches) doesn't know that: it
/// serializes the call, cancels local execution, and replays it from the tick loop, where the
/// cursor/selection context the filter needs doesn't exist — so drags fail with "No Selectables"
/// and any selection it did make would be discarded with the replay's selection sandbox.
///
/// This used to be handled by *unpatching* Multiplayer's prefixes from the three Designate*
/// overrides declared on Designator_SelectSimilar. That left a hole: DesignateMultiCell calls
/// base.DesignateMultiCell, and Multiplayer also patches Verse.Designator's own Designate*
/// methods (it patches every declared override on every Designator subtype, the base class
/// included). A base call goes straight through the detour on the base method, so drags were
/// still being synced and cancelled even with 3/3 overrides unpatched.
///
/// Instead, prefix Multiplayer's sync prefixes themselves: DesignatorPatches.Designate* receive
/// the designator instance no matter which subtype's method was patched — including the
/// base-call route — so a single instance check covers every path. When the instance is
/// Designator_SelectSimilar we skip the sync body and tell the patched method to run its
/// original, client-local code. Outside of Select Similar (or with Multiplayer's internals
/// renamed) nothing changes and we complain loudly instead of failing silent.
///
/// This is the same pattern Multiplayer-Compatibility uses for Allow Tool's select similar.
/// </summary>
public static class MpSelectSimilarUnpatch
{
    private const string MpDesignatorPatchesTypeName = "Multiplayer.Client.DesignatorPatches";
    private const string HarmonyId = "keyz182.rimworld.KeyzAllowUtilities.mp";

    private static readonly string[] Targets = ["DesignateSingleCell", "DesignateMultiCell", "DesignateThing"];

    private static bool applied;
    private static bool complained;

    /// <summary>
    /// Called from MpCompatMod's constructor. No ordering constraints: this patches Multiplayer's
    /// own (static) prefix methods, which works whether or not Multiplayer has applied its
    /// designator patches yet — all mod assemblies are loaded before any Mod is constructed, so
    /// the type lookup is reliable here.
    /// </summary>
    public static void Apply()
    {
        if (applied)
        {
            return;
        }

        Type mp = AccessTools.TypeByName(MpDesignatorPatchesTypeName);
        if (mp == null)
        {
            Complain($"{MpDesignatorPatchesTypeName} not found — Multiplayer may have renamed its designator sync internals");
            return;
        }

        var harmony = new Harmony(HarmonyId);
        var prefix = new HarmonyMethod(typeof(MpSelectSimilarUnpatch), nameof(SkipSyncForSelectSimilar));
        int patched = 0;

        foreach (string name in Targets)
        {
            MethodInfo target = AccessTools.DeclaredMethod(mp, name);
            if (target == null)
            {
                Complain($"{MpDesignatorPatchesTypeName}.{name} not found — Multiplayer may have refactored its designator sync");
                continue;
            }

            harmony.Patch(target, prefix: prefix);
            patched++;
        }

        if (patched == 0)
        {
            return;
        }

        applied = true;
        ModLog.Log($"Multiplayer: Select Similar left un-synced (it only changes local selection) — sync bypassed on {patched}/{Targets.Length} designate hooks");
    }

    /// <summary>
    /// Prefix on Multiplayer's DesignatorPatches.Designate* sync prefixes. Their contract:
    /// return true to let the original designate method run, false to swallow it (after queueing
    /// a synced replay). For Select Similar we force "run the original" and skip the sync body;
    /// every other designator is left to Multiplayer untouched.
    /// </summary>
    private static bool SkipSyncForSelectSimilar([HarmonyArgument("__instance")] Designator designator, ref bool __result)
    {
        if (designator is not Designator_SelectSimilar)
        {
            return true;
        }

        __result = true;
        return false;
    }

    private static void Complain(string message)
    {
        if (complained)
        {
            return;
        }

        complained = true;
        ModLog.Error($"Multiplayer compat: {message}");
    }
}
