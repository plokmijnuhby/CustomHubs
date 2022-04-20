class patch_Achievements : Achievements
{
    extern public static void orig_Achieve(string name);
    new public static void Achieve(string name)
    {
        if (!patch_World.inCustomHub)
        {
            orig_Achieve(name);
        }
    }
}
