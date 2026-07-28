using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs
{
    public class FlawlessBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen += 10;
            player.GetArmorPenetration(DamageClass.Generic) += 10;
            player.GetCritChance(DamageClass.Generic) += 10;
            player.statDefense += 10;
            player.endurance += 0.1f;

            if (player.buffTime[buffIndex] <= 0) { buffIndex = -1; return; }
            base.Update(player, ref buffIndex);
        }
        
    }
    public class FlawlessPlayer : ModPlayer
    {
        public int FlawlessTimer = 0;
        public override void OnHurt(Player.HurtInfo info)
        {
            FlawlessTimer = 0;
            base.OnHurt(info);
        }
    }
}
