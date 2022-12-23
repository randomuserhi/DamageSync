﻿using API;
using HarmonyLib;

namespace DamageSync.Patches
{
    [HarmonyPatch]
    internal class DamageSync
    {
        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
        [HarmonyPostfix]
        public static void ProcessReceivedDamage(Dam_EnemyDamageBase __instance)
        {
            if (!SNetwork.SNet.IsMaster) return;

            if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Sent health value: {__instance.Health}");
            
            __instance.SendSetHealth(__instance.Health);
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveSetHealth))]
        [HarmonyPostfix]
        public static void ReceiveSetHealth(Dam_EnemyDamageBase __instance)
        {
            if (SNetwork.SNet.IsMaster) return;

            if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Received health value: {__instance.Health}");
        }
    }
}
