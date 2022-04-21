using System.IO;

class patch_SaveFile : SaveFile
{
    extern private static string orig_GetSaveFilePath(int slot);
    private static string GetSaveFilePath(int slot)
    {
        if (patch_World.inCustomHub)
        {
            return Path.Combine(Path.GetDirectoryName(patch_World.paths["hub"]), "save" + slot + ".txt");
        }
        else
        {
            return orig_GetSaveFilePath(slot);
        }
    }
}
