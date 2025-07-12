using System;
using Verse;
using UnityEngine;

namespace KeyzAllowUtilities;

public class Command_Toggle_WithContext: Command_Toggle
{
    public Action RightClickAction;

    public override void ProcessInput(Event ev)
    {
        if(ev.button == 1) RightClickAction();
        else base.ProcessInput(ev);
    }

}
