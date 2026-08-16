using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bossbar
{
    public static class EnemyDetector
    {
        private const float MAX_DETECTION_DISTANCE = 9000f;
        public static NPC GetTargetNPC(BigProgressBarInfo info)
        {
            NPC indexedNPC = GetIndexedNPC(info);
            if (indexedNPC != null)
            {
                return indexedNPC;
            }
            return FindNearestCustomBoss();
        }
        private static NPC GetIndexedNPC(BigProgressBarInfo info)
        {
            if (info.npcIndexToAimAt >= 0 && info.npcIndexToAimAt < Main.npc.Length)
            {
                NPC indexedNPC = Main.npc[info.npcIndexToAimAt];
                if (IsValidBossTarget(indexedNPC))
                {
                    return indexedNPC;
                }
            }
            return null;
        }
        private static NPC FindNearestCustomBoss()
        {
            NPC closestBoss = null;
            float closestDistance = float.MaxValue;
            var enumerator = Main.ActiveNPCs.GetEnumerator();
            while (enumerator.MoveNext())
            {
                NPC npc = enumerator.Current;
                if (IsValidBossTarget(npc))
                {
                    float distance = Vector2.Distance(npc.Center, Main.LocalPlayer.Center);
                    if (distance < closestDistance && distance < 9000f)
                    {
                        closestDistance = distance;
                        closestBoss = npc;
                    }
                }
            }
            return closestBoss;
        }
        private static bool IsValidBossTarget(NPC npc)
        {
            if (npc == null || !npc.active)
            {
                return false;
            }
            if (BossBarRegistry.HasCustomBossBar(npc.type))
            {
                return true;
            }
            if (npc.boss)
            {
                return npc.realLife < 0;
            }
            return false;
        }
    }
    public class MultiBossBarSystem : ModSystem
    {
        private static List<BossBarData> _activeBossBars = [];
        public static List<BossBarData> GetActiveBosses() => _activeBossBars;
        public readonly static List<int> blacklist = [NPCID.MoonLordHand, NPCID.MoonLordHead, NPCID.MoonLordFreeEye];
        public override void PostUpdateNPCs()
        {
            for (int i = _activeBossBars.Count - 1; i >= 0; i--)
            {
                BossBarData bossData = _activeBossBars[i];
                if (!IsValidBoss(bossData.NPCWhoAmI) || !IsInRange(bossData.NPCWhoAmI))
                {
                    _activeBossBars.RemoveAt(i);
                }
            }
            var enumerator = Main.ActiveNPCs.GetEnumerator();
            while (enumerator.MoveNext())
            {
                NPC npc = enumerator.Current;
                if (ShouldTrackBoss(npc) && !IsAlreadyTracked(npc.whoAmI))
                {
                    TryAddBoss(npc);
                }
            }
            _activeBossBars.Sort(delegate (BossBarData a, BossBarData b)
            {
                NPC val = Main.npc[a.NPCWhoAmI];
                NPC val2 = Main.npc[b.NPCWhoAmI];
                float num = (float)val.life / (float)val.lifeMax;
                float value = (float)val2.life / (float)val2.lifeMax;
                return num.CompareTo(value);
            });
            if (_activeBossBars.Count > 4)
            {
                _activeBossBars.RemoveRange(4, _activeBossBars.Count - 4);
            }
        }
        private static void TryAddBoss(NPC npc)
        {
            if (_activeBossBars.Count >= 4)
            {
                return;
            }
            _activeBossBars.Add(new BossBarData(npc.whoAmI, npc.type));
        }
        private static bool ShouldTrackBoss(NPC npc)
        {
            if (!npc.active || npc.life <= 0)
            {
                return false;
            }
            if (!IsInRange(npc.whoAmI))
            {
                return false;
            }
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
            {
                return false;
            }
            if (BossBarRegistry.HasCustomBossBar(npc.type))
            {
                return true;
            }
            if (!npc.boss)
                return false;
            if (blacklist.Contains(npc.type))
                return false;
            return true;
        }
        private static bool IsAlreadyTracked(int whoAmI)
        {
            return _activeBossBars.Exists((BossBarData b) => b.NPCWhoAmI == whoAmI);
        }
        private static bool IsValidBoss(int whoAmI)
        {
            if (whoAmI < 0 || whoAmI >= Main.npc.Length)
            {
                return false;
            }
            NPC npc = Main.npc[whoAmI];
            if (npc != null && npc.active)
            {
                return npc.life > 0;
            }
            return false;
        }
        private static bool IsInRange(int whoAmI)
        {
            if (whoAmI < 0 || whoAmI >= Main.npc.Length)
            {
                return false;
            }
            NPC npc = Main.npc[whoAmI];
            if (!npc.active)
            {
                return false;
            }
            return Vector2.Distance(npc.Center, Main.LocalPlayer.Center) <= 9000;
        }
    }
}
