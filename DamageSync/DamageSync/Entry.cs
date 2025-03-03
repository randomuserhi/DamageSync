using API;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace DamageSync {
    public static class Module {
        public const string GUID = "randomuserhi.DamageSync";
        public const string Name = "DamageSync";
        public const string Version = "0.0.6";
    }

    [BepInPlugin(Module.GUID, Module.Name, Module.Version)]
    internal class Entry : BasePlugin {
        public override void Load() {
            APILogger.Debug(Module.Name, "Loaded DamageSync");
            harmony = new Harmony(Module.GUID);
            harmony.PatchAll();

            APILogger.Debug(Module.Name, "Debug is " + (ConfigManager.Debug ? "Enabled" : "Disabled"));
        }

        private Harmony harmony;
    }
}