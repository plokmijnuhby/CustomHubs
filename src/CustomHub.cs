using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomHubs;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class CustomHub : BaseUnityPlugin
{
    public static bool inCustomHub;
    public static bool walking;
    public static string customHubDir;
    public static Dictionary<string, string> paths = new Dictionary<string, string>();
    public static Dictionary<string, int> oldFMODIndex = FMODSquare.AreaNameToFMODIndex;

    public void Awake()
    {
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
    }

    public static bool IsFirstArea(Level level)
    {
        // Check if level precedes area without unlockable walls
        var outside = level.GetExitBlock().OuterLevel;
        if (outside != null && outside.hubAreaName != null)
        {
            if (World.wallUnlockAnimPlayed.ContainsKey(outside.hubAreaName))
            {
                return false;
            }
            foreach (var block in outside.blockList)
            {
                if (block.unlockerScene != null)
                {
                    foreach (var floor in outside.floorList)
                    {
                        if (floor.SceneName == block.unlockerScene)
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

    public static IEnumerable<string> GetFiles(string pattern)
    {
        return Directory.EnumerateFiles(customHubDir, pattern, SearchOption.AllDirectories);
    }
    public static IEnumerable<string[]> GetTokens(string name)
    {
        return from file in GetFiles(name + ".txt")
               from line in World.ReadTextFile(file).Replace("\r", "").Split('\n')
               select line.Split(' ');
    }

    public static void LoadImages()
    {
        foreach (string file in GetFiles("*"))
        {
            if (Path.GetExtension(file) == ".txt") continue;
            string name = Path.GetFileNameWithoutExtension(file);
            if (Hub.puzzleData.ContainsKey(name)
                && Hub.puzzleData[name].thumbnail == null)
            {
                var tex = new Texture2D(0, 0);
                tex.LoadImage(File.ReadAllBytes(file));
                Hub.puzzleData[name].thumbnail = tex;
            }
        }
    }
}