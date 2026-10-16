using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Monochrome.Content.Status;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Buffs
{
    public class HunterMark : MonoBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        /// <summary>层数要跨端：两端读的是同一个数，减伤与图标才不会各说各话。</summary>
        public override bool SyncAi => true;

        public static int Level(MonoBuffSlotData data)
        {
            if (data is null)
                return 0;
            if (data.Ai[0] < 1f)
                data.Ai[0] = 1f;
            return (int)data.Ai[0];
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance *= 0.8f;
            player.statDefense += 5;//赫赫
        }
        public override void UpdateBadLifeRegen(Player player)
        {
            if (player.lifeRegen > 0)
                player.lifeRegen = 0;
        }
        public override bool ReApply(Player player, int time, int buffIndex)
        {
            MonoBuffSlotData data = player.MonoBuffs().GetOrCreateData<HunterMark>();
            player.SetTime(Type, time);
            data.Ai[0] += 1f;
            if (data.Ai[0] > 20f)
                data.Ai[0] = 20f;
            return true;
        }
        public override bool ReApply(NPC npc, int time, int buffIndex)
        {
            MonoBuffSlotData data = npc.MonoBuffs().GetOrCreateData<HunterMark>();
            npc.SetTime(Type, time);
            data.Ai[0] += 1f;
            if (data.Ai[0] > 20f)
                data.Ai[0] = 20f;
            return true;
        }
        public override void ResetEffects(Player player)
        {
            MonoBuffSlotData data = player.MonoBuffs().Data<HunterMark>();
            Level(data);
        }
        public override void ResetEffects(NPC npc)
        {
            MonoBuffSlotData data = npc.MonoBuffs().Data<HunterMark>();
            Level(data);
        }
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            int level = Level(npc.MonoBuffs().Data<HunterMark>());
            if (level <= 0)
                return;
            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;
        }
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            int level = Level(npc.MonoBuffs().Data<HunterMark>());
            for (int i = 0; i < MathHelper.Min(level, 2); i++)
            {
                if (Main.rand.NextBool(4))
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.DemonTorch, npc.velocity.X * 0.4f, npc.velocity.Y * 0.4f, 0, new Color(220, 255, 220), 2.5f);
                    Main.dust[d].velocity.Y -= 1;
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }
            }
        }
        public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
        {
            MonoBuffIcon.DrawRomanCount(spriteBatch, drawParams, Level(Main.LocalPlayer.MonoBuffs().Data<HunterMark>()), 0.55f);
        }
    }
}
