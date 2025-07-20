using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;

namespace KeyzAllowUtilities.HarmonyPatches;

[HarmonyPatch(typeof(Selector))]
public static class Selector_Patch
{
    [HarmonyPatch("SelectInternal")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> SelectInternalPatch(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo settingsField = AccessTools.Field(typeof(KeyzAllowUtilitiesMod), nameof(KeyzAllowUtilitiesMod.settings));
        FieldInfo maxSelectField = AccessTools.Field(typeof(Settings), nameof(Settings.MaxSelect));

        foreach (CodeInstruction instruction in instructions)
        {
            // Check if this is the instruction loading the constant 200
            if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == 200)
            {
                yield return new CodeInstruction(OpCodes.Ldsfld, settingsField); // Load static settings field
                yield return new CodeInstruction(OpCodes.Ldfld, maxSelectField); // Get MaxSelect value
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
