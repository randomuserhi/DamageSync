using Agents;
using API;
using Enemies;
using HarmonyLib;
using SNetwork;
using UnityEngine;

namespace DamSync {
    [HarmonyPatch]
    public class DamageSync {
        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
        [HarmonyPostfix]
        private static void ProcessReceivedDamage(Dam_EnemyDamageBase __instance, float damage, Agent damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool tryForceHitreact, int limbID) {
            Sync(__instance, limbID);
        }

        [HarmonyPatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveSetHealth))]
        [HarmonyPostfix]
        private static void ReceiveSetHealth(Dam_EnemyDamageBase __instance) {
            if (SNet.IsMaster) return;

            if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Received health value: {__instance.Health}");
        }

        public static void Sync(EnemyAgent agent, int limbID = -1) {
            Sync(agent.Damage, limbID);
        }

        public static void Sync(Dam_EnemyDamageBase __instance, int limbID = -1) {
            if (!SNet.IsMaster) return;

            // Big thanks to @Dex on GTFO modding discord:
            // - The below snippet sends a sethleathpacket with a channel type thats more important than what it normally is

            var data = default(pSetHealthData);
            data.health.Set(__instance.Health, __instance.HealthMax);
            __instance.m_setHealthPacket.Send(data, SNet_ChannelType.GameReceiveCritical);
            //line below is potentially useless, but thats what the game does natively in SendSetHealth
            //__instance.Health = data.health.Get(__instance.HealthMax);

            if (limbID >= 0 && limbID < __instance.DamageLimbs.Count) {
                Network.SendLimbHealth(__instance.Owner, limbID, __instance.DamageLimbs[limbID].m_health);
            }

            if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Sent health value: {__instance.Health}");
        }
    }
}