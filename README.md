# MCGalaxy-Plugins
This repository contains plugins and commands for MCGalaxy submitted by members of the community. You are free to use and modify these plugins as you wish for your own servers.

### Adding them to your server

#### Commands
- Place the .cs file in */extra/commands/source*
- `/compile [command name]`
- `/cmdload [command name]`
- Commands automatically load on server start (as of MCGalaxy 1.9.4.2)

#### Plugins
- Place the .cs file in */plugins*
- `/pcompile [plugin name]`
- `/pload [plugin name]`
- Plugins automatically load on server start

### Commands list

#### Features
These commands add new features to the server.

| Name | Description |
| ------------- | -----|
| **CmdAnnounce** | Shows text in the middle of the screen.
| **CmdBestMaps** | Teleports to a randomly curated map. Often used for displaying the most impressive maps on the server.
| **CmdBottom** | /top, but sorts in ascending order.
| **CmdChevify** | Turns your map into a funky ~~abomination~~ masterpiece.
| **CmdExportDat** | Saves a level as a .dat file.
| **CmdFakeGive** | A troll command to falsely tell players they have received money.
| **CmdFileManager** | Allows players to modify files without file access. **Use at your own risk.**
| **CmdFixTP** | Replaces all map textures with a newly-specified one. Useful for mass-fixing levels with broken textures.
| **CmdImpersonate** | Fake a chat message as if it came from another player.
| **CmdImportICraft** | Imports an ICraft level.
| **CmdImportPRSchematic** | Imports a .schematic level using the Puissant Royale blocks.
| **CmdListRankLevels** | Shows a list of levels with perbuild/pervisit matching a specified rank. 
| **CmdMapHack** | Allows you to bypass -hax on your own `/os` maps. (or ranks >= the extra permission)
| **CmdMapsBy** | Lists all maps created by the given user.
| **CmdPreset** | Easier access for /os env preset that also allows people with build access to change.	|  **CmdQuote** | Add and view quotes from players.
| **CmdReward** | Used primarily in message blocks to give rewards to players.
| **CmdTempBlock** | Creates a client-side block. (only the given player sees the block change)
| **CmdZoneInfo** | Shows some information of a specified zone as well as visually highlighting it in-game.

#### Utilities
These commands are tools designed to help staff, improve server quality, and automate tasks.

| Name | Description |
| ------------- | -----|
| **CmdAdventure** | Easily toggle /map buildable and /map deletable in one command.
| **CmdBiggestTables** | Lists database tables with the most rows. Unlikely to be useful except for debugging.
| **CmdCopyServerMap** | Copies a map from another server located on the same computer.
| **CmdFileManager** | Allows players to modify files without file access. Useful for trusted admins needing remote file access. **Please only allow this command to be used by players you trust as it has huge security risks.**
| **CmdGBInsert** | Reorders a global block's position in the inventory.
| **CmdLevelMemEstimate** | Estimates how much memory is being used by all currently loaded levels.
| **CmdMoveEverything** | Moves all bots/MBs/portals in a map. Useful for moving builds.
| **CmdPruneDB** | Removes a player's entries from a level's BlockDB. **Use at your own risk.**
| **CmdRemove** | Removes a player from the database. **Use at your own risk.**
| **CmdRemoveTable** | Removes a table from the database. **Use at your own risk.**
| **CmdSetSoftwareName** | Sets the name of the software shown in `/sinfo` and the server list.

### Plugins list

#### Examples
These plugins are designed to show how to achieve behaviour for your own plugins. They are not designed to be used as is.

| Name | Description |
| ------------- | -----|
| **CommandsInPluginExample** | Example plugin showing how to add custom commands.
| **CustomEventExample** | Example plugins showing how to send and receive custom events. Useful for cross-plugin communication.
| **CustomStatsExample** | Example plugin showing how to add custom /top stats.
| **CustomWorldGenExample** | Example plugin showing how to add custom /newlvl themes.
| **Example.cs** | Example plugin using C# *(the norm)*.
| **Example.vb** | Example plugin using Visual Basic.
| **ExampleStoreItem** | Example plugin showing how to add items to /store.
| **GamemodeTemplate** | Template plugin showing how to make your own gamemodes.
| **physicsexample** | Example plugin showing how to add custom block physics. Makes block 103 fall up (instantly on physics 1, gradually on higher levels).
| **playerclickexample** | Example plugin showing how to detect player clicks.

#### Features
These plugins add new features to the server.

