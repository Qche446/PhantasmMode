using FargosPhantasmMode.Content.Buffs;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Core.Players
{
    public partial class PmodePlayer
    {
        public bool Sublimation = false;
        public int MaxHallowLevel = 10;
        public bool Fracture = false;
        public bool BulletReload;
        public int BulletReloadTime;
        public bool PrayerofTaintedBlood;
        public bool CrushBuff;
        public int CrushCumulative;
        public const int MaxCrushCumulative = 100;
        private void BuffReset()
        {
            Sublimation = false;
            MaxHallowLevel = 10;
            Fracture = false;
            if (BulletReloadTime > 3)
                BulletReloadTime = 0;
            BulletReload = false;
            PrayerofTaintedBlood = false;
            CrushBuff = false;
        }
        private void BuffPostUpdateEquips()
        {
            if (Fracture)
                Player.noFallDmg = false;
            if (CrushCumulative >= MaxCrushCumulative)
            {
                Player.AddBuff(ModContent.BuffType<CrushBuff>(), 120);
                CrushCumulative = 0;
            }
        }
        private void DrawBuffEffect(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (Sublimation)
            {
                if (Main.rand.NextBool(4))
                {
                    int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.PortalBolt, Player.velocity.X * 0.4f, Player.velocity.Y * 0.4f, 0, new Color(220, 255, 220), 2.5f);
                    Main.dust[d].velocity.Y -= 1;
                    Main.dust[d].velocity *= 1.5f;
                    Main.dust[d].noGravity = true;
                }
            }
        }
    }
}
