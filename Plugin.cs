using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SteadySpeed
{
    // Mod identity. BepInEx uses the GUID to tell mods apart, so never change it after release.
    public static class PluginInfo
    {
        public const string Guid = "laserbees.solarexpanse.steadyspeed";
        public const string Name = "Steady Speed";
        public const string Version = "1.0.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        // Static so any class in the mod can write to the BepInEx log via Plugin.Log.
        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Applies every [HarmonyPatch] class in this mod, one at a time, so a patch that no
            // longer matches the game (e.g. after a game update) is logged instead of stopping the rest.
            _harmony = new Harmony(PluginInfo.Guid);
            int applied = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                {
                    continue;
                }
                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                    applied++;
                }
                catch (Exception e)
                {
                    Log.LogError("Patch " + type.Name + " failed: " + e);
                }
            }
            Log.LogInfo("Loaded, applied " + applied + " patches.");
        }
    }
}
