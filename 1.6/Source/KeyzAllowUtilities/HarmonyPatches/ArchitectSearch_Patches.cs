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

    public static IEnumerable<Designator> FilterForSearch(IEnumerable<Designator> designators, ArchitectCategoryTab tab)
    {
        if (!KeyzAllowUtilitiesMod.settings.ArchitectSearchFilter || !QuickSearchFilterRef(tab).Active)
            return designators;

        List<Designator> matches = new();
        foreach (Designator designator in designators)
        {
            if (MatchesFunc(tab, designator)) matches.Add(designator);
        }
        return matches;
    }

    /// <summary>
    /// Feeds the gizmo grid in DesignationTabOnGUI through <see cref="FilterForSearch"/>.
    /// Only this call site changes, so search state and category colours still see the full list.
    /// </summary>
    [HarmonyPatch(nameof(ArchitectCategoryTab.DesignationTabOnGUI))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DesignationTabOnGUI_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo getter = AccessTools.PropertyGetter(typeof(DesignationCategoryDef),
            nameof(DesignationCategoryDef.ResolvedAllowedDesignators));
        MethodInfo filter = AccessTools.Method(typeof(ArchitectSearch_Patches), nameof(FilterForSearch));

        bool patched = false;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (patched || !instruction.Calls(getter)) continue;

            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, filter);
            patched = true;
        }

        if (!patched)
            Log.Error("[KeyzAllowUtilities] Architect search filter: ResolvedAllowedDesignators call not found; search will dim instead of filter.");
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
