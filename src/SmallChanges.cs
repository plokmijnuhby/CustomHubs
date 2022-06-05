using HarmonyLib;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomHubs;

[HarmonyPatch(typeof(SaveFile), "GetSaveFilePath")]
public class SaveFile_GetSaveFilePath
{
    public static bool Prefix(ref string __result, int slot)
    {
        if (CustomHub.inCustomHub)
        {
            __result = Path.Combine(
                Path.GetDirectoryName(CustomHub.paths["hub"]),
                "save" + slot + ".txt");
            return false;
        }
        else return true;
    }
}

[HarmonyPatch(typeof(Localization), "Localize")]
public class Localization_Localize
{
    public static void Prefix()
    {
        // I know what I'm doing, unity, please shut up
        Debug.unityLogger.logEnabled = false;
    }

    public static void Postfix(ref string __result)
    {
        Debug.unityLogger.logEnabled = true;
        __result = __result.Replace('_', ' ');
    }
}

[HarmonyPatch(typeof(Screenshot), "TakeScreenshot")]
public class Screenshot_TakeScreenshot
{
    public static void Prefix(ref string filename)
    {
        string levelPath = CustomHub.paths[World.currentLevelName];
        filename = Path.Combine(levelPath, "../" + World.currentLevelName + ".png");
    }
}

[HarmonyPatch(typeof(Achievements), "Achieve")]
public class Achievements_Achieve
{
    public static bool Prefix(string name)
    {
        string[] ignored =
        {
            "ACH_GAME_CLEAR",
            "ACH_200_SOLVED",
            "ACH_100_PERCENT",
            "ACH_TIDY"
        };
        return !CustomHub.inCustomHub || !ignored.Contains(name);
    }
}