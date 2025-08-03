using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace KeyzAllowUtilities.HarmonyPatches;

// [HarmonyPatch(typeof(DirectXmlToObjectNew))]
public static class DirectXmlLoader_Patch
{
    public static Lazy<FieldInfo> MessageQueue = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(Log), "messageQueue"));
    public static Lazy<FieldInfo> MessageCount = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(Log), "messageCount"));
    public static Lazy<FieldInfo> LastMessage = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(LogMessageQueue), "lastMessage"));

    // [HarmonyPatch(nameof(DirectXmlToObjectNew.DefFromNodeNew))]
    // [HarmonyPrefix]
    public static void DefFromNodeNew_Prefix(ref LogMessage __state)
    {
        LogMessage message = LastLogMessage();
        if(message == null) return;
        __state = message;
    }

    public static LogMessage LastLogMessage()
    {
        LogMessageQueue queue = (LogMessageQueue) MessageQueue.Value.GetValue(typeof(Log));
        if(queue == null) return null;
        LogMessage message = (LogMessage) LastMessage.Value.GetValue(queue);
        return message;
    }

    // [HarmonyPatch(nameof(DirectXmlToObjectNew.DefFromNodeNew))]
    // [HarmonyPostfix]
    public static void DefFromNodeNew_Postfix(Def __result, LoadableXmlAsset loadingAsset, LogMessage __state)
    {
        // if (!KeyzAllowUtilitiesMod.settings.DefErrorLog)
        // {
        //     return;
        // }

        if (__result == null  || LastLogMessage() == __state)
        {
            return;
        }

        ModContentPack mod = loadingAsset?.mod;
        if ((int)MessageCount.Value.GetValue(typeof(Log)) > 900)
        {
            Log.ResetMessageCount();
        }


        string text = null;
        if (loadingAsset != null)
        {
            string fullFilePath = loadingAsset.FullFilePath;
            text = fullFilePath?.Replace(mod?.RootDir ?? string.Empty, "");
        }

        ModLog.DefError($"From Def Named: {__result.defName} | In mod named: {mod?.Name} | At path: {text}");
    }
}
