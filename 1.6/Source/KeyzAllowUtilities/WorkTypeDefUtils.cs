using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeyzAllowUtilities;

public static class WorkTypeDefUtils
{
    public static Lazy<FieldInfo> Visible = new(()=>AccessTools.Field(typeof(WorkTypeDef), "visible"));
    private static Lazy<FieldInfo> WorkTabTable = new(()=>AccessTools.Field(typeof(MainTabWindow_PawnTable), "table"));

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

            if (UnityData.IsInMainThread)
            {
                // On the main thread (settings UI), rebuild the table synchronously.
                // This handles the case where the tab is already in the window stack
                // and PostOpen() won't fire again.
                workTab.Notify_ResolutionChanged();
            }
            else
            {
                // On a background thread (game startup/load), we can't call GUI
                // methods. Null the table so PostOpen() recreates it when the tab
                // is next opened.
                WorkTabTable.Value?.SetValue(workTab, null);
            }
        }
        catch
        {
            // Non-critical — work tab may not be open yet
        }
    }
}
