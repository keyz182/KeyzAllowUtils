using Verse;
using UnityEngine;
using HarmonyLib;

namespace KeyzAllowUtilities;

public class KeyzAllowUtilitiesMod : Mod
{
    public static Settings settings;

    public KeyzAllowUtilitiesMod(ModContentPack content) : base(content)
    {

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("keyz182.rimworld.KeyzAllowUtilities.main");	
        harmony.PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "KeyzAllowUtilities_SettingsCategory".Translate();
    }
}
