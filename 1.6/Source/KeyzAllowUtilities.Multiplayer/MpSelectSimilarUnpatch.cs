using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace KeyzAllowUtilities.Multiplayer;

/// <summary>
/// Select Similar is a UI-local designator — DesignateSingleCell/DesignateThing only call
/// Find.Selector.Select, it writes no Designation and mutates no game state. Multiplayer's
/// generic designation patch (Multiplayer.Client.DesignatorPatches) doesn't know that: it patches
/// every Designator subtype's declared DesignateSingleCell/DesignateMultiCell/DesignateThing,
/// serializes the call, and replays it on a *fresh* Designator_SelectSimilar instance from the
/// tick loop. That fresh instance never had Selected() called, so its filter is empty and the
/// replay always fails with "No Selectables" — see Designator_SelectSimilar.CanDesignateCell.
/// Worse, if it ever did "succeed" under replay it would overwrite every other client's local
/// selection with this client's.
///
/// The fix is to remove Multiplayer's patch from these three methods, so they keep running
/// client-locally — exactly like every other purely-local UI action (e.g. selecting a thing by
/// clicking it with the vanilla select tool, which Multiplayer does not sync either).
/// </summary>
[StaticConstructorOnStartup]
public static class MpSelectSimilarUnpatch
{
    private const string MpDesignatorPatchesTypeName = "Multiplayer.Client.DesignatorPatches";
    private const string MpFinalizerMethodName = "DesignateFinalizer";
    private const string HarmonyId = "keyz182.rimworld.KeyzAllowUtilities.mp";

    private static readonly (string Name, Type[] Args)[] Targets =
    {
        ("DesignateSingleCell", new[] { typeof(IntVec3) }),
        ("DesignateMultiCell", new[] { typeof(IEnumerable<IntVec3>) }),
        ("DesignateThing", new[] { typeof(Thing) }),
    };

    private static bool done;
    private static bool complained;

    static MpSelectSimilarUnpatch()
    {
        // Primary attempt. About.xml's loadAfter rwmt.Multiplayer means our assemblies load after
        // Multiplayer's, but StaticConstructorOnStartup execution order across mods is only a
        // best-effort ordering, not a guarantee — so this is allowed to no-op (see Ensure's "don't
        // latch on zero" rule); the Selected() prefix below is the guaranteed-timing net.
        Ensure();
    }

    /// <summary>
    /// Harmony prefix on Designator_SelectSimilar.Selected() — a method Multiplayer does not
    /// patch, and which can only run once the player has actually equipped the tool, i.e. long
    /// after every mod's startup patching (Multiplayer's included) has finished. Guarantees
    /// Ensure() completes even if the static constructor above ran too early.
    /// </summary>
    [HarmonyPatch(typeof(Designator_SelectSimilar), nameof(Designator_SelectSimilar.Selected))]
    [HarmonyPrefix]
    private static void Selected_Patch()
    {
        Ensure();
    }

    public static void Ensure()
    {
        if (done)
        {
            return;
        }

        Type mp = AccessTools.TypeByName(MpDesignatorPatchesTypeName);
        if (mp == null)
        {
            // Multiplayer isn't actually loaded under this name, or was refactored away — either
            // way there is no designator sync for anything here to have broken. This can never
            // change at runtime, so it's safe to latch.
            done = true;
            Complain($"{MpDesignatorPatchesTypeName} not found — Multiplayer may have renamed its designator sync internals");
            return;
        }

        MethodInfo finalizer = AccessTools.DeclaredMethod(mp, MpFinalizerMethodName);
        var harmony = new Harmony(HarmonyId);
        int removed = 0;

        foreach ((string name, Type[] args) in Targets)
        {
            MethodInfo target = AccessTools.DeclaredMethod(typeof(Designator_SelectSimilar), name, args);
            if (target == null || target.DeclaringType != typeof(Designator_SelectSimilar))
            {
                // A future refactor could rename/remove the override, or (worse) hoist it onto a
                // shared base class. Either way, resolving by walking the type hierarchy would
                // risk unpatching a base Designator method and stripping Multiplayer's designator
                // sync for every OTHER designator in the game — refuse instead and complain loudly.
                string argList = string.Join(", ", args.Select(a => a.Name));
                Complain($"Designator_SelectSimilar no longer declares {name}({argList}) — refusing to unpatch a base Designator method");
                continue;
            }

            MethodInfo prefix = AccessTools.DeclaredMethod(mp, name);
            if (prefix == null)
            {
                Complain($"{MpDesignatorPatchesTypeName}.{name} not found — Multiplayer may have refactored its designator sync");
                continue;
            }

            if (!HasPrefix(target, prefix))
            {
                // Multiplayer hasn't patched this method yet (we ran first). Leave `done` false so
                // the Selected() net retries later instead of latching a false success.
                continue;
            }

            harmony.Unpatch(target, prefix);
            if (finalizer != null)
            {
                harmony.Unpatch(target, finalizer);
            }

            if (HasPrefix(target, prefix))
            {
                Complain($"Unpatch of {name} did not take");
            }
            else
            {
                removed++;
            }
        }

        if (removed == 0)
        {
            return;
        }

        done = true;
        ModLog.Log($"Multiplayer: Select Similar left un-synced (it only changes local selection) — {removed}/{Targets.Length} designate methods unpatched");
    }

    private static bool HasPrefix(MethodBase method, MethodInfo patch)
    {
        Patches info = Harmony.GetPatchInfo(method);
        return info != null && info.Prefixes.Any(p => p.PatchMethod == patch);
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
