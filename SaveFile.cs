using System.IO;

class patch_SaveFile : SaveFile
{
    extern private static string orig_GetSaveFilePath(int slot);
    new private static string GetSaveFilePath(int slot)
    {
        if (patch_World.inCustomHub)
        {
            return Path.Combine(patch_World.paths["hub"], "../save" + slot.ToString() + ".txt");
        }
        else
        {
            return orig_GetSaveFilePath(slot);
        }
    }
}
