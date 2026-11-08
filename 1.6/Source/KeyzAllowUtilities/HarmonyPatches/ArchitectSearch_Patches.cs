using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace KeyzAllowUtilities.HarmonyPatches;

[StaticConstructorOnStartup]
public static class ArchitectSearchTextures
{
    public static readonly Texture2D FilterIcon = ContentFinder<Texture2D>.Get("UI/KUA_SearchFilter");
}

/// <summary>
/// Vanilla Architect search only dims gizmos that do not match. When
/// <see cref="Settings.ArchitectSearchFilter"/> is on, the gizmo grid gets only the matches.
/// </summary>
[HarmonyPatch(typeof(ArchitectCategoryTab))]
public static class ArchitectSearch_Patches
{
    private static readonly AccessTools.FieldRef<ArchitectCategoryTab, QuickSearchFilter> QuickSearchFilterRef =
        AccessTools.FieldRefAccess<ArchitectCategoryTab, QuickSearchFilter>("quickSearchFilter");

    // Calls the (patched) vanilla Matches, so the dropdown postfix below and other mods' patches apply.
    private static readonly Func<ArchitectCategoryTab, Command, bool> MatchesFunc =
        AccessTools.MethodDelegate<Func<ArchitectCategoryTab, Command, bool>>(
            AccessTools.Method(typeof(ArchitectCategoryTab), "Matches"));

    private static bool missWarned;

    public static IEnumerable<Gizmo> FilterForSearch(IEnumerable<Gizmo> gizmos, ArchitectCategoryTab tab)
    {
        if (!KeyzAllowUtilitiesMod.settings.ArchitectSearchFilter || !QuickSearchFilterRef(tab).Active)
            return gizmos;

        List<Gizmo> matches = new();
        foreach (Gizmo gizmo in gizmos)
        {
            if (gizmo is not Command command || MatchesFunc(tab, command)) matches.Add(gizmo);
        }
        return matches;
    }

    /// <summary>
    /// Stands in for <see cref="GizmoGridDrawer.DrawGizmoGrid"/> inside DesignationTabOnGUI.
    /// Same parameters plus the tab, so the call swap keeps the stack balanced.
    /// </summary>
    public static void DrawGizmoGridFiltered(IEnumerable<Gizmo> gizmos, float startX, out Gizmo mouseoverGizmo,
        Func<Gizmo, bool> customActivatorFunc, Func<Gizmo, bool> highlightFunc, Func<Gizmo, bool> lowlightFunc,
        bool multipleSelected, ArchitectCategoryTab tab)
    {
        GizmoGridDrawer.DrawGizmoGrid(FilterForSearch(gizmos, tab), startX, out mouseoverGizmo,
            customActivatorFunc, highlightFunc, lowlightFunc, multipleSelected);
    }

    /// <summary>
    /// Routes the gizmo grid draw in DesignationTabOnGUI through <see cref="DrawGizmoGridFiltered"/>.
    /// Anchors on the draw call, not on how the gizmo list is built, so other mods that change
    /// the list source do not break it. Search state and category colours still see the full list.
    /// </summary>
    [HarmonyPatch(nameof(ArchitectCategoryTab.DesignationTabOnGUI))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DesignationTabOnGUI_Transpiler(
        IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        MethodInfo draw = AccessTools.Method(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid));
        MethodInfo filtered = AccessTools.Method(typeof(ArchitectSearch_Patches), nameof(DrawGizmoGridFiltered));

        bool patched = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (patched || !instruction.Calls(draw))
            {
                yield return instruction;
                continue;
            }

            yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
            yield return new CodeInstruction(OpCodes.Call, filtered).MoveBlocksFrom(instruction);
            patched = true;
        }

        if (patched || missWarned) yield break;
        missWarned = true;
        Log.Warning("[KeyzAllowUtilities] Architect search filter: DrawGizmoGrid call not found in "
                    + "DesignationTabOnGUI; search will dim instead of filter. Other patches on it: "
                    + OtherPatchOwners(__originalMethod));
    }

    private static string OtherPatchOwners(MethodBase original)
    {
        Patches info = original == null ? null : Harmony.GetPatchInfo(original);
        if (info == null) return "none";

        List<string> owners = new();
        foreach (Patch patch in info.Prefixes) owners.Add($"{patch.owner} (prefix)");
        foreach (Patch patch in info.Transpilers) owners.Add($"{patch.owner} (transpiler)");
        owners.RemoveAll(owner => owner.StartsWith("keyz182.rimworld.KeyzAllowUtilities", StringComparison.Ordinal));
        return owners.Count == 0 ? "none" : string.Join(", ", owners);
    }

    /// <summary>
    /// A dropdown (e.g. tables) matches only on its current item's label in vanilla.
    /// Also match it when any item inside it matches.
    /// </summary>
    [HarmonyPatch("Matches")]
    [HarmonyPostfix]
    public static void Matches_Postfix(ArchitectCategoryTab __instance, Command c, ref bool __result)
    {
        if (__result || c is not Designator_Dropdown dropdown) return;

        QuickSearchFilter quickSearchFilter = QuickSearchFilterRef(__instance);
        foreach (Designator element in dropdown.Elements)
        {
            if (!quickSearchFilter.Matches(element.Label)) continue;
            __result = true;
            return;
        }
    }
}

/// <summary>
/// Draws the filter toggle to the right of the Architect search box's clear (X) button.
/// </summary>
[HarmonyPatch(typeof(QuickSearchWidget), nameof(QuickSearchWidget.OnGUI))]
public static class ArchitectSearchToggle_Patch
{
    private const float ButtonSize = 18f;
    private const float Gap = 4f;

    private static readonly AccessTools.FieldRef<MainTabWindow_Architect, QuickSearchWidget> QuickSearchWidgetRef =
        AccessTools.FieldRefAccess<MainTabWindow_Architect, QuickSearchWidget>("quickSearchWidget");

    [HarmonyPrefix]
    public static void Prefix(QuickSearchWidget __instance, ref Rect rect)
    {
        if (MainButtonDefOf.Architect?.TabWindow is not MainTabWindow_Architect architect
            || QuickSearchWidgetRef(architect) != __instance)
            return;

        rect.width -= ButtonSize + Gap * 2f;
        Rect button = new(rect.xMax + Gap, rect.y + (rect.height - ButtonSize) / 2f, ButtonSize, ButtonSize);

        Settings settings = KeyzAllowUtilitiesMod.settings;
        bool on = settings.ArchitectSearchFilter;
        TooltipHandler.TipRegion(button, (on ? "KAU_ArchitectSearchFilterOn" : "KAU_ArchitectSearchFilterOff").Translate());
        if (!Widgets.ButtonImage(button, ArchitectSearchTextures.FilterIcon, on ? Color.white : Color.grey, GenUI.MouseoverColor))
            return;

        settings.ArchitectSearchFilter = !on;
        settings.Write();
        (on ? SoundDefOf.Checkbox_TurnedOff : SoundDefOf.Checkbox_TurnedOn).PlayOneShotOnCamera();
    }
}
