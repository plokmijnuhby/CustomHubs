using System.IO;

class patch_Screenshot : Screenshot
{
    // This is not exactly the intended purpose of this class, but I don't care
    extern private static void orig_TakeScreenshot(bool border, bool scale, string filename);
    new private static void TakeScreenshot(bool border, bool scale, string filename)
    {
        string levelPath = patch_World.paths[World.currentLevelName];
        string imagePath = Path.Combine(levelPath, "../" + World.currentLevelName + ".png");
        orig_TakeScreenshot(border, scale, imagePath);
    }
}
