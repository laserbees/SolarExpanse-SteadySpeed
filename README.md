# Steady Speed (Solar Expanse mod)

A [BepInEx 5](https://github.com/BepInEx/BepInEx) mod for **Solar Expanse** that keeps the game
speed where you set it. The game pauses when you complete a quest or get a notification, and when
you resume with the spacebar it often comes back at a different speed. With this mod it only
changes speed when you ask it to.

## Features

**Spacebar resumes at your speed**
- The game has two separate pause systems. The spacebar saves your speed and restores it, but
  automatic pauses (completed quests and subquests, notifications, popups, trivia windows) don't
  save anything, so the next spacebar press resumes at an old saved speed or at speed 1.
- With this mod, the spacebar always resumes at the last speed the game was running at.

**No automatic drops to speed 1**
- The game switches to speed 1 on its own in a few situations, such as when a contract centres the
  camera on your mission partway through its flight, or when an impact happens on the object
  you're watching. These are blocked; speed 1 is only set when you press the 1 key or click its
  button.

**Unchanged:** the automatic pauses themselves, the speed keys and buttons, the Plan Mission
window's own pause, the launch countdown, and the speed saved in your save game.

## Installing

1. Install **BepInEx 5** (64-bit Windows, e.g. `BepInEx_win_x64_5.4.23.x.zip` from the
   [BepInEx releases](https://github.com/BepInEx/BepInEx/releases)): extract it into the Solar
   Expanse game folder, so `BepInEx` sits next to `Solar Expanse.exe`, then start the game once
   and close it.
2. Download `SteadySpeed-<version>.zip` from this repository's
   [Releases](../../releases) and extract it into the same game folder. The DLL ends up at
   `BepInEx\plugins\SteadySpeed\SteadySpeed.dll`.
3. Start the game.

To uninstall, delete the `BepInEx\plugins\SteadySpeed` folder.

There are no settings.

## Building from source

Requires the .NET SDK (any recent version, e.g. 8 or 9) and the game with BepInEx installed, since
the project references the game's own DLLs.

```
dotnet build -c Release
```

The build copies `SteadySpeed.dll` into `<game>\BepInEx\plugins\SteadySpeed\`. If the game
isn't at `F:\SteamLibrary\steamapps\common\Solar Expanse`, edit `GamePath` in
`SteadySpeed.csproj` or pass it in:

```
dotnet build -c Release -p:GamePath="D:\Path\To\Solar Expanse"
```

Close the game before building; Windows locks the DLL while the game is running.

### Project layout

- `Plugin.cs`: mod entry point, Harmony setup
- `SpeedPatches.cs`: remembers the running speed, makes the spacebar resume at it, and blocks
  automatic changes to speed 1

See [HOW_IT_WORKS.md](HOW_IT_WORKS.md) for how each change works and which parts of the game it hooks.

## License

[MIT](LICENSE)
