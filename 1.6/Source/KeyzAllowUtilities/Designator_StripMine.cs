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

    Texture2D Diagram = ContentFinder<Texture2D>.Get("UI/KAU_StripMineDiagram");

    public Designator_StripMine()
    {
        defaultLabel = "KAU_StripMine".Translate();
        defaultDesc = "KAU_StripMineDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/KAU_StripMine");
    }

    public override void DoExtraGuiControls(float leftX, float bottomY)
    {
        float width = 200f;
        float height = 500f;
        Rect winRect = new(leftX, bottomY - 90f, width, height);
        Find.WindowStack.ImmediateWindow(73445, winRect, WindowLayer.GameUI, (Action) (() =>
        {
            Rect rect = new Rect(0, 0, width, height);
            // Widgets.DrawRectFast(rect, Color.blue);
            Rect inset = rect.ContractedBy(4f);
            // Widgets.DrawRectFast(inset, Color.green);

            Listing_Standard options = new();
            options.Begin(inset);

            Rect diagram = options.GetRect(192f);
            Widgets.DrawTextureFitted(diagram, Diagram, 1f);

            options.Label("Horizontal <color=red>Spacing</color>");
            options.IntAdjusterWithDisplay(ref SpacingX, 1, 2);
            options.Label("Horizontal <color=blue>Offset</color>");
            options.IntAdjusterWithDisplay(ref OffsetX, 1, 0);
            options.Label("Vertical <color=red>Spacing</color>");
            options.IntAdjusterWithDisplay(ref SpacingZ, 1, 2);
            options.Label("Vertical <color=blue>Offset</color>");
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
