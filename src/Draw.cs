using System.IO;
using System.Text.RegularExpressions;

class patch_Draw : Draw
{
    extern public static void orig_UpdateCameraMode();
    new public static void UpdateCameraMode()
    {
        // We don't want to display the hub on the title,
        // under any circumstances
        if (patch_World.inCustomHub)
        {
            World.FirstLevelSinceStartup = false;
        }
        orig_UpdateCameraMode();
    }

    extern public static void orig_ConstructCreditsString();
    new public static void ConstructCreditsString()
    {
        if (patch_World.inCustomHub)
        {
            string path;
            if (patch_World.paths.TryGetValue("credits", out path))
            {
                creditsString = World.ReadTextFile(path);
                creditsLines = Regex.Matches(creditsString, "\n").Count;
                return;
            }
        }
        orig_ConstructCreditsString();
    }

    extern public static void orig_PerpetualZoomOutPickNewBlock();
    new public static void PerpetualZoomOutPickNewBlock()
    {
        if (!patch_World.inCustomHub
            || !patch_LoadLevel.IsFirstArea(ZoomOutAnimFocusBlock.SubLevel))
        {
            orig_PerpetualZoomOutPickNewBlock();
            return;
        }
        // This tells parabox to still run its checks, but take this to be the last block
        int oldEffect = ZoomOutAnimFocusBlock.SpecialEffect;
        ZoomOutAnimFocusBlock.SpecialEffect = 6;
        orig_PerpetualZoomOutPickNewBlock();
        ZoomOutAnimFocusBlock.SpecialEffect = oldEffect;
    }
}
