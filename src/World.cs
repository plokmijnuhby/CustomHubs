﻿using MonoMod;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

class patch_World : World
{
    public static bool inCustomHub;
    public static bool walking;
    public static string customHubDir;
    public static Dictionary<string, string> paths = new Dictionary<string, string>();
    extern public static void orig_GoToLevel(string levelName);
    new public static void GoToLevel(string levelName)
    {
        if (!inCustomHub)
        {
            orig_GoToLevel(levelName);
            return;
        }
        currentLevelName = levelName;
        if (paths.ContainsKey(levelName))
        {
            LoadLevel.LoadWithFailsafe(ReadTextFile(paths[levelName]));
        }
        else
        {
            Debug.LogError("Couldn't find level " + levelName);
            GoToHub();
        }
    }

    [MonoModIgnore]
    extern private static void WalkGoToNext();

    new public static void GoToHub()
    {
        // If we encountered an error while walking levels, go to the next one.
        // If we just finished walking levels to make screenshots, or the user is exiting a portal,
        // or the user toggled "unlock all puzzles", go to the custom hub,
        // otherwise to go to the real hub.
        if (WalkingLevels)
        {
            WalkGoToNext();
        }
        else if (walking)
        {
            walking = false;
            State = WS.Paused;
            Controls.devShrinkScreenDown = false;
            patch_LoadLevel.LoadImages(customHubDir);
            GoToLevel("hub");
        }
        else if (!inCustomHub || doJumpOut || PauseMenu.Menu == PauseMenu.M.Settings)
        {
            GoToLevel("hub");
        }
        else
        {
            inCustomHub = false;
            hubLoaded = false;
            Hub.puzzleData.Clear();
            Hub.puzzleLineRefs.Clear();
            Hub.LoadPuzzleData();
            FMODSquare.AreaNameToFMODIndex = patch_LoadLevel.oldFMODIndex;
            UnlockAllRepositionAreaName = null;
            SaveFile.Load();
            orig_GoToLevel("hub");
        }
    }

    extern public static void orig_GoToSaveFilesStartupLevel();
    new public static void GoToSaveFilesStartupLevel()
    {
        // If we are in the real hub, or the user pressed "Return to title",
        // run the real function, else go to the fake hub.
        if (PauseMenu.Menu == PauseMenu.M.Pause || !inCustomHub)
        {
            orig_GoToSaveFilesStartupLevel();
        }
        else
        {
            GoToLevel("hub");
            // At this point in the real function, deleting the save file
            // returns you to the title, but we don't want that to happen here.
        }
    }

    extern private void orig_KeyboardShortcuts();
    private void KeyboardShortcuts()
    {
        if(Keyboard.current.f5Key.wasPressedThisFrame && inCustomHub)
        {
            if (currentLevelName == "hub")
            {
                currentLevelName = "custom_level";
                LoadLevel.Load(ReadTextFile(lastLoadedCustomLevelPath));
            }
            else GoToLevel(currentLevelName);
        }
        orig_KeyboardShortcuts();
    }

    extern public static void orig_UpdateButtonsPressed();
    new public static void UpdateButtonsPressed()
    {
        orig_UpdateButtonsPressed();
        // Control whether the credits are switched on.
        if (inCustomHub && InHub())
        {
            foreach (var floor in (from floor in floors
                                   where floor.Type == Floor.FloorType.PlayerButton
                                   from floor2 in floor.OuterLevel.floorList
                                   select floor2))
            {
                if (floor.Type == Floor.FloorType.LevelPortal && floor.Hard == 0 && !floor.Won
                    && !Hub.puzzleLineRefs[floor.SceneName].fromMe.Where(
                        line => Hub.puzzleData[line.to].hard == 0
                    ).Any())
                {
                    ButtonsSatisfied = false;
                    return;
                }
            }
        }
    }

    extern private void orig_UpdateInner();
    private void UpdateInner()
    {
        orig_UpdateInner();
        if (celebration && !unlocking && !counting && inCustomHub && celebrationNumLevels <= 6)
        {
            // The original celebration time was 3.25 seconds, no matter how many puzzles in the
            // area. We are changing to have 0.5 seconds per puzzle,
            // by adding to the celebration timer.
            celebrationT += Time.deltaTime * (3.25f / (celebrationNumLevels * 0.5f) - 1);
        }
    }
}
