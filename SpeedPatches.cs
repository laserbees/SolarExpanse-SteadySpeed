using HarmonyLib;
using Manager;

namespace SteadySpeed
{
    // The game has two pause systems that don't talk to each other. Spacebar (PauseGame2 /
    // UnPauseGame2) saves the speed and restores it. Automatic pauses (quest popups, notifications,
    // trivia...) call PauseGame(), which saves nothing, so the next spacebar resumes at a stale
    // saved speed or falls back to speed 1. This mod remembers the last running speed and makes
    // spacebar resume at it.
    internal static class SpeedMemory
    {
        // Last non-zero speed the game actually ran at; 0 until the first one is seen.
        internal static float LastRunningSpeed;

        // True while InputManager is handling the keyboard, i.e. the player pressed a speed key.
        internal static bool InKeyboardInput;
    }

    [HarmonyPatch(typeof(TimeController), nameof(TimeController.SetTimescale))]
    internal static class RecordSpeedPatch
    {
        private static void Postfix(TimeController __instance)
        {
            if (__instance.CurrentTimeScale != 0f)
            {
                SpeedMemory.LastRunningSpeed = __instance.CurrentTimeScale;
            }
        }
    }

    [HarmonyPatch(typeof(TimeController), nameof(TimeController.UnPauseGame2))]
    internal static class ResumeAtLastSpeedPatch
    {
        private static readonly AccessTools.FieldRef<TimeController, float?> BeforePause =
            AccessTools.FieldRefAccess<TimeController, float?>("beforePauseTimeScale2");

        // Overwrite the game's saved speed so the original method resumes at ours.
        private static void Prefix(TimeController __instance)
        {
            if (SpeedMemory.LastRunningSpeed != 0f)
            {
                BeforePause(__instance) = SpeedMemory.LastRunningSpeed;
            }
        }
    }

    [HarmonyPatch(typeof(InputManager), "UpdateKeyboard")]
    internal static class KeyboardInputScopePatch
    {
        private static void Prefix()
        {
            SpeedMemory.InKeyboardInput = true;
        }

        // A finalizer runs even if UpdateKeyboard throws, so the flag can't get stuck on.
        private static void Finalizer()
        {
            SpeedMemory.InKeyboardInput = false;
        }
    }

    // The game drops to speed 1 on its own when a contract centres the camera on a mission and
    // when an impact happens on the watched object. Only the "1" key may call SetTime1.
    [HarmonyPatch(typeof(TimeController), nameof(TimeController.SetTime1))]
    internal static class BlockAutomaticSpeedOnePatch
    {
        private static bool Prefix()
        {
            if (SpeedMemory.InKeyboardInput)
            {
                return true;
            }
            Plugin.Log.LogInfo("Blocked an automatic change to speed 1.");
            return false;
        }
    }
}
