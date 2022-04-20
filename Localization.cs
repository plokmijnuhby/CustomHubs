using UnityEngine;

class patch_Localization : Localization
{
    extern public static string orig_Localize(string id, string language = "");
    new public static string Localize(string id, string language = "")
    {
        // I know what I'm doing, unity, please shut up
        Debug.unityLogger.logEnabled = false;
        string res = orig_Localize(id, language);
        Debug.unityLogger.logEnabled = true;
        return res.Replace('_', ' ');
    }
}
