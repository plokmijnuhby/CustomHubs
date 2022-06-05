using HarmonyLib;
using System.IO;

namespace CustomHubs;

[HarmonyPatch(typeof(PauseMenu), "UpdateOptionNames")]
public class PauseMenu_UpdateOptionNames
{
    public static void Postfix()
    {
        if (PauseMenu.Menu == PauseMenu.M.CustomLevels)
        {
            string path = Path.Combine(
                Path.GetFullPath(PauseMenu.RootCustomLevelsDir()),
                PauseMenu.GetCustomLevelSubdir());
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                string hub = Path.Combine(dir, "hub.txt");
                if (File.Exists(hub))
                {
                    var displayName = Path.GetFileName(dir);
                    int index = PauseMenu.OptionNames.IndexOf(
                        displayName + Path.DirectorySeparatorChar);
                    PauseMenu.OptionNames[index] = displayName;
                    PauseMenu.CustomLevelItems[index] = new PauseMenu.CustomLevelItem {
                        path = hub
                    };
                }
            }
        }
    }
}
