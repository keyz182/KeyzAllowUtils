using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

[StaticConstructorOnStartup]
public class Designator_StripMine : Designator_Mine
{
    public static int OffsetX = 0;
    public static int OffsetZ = 0;
    public static int SpacingX = 1;
    public static int SpacingZ = 1;

    public override bool Disabled
    {
        get => disabled || KeyzAllowUtilitiesMod.settings.DisableStripMine;
        set => disabled = value;
    }

    public override bool Visible => !KeyzAllowUtilitiesMod.settings.DisableStripMine;

    Texture2D Diagram = ContentFinder<Texture2D>.Get("UI/KAU_StripMineDiagram");

    public Designator_StripMine()
    {
        defaultLabel = "KAU_StripMine".Translate();
        defaultDesc = "KAU_StripMineDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KAU_StripMine");
        hotKey = KeyzAllowUtilitesDefOf.KAU_StripMine;
    }

    public static readonly float ControlWindowWidth = 200f;
    public static readonly float ControlWindowHeight = 320f;
    public static readonly float ControlWindowBottomOffset = 300f;
    public static readonly float DiagramSize = 192f;

    public override void DoExtraGuiControls(float leftX, float bottomY)
    {
        Rect winRect = new(leftX, Mathf.Clamp(bottomY - ControlWindowBottomOffset, ControlWindowHeight, 1000f), ControlWindowWidth, ControlWindowHeight);
        Find.WindowStack.ImmediateWindow(73445, winRect, WindowLayer.GameUI, (Action) (() =>
        {
            Rect rect = new Rect(0, 0, ControlWindowWidth, ControlWindowHeight);
            // Widgets.DrawRectFast(rect, Color.blue);
            Rect inset = rect.ContractedBy(4f);
            // Widgets.DrawRectFast(inset, Color.green);

            RectDivider div = new RectDivider(inset, 1412412441);
            Rect diagram = div.NewRow(DiagramSize);
            Widgets.DrawTextureFitted(diagram, Diagram, 1f);

            RectDivider col1 = div.NewCol(96f, marginOverride:0f);
            // Widgets.DrawRectFast(col1, Color.yellow);
            RectDivider col2 = div.NewCol(96f, marginOverride:0f);
            // Widgets.DrawRectFast(col2, Color.magenta);

            Listing_Standard options = new();
            options.Begin(col1);
            options.Label("<size=10%>Horiz <color=red>Spacing</color></size>");
            options.IntAdjusterWithDisplay(ref SpacingX, 1, 2);
            options.Label("<size=10%>Vert <color=red>Spacing</color></size>");
            options.IntAdjusterWithDisplay(ref SpacingZ, 1, 2);
            options.End();

            options = new();
            options.Begin(col2);
            options.Label("<size=10%>Horiz <color=blue>Offset</color></size>");
            options.IntAdjusterWithDisplay(ref OffsetX, 1, 0);
            options.Label("<size=10%>Vert <color=blue>Offset</color></size>");
            options.IntAdjusterWithDisplay(ref OffsetZ, 1, 0);
            options.End();
        }));
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!base.CanDesignateCell(c).Accepted)
            return false;

        return (OffsetX + c.x) % (SpacingX + 1) == 0 || (OffsetZ + c.z) % (SpacingZ + 1) == 0;
    }
}
