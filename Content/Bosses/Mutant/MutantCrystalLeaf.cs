using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using Luminance.Common.DataStructures;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    public class PHMutantCrystalLeaf : MutantCrystalLeaf, IProjOwnedByBoss<MutantBoss>
    {
        //ai0传mark2参数,ai1传初始角度，ai2传旋转方向(大小)
        float Length = 100f;
        int timer = 0;
        public override string Texture => FargoSoulsUtil.AprilFools ?
            "FargowiltasSouls/Content/Bosses/MutantBoss/MutantCrystalLeaf_April"
            : "FargowiltasSouls/Content/Projectiles/Souls/Chlorofuck";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystal Leaf");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 300;
            Projectile.aiStyle = -1;
            Projectile.scale = 2.5f;
            CooldownSlot = 1;
        }
        public override void AI()
        {
            if ((base.Projectile.localAI[0] += 1f) == 0f)
            {
                for (int i = 0; i < 30; i++)
                {
                    int num = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 0, default(Color), 2f);
                    Main.dust[num].noGravity = true;
                    Main.dust[num].velocity *= 5f;
                }
            }

            Lighting.AddLight(base.Projectile.Center, 0.1f, 0.4f, 0.2f);
            //base.Projectile.scale = ((float)(int)Main.mouseTextColor / 200f - 0.35f) * 0.2f + 0.95f;
            //base.Projectile.scale *= 2.5f;
            int projectileByIdentity = FargoSoulsUtil.GetProjectileByIdentity(base.Projectile.owner, (int)base.Projectile.ai[0], ModContent.ProjectileType<IronVirgin>());

            //收缩
            if (timer < 15)
            {
                Length = MathHelper.Lerp(Length, 300, 0.12f);//424约等于300*sqrt(2)
            }
            else if (timer < 75 && timer >= 60)
            {
                Length = MathHelper.Lerp(Length, 60, 0.12f);
            }
            timer++;
            if (timer >= 120)
                timer = 0;
            //定位
            Vector2 vector = new Vector2(Length, 0f).RotatedBy(Projectile.ai[1]);
            if (projectileByIdentity != -1)
            {
                base.Projectile.Center = Main.projectile[projectileByIdentity].Center + vector;
                base.Projectile.ai[1] += Projectile.ai[2] * 240 * MathF.PI / (180 * 60);//高速旋转
            }

            base.Projectile.rotation = Projectile.ai[1] + MathF.PI / 2f;

        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26);

            Main.spriteBatch.UseBlendState(BlendState.Additive);

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = Color.White * Projectile.Opacity;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.spriteBatch.ResetToDefault();

            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
