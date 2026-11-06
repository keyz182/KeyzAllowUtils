using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

/// <summary>
/// Chooses how a pawn finishes off a downed target, so the work giver and the right-click menu
/// agree. On 1.7 a pawn holding a ranged weapon whose Shooting is higher than its Melee uses a
/// point-blank kill shot; everyone else uses melee, as before.
/// </summary>
public static class FinishOffVerb
{
    public const int MinSkill = 5;

    public static Verb Choose(Pawn pawn, Pawn victim, out bool ranged)
    {
        ranged = false;
#if NET10_0_OR_GREATER
        if (!(KeyzAllowUtilitiesMod.settings?.DisableRangedFinishOff ?? true))
        {
            Verb primary = pawn.equipment?.PrimaryEq?.PrimaryVerb;
            if (primary != null && !primary.verbProps.IsMeleeAttack
                && SkillLevel(pawn, SkillDefOf.Shooting) > SkillLevel(pawn, SkillDefOf.Melee))
            {
                ranged = true;
                return primary;
            }
        }
#endif
        return pawn.meleeVerbs?.TryGetMeleeVerb(victim);
    }

    public static int SkillLevel(Pawn pawn, SkillDef skill) => pawn.skills?.GetSkill(skill)?.Level ?? 0;

    /// <summary>Null when the pawn is skilled enough, otherwise the translated reason.</summary>
    public static string SkillTooLowReason(Pawn pawn, bool ranged)
    {
        if (KeyzAllowUtilitiesMod.settings.DisableMeleeRequirementForFinishOff) return null;
        int level = SkillLevel(pawn, ranged ? SkillDefOf.Shooting : SkillDefOf.Melee);
        if (level >= MinSkill) return null;
        return (ranged ? "KAU_ShootingTooLow" : "KAU_MeleeTooLow").Translate(level);
    }
}
