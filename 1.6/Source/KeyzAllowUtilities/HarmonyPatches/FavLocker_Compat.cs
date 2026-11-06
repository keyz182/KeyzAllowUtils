using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

/// <summary>
/// Fav Locker (Continued) fills lockers through its own WorkGiver_LoadLockers, which never calls
/// HaulAIUtility, so Do Not Haul was ignored. Its findThingToLoad picks the closest needed
/// apparel with a compiler-generated lambda (Thing => bool); a postfix on that lambda rejects
/// items marked Do Not Haul. Patched by reflection, so there is no compile-time dependency.
/// Only applied when the mod is active.
/// </summary>
public static class FavLocker_Compat
{
    public const string PackageId = "Mlie.FavLocker";
    private const string WorkGiverType = "Locker.WorkGiver_LoadLockers";

    public static void Apply(Harmony harmony)
    {
        // Gate on the mod being loaded. ModsConfig.IsActive returned false for this id on 1.7
        // even with the mod running, so check the running mod list case-insensitively.
        if (!LoadedModManager.RunningMods.Any(m => string.Equals(m.PackageId, PackageId, System.StringComparison.OrdinalIgnoreCase))) return;

        MethodInfo validator = FindValidator();
        if (validator == null)
        {
            ModLog.Warn("Fav Locker is active but its locker-loading filter was not found — Do Not Haul will not apply to lockers");
            return;
        }

        harmony.Patch(validator, postfix: new HarmonyMethod(typeof(FavLocker_Compat), nameof(Validator_Postfix)));
        ModLog.Log("Fav Locker compatibility applied: lockers skip items marked Do Not Haul");
    }

    // The lambda lives on a nested display class; its name contains "<findThingToLoad>".
    private static MethodInfo FindValidator()
    {
        System.Type workGiver = AccessTools.TypeByName(WorkGiverType);
        if (workGiver == null) return null;

        return workGiver.GetNestedTypes(BindingFlags.NonPublic)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .FirstOrDefault(m => m.Name.Contains("<findThingToLoad>")
                                 && m.ReturnType == typeof(bool)
                                 && m.GetParameters() is { Length: 1 } ps
                                 && ps[0].ParameterType == typeof(Thing));
    }

    public static void Validator_Postfix(Thing __0, ref bool __result)
    {
        if (!__result) return;
        if (KeyzAllowUtilitiesMod.settings?.DisableNoHauling ?? true) return;
        if (HaulAIUtility_Patches.HasNoHaulDesignation(__0)) __result = false;
    }
}
