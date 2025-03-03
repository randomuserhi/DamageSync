using API;
using HarmonyLib;
using SNetwork;

namespace DamageSync.Patches {
    [HarmonyPatch]
    internal class DamageSync {
        [HarmonyPatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.DoDamage))]
        [HarmonyPostfix]
        public static void LimbDoDamage(Dam_EnemyDamageLimb __instance) {
            if (!SNet.IsMaster) return;

            Network.SendLimbHealth(__instance.m_base.Owner, __instance.m_limbID, __instance.m_health);
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
        [HarmonyPostfix]
        public static void ProcessReceivedDamage(Dam_EnemyDamageBase __instance) {
            if (!SNet.IsMaster) return;

            // Big thanks to @Dex on GTFO modding discord:
            // - The below snippet sends a sethleathpacket with a channel type thats more important than what it normally is

            var data = default(pSetHealthData);
            data.health.Set(__instance.Health, __instance.HealthMax);
            __instance.m_setHealthPacket.Send(data, SNet_ChannelType.GameReceiveCritical);
            //line below is potentially useless, but thats what the game does natively in SendSetHealth
            //__instance.Health = data.health.Get(__instance.HealthMax);

            if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Sent health value: {__instance.Health}");
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveSetHealth))]
        [HarmonyPostfix]
        public static void ReceiveSetHealth(Dam_EnemyDamageBase __instance) {
            if (SNet.IsMaster) return;

            if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Received health value: {__instance.Health}");
        }
    }
}
