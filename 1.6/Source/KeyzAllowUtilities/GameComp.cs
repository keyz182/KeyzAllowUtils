using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace KeyzAllowUtilities;

public class GameComp(Game game) : GameComponent
{
    public static GameComp Instance;
    public Game Game = game;

    public bool EditModeActive = false;
    public TimeSpeed TimeSpeedBeforeEditMode;

    public override void FinalizeInit()
    {
        KeyzAllowUtilitiesMod.settings.ValidateDesignators();
        Instance = this;
    }

    public override void StartedNewGame()
    {
        KeyzAllowUtilitiesMod.settings.ValidateDesignators();
        Instance = this;
    }

    public override void LoadedGame()
    {
        KeyzAllowUtilitiesMod.settings.ValidateDesignators();
        Instance = this;
    }

    public override void GameComponentOnGUI()
    {
        if (KeyzAllowUtilitesDefOf.KAU_MapEditMode.KeyDownEvent)
        {
            EditModeActive = !EditModeActive;
            if (EditModeActive)
            {
                TimeSpeedBeforeEditMode = Find.TickManager.CurTimeSpeed;
            }
            else
            {
                Find.TickManager.CurTimeSpeed = TimeSpeedBeforeEditMode;
            }
        }
    }

    public override void GameComponentTick()
    {
        if (EditModeActive && Find.TickManager.CurTimeSpeed != TimeSpeed.Paused)
        {
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        }
    }

    public static Color EditModeIndicatorColor = new Color(0.75f, 0.2f, 0.2f);

    public virtual void DrawEditModeIndicator()
    {
        Rect rect = new Rect(0, 0, UI.screenWidth, UI.screenHeight);

        Rect left = new Rect(0,0, 5, UI.screenHeight);
        Rect right = new Rect(rect.xMax-5, 0, 5, UI.screenHeight);
        Rect top = new Rect(0, 0, UI.screenWidth, 5);
        Rect bottom = new Rect(0, rect.yMax - 5, UI.screenWidth, 5);

        Widgets.DrawBoxSolid(left,   EditModeIndicatorColor);
        Widgets.DrawBoxSolid(top,    EditModeIndicatorColor);
        Widgets.DrawBoxSolid(bottom, EditModeIndicatorColor);
        Widgets.DrawBoxSolid(right,  EditModeIndicatorColor);
    }

    public virtual void MapInterface_EditModeOnGUI(MapInterface mapInterface)
    {
        if (WorldRendererUtility.DrawingMap)
        {
            // mapInterface.thingOverlays.ThingOverlaysOnGUI();
            Find.CurrentMap.MapOnGUI();
            // MapComponentUtility.MapComponentOnGUI(Find.CurrentMap);
            // mapInterface.selector.dragBox.DragBoxOnGUI();
            // mapInterface.designatorManager.DesignationManagerOnGUI();
            // mapInterface.targeter.TargeterOnGUI();
            // mapInterface.selector.SelectorOnGUI_BeforeMainTabs();
            DrawEditModeIndicator();
        }
        else
            mapInterface.targeter.StopTargeting();
    }
}
