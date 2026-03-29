using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

public static class WorkTypeDefUtils
{
    public static Lazy<FieldInfo> Visible = new(()=>AccessTools.Field(typeof(WorkTypeDef), "visible"));

    public static void Toggle(this WorkTypeDef def, bool state)
    {
        if(state) def.Show();
        else def.Hide();
        InvalidateWorkTabLayout();
    }
    public static void Hide(this WorkTypeDef def)
    {
        Visible.Value.SetValue(def, false);
    }

    public static void Show(this WorkTypeDef def)
    {
        Visible.Value.SetValue(def, true);
    }

    private static void InvalidateWorkTabLayout()
    {
        try
        {
            var workButtonDef = DefDatabase<MainButtonDef>.GetNamedSilentFail("Work");
            if (workButtonDef?.TabWindow is not MainTabWindow_PawnTable workTab) return;
            // Rebuild the column layout synchronously. This works whether the tab is
            // currently open or closed — unlike nulling the table, which skips the
            // rebuild if PostOpen() isn't called again (tab already in window stack).
            workTab.Notify_ResolutionChanged();
        }
        catch
        {
            // Non-critical — work tab may not be open yet
        }
    }
}
