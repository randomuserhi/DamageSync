using BepInEx.Configuration;
using BepInEx;

namespace DamageSync
{
    public static class ConfigManager
    {
        static ConfigManager()
        {
            string text = Path.Combine(Paths.ConfigPath, "DamageSync.cfg");
            ConfigFile configFile = new ConfigFile(text, true);

            debug = configFile.Bind(
                "Debug",
                "enable",
                false,
                "Enables debug messages when true.");
        }

        public static bool Debug
        {
            get { return debug.Value; }
        }

        private static ConfigEntry<bool> debug;
    }
}