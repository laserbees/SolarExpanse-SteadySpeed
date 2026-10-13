# How it works

Technical notes on what this mod changes in Solar Expanse, which parts of the game it hooks, and
why each change is done the way it is. The [README](README.md) covers what the mod does from a
player's point of view; this file is for anyone reading or changing the code.

All game code is patched with [Harmony](https://harmony.pardeike.net/) (prefixes, postfixes,
finalizers and field accessors). Nothing in the game's assets or scenes is modified.

- [How the game handles speed](#how-the-game-handles-speed)
- [Spacebar resumes at your speed](#spacebar-resumes-at-your-speed)
- [Blocking automatic drops to speed 1](#blocking-automatic-drops-to-speed-1)
- [What's intentionally left alone](#whats-intentionally-left-alone)
- [Robustness](#robustness)
- [Game code touched](#game-code-touched)

---

## How the game handles speed

Everything goes through `Manager.TimeController`. The current speed is the private field
`_currentTimeScale` (0 = paused), and every change goes through
`SetTimescale(float timescale, bool force, bool silent)`. The speed keys and buttons map to
`Economic.baseTimeScale` multiplied by `multiTime2` … `multiTime6`.

The game has several separate pause/resume pairs, each with its own saved speed:

| Pair | Saved speed | Used by |
|---|---|---|
| `PauseGame2` / `UnPauseGame2` | `beforePauseTimeScale2` | Spacebar (`InputManager.UpdateKeyboard`) |
| `PauseGame3` / `UnPauseGame3` | `beforePauseTimeScale2` (shared!) | Bug-report window (`ErrorReporter`) |
| `PauseGamePlanMission` / `UnPauseGamePlanMission` | `beforePauseTimeScale` | Plan Mission window |
| `PauseTutorial` / `PlayTutorial` | `currentTimeScaleCopy` | Tutorial |
| `PauseGame()` | **nothing** | Notifications (when the pause toggle is on, e.g. completed quests), yes/no popups, trivia windows, bankruptcy warning, end-game impact |

## Spacebar resumes at your speed

**File:** `SpeedPatches.cs`

### The problem

`PauseGame()` sets the speed to 0 without saving the speed it replaced. Pressing the spacebar
while paused calls `UnPauseGame2`, which resumes at `beforePauseTimeScale2`. After an automatic
pause, that value is either:

- `null`, so the game falls back to speed 1 (`baseTimeScale * multiTime2`), or
- left over from the last time the player paused with the spacebar, which may have been a
  different speed.

So the speed after resuming looks random to the player.

### The approach

1. A **postfix on `SetTimescale`** records `CurrentTimeScale` in `SpeedMemory.LastRunningSpeed`
   whenever it isn't 0. Pauses are never recorded, so this always holds the speed the game was
   running at just before it paused.
2. A **prefix on `UnPauseGame2`** writes that speed into the private `beforePauseTimeScale2`
   field, through a Harmony `FieldRef`. The original method then runs unchanged and resumes at
   it.

Reading the speed after the call, rather than the `timescale` argument, means calls that the game
refused (for example while the Plan Mission window is open or during the launch countdown) aren't
recorded.

`TimeController.TimeStep` briefly calls `SetTimescale(0, force, silent)` around scheduled
actions and then restores `_currentTimeScale` directly; the postfix sees 0 at that moment and
ignores it, and records the restored speed on the matching `SetTimescale` call afterwards.

If nothing has been recorded yet (for example right after loading a save that was paused), the
prefix does nothing and the game's own fallback applies.

## Blocking automatic drops to speed 1

**File:** `SpeedPatches.cs`

### The problem

`TimeController.SetTime1()` sets speed 1. Besides the "1" key, the game calls it on its own:

| Caller | When |
|---|---|
| `ContractManager` | A contract objective centres the camera on the player's mission once it's more than 25% of the way to its target |
| `ObjectInfo` | An asteroid impact adds its resources to the object the camera is watching |
| `SpaceraftCustom3dview` | A spacecraft impacts the object the camera is watching |

### The approach

1. A **prefix and finalizer on `InputManager.UpdateKeyboard`** set `SpeedMemory.InKeyboardInput`
   while the game is reading the keyboard. A finalizer is used rather than a postfix so the flag
   is always cleared, even if the method throws.
2. A **prefix on `SetTime1`** returns `false` (skips the original) unless that flag is set, and
   logs `Blocked an automatic change to speed 1.`

The speed buttons in `TimeControlPanel` call `SetTimescale` directly rather than `SetTime1`, so
they aren't affected. Everything else the callers do (such as moving the camera) still happens.

## What's intentionally left alone

- **The automatic pauses themselves.** The player is happy for quests and notifications to pause
  the game; only the resume speed was the problem.
- **The Plan Mission window** pauses and restores its own speed correctly.
- **Menu screens** (`BaseScreen.Visible`) pause and restore their own speed correctly.
- **The launch countdown** (`UIManager.CountDown`) sets speed 1 with `force: true` after the
  first launch. It only happens once per game.
- **Loading a save** restores the speed stored in the save. The postfix records it like any other
  speed.

## Robustness

**File:** `Plugin.cs`

- **Patches are applied one class at a time.** If one no longer matches the game (a renamed or
  removed method), it's logged as an error and the rest still apply. The log line
  `Loaded, applied 4 patches.` shows they all loaded.
- Each feature works on its own: if the `UnPauseGame2` patch fails, speed 1 is still blocked, and
  the other way round.

---

## Game code touched

A quick reference of every game type and member the mod depends on. If a game update changes one
of these, the matching feature is the one to check.

### Patched methods

| Type | Member | Patch | Feature |
|---|---|---|---|
| `Manager.TimeController` | `SetTimescale` | Postfix | Remember speed |
| `Manager.TimeController` | `UnPauseGame2` | Prefix | Spacebar resume |
| `Manager.TimeController` | `SetTime1` | Prefix (skip) | Block speed 1 |
| `Manager.InputManager` | `UpdateKeyboard` | Prefix/Finalizer | Block speed 1 |

### Private fields written

| Type | Field | Used for |
|---|---|---|
| `TimeController` | `beforePauseTimeScale2` (`float?`) | The speed the spacebar resumes at |

### Public members used

`TimeController.CurrentTimeScale`.
