#if NET10_0_OR_GREATER
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

/// <summary>
/// Party hunt: a drafted colonist with the toggle on shoots animals marked for hunting from
/// where they stand, using the same "fire at will" path vanilla uses for hostiles. 1.7 only.
/// </summary>
[StaticConstructorOnStartup]
public static class PartyHunt
{
    public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Designators/Hunt");

    private static GameComp Comp => Current.Game?.GetComponent<GameComp>();

    public static bool IsEnabled(Pawn pawn) => pawn != null && (Comp?.PartyHunters.Contains(pawn) ?? false);

    /// <summary>Synced under Multiplayer (see MpCompatMod). Always change state through here.</summary>
    public static void SetEnabled(Pawn pawn, bool enabled)
    {
        GameComp comp = Comp;
        if (comp == null || pawn == null) return;
        if (enabled) comp.PartyHunters.Add(pawn);
        else comp.PartyHunters.Remove(pawn);
    }

    /// <summary>True when the pawn should get the toggle: a drafted player pawn with a ranged weapon.</summary>
    public static bool CanUse(Pawn pawn)
    {
        if (KeyzAllowUtilitiesMod.settings?.DisablePartyHunt ?? true) return false;
        if (pawn == null || !pawn.Drafted || pawn.Faction != Faction.OfPlayer) return false;
        if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return false;
        Verb verb = pawn.CurrentEffectiveVerb;
        return verb != null && !verb.verbProps.IsMeleeAttack;
    }

    /// <summary>
    /// Nearest animal marked for hunting that the verb can hit from the pawn's current cell.
    /// Line-of-sight (CanHitTarget) is only checked for candidates nearer than the best so far.
    /// Downed animals are skipped: vanilla hunters or Finish Off deal with those.
    /// </summary>
    public static Pawn FindTarget(Pawn hunter, Verb verb)
    {
        Map map = hunter.Map;
        if (map == null) return null;

        Pawn best = null;
        float bestDistSq = float.MaxValue;
        foreach (Designation des in map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.Hunt))
        {
            if (des.target.Thing is not Pawn animal || animal.Dead || animal.Downed || !animal.Spawned) continue;
            float distSq = (animal.Position - hunter.Position).LengthHorizontalSquared;
            if (distSq >= bestDistSq) continue;
            if (!verb.CanHitTarget(animal)) continue;
            best = animal;
            bestDistSq = distSq;
        }
        return best;
    }
}
#endif
