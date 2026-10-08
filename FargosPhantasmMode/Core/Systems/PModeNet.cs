using FargowiltasSouls;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Monochrome.Core.Net;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using static Terraria.GameContent.Creative.CreativePowers;

namespace FargosPhantasmMode.Core.Systems
{
    /// <summary>
    /// FPM 的协议表：客户端→服务端的请求、它们的载荷形状，以及服务端那一侧的处理。
    /// <para>
    /// 投递与复核由 Monochrome 的 <see cref="MonoNetRequest{T}"/> 负责：发送者身份由传输层给出，
    /// 服务端复核不通过就丢弃并记日志。<b>客户端想改世界一律走这里，不要再自己拼包。</b>
    /// </para>
    /// </summary>
    public static class PModeNet
    {
        /// <summary>切换世界难度。载荷是档位：0 = 无，1 = 专家，2 = 受虐，3 = Phantasm。</summary>
        public static readonly MonoNetRequest<byte> SetDifficulty = MonoNet.Request<byte>("fpm.setDifficulty");

        /// <summary>仙人掌掉落。载荷是目标 NPC 下标与掉心还是掉星。</summary>
        public static readonly MonoNetRequest<CactusDropPayload> CactusDrop = MonoNet.Request(
            "fpm.cactusDrop",
            static (writer, payload) =>
            {
                writer.Write(payload.NpcIndex);
                writer.Write(payload.IsHeart);
            },
            static reader => new CactusDropPayload(reader.ReadByte(), reader.ReadBoolean()));

        /// <summary>玩家挣脱咬击。载荷是那只 CursedSpirit 的 NPC 下标。</summary>
        public static readonly MonoNetRequest<byte> ReleaseCursedSpirit = MonoNet.Request<byte>("fpm.releaseCursedSpirit");

        /// <summary>仙人掌掉落的载荷。</summary>
        public readonly struct CactusDropPayload
        {
            /// <summary>建一个载荷。</summary>
            /// <param name="npcIndex">目标 NPC 下标。</param>
            /// <param name="isHeart">掉心（否则掉星）。</param>
            public CactusDropPayload(byte npcIndex, bool isHeart)
            {
                NpcIndex = npcIndex;
                IsHeart = isHeart;
            }

            /// <summary>目标 NPC 下标。</summary>
            public byte NpcIndex { get; }

            /// <summary>掉心还是掉星。</summary>
            public bool IsHeart { get; }
        }

        /// <summary>在 <c>Mod.Load</c> 里调一次：注册请求并挂上服务端处理。</summary>
        public static void Register()
        {
            // 难度切换是有意由客户端决定的（每个客户端都能改世界难度都在预期之内），复核只挡越界的档位。
            SetDifficulty.Validate = static (_, diff) => diff <= 3;
            SetDifficulty.OnServer = static (_, diff) => ApplyDifficulty(diff);

            // 掉落请求：只做结构检查（下标在内、目标还在场）。"这个人有没有仙人掌效果"仍然由发起端判断，
            // 因为那是它自己的增益状态，服务端复核它得再查一遍玩家的效果表，收益不值那些耦合。
            CactusDrop.Validate = static (whoAmI, payload) =>
                (uint)whoAmI < Main.maxPlayers
                && payload.NpcIndex < Main.maxNPCs
                && Main.npc[payload.NpcIndex].active;

            CactusDrop.OnServer = static (whoAmI, payload) =>
            {
                NPC npc = Main.npc[payload.NpcIndex];
                Item.NewItem(Main.player[whoAmI].GetSource_OnHit(npc), npc.Hitbox, payload.IsHeart ? ItemID.Heart : ItemID.Star);
            };

            ReleaseCursedSpirit.Validate = static (whoAmI, spiritIndex) =>
                (uint)whoAmI < Main.maxPlayers
                && spiritIndex < Main.maxNPCs
                && Main.npc[spiritIndex].active;

            ReleaseCursedSpirit.OnServer = static (whoAmI, spiritIndex) => P_CursedSpirit.TryApplyRelease(spiritIndex, whoAmI);
        }

        /// <summary>把难度档位落实到世界设置上。<b>只有权威端会走到这里</b>（请求的复核那一步）。</summary>
        /// <param name="diff">档位。</param>
        private static void ApplyDifficulty(byte diff)
        {
            string toggle = diff switch
            {
                3 => "Phantasm",
                2 => "Master",
                1 => "Expert",
                _ => "None",
            };

            if (diff != 0)
            {
                bool changed = false;
                if (Main.GameModeInfo.IsJourneyMode)
                {
                    float value = diff >= 2 ? 1f : 0.66f;
                    DifficultySliderPower slider = CreativePowerManager.Instance.GetPower<DifficultySliderPower>();
                    typeof(DifficultySliderPower).GetMethod("SetValueKeyboardForced", Utilities.UniversalBindingFlags).Invoke(slider, [value]);
                }
                else
                {
                    switch (diff)
                    {
                        case 1:
                            if (Main.GameMode != GameModeID.Expert)
                                changed = true;
                            Main.GameMode = GameModeID.Expert;
                            break;
                        case 2:
                        case 3:
                            if (Main.GameMode != GameModeID.Master)
                                changed = true;
                            Main.GameMode = GameModeID.Master;
                            break;
                    }
                }

                if (changed)
                    FargoSoulsUtil.PrintLocalization($"Mods.Fargowiltas.Items.ModeToggle.{toggle}", new Color(175, 75, 255));
            }

            WorldSavingSystem.ShouldBeEternityMode = diff != 0;
            PModeWorldSavingSystem.CanPlayPhantasm = diff == 3;
            if (diff != 0)
                WorldSavingSystem.SpawnedDevi = true;

            // 原版的世界设置（GameMode 等）不在 MonoNet 的字段表里，仍然靠整份世界数据下发。
            NetMessage.SendData(MessageID.WorldData);
        }
    }
}
