#if NET10_0_OR_GREATER
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

/// <summary>
/// When an animal marked for hunting dies, mark its corpse Haul Urgently so it does not stay
/// where it fell. Covers every way it can die: vanilla hunters, party hunt, drafted colonists
/// and turrets. Animals not marked for hunting are left alone. 1.7 only.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class Pawn_Kill_HuntHaul_Patch
{
    // The Hunt designation is removed while the pawn dies, so it has to be read beforehand.
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance, out bool __state)
    {
        __state = false;
        if (!(KeyzAllowUtilitiesMod.settings?.AutoHaulHuntedAnimals ?? false)) return;
        if (__instance is not { Spawned: true, Dead: false } || !__instance.IsAnimal) return;
        __state = __instance.Map.designationManager.DesignationOn(__instance, DesignationDefOf.Hunt) != null;
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, bool __state)
    {
        if (!__state) return;
        Corpse corpse = __instance.Corpse;
        if (corpse is not { Spawned: true }) return;
        HaulUrgentlyActions.DesignateHaulUrgently(corpse);
    }
}
#endif
