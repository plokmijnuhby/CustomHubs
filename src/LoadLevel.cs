using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CustomHubs;

[HarmonyPatch(typeof(LoadLevel), "Load")]
public class LoadLevel_Load
{
    public enum State
    {
        NOT_CUSTOM,
        ALREADY_LOADED,
        LOADING,
        WALKING
    }

    private static void LastMinuteHubFixes()
    {
        var music = FMODSquare.AreaNameToFMODIndex;
        string area = World.FindPlayerBlock().OuterLevel.hubAreaName;
        if (area != null && music.ContainsKey(area))
        {
            music["Area_Intro"] = music[area];
        }
        else Debug.LogError("Couldn't find music for area " + area);

        // Fix some things in DoHubModifications that were just too complicated to do in assembly.
        foreach (var floor in World.floors)
        {
            // In the first area, main-path portals with no lines should not be given a line out
            // of the level, because there's no previous level for them to be connected to.
            if (floor.Type == Floor.FloorType.LevelPortal
                && Hub.puzzleLineRefs[floor.SceneName].toMe.Count == 0
                && CustomHub.IsFirstArea(floor.OuterLevel))
            {
                floor.UnlockLines = floor.UnlockLines.Where(
                    line => floor.ypos + line.dy != floor.OuterLevel.height
                    ).ToArray();
            }
            
            // Add a line from the final level to the credits
            if (floor.Type == Floor.FloorType.LevelPortal
                && !Hub.puzzleLineRefs[floor.SceneName].fromMe.Where(
                    line => Hub.puzzleData[line.to].hard == 0
                    ).Any()
                && floor.Hard == 0)
            {
                foreach (var floor2 in floor.OuterLevel.floorList)
                {
                    if (floor2.Type == Floor.FloorType.PlayerButton)
                    {
                        var unlockLine = new Floor.UnlockLine
                        {
                            dx = floor2.xpos - floor.xpos,
                            dy = floor2.ypos - floor.ypos,
                            lastPortal = true,
                            lit = floor.Won,
                            thick = true
                        };
                        Floor.ComputeUnlockLineShape(floor, floor2, unlockLine, false);
                        floor.UnlockLines = floor.UnlockLines.AddItem(unlockLine).ToArray();
                    }
                }
            }
        }
    }
    
    public static bool Prefix(out State __state)
    {
        // Not a custom hub
        if (World.currentLevelName == "custom_level"
            && Path.GetFileName(World.lastLoadedCustomLevelPath) != "hub.txt")
        {
            CustomHub.inCustomHub = false;
            __state = State.NOT_CUSTOM;
            return true;
        }
        // The real hub, or the previous loaded custom hub
        else if (World.currentLevelName != "custom_level")
        {
            __state = State.ALREADY_LOADED;
            return true;
        }

        // A new custom hub, or the old one being reloaded.

        // I mean, it's not loaded *yet*.
        // This encourages the game to reload a couple of things.
        World.hubLoaded = false;
        CustomHub.inCustomHub = true;
        World.currentLevelName = "hub";
        Hub.puzzleData.Clear();
        CustomHub.paths.Clear();
        string dir = Path.GetDirectoryName(World.lastLoadedCustomLevelPath);
        CustomHub.customHubDir = dir;

        var music = FMODSquare.AreaNameToFMODIndex = new Dictionary<string, int>();
        music["Menu_Paused"] = 20;
        music["Menu_Credits"] = 21;
        foreach (string[] tokens in CustomHub.GetTokens("area_data"))
        {
            string name = tokens[0];
            music[name] = int.Parse(tokens[1]);
        }

        var puzzleNames = new List<string>();
        foreach (string[] tokens in CustomHub.GetTokens("puzzle_data"))
        {
            Hub.puzzleData[tokens[0]] = new Hub.PuzzleData
            {
                hard = int.Parse(tokens[1]),
                eyesJump = Hub.ParseBool(tokens[2]),
                referenceName = tokens[3],
                musicArea = -1
            };
            puzzleNames.Add(tokens[0]);
        }
        Hub.puzzleNamesSorted = puzzleNames.ToArray();

        foreach (string file in CustomHub.GetFiles("*.txt"))
        {
            CustomHub.paths[Path.GetFileNameWithoutExtension(file)] = file;
        }
        CustomHub.paths["save"] = Path.Combine(dir, "save.txt");
        SaveFile.Load();
        CustomHub.LoadImages();

        Hub.puzzleLineRefs.Clear();
        foreach (string key in Hub.puzzleData.Keys)
        {
            Hub.puzzleLineRefs[key] = new Hub.PuzzleLineRef
            {
                fromMe = new List<Hub.PuzzleLine>(),
                toMe = new List<Hub.PuzzleLine>()
            };
        }
        var puzzleLines = new List<Hub.PuzzleLine>();
        foreach (string[] tokens in CustomHub.GetTokens("puzzle_lines"))
        {
            var puzzleLine = new Hub.PuzzleLine
            {
                from = tokens[0],
                to = tokens[1],
                immediate = Hub.ParseBool(tokens[2])
            };
            puzzleLines.Add(puzzleLine);
            Hub.puzzleLineRefs[puzzleLine.from].fromMe.Add(puzzleLine);
            Hub.puzzleLineRefs[puzzleLine.to].toMe.Add(puzzleLine);
        }
        Hub.puzzleLines = puzzleLines.ToArray();
        SaveFile.Load();

        var walking = new List<string>();
        foreach (var entry in Hub.puzzleData)
        {
            if (entry.Value.thumbnail == null)
            {
                walking.Add(entry.Key);
            }
        }
        if (walking.Count != 0)
        {
            Hub.puzzleNamesForWalking = walking.ToArray();
            World.Screenshotting = true;
            // devShrinkScreenDown does several things, most importantly
            // not rendering the player
            Controls.devShrinkScreenDown = true;
            CustomHub.walking = true;
            typeof(PauseMenu)
                .GetMethod("Resume", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, null);
            World.StartWalkingLevels();
            __state = State.WALKING;
            return false;
        }
        __state = State.LOADING;
        return true;
    }
    
