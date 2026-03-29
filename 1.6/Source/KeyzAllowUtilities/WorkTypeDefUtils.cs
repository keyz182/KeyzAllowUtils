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
    private static Lazy<FieldInfo> VisibleCacheFrame = new(()=>AccessTools.Field(typeof(WorkTypeDef), "cachedFrameVisibleCurrently"));

    public static void Toggle(this WorkTypeDef def, bool state)
    {
        if(state) def.Show();
        else def.Hide();
        RestaggerWorkColumnHeaders();
        InvalidateWorkTabLayout();
    }
    public static void Hide(this WorkTypeDef def)
    {
        Visible.Value.SetValue(def, false);
        VisibleCacheFrame.Value?.SetValue(def, -1);
    }

    public static void Show(this WorkTypeDef def)
    {
        Visible.Value.SetValue(def, true);
        VisibleCacheFrame.Value?.SetValue(def, -1);
    }

    /// <summary>
    /// Vanilla sets moveWorkTypeLabelDown by alternating a boolean across visible
    /// work type columns at startup (PawnColumnDefGenerator.ImpliedPawnColumnDefs).
    /// When we hide/show columns at runtime, the stagger pattern breaks — two
    /// adjacent columns can end up with the same Y offset, causing header text
    /// overlap. This method re-applies the alternating pattern to currently-visible
    /// work type columns.
    /// </summary>
    private static void RestaggerWorkColumnHeaders()
    {
        try
        {
            var workTableDef = PawnTableDefOf.Work;
            if (workTableDef == null) return;

            bool down = false;
            foreach (var colDef in workTableDef.columns)
            {
                if (colDef.workType == null) continue;
                if (!colDef.workType.VisibleCurrently) continue;
                down = !down;
                colDef.moveWorkTypeLabelDown = down;
            }
        }
        catch
        {
            // Non-critical
        }
    }

    private static void InvalidateWorkTabLayout()
    {
        try
        {
            var workButtonDef = DefDatabase<MainButtonDef>.GetNamedSilentFail("Work");
            if (workButtonDef?.TabWindow is not MainTabWindow_PawnTable workTab) return;

            if (UnityData.IsInMainThread)
            {
                workTab.Notify_ResolutionChanged();
            }
            else
            {
                WorkTabTable.Value?.SetValue(workTab, null);
            }
        }
        catch
        {
            // Non-critical — work tab may not be open yet
        }
    }
}
