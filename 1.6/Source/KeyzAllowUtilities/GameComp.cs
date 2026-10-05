using Verse;

namespace KeyzAllowUtilities;

public class GameComp(Game game) : GameComponent
{
    public Game Game = game;

    /// <summary>
    /// True once colony mechs that predate the mod have been given a Haul+ priority. Mechs created
    /// after that get it from vanilla EnableAndInitialize (KAU_UrgentHaul is alwaysStartActive), so
    /// this runs once per save — re-running it would clobber a player's explicit 0 from a mech
    /// work-tab mod on every load.
    /// </summary>
    private bool mechUrgentHaulMigrated;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref mechUrgentHaulMigrated, "mechUrgentHaulMigrated");
    }

    public override void FinalizeInit()
    {
        KeyzAllowUtilitiesMod.settings.ValidateDesignators();
    }

    public override void StartedNewGame()
    {
        KeyzAllowUtilitiesMod.settings.ValidateDesignators();
        mechUrgentHaulMigrated = true;
    }

    public override void LoadedGame()
    {
        KeyzAllowUtilitiesMod.settings.ValidateDesignators();
        if (mechUrgentHaulMigrated) return;
        MigrateMechUrgentHaul();
        mechUrgentHaulMigrated = true;
    }

    private static void MigrateMechUrgentHaul()
    {
        WorkTypeDef urgentHaul = KeyzAllowUtilitesDefOf.KAU_UrgentHaul;
        if (urgentHaul == null) return;

        foreach (Map map in Find.Maps)
        {
            foreach (Pawn mech in map.mapPawns.SpawnedColonyMechs)
            {
                if (!mech.RaceProps.mechEnabledWorkTypes.Contains(urgentHaul)) continue;
                mech.workSettings.Notify_UseWorkPrioritiesChanged();
                if (mech.workSettings.GetPriority(urgentHaul) <= 0)
                    mech.workSettings.SetPriority(urgentHaul, 1);
            }
        }
    }
}
