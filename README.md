The APWorld definition can be found here: https://github.com/lonesurv1vor/Archipelago/tree/main/worlds/cavesofqud

# Features
* Locations are checked by gaining XP and on completing some quests. The amount of checks per level can be configured as well as the max level to include as location.
* Level up gains (HP, mutation points, skill points, attribute points / bonus) are received as items
* Goal can be set to the completion of "Decoding the signal" or "More than a willing spirit"
* A couple of random filler items and traps

# Known problems
* There is no balance at all, the item placement is completely random. Since there are no hard blockers in the game that prevent one from reaching the goal, it should be mostly fine for now. Another players important early item might be placed late in Caves of Qud though, so some !getitem might be needed.
* Rapid mutation advancement seems to be kind of bugged and not giving 3 extra points but only 1 (at least that happened in my test session)
* Certain gained stats might not be increased correctly on gaining other stats (e.g. gaining an intelligence boost might not increase the total skill points)
* Game might ask for a reconnect when restoring an earlier game state (or not, in that case it is currently advised to save and quit and reload the game to get back in sync)
* All traps are reapplied when loading an earlier game state. This might mess things up.

# Installation and creating a game
* Download the mod zip file, the APWorld and the yaml from the release page.
* Extract the mod to your caves of qud Mods folder found under `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods` (on Windows). The mod shoud have its own directory under `Mods`, e.g. `Mods/Archipelago`
* Install the APWorld file as usual and either generate the template files using the Archipelago launcher or use the provided yaml to generate your game
* Start Caves of Qud and enable the mod under "Mods", then start a new game.
* It's recommended to 1) play roleplay mode, so you don't need to start over when dying, and 2) choose a mutant. And only Joppa start is supported (others work as well but the Joppa related quest locations are unreachable then).
* When creating a new character, the connection is opened after the character has been created. If the connection fails, the game will hang. This does not happen when loading an already saved game later on.