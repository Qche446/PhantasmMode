using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.InfoDisplays;
using FargosPhantasmMode.Content.Items.Accessories;
using FargosPhantasmMode.Content.Items.Global;
using FargowiltasSouls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles
{
    public class PModeGlobalProj : GlobalProjectile
    {
        public int GrazeCD = 0;
        public int CaseCrush = 0;
        public override bool InstancePerEntity => true;
        public override void PostAI(Projectile projectile)
        {
            if (projectile.hostile && projectile.damage > 0 && projectile.aiStyle != ProjAIStyleID.FallingTile && --GrazeCD < 0)
            {
                GrazeCD = 6; //don't check per tick ech
                Player py = Main.LocalPlayer;
                if (py.active && !py.dead)
                {
                    if (py.FargoSouls().Graze && !py.immune && py.hurtCooldowns[0] <= 0 && py.hurtCooldowns[1] <= 0)
                    {
                        var fproj = projectile.FargoSouls();
                        if (ProjectileLoader.CanDamage(projectile) != false && ProjectileLoader.CanHitPlayer(projectile, py) && fproj.GrazeCheck(projectile))
                        {
                            GrazeCD = 30 * projectile.MaxUpdates;
                            ShadowveilHeart.OnGraze(py);
                            py.GetModPlayer<InfoModPlayer>().GrazeCount++;
                        }
                    }
                }
            }
        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.Phantasm().CrushCumulative += CaseCrush;
        }
        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            target.Phantasm().CrushCumulative += CaseCrush;
        }
    }
}