| Name | Description |
| ------------- | -----|
| **AutoIncreasePlayerCount** | Increases the server's max player count by 1 whenever a player joins.
| **BinVoxImport** | Imports [binvox](http://www.patrickmin.com/binvox/) files from `extra/import` folder. BinBox is useful for voxelising .obj models.
| **Compass** | Adds a compass to the player's HUD.
| **Crouching** | Adds an option to crouch by pressing left shift (does not prevent falling off blocks).
| **CustomSoftware** | Allows changing the software name both in-game and in the launcher.
| **CustomTabList** | Allows changing the format of the tab list.
| **DailyBonus** | Gives people money once per day when they login OR type /daily OR have been on the server for 30+ minutes.
| **DayNightCycle** | Adds a day-night cycle into the game. Does NOT modify sun or shadow values due to current limitations with how the client handles chunk rendering.
| **DiscordActionLog** | Sends a message to a Discord channel whenever a player is punished.
| **DiscordChannelName** | Updates a Discord channel name to include the server's online player count.
| **DiscordNotify** | Pings a role on Discord whenever there are x players online.
| **DiscordVerify** | Allow players to link their Discord accounts to their in-game accounts.
| **FavouriteMap** | Allows players to set their favourite map which is shown in /whois.
| **FootballInstruction** | Adds a bot AI instruction which allows kicking a bot around like a football.
| **GoodlyEffects** | Adds support for CustomParticles CPE. Documentation can be found [here](documentation/GoodlyEffects.md). This plugin requires [_extralevelprops](https://github.com/NotAwesome2/Plugins?tab=readme-ov-file#_extralevelpropscs) plugin to be loaded first.
| **HighFiveConsent** | Replaces the default /high5 behaviour and requires the recipient of a high five to give consent before broadcasting to the rest of the server.
| **HoldBlocks** | Shows the block players are holding in their player's model for other players to see.
| **JScriptCompiler** | Compiles a plugin written in Microsoft JScript (NOT JavaScript).
| **HoldBlocks** | Shows the block players are holding in their player's model for other players to see.
| **LocationJoin** | Announces the country players connect from in chat. Note: Slight invasion of privacy.
| **Lottery** | Players can create and enter lotteries for a chance to win server money.
| **MagicaVoxelImport** | Imports [MagicaVoxel](https://ephtracy.github.io/) files from `extra/import` folder.
| **Marriage** | Allows players to get married and have their spouse shown in /whois.
| **MatrixRelay** | Creates a bot relay for [Matrix](matrix.org).
| **MobAI** | Adds custom bot AI instructions. Warning: Experimental.
| **MoveSelection** | Advanced build movement system using hotkeys and rotational movement.
| **Parties** | Create/join parties and talk privately with specific players (temporary /team).
| **passjoin** | Force players to enter a pre-defined password before they can play and talk. Change the password before compiling! This plugin is MUTUALLY EXCLUSIVE with admin verification.
| **Rainbow** | Makes the &r custom colour constantly change in rainbow pattern (you must define &r first)
| **Reward** | Allows using `/Reward` in Message Blocks, which will give the player money when clicked on.
| **SchematicImporter** | Imports [Schematic](https://minecraft.fandom.com/wiki/Schematic_file_format) files from `extra/import` folder.
| **SneakAI** | Adds an AI that instantly kills players who get too close to the bot.
| **StaffEligibility** | Provides $ tokens for requirements needing to be met before applying for staff.
| **Stopwatch** | Adds a stopwatch into the game.
| **TeamChat** | Allows using *=message* as shortcut for */team message*.
| **TimeAFK** | Shows the amount of time players have been AFK for.
| **TopReach** | Adds a new /top stat that shows players with the highest reach distances.
| **TopTeams** | Adds a new /top stat that shows the teams with the most players.
| **VenkLib** | Essential commands every server should have.
| **VenkSurvival** | Adds survival options such as PvP, drowning, fall damage, hunger, mining and more. Requires VenkLib plugin. Note: Extremely old.
| **VisualBasicCompiler** | Allows compilation of plugins written in Visual Basic.
| **XP** | Adds an XP leveling system.

#### Utilities
These plugins are tools designed to help staff, improve server quality, and automate tasks.

| Name | Description |
| ------------- | -----|
| **AntiVPN** | Prevents players from using VPNs to join the server. Note: Known to have issues with rate limiting.
| **BugWebhook** | Send a message to a Discord channel whenever an error occurs.
| **DisableCommands** | Disables all non-essential commands.
| **KickJini** | Prevents players using Jini client from joining the server.
| **KickNoCC** | Only allows players using the ClassiCube client in Enhanced mode to join the server.
| **LockedModel** | Forces players to only use specified model(s) on a map.
| **LockedReach** | Restricts reach distance of players on a map.
| **NickBlocker** | Prevents using /whonick in a level which has -nicks in its MOTD.
| **no_tp_zs** | Prevents using `/tp` on maps that start with "zs".
| **PreventOPBlocks** | Prevents being able to delete OP blocks.
| **RelayRoute** | Relays messages between routes. For example, Discord channel -> IRC channel.
| **SessionPunishments** | Forces players to be online for the entire duration of their mute/freeze.
| **tp_control** | Disables teleporting within -hax maps and adds +tp and -tp flags to MOTD.

## Other commands and plugins available

https://github.com/123DMWM/MCGalaxy-Stuff

https://github.com/AllergenStudios/MCGalaxy-Plugins

https://github.com/brycemthompson/McClassic-ClassiCube-Plugins-Cmds

https://github.com/commaster101/classicube-thingys

https://github.com/dflat2/MCGalaxyPlugins

https://github.com/LawrenceBorst/Upsurge

https://github.com/morgana-x/Classicube-Corpses
https://github.com/morgana-x/Classicube-Doors
https://github.com/morgana-x/classicube-herobrine
https://github.com/morgana-x/Classicube-SpawnEggs

https://github.com/NotAwesome2/Commands
https://github.com/NotAwesome2/MCGalaxy-CustomModels
https://github.com/NotAwesome2/Not-Awesome-Script
https://github.com/NotAwesome2/Plugins

https://github.com/opapinguin/FPS-Plugin
https://github.com/opapinguin/MCGalaxy-plugins

https://github.com/Siriusmart/ccBancho
https://github.com/Siriusmart/ccBancho_old

https://github.com/ccatgirl/LuaS
