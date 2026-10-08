using System.Collections.Generic;
using FargowiltasSouls;
using Microsoft.Xna.Framework;
using Monochrome.Core.Net;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Core.Systems
{
    public class PModeWorldSavingSystem : ModSystem
    {
        /// <summary>
        /// 两个世界级开关的权威副本。下发交给 MonoNet：值变时只发变化的字节，客户端加入时随世界数据
        /// 一次性补齐。原先每变一次都要重发整份 WorldData，那是全量世界快照。
        /// </summary>
        private static readonly MonoNetField<bool> phantasmMode = MonoNet.WorldFlag("fpm.phantasmMode", false);

        private static readonly MonoNetField<bool> canPlayPhantasm = MonoNet.WorldFlag("fpm.canPlayPhantasm", false);

        public static bool PhantasmMode { get => phantasmMode.Value; set => phantasmMode.Value = value; }

        public static bool CanPlayPhantasm { get => canPlayPhantasm.Value; set => canPlayPhantasm.Value = value; }

        /// <summary>
        /// 字段写在静态字段里，而静态初始化是惰性的——这里强制触发一次，保证它们在世界数据下发之前就注册好。
        /// <b>播报也在这里挂上</b>：它要读 <c>Mod.Name</c>，而 <c>Mod</c> 是实例上的东西，所以用实例方法而不是静态构造。
        /// </summary>
        public override void Load()
        {
            _ = phantasmMode;
            _ = canPlayPhantasm;

            // 值一变就在本机播报。写在通知里而不是写在"做决定"的地方，客户端才能在自己收到下发值时也播一次：
            phantasmMode.OnChanged = (_, enabled) => AnnouncePhantasm(enabled);
        }

        /// <summary>
        /// Phantasm 开或关时在本机播报（消息 + 音效）。<b>客户端也会走到这里</b>，它播的是下发下来的结果。
        /// </summary>
        /// <param name="enabled">新状态。</param>
        private void AnnouncePhantasm(bool enabled)
        {
            Color color = new(51, 255, 191, 0);
            if (enabled)
            {
                FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.UI.PhantasmOn", color);
                if (Main.getGoodWorld)
                    FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.UI.PhantasmFTWWarning", color);
            }
            else
            {
                FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.UI.PhantasmOff", color);
            }

            if (Main.dedServ)
                return;

            SoundStyle sound = enabled
                ? new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyMaso") with { Volume = 0.5f }
                : new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyDeactivate");
            SoundEngine.PlaySound(sound, Main.LocalPlayer.Center);
        }

        private static void ResetFlags()
        {
            // 客户端的世界级字段由下发决定，本地写会被拒绝并计数。
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // 复位不是"发生了一次变化"，所以静默写：不播报、也不发一轮多余的包。
            canPlayPhantasm.SetSilently(false);
            phantasmMode.SetSilently(false);
        }

        public override void OnWorldLoad() => ResetFlags();

        public override void OnWorldUnload() => ResetFlags();

        public override void SaveWorldData(TagCompound tag)
        {
            List<string> downed = [];
            if (CanPlayPhantasm)
                downed.Add("CanPlayPhantasm");
            if (PhantasmMode)
                downed.Add("phantasm");
            tag.Add("downed", downed);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> downed = tag.GetList<string>("downed");

            // 读档同样不是"发生了一次变化"：静默放进值，播报留给真正改变它的那一刻。
            canPlayPhantasm.SetSilently(downed.Contains("CanPlayPhantasm"));
            phantasmMode.SetSilently(downed.Contains("phantasm"));
        }
    }
}
