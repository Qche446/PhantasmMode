using FargosPhantasmMode.Common.MetaBalls;
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls.Assets.ExtraTextures;
using Mono.Cecil;
using Monochrome.Content.Status;
using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Shaders;
using Monochrome.Core.Graphics.Trails;
using Terraria.GameContent;
using Monochrome.Common.MonoUtil;

namespace FargosPhantasmMode.Content.Items.Weapons
{
    public class HunterMark_Black : ModProjectile
    {
        /// <summary>身后那条拖尾。第一帧 AI 里建，交给 <see cref="MonoTrailSystem"/> 自动绘制。</summary>
        private MonoTrail trail;
        public int SpawnAgain = -1;
        private int hitindex = -1;
        public override string Texture => "Terraria/Images/Projectile_" + 661;
        public override void SetStaticDefaults()
        {
            Main.projFrames[base.Projectile.type] = Main.projFrames[661];
            ProjectileID.Sets.TrailCacheLength[base.Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[base.Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(661);
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = true;
            Projectile.scale *= 0.5f;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override void AI()
        {
            trail ??= new MonoTrail(10)
            {
                Space = MonoGraphicsSpace.World,
                MinDistance = 0f,
                PointsToCreate = 30,
                UseUnscaledMatrix = true,
                WidthFunction = t => MathHelper.SmoothStep(base.Projectile.scale * base.Projectile.width, 3.5f, 1f - t),
                ColorFunction = t => Color.Lerp(Color.Purple, Color.Black, 1 - t) * 0.8f,
                Texture = FargosTextureRegistry.FadedStreak.Value,
                Offset = 0.5f * Projectile.Size
            };
            ref int alpha = ref base.Projectile.alpha;
            if (alpha <= 0)
            {
                for (int num117 = 0; num117 < 3; num117++)
                {
                    int num118 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.Granite);
                    Main.dust[num118].noGravity = true;
                    Main.dust[num118].velocity *= 0.3f;
                    Main.dust[num118].noLight = true;
                }
            }
            if (alpha > 0)
            {
                alpha -= 95;
                Projectile.scale = 1.3f;
                if (alpha < 0)
                {
                    alpha = 0;
                    float num119 = 16f;
                    for (int i = 0; (float)i < num119; i++)
                    {
                        Vector2 spinningpoint7 = Vector2.UnitX * 0f;
                        spinningpoint7 += -Vector2.UnitY.RotatedBy((float)i * ((float)Math.PI * 2f / num119)) * new Vector2(1f, 4f);
                        spinningpoint7 = spinningpoint7.RotatedBy(base.Projectile.velocity.ToRotation());
                        int num121 = Dust.NewDust(base.Projectile.Center, 0, 0, DustID.PurpleTorch);
                        Main.dust[num121].scale = 1.5f;
                        Main.dust[num121].noLight = true;
                        Main.dust[num121].noGravity = true;
                        Main.dust[num121].position = base.Projectile.Center + spinningpoint7;
                        Main.dust[num121].velocity = Main.dust[num121].velocity * 4f + base.Projectile.velocity * 0.3f;
                    }
                }
            }
            if (Main.rand.NextBool())
            {
                Vector2 vector = Vector2.Normalize(base.Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.5f));
                float num122 = Math.Max(4f, base.Projectile.velocity.Length() / 2f);
                ModContent.GetInstance<CosmicFireMetaBall>().CreateParticle(base.Projectile.Center, num122 * vector, 15f);
            }
            base.Projectile.rotation = base.Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
        }
        public override void OnKill(int timeLeft)
        {
            trail = null;

            base.Projectile.position = base.Projectile.Center;
            base.Projectile.width = (base.Projectile.height = 160);
            base.Projectile.Center = base.Projectile.position;
            SpawnHunterMarkDust();
            if (SpawnAgain != -1)
                return;
            int index = -1;
            float distance = 1600;
            foreach(NPC npc in Main.npc)
            {
                if (npc.CanBeChasedBy() && npc.whoAmI != SpawnAgain && npc.whoAmI != hitindex)
                {
                    if (Vector2.Distance(npc.Center, base.Projectile.Center) < distance)
                    {
                        distance = Vector2.Distance(npc.Center, base.Projectile.Center);
                        index = npc.whoAmI;
                    }
                    break;
                }
            }
            if (index < 0) return;
            SpawnAgain = index;
            int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, 
                Projectile.velocity.Length() * Projectile.SafeDirectionTo(Main.npc[index].Center + 10 * Main.npc[index].velocity), Type, (int)(0.6f * Projectile.damage), Projectile.knockBack, Main.myPlayer);
            Main.projectile[p].As<HunterMark_Black>().SpawnAgain = index;
            if (hitindex >= 0)
                Main.projectile[p].localNPCImmunity[hitindex] = 20;
        }
        private void SpawnHunterMarkDust()
        {
            SoundEngine.PlaySound(in SoundID.Item14, base.Projectile.position);
            Vector2 vector27 = base.Projectile.Center;
            int num157 = 40;
            int num158 = num157;
            for (int i = 0; i < 4; i++)
            {
                int num160 = Dust.NewDust(vector27, num157, num158, DustID.Granite, 0f, 0f, 100, default, 1.5f);
                Main.dust[num160].position = base.Projectile.Center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * (float)Main.rand.NextDouble() * num157 / 2f;
            }
            for (int j = 0; j < 20; j++)
            {
                int num162 = Dust.NewDust(vector27, num157, num158, DustID.PurpleTorch, 0f, 0f, 200, default, 3.7f);
                Main.dust[num162].position = base.Projectile.Center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * (float)Main.rand.NextDouble() * num157 / 2f;
                Main.dust[num162].noGravity = true;
                Main.dust[num162].noLight = true;
                Dust dust80 = Main.dust[num162];
                Dust dust81 = dust80;
                dust81.velocity *= 3f;
                dust80 = Main.dust[num162];
                dust81 = dust80;
                dust81.velocity += base.Projectile.DirectionTo(Main.dust[num162].position) * (2f + Main.rand.NextFloat() * 4f);
                num162 = Dust.NewDust(vector27, num157, num158, DustID.PurpleTorch, 0f, 0f, 100, default, 1.5f);
                Main.dust[num162].position = base.Projectile.Center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * (float)Main.rand.NextDouble() * num157 / 2f;
                dust80 = Main.dust[num162];
                dust81 = dust80;
                dust81.velocity *= 2f;
                Main.dust[num162].noGravity = true;
                Main.dust[num162].fadeIn = 1f;
                Main.dust[num162].color = Color.Crimson * 0.5f;
                Main.dust[num162].noLight = true;
                dust80 = Main.dust[num162];
                dust81 = dust80;
                dust81.velocity += base.Projectile.DirectionTo(Main.dust[num162].position) * 8f;
            }
            for (int k = 0; k < 20; k++)
            {
                int num164 = Dust.NewDust(vector27, num157, num158, DustID.PurpleTorch, 0f, 0f, 0, default, 2.7f);
                Main.dust[num164].position = base.Projectile.Center + Vector2.UnitX.RotatedByRandom(3.1415927410125732).RotatedBy(base.Projectile.velocity.ToRotation()) * num157 / 2f;
                Main.dust[num164].noGravity = true;
                Main.dust[num164].noLight = true;
                Dust dust82 = Main.dust[num164];
                Dust dust83 = dust82;
                dust83.velocity *= 3f;
                dust82 = Main.dust[num164];
                dust83 = dust82;
                dust83.velocity += base.Projectile.DirectionTo(Main.dust[num164].position) * 2f;
            }
            for (int l = 0; l < 70; l++)
            {
                int num166 = Dust.NewDust(vector27, num157, num158, DustID.Granite, 0f, 0f, 0, default, 1.5f);
                Main.dust[num166].position = base.Projectile.Center + Vector2.UnitX.RotatedByRandom(3.1415927410125732).RotatedBy(base.Projectile.velocity.ToRotation()) * num157 / 2f;
                Main.dust[num166].noGravity = true;
                Dust dust84 = Main.dust[num166];
                Dust dust85 = dust84;
                dust85.velocity *= 3f;
                dust84 = Main.dust[num166];
                dust85 = dust84;
                dust85.velocity += base.Projectile.DirectionTo(Main.dust[num166].position) * 3f;
            }
        }
        public override void OnHitNPC(NPC npc, NPC.HitInfo hit, int damageDone)
        {
            npc.AddBuff(ModContent.BuffType<HunterMark>(), 60 * 10);
            //SpawnHunterMarkDust();
            MonoBuffSlotData data = npc.MonoBuffs().GetOrCreateData<HunterMark>();
            if (data.Ai[0] >= 20)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 vector = Vector2.UnitX.RotatedBy(i * MathHelper.TwoPi / 40f);
                    float num122 = 6;
                    ModContent.GetInstance<CosmicFireMetaBall>().CreateParticle(base.Projectile.Center, num122 * vector, 60f);
                }
            }
            hitindex = npc.whoAmI;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            int num = TextureAssets.Projectile[base.Projectile.type].Value.Height / Main.projFrames[base.Projectile.type];
            int y = num * base.Projectile.frame;
            Rectangle rectangle = new Rectangle(0, y, value.Width, num);
            Vector2 origin = rectangle.Size() / 2f;
            Vector2 vector = base.Projectile.rotation.ToRotationVector2() * (value.Width - base.Projectile.width) / 2f;
            vector = Vector2.Zero;
            SpriteEffects effects = ((base.Projectile.spriteDirection <= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            float alphaMultiplier = 1f;
            Color drawColor = lightColor * alphaMultiplier;
            Main.EntitySpriteDraw(value, base.Projectile.Center + vector - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, base.Projectile.GetAlpha(drawColor), base.Projectile.rotation, origin, base.Projectile.scale, effects);

            trail?.DrawFrom(Projectile.oldPos);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
            => new Color(255, 255, 255, 128) * (1f - (float)base.Projectile.alpha / 255f);
    }
}
