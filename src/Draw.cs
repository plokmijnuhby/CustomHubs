using HarmonyLib;
using System.Text.RegularExpressions;

namespace CustomHubs;

[HarmonyPatch(typeof(Draw), "UpdateCameraMode")]
public class Draw_UpdateCameraMode
{
    // We don't want to display the hub on the title,
    // under any circumstances
    public static void Prefix()
    {
        if (CustomHub.inCustomHub)
        {
            World.FirstLevelSinceStartup = false;
        }
    }
}

[HarmonyPatch(typeof(Draw), "ConstructCreditsString")]
public class Draw_ConstructCreditsString
{
    public static bool Prefix()
    {
        if (CustomHub.inCustomHub && CustomHub.paths.ContainsKey("credits"))
        {
            Draw.creditsString = World.ReadTextFile(CustomHub.paths["credits"]);
            Draw.creditsLines = Regex.Matches(Draw.creditsString, "\n").Count;
            return false;
        }
        else return true;
    }
}
[HarmonyPatch(typeof(Draw), "PerpetualZoomOutPickNewBlock")]
public class Draw_PerpetualZoomOutPickNewBlock
{
    public struct State
    {
        public bool firstArea;
        public int oldEffect;
    }
    public static void Prefix(ref State __state)
    {
        if (CustomHub.inCustomHub
            && CustomHub.IsFirstArea(Draw.ZoomOutAnimFocusBlock.SubLevel))
        {
            __state = new State
            {
                firstArea = true,
                oldEffect = Draw.ZoomOutAnimFocusBlock.SpecialEffect
            };
            Draw.ZoomOutAnimFocusBlock.SpecialEffect = 6;
        }
    }

    public static void Postfix(ref State __state)
    {
        if (__state.firstArea)
        {
            Draw.ZoomOutAnimFocusBlock.SpecialEffect = __state.oldEffect;
        }
    }
}
