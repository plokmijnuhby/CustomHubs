using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CustomHubs;

[HarmonyPatch(typeof(World), "GoToLevel")]
public class World_GoToLevel
{
    public static bool Prefix(string levelName)
    {
        if (!CustomHub.inCustomHub)
        {
            return true;
        }
        World.currentLevelName = levelName;
        if (CustomHub.paths.ContainsKey(levelName))
        {
            LoadLevel.LoadWithFailsafe(World.ReadTextFile(CustomHub.paths[levelName]));
        }
        else
        {
            Debug.LogError("Couldn't find level " + levelName);
            World.GoToHub();
        }
        return false;
    }
}

[HarmonyPatch(typeof(World), "GoToHub")]
public class World_GoToHub
{
    public static bool Prefix()
    {
        // If we encountered an error while walking levels, go to the next one.
        // If we just finished walking levels to make screenshots, or the user is exiting a portal,
        // or the user toggled "unlock all puzzles", go to the custom hub.
        // In any other scenario, go to the real hub.
        if (World.WalkingLevels)
        {
            typeof(World)
                .GetMethod("WalkGoToNext", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, null);
            return false;
        }
        else if (CustomHub.walking)
        {
            CustomHub.walking = false;
            World.State = World.WS.Paused;
            Controls.devShrinkScreenDown = false;
            CustomHub.LoadImages();
        }
        else if ((CustomHub.inCustomHub && !World.doJumpOut || !CustomHub.inCustomHub)
            && PauseMenu.Menu != PauseMenu.M.Settings)
        {
            CustomHub.inCustomHub = false;
            World.hubLoaded = false;
            Hub.puzzleData.Clear();
            Hub.puzzleLineRefs.Clear();
            Hub.LoadPuzzleData();
            FMODSquare.AreaNameToFMODIndex = CustomHub.oldFMODIndex;
            World.UnlockAllRepositionAreaName = null;
            SaveFile.Load();
        }
        return true;
    }
}

[HarmonyPatch(typeof(World), "GoToSaveFilesStartupLevel")]
public class World_GoToSaveFilesStartupLevel
{
    public static bool Prefix()
    {
        // If we are in the real hub, or the user pressed "Return to title",
        // run the real function, else go to the fake hub.
        if (PauseMenu.Menu == PauseMenu.M.Pause || !CustomHub.inCustomHub)
        {
            return true;
        }
        else
        {
            World.GoToLevel("hub");
            // At this point in the real function, deleting the save file
            // returns you to the title, but we don't want that to happen here.
            return false;
        }
    }
}

[HarmonyPatch(typeof(World), "KeyboardShortcuts")]
public class World_KeyboardShortcuts
{
    public static void Prefix()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame && CustomHub.inCustomHub)
        {
            if (World.currentLevelName == "hub")
            {
                World.currentLevelName = "custom_level";
                LoadLevel.LoadWithFailsafe(
                    World.ReadTextFile(World.lastLoadedCustomLevelPath));
            }
            else World.GoToLevel(World.currentLevelName);
        }
    }
}

[HarmonyPatch(typeof(World), "UpdateButtonsPressed")]
public class World_UpdateButtonsPressed
{
    public static void Postfix()
    {
        // Control whether the credits are switched on.
        if (CustomHub.inCustomHub && World.InHub())
        {
            foreach (var floor in from floor in World.floors
                                  where floor.Type == Floor.FloorType.PlayerButton
                                  from floor2 in floor.OuterLevel.floorList
                                  select floor2)
            {
                if (floor.Type == Floor.FloorType.LevelPortal && floor.Hard == 0 && !floor.Won
                    && !Hub.puzzleLineRefs[floor.SceneName].fromMe.Where(
                        line => Hub.puzzleData[line.to].hard == 0
                    ).Any())
                {
                    World.ButtonsSatisfied = false;
                    return;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(World), "UpdateInner")]
public class World_UpdateInner
{
    public static void Postfix()
    {
        if (World.celebration && !World.unlocking && !World.counting && CustomHub.inCustomHub)
        {
            // The original celebration time was 3.25 seconds, no matter how many puzzles in the
            // area. We are changing to have a shorter celebration time.
            var duration = Mathf.Pow(2, -World.celebrationNumLevels/2f + 2) + 3.5f;
            World.celebrationT += Time.deltaTime * (3.25f / duration - 1);
        }
    }
}
