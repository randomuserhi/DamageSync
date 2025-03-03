using Agents;
using API;
using Enemies;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using SNetwork;

// TODO(randomuserhi): Add MTFO support to not add too many network hooks
// TODO(randomuserhi): Add an agreement packet on player join to prevent sending bad packets all the time
namespace DamageSync.Patches {
    [HarmonyPatch]
    internal static class Network {
        public static void SendLimbHealth(Agent target, int limbID, float health) {
            // client cannot send hit indicators
            if (!SNet.IsMaster) return;

            SNet_ChannelType channelType = SNet_ChannelType.SessionOrderCritical;
            SNet.GetSendSettings(ref channelType, out _, out SNet_SendQuality quality, out int channel);
            Il2CppSystem.Collections.Generic.List<SNet_Player> il2cppList = new();
            foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel) {
                if (player.Owner.IsBot) continue;
                if (player.Owner.Lookup == SNet.LocalPlayer.Lookup) continue;
                il2cppList.Add(player.Owner);
            }

            const int sizeOfHeader = sizeof(ushort) + sizeof(uint) + 1 + sizeof(int);
            const int sizeOfContent = sizeof(ushort) + 1 + sizeof(float);

            int index = 0;
            byte[] packet = new byte[sizeOfHeader + sizeOfContent];
            BitHelper.WriteBytes(repKey, packet, ref index);
            BitHelper.WriteBytes(magickey, packet, ref index);
            BitHelper.WriteBytes(msgtype, packet, ref index);
            BitHelper.WriteBytes(sizeOfContent, packet, ref index);

            BitHelper.WriteBytes((ushort)(target.m_replicator.Key + 1), packet, ref index);
            BitHelper.WriteBytes((byte)limbID, packet, ref index);
            BitHelper.WriteBytes(health, packet, ref index);
            SNet.Core.SendBytes(packet, quality, channel, il2cppList);

            if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Sent limb health GlobalId={target.GlobalID} id={limbID} health={health}.");
        }

        private static byte msgtype = 52;
        private static uint magickey = 10183481;
        private static ushort repKey = 0xFFFB; // make sure doesnt clash with GTFO-API

        // https://github.com/Kasuromi/GTFO-API/blob/main/GTFO-API/Patches/SNet_Replication_Patches.cs#L56
        [HarmonyPatch(typeof(SNet_Replication), nameof(SNet_Replication.RecieveBytes))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool RecieveBytes_Prefix(Il2CppStructArray<byte> bytes, uint size, ulong messagerID) {
            if (size < 12) return true;

            // The implicit constructor duplicates the memory, so copying it once and using that is best
            byte[] _bytesCpy = bytes;

            ushort replicatorKey = BitConverter.ToUInt16(_bytesCpy, 0);
            if (repKey == replicatorKey) {
                uint receivedMagicKey = BitConverter.ToUInt32(bytes, sizeof(ushort));
                if (receivedMagicKey != magickey) {
                    return true;
                }

                byte receivedMsgtype = bytes[sizeof(ushort) + sizeof(uint)];
                if (receivedMsgtype != msgtype) {
                    return true;
                }

                int msgsize = BitConverter.ToInt32(bytes, sizeof(ushort) + sizeof(uint) + 1);
                byte[] message = new byte[msgsize];
                Array.Copy(bytes, sizeof(ushort) + sizeof(uint) + 1 + sizeof(int), message, 0, msgsize);

                int index = 0;
                ushort agentRepKey = BitHelper.ReadUShort(message, ref index);
                byte limbID = BitHelper.ReadByte(message, ref index);
                float health = BitHelper.ReadFloat(message, ref index);

                SNetStructs.pReplicator pRep;
                pRep.keyPlusOne = agentRepKey;
                pAgent _agent;
                _agent.pRep = pRep;
                _agent.TryGet(out Agent agent);
                EnemyAgent? targetEnemy = agent.TryCast<EnemyAgent>();
                if (targetEnemy != null) {
                    if (ConfigManager.Debug) APILogger.Debug(Module.Name, $"Received limb health GlobalId={targetEnemy.GlobalID} id={limbID} health={health}.");

                    // Set limb health
                    targetEnemy.Damage.DamageLimbs[limbID].m_health = health;

                    return false;
                }

                APILogger.Error(Module.Name, "Received limb health packet but could not get player / enemy agent. This should not happen.");

                return false;
            }
            return true;
        }
    }
}
