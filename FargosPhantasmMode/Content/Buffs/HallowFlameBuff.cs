using FargosPhantasmMode.Content.Buffs.Global;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Monochrome.Content.Status;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Buffs
{
    /// <summary>
    /// 圣炎。层数写在 <see cref="MonoBuffSlotData.Ai"/>[0]，随实体各存一份。
    /// <para>
    /// 玩家侧的层数由重复施加累加、上限见 <see cref="ResetEffects(Player)"/>；NPC 侧的层数只在施加时由
    /// 外部拷进去，不再增长。
    /// </para>
    /// </summary>
    public class HallowFlameBuff : MonoBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        //下限兜底
        private static int Level(MonoBuffSlotData data)
        {
            if (data is null)
                return 0;

            if (data.Ai[0] < 1f)
                data.Ai[0] = 1f;

            return (int)data.Ai[0];
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            int left = MonoBuffControl.TimeLeft(player, Type);
            MonoBuffControl.SetTime(player, Type, left > time ? left + time / 2 : left / 2 + time);

            MonoBuffSlotData data = player.MonoBuffs().GetOrCreateData<HallowFlameBuff>();
            data.Ai[0] += 1f;
            return true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int level = Level(player.MonoBuffs().Data<HallowFlameBuff>());

            player.buffImmune[BuffID.PotionSickness] = true;
            player.potionDelay = 0;
            if (player.FargoSouls().HallowHealTime > 0)
                player.FargoSouls().HallowHealTime = 0;

            player.statDefense -= level * 4;
            player.statLifeMax2 = (int)((1f - 0.05f * level) * player.statLifeMax2);
            player.endurance -= level * 0.02f;
            player.resistCold = true;
        }

        public override void ResetEffects(Player player)
        {
            MonoBuffSlotData data = player.MonoBuffs().Data<HallowFlameBuff>();
            if (data is null)
                return;

            int max = player.GetModPlayer<PModeBuffPlayer>().MaxHallowLevel;
            if (Level(data) > max)
                data.Ai[0] = max;
        }

        public override void UpdateBadLifeRegen(Player player)
        {
            MonoBuffSlotData data = player.MonoBuffs().Data<HallowFlameBuff>();
            int level = Level(data);
            if (level <= 0)
                return;

            int amount = (int)MathHelper.Min(level * 10, player.HasEffect<SpiritTornadoEffect>() ? 60 : 50);
            float dotMu = player.FargoSouls().Oiled ? 1.5f : 1f;

            if (player.lifeRegen > 0)
                player.lifeRegen = 0;

            player.lifeRegenTime = 0;
            player.lifeRegen -= (int)(amount * dotMu);
        }

        public override void DrawEffects(Player player, PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            int level = Level(player.MonoBuffs().Data<HallowFlameBuff>());
            for (int i = 0; i < MathHelper.Min(level, 4); i++)
            {
                if (Main.rand.NextBool(8))
                {
                    int d = Dust.NewDust(player.position, player.width, player.height, DustID.HallowedTorch, player.velocity.X * 0.4f, player.velocity.Y * 0.4f, 0, new Color(220, 255, 220), 2.5f);
                    Main.dust[d].velocity.Y -= 1;
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }
            }
        }

        public override void ResetEffects(NPC npc)
        {
            // 只补下限；NPC 侧层数不增长是有意为之。
            MonoBuffSlotData data = npc.MonoBuffs().Data<HallowFlameBuff>();
            if (data is not null)
                Level(data);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            int level = Level(npc.MonoBuffs().Data<HallowFlameBuff>());
            if (level <= 0)
                return;

            int a = Main.LocalPlayer.ForceEffect<HallowFlameEffect>() ? 8 : 4;

            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;

            npc.lifeRegen -= a * 10 * level;
            if (damage < a * level)
                damage = a * level;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            int level = Level(npc.MonoBuffs().Data<HallowFlameBuff>());
            if (level <= 0)
                return;

            modifiers.Defense.Flat -= 4 * level;
            float a = Main.LocalPlayer.FargoSouls().MutantPresence ? 0.005f : 0.01f;
            modifiers.FinalDamage *= 1f + a * level;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            int level = Level(npc.MonoBuffs().Data<HallowFlameBuff>());
            for (int i = 0; i < MathHelper.Min(level, 4); i++)
            {
                if (Main.rand.NextBool(4))
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.HallowedTorch, npc.velocity.X * 0.4f, npc.velocity.Y * 0.4f, 0, new Color(220, 255, 220), 2.5f);
                    Main.dust[d].velocity.Y -= 1;
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }
            }
        }

        /// <summary>
        /// 在本地玩家的圣炎图标右下角画层数。原版把剩余时间画在图标下方，而且是在这个钩子之后才画，
        /// 所以两者不重叠；层数小于 2 时什么都不画。
        /// </summary>
        public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
        {
            MonoBuffIcon.DrawRomanCount(spriteBatch, drawParams, Level(Main.LocalPlayer.MonoBuffs().Data<HallowFlameBuff>()), 0.55f);
        }
    }
}