    public static void Postfix(ref State __state, bool ___randomize, int ___customLevelPalette)
    {
        World.doJumpOut = false;
        if (__state == State.ALREADY_LOADED && CustomHub.inCustomHub
            && World.lastLoadedCustomLevelPath == CustomHub.paths["hub"])
        {
            // Fix palette, which was set to the one in puzzledata
            if (!___randomize)
            {
                if (___customLevelPalette >= 0)
                {
                    LoadLevel.ApplyPalette(___customLevelPalette);
                }
                else
                {
                    Draw.Palette = 0;
                    foreach (var block in World.blocks)
                    {
                        block.hue = block.startHue;
                        block.sat = block.startSat;
                        block.val = block.startVal;
                    }
                    LoadLevel.ComputeBorderColors();
                }
            }
            // ...and the music, which was also set to the one in puzzledata.
            // It is not possible to play *no* music.
            if (World.currentLevelName != "hub")
            {
                Hub.puzzleData[World.currentLevelName].musicArea = World.customLevelMusic;
            }

            if (World.InHub()) LastMinuteHubFixes();
        }
        else if (__state == State.LOADING)
        {
            LastMinuteHubFixes();
        }
    }
}

[HarmonyPatch(typeof(LoadLevel), "DoHubModifications")]
public class LoadLevel_DoHubModifications
{
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilprocessor)
    {
        var instrs = instructions.ToList();
        var unlock_by_default_label = ilprocessor.DefineLabel();
        
        for (int i = 0; i < instrs.Count; i++)
        {
            var instr = instrs[i];
            
            // Every time the method sets level.hubShortcutWon on a level,
            // we first check if level is null, and if so skip the access.
            // This fixes a crash involving hub areas with certain names
            // not being present.
            if (instr.StoresField(AccessTools.Field(typeof(Level), "hubShortcutWon")))
            {
                // The instruction at ifTrue pushed the value to be assigned.
                // Both branch instructions pop the stack.
                var ifTrue = instrs[i - 1];
                var ifTrueLabel = ilprocessor.DefineLabel();
                ifTrue.labels.Add(ifTrueLabel);
                
                var ifFalse = instrs[i + 1];
                var ifFalseLabel = ilprocessor.DefineLabel();
                ifFalse.labels.Add(ifFalseLabel);
                
                instrs.Insert(i - 1, new CodeInstruction(OpCodes.Dup));
                instrs.Insert(i, new CodeInstruction(OpCodes.Brtrue_S, ifTrueLabel));
                instrs.Insert(i + 1, new CodeInstruction(OpCodes.Brfalse_S, ifFalseLabel));
                i += 4;
            }
            // If a hub level is not in another hub level, we forgo the usual rules
            // about levels that aren't connected to anything being unlocked.
            // In game, this only occurs in the intro, which the game fixes by checking
            // area name.
            else if (instr.Calls(AccessTools.Method(typeof(Level), "GetExitBlock")))
            {
                instrs[i + 4].operand = unlock_by_default_label;
            }
            else if (instr.LoadsField(AccessTools.Field(typeof(Floor), "Hard")))
            {
                instrs[i - 1].labels.Add(unlock_by_default_label);
                break;
            }
        }
        return instrs.AsEnumerable();
    }
}