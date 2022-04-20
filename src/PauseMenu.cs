using MonoMod;
using System.IO;

class patch_PauseMenu : PauseMenu
{
    [MonoModIgnore]
    extern private static void Resume();
    public static void ResumePublic()
    {
        Resume();
    }

    extern public static void orig_UpdateOptionNames();
    new public static void UpdateOptionNames()
    {
        orig_UpdateOptionNames();
        if (Menu == M.CustomLevels)
        {
            string path = Path.Combine(RootCustomLevelsDir(), GetCustomLevelSubdir());
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                string hub = Path.Combine(dir, "hub.txt");
                if (File.Exists(hub))
                {
                    var displayName = Path.GetFileName(dir);
                    int index = OptionNames.IndexOf(displayName + Path.DirectorySeparatorChar);
                    OptionNames[index] = displayName;
                    CustomLevelItems[index] = new CustomLevelItem { path = hub };
                }
            }
        }
    }
}
