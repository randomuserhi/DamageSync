using API;
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

#if DEBUG
            APILogger.Debug(Module.Name, $"Sent health value: {__instance.Health}");
#endif
            
            __instance.SendSetHealth(__instance.Health);
        }

#if DEBUG
        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveSetHealth))]
        [HarmonyPostfix]
        public static void ReceiveSetHealth(Dam_EnemyDamageBase __instance)
        {
            if (SNetwork.SNet.IsMaster) return;

            APILogger.Debug(Module.Name, $"Received health value: {__instance.Health}");
        }
#endif
    }
}
