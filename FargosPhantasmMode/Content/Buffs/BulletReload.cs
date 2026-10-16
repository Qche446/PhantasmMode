
using FargosPhantasmMode.Common;
using Monochrome.Content.Status;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Buffs
{
    public class BulletReload : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.Phantasm().BulletReload = true;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            MonoBuffIcon.DrawRomanCount(spriteBatch, drawParams, Main.LocalPlayer.Phantasm().BulletReloadTime + 1, 0.65f);
            return true;
        }
    }
}
