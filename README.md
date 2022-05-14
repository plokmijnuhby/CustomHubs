# CustomHubs
This is a mod for Patrick's Parabox, to allow support for custom hub areas to be created. Using it is a little complicated, so feel free to DM me if I haven't made something clear enough here.
## Installation
To install this mod, first locate the parabox game files. These are usually found at "C:\Program Files (x86)\Steam\steamapps\common\Patrick's Parabox". Go to [the MonoMod releases page](https://github.com/MonoMod/MonoMod/releases/) and download the version ending in "net452.zip". Unzip it and copy the entire contents into the parabox game files, in the folder "Patrick's Parabox\Patrick's Parabox_Data\Managed". Download the file "Assembly-CSharp.CustomHubs.mm.dll" from [the releases page of this repository](https://github.com/plokmijnuhby/CustomHubs/releases), and move it into the same folder.

If you are on windows, open up a command prompt, navigate to that folder, and type in
```
MonoMod.exe Assembly-CSharp.dll
```
On Linux, you can instead type this (you may need to install mono first):
```
mono MonoMod.exe Assembly-CSharp.dll
```
This should generate a file called "MONOMODDED_Assembly-CSharp.dll" in that folder. Delete the file "Assembly-CSharp.dll" (or move it outside of the game files) and rename the MONOMODDED file to Assembly-CSharp.dll.

You can now load custom hubs, by moving them into your custom level folder and loading them as normal.

## Making a custom hub
One point before I begin: I have generally taken the approach of not patching bugs that were in the main game to start with. This can lead to janky interactions if you do anything unusual.

If you didn't understand any of this explanation, a complete example called test_hub is given in this repository.
### The hub file
The most important file (and technically the only required file) is the map file for the hub, which should be called hub.txt. The format is mostly the same as a custom level, but with a few changes. The hub file allows the use of portals, which have the following syntax:
```
Floor x y Portal puzzle_name
```
References have a slightly different syntax to normal, since they have an additional argument at the end, giving the name of the area they are pointing to. Obviously, every area should have at least one reference to it, otherwise the game will not know which area it is.

In addition, walls also have an extra argument, with the following effects:
- `_`: a regular wall.
- the name of a puzzle: indicates this wall will unlock when enough puzzles are complete, and it should be connected to the named puzzle.
- some other values are possible, but I didn't bother testing them to make sure they work properly. I wouldn't advise using them.

The required number of puzzles to unlock a wall connected to a puzzle is the number of main line puzzles in the area. (In the main game, this is also true for most areas, but Wall and Swap are hardcoded to have a different number of puzzles required.)

Finally, a playerButton will launch the credits sequence, if unlocked.

There are a few more important points to bear in mind. Firstly, for unlockable walls to work properly, the wall should be on one of the four spaces next to a reference or block. Secondly, the player should not start next to a portal, since this interferes with the way the portal is exited. Finally, when reloading a save, the player will be placed on the center of the second row from the top in the current area, so this spot should be kept clear in every area.

### Other files
All other files can be placed in subdirectories if you choose, which you may find useful for organising levels. In addition, most of the required data can be split across multiple files and placed in different directories if needed - two files named "area_data.txt" in different subdirectories will be treated as if they were appended.

Each puzzle should be given a level file called "{name_of_puzzle}.txt". This can be any normal custom level, with one exception - custom_level_music must be set, to something other than -1. There should also be an image file, eg "{name_of_puzzle}.png", indicating what should be displayed on the portal. The image can be png, jpeg, or any other format that unity understands. If you don't provide an image, one will be generated for you.

The file "area_data.txt" should have an entry for every area of the hub. Each line has the format:
```
area_name music
```
where `music` indicates the id of the music for that area (again, this cannot be -1).
The file "puzzle_data.txt" is more complex. There should be a line for every puzzle, with the format:
```
puzzle_name hard eyesJump referenceName
```
`hard` should be 0 for main-line levels, 1 for challenge levels, and 2 for special levels. `eyesJump` indicates whether the player should possess the portal rather than jumping into it. referenceName indicates the number of the level.

Another file, "puzzle_lines.txt", indicates which puzzles are connected to each other. The format is:
```
from to immediate
```
if the line goes from the puzzle called `from` to the puzzle `to`. `immediate` indicates whether completing `from` immediately takes you to `to`.

The only remaining file is "credits.txt", an optional file indicating what credits will play if you complete the hub.