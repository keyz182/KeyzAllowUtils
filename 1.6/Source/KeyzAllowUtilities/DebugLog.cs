using System.Diagnostics;
using System;

namespace KeyzAllowUtilities;

public static class ModLog
{
    private static string Tag => $"<color=#1c6beb>[KeyzAllowUtilities v{KeyzAllowUtilitiesMod.Version}]</color>";

    [Conditional("DEBUG")]
    public static void Debug(string x)
    {
        Verse.Log.Message(x);
    }

    public static void Log(string msg)
    {
        Verse.Log.Message($"{Tag} {msg ?? "<null>"}");
    }

    public static void Warn(string msg)
    {
        Verse.Log.Warning($"{Tag} {msg ?? "<null>"}");
    }

    public static void Error(string msg, Exception e = null)
    {
        Verse.Log.Error($"{Tag} {msg ?? "<null>"}");
        if (e != null)
            Verse.Log.Error(e.ToString());
    }

    public static void DefError(string msg)
    {
        Verse.Log.Warning($"<color=#6beb1c>[^^^Def Mod Error^^^]</color> {msg ?? "<null>"}");
    }

}
