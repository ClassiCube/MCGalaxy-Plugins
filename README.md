# MCGalaxy-Plugins
Some custom commands and plugins for MCGalaxy.

### Usage

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
| Name | Description |
| ------------- | -----|
| **CmdBiggestTables** | Lists the database tables with most rows. Unlikely to be useful except for debugging.
| **CmdServerMap** | Copies a map from another server located on the same computer.
| **CmdGBInsert** | Reorders a global block's position in the inventory.
| **CmdImpersonate** | Fake a chat message as if it came from another player.
| **CmdMapHack** | Allows you to bypass -hax on your own `/os` maps. (or ranks >= the extra permission)
| **CmdMapsBy** | Lists all maps created by the given user.
| **CmdPruneDB** | Removes a player's entries from a level's BlockDB. **Use at your own risk.**
| **CmdSetSoftwareName** | Sets the name of the software shown in `/sinfo` and in the server list.
| **CmdTempBlock** | Creates a client-side block. (only the given player sees the block change)
| **CmdZoneInfo** | Shows some information of a specified zone as well as visually highlighting it in-game.

### Plugins list
| Name | Description |
| ------------- | -----|
| **BinVoxImport** | Imports [binvox](http://www.patrickmin.com/binvox/) files from `extra/import` folder. BinBox is useful for voxelising .obj models.
| **FootballInstruction** | Adds a bot AI instruction which allows kicking a bot around like a football.
| **GoodlyEffects** | Adds support for CustomParticles CPE. Documentation can be found [here](documentation/GoodlyEffects.md).
| **KickJini** | Prevents people using Jini client from logging in.
| **KickNoCC** | Only allows people using the ClassiCube client in Enhanced mode to login.
| **LockedModel** | Forces players to only use specified model(s) on a map.
| **LockedReach** | Restricts reach distance of players on a map.
| **MagicaVoxelImport** | Imports [MagicaVoxel](https://ephtracy.github.io/) files from `extra/import` folder.
| **Marry** | Allows you to show as married to another player in `/info`.
| **no_tp_zs** | Prevents using `/tp` on maps that start with "zs".
| **passjoin** | Force players to enter a pre-defined password before they can play and talk. Change the password before compiling! This plugin is MUTUALLY EXCLUSIVE with admin verification.
| **physicsexample** | Example plugin for custom block physics. Makes block 103 fall up (instantly on physics 1, gradually on higher levels).
| **Rainbow** | Makes the &r custom colour constantly change in rainbow pattern (you must define &r first)
| **Reward** | Allows using `/Reward` in Message Blocks, which will give the player money when clicked on.
| **SchematicImporter** | Imports [Schematic](https://minecraft.fandom.com/wiki/Schematic_file_format) files from `extra/import` folder.
| **TeamChat** | Allows using *=message* as shortcut for */team message*.
| **tp_control** | Disables teleporting within -hax maps and adds +tp and -tp flags to MOTD.
| **MicrosoftAuthentication** | Allows users to authenticate using the online-mode protocol, if verify-names fails or no mppass is provided.


## Other plugins available
https://github.com/NotAwesome2/MCGalaxy-CustomModels

https://github.com/ddinan/ClassiCube-Stuff

https://github.com/NotAwesome2/Plugins

https://github.com/NotAwesome2/Not-Awesome-Script
