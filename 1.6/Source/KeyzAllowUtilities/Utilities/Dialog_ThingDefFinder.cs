using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities.Utilities;

public class Dialog_ThingDefFinder() : Dialog_OptionLister
{ public void NewColumn(float columnWidth)
    {
        curY = 0.0f;
        curX += columnWidth + 17f;
    }

    protected void NewColumnIfNeeded(float columnWidth, float neededHeight)
    {
        if (curY + (double)neededHeight <= windowRect.height)
            return;
        NewColumn(columnWidth);
    }

    protected override void DoListingItems(Rect inRect, float columnWidth)
    {
        foreach (
            ThingDef thingDef in DefDatabase<ThingDef>
                .AllDefsListForReading.Where(def => def.EverHaulable)
                .Where(def => def.defName.Contains(filter) || def.label.Contains(filter))
                .Except(KeyzAllowUtilitiesMod.settings.DefSelectionDenyList)
        )
        {
            Text.Font = GameFont.Tiny;
            NewColumnIfNeeded(columnWidth, 22f);
            Rect rect = new(curX, curY, columnWidth, 22f);
            TooltipHandler.TipRegion(rect, thingDef.description);
            if (DevGUI.ButtonText(rect, $"Add {thingDef.LabelCap}"))
            {
                KeyzAllowUtilitiesMod.settings.AddThingDefToDenyList(thingDef);
            }

            curY += 22f + verticalSpacing;
            totalOptionsHeight += 22f + verticalSpacing;
        }
    }
}
