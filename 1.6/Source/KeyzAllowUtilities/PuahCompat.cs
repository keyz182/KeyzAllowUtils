using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace KeyzAllowUtilities;

/// <summary>
/// Pick Up And Haul compatibility, found by reflection so no hard reference is needed.
///
/// Works with every PUAH variant that keeps the <c>PickUpAndHaul</c> type names: the original
/// (Mehni.PickUpAndHaul, a 1.6 build that 1.7 players run as a fallback) and
/// "Pick Up and Haul - Continued" (Zzyxzz.PickUpAndHaulReworked, 1.6 and 1.7).
///
/// Three parts:
/// 1. Urgent haul jobs are built by PUAH's WorkGiver, so pawns fill their inventory.
/// 2. PUAH's "haul urgently" designation is pointed at ours. PUAH only knows Allow Tool's
///    designation; without this, Continued's HasOtherDesignation treats ours as foreign and
///    falls back to a single-item vanilla haul.
/// 3. PUAH limits extra pickups to urgent items only when Allow Tool is active. A transpiler
///    swaps that check for ours. Setting the get-only property's backing field is not an option:
///    1.7 runs on CoreCLR, which refuses writes to an initonly static once its type is loaded.
/// </summary>
[StaticConstructorOnStartup]
public static class PuahCompat
{
    private const string WorkGiverTypeName = "PickUpAndHaul.WorkGiver_HaulToInventory";
    private const string DesignationDefOfTypeName = "PickUpAndHaul.PickUpAndHaulDesignationDefOf";
    private const string CompatCheckTypeName = "PickUpAndHaul.ModCompatibilityCheck";

    private static MethodInfo allowToolIsActiveGetter;
    private static bool allowToolWasActive;
    private static bool urgentDesignationIsOurs;
    private static int replacedCalls;

    static PuahCompat()
    {
        try
        {
            Apply();
        }
        catch (Exception e)
        {
            ModLog.Error("Failed to add Pick Up And Haul compat", e);
        }
    }

    private static void Apply()
    {
        Type workGiverType = AccessTools.TypeByName(WorkGiverTypeName);
        if (workGiverType == null) return;

        WorkGiver_Scanner hauler = FindHauler(workGiverType);
        if (hauler == null)
        {
            ModLog.Warn($"Found {WorkGiverTypeName} but could not create it; urgent haul stays vanilla");
            return;
        }

        WorkGiver_HaulUrgently.JobOnThingDelegate = (pawn, thing, forced) =>
            PuahJob(hauler, pawn, thing, forced) ?? HaulAIUtility.HaulToStorageJob(pawn, thing, forced);

        urgentDesignationIsOurs = PointUrgentDesignationAtOurs();
        PatchUrgentOnlyCheck(workGiverType);

        ModLog.Log($"Pick Up And Haul compat applied (urgent designation ours: {urgentDesignationIsOurs}, "
                   + $"urgent-only filter: {replacedCalls == 1})");
    }

    /// <summary>
    /// PUAH skips pawns it cannot handle (no inventory comp, over its gear-mass limit, races it
    /// does not allow). Those pawns still get a vanilla urgent haul via the caller's fallback.
    /// </summary>
    private static Job PuahJob(WorkGiver_Scanner hauler, Pawn pawn, Thing thing, bool forced) =>
        hauler.ShouldSkip(pawn, forced) ? null : hauler.JobOnThing(pawn, thing, forced);

    /// <summary>Prefer the def's own worker so <c>def</c> is set; fall back to a bare instance.</summary>
    private static WorkGiver_Scanner FindHauler(Type workGiverType)
    {
        foreach (WorkGiverDef def in DefDatabase<WorkGiverDef>.AllDefsListForReading)
        {
            if (def.giverClass == workGiverType && def.Worker is WorkGiver_Scanner worker)
                return worker;
        }

        return Activator.CreateInstance(workGiverType) as WorkGiver_Scanner;
    }

    /// <summary>
    /// Sets PUAH's urgent designation to ours when it is empty (Allow Tool absent). Reading the
    /// field first runs PUAH's static initializer, so it cannot overwrite our value later.
    /// </summary>
    private static bool PointUrgentDesignationAtOurs()
    {
        FieldInfo field = AccessTools.Field(AccessTools.TypeByName(DesignationDefOfTypeName), "haulUrgently");
        if (field == null || field.IsInitOnly || field.FieldType != typeof(DesignationDef))
        {
            ModLog.Warn("Pick Up And Haul urgent designation field not found; it may pick up non-urgent items too");
            return false;
        }

        DesignationDef ours = KeyzAllowUtilitesDefOf.KAU_HaulUrgentlyDesignation;
        if (field.GetValue(null) is DesignationDef current)
            return current == ours;

        field.SetValue(null, ours);
        return true;
    }

    private static void PatchUrgentOnlyCheck(Type workGiverType)
    {
        Type compatCheck = AccessTools.TypeByName(CompatCheckTypeName);
        allowToolIsActiveGetter = compatCheck == null ? null : AccessTools.PropertyGetter(compatCheck, "AllowToolIsActive");
        MethodInfo jobOnThing = AccessTools.Method(workGiverType, "JobOnThing", new[] { typeof(Pawn), typeof(Thing), typeof(bool) });
        if (allowToolIsActiveGetter == null || jobOnThing == null)
        {
            ModLog.Warn("Pick Up And Haul urgent-only check not found; it may pick up non-urgent items too");
            return;
        }

        allowToolWasActive = (bool)allowToolIsActiveGetter.Invoke(null, null);

        new Harmony("keyz182.rimworld.KeyzAllowUtilities.puah")
            .Patch(jobOnThing, transpiler: new HarmonyMethod(typeof(PuahCompat), nameof(JobOnThingTranspiler)));

        if (replacedCalls != 1)
            ModLog.Warn($"Expected 1 Allow Tool check in Pick Up And Haul's JobOnThing, replaced {replacedCalls}");
    }

    /// <summary>Replaces PUAH's "is Allow Tool active" check with <see cref="UrgentOnlyEnabled"/>.</summary>
    private static IEnumerable<CodeInstruction> JobOnThingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo replacement = AccessTools.Method(typeof(PuahCompat), nameof(UrgentOnlyEnabled));
        replacedCalls = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(allowToolIsActiveGetter))
            {
                replacedCalls++;
                yield return new CodeInstruction(OpCodes.Call, replacement).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                continue;
            }

            yield return instruction;
        }
    }

    /// <summary>
    /// True when PUAH's urgent designation is ours, or when Allow Tool really is active (keeps
    /// PUAH's own behaviour for Allow Tool users).
    /// </summary>
    public static bool UrgentOnlyEnabled() => allowToolWasActive || urgentDesignationIsOurs;
}
