using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        LoadLevel.LoadWithFailsafe(ReadTextFile(paths[levelName]));
    }

    new public static void GoToHub()
    {
        // If we are walking levels to make screenshots, or the user is exiting a portal,
        // go to the custom hub, otherwise to go to the real hub.
        if (walking)
        {
            walking = false;
            State = WS.Paused;
            Controls.devShrinkScreenDown = false;
            patch_LoadLevel.LoadImages(customHubDir);
            GoToLevel("hub");
        }
        else if (doJumpOut)
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
    new private void KeyboardShortcuts()
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
                if (floor.Type == Floor.FloorType.LevelPortal && floor.Hard == 0
                    && Hub.puzzleLineRefs[floor.SceneName].fromMe.Count == 0 && !floor.Won)
                {
                    ButtonsSatisfied = false;
                    return;
                }
            }
        }
    }
}
