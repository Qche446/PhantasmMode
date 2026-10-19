using ReLogic.Content;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Bosses.Trojan
{
    public class SqBomb : ModProjectile
    {
        public override string Texture => "FargowiltasSouls/Content/NPCs/Critters/TophatSquirrelCritter";
        private const int DefaultHopTime = 180;
        private const int FuseTime = 60;
        private const int IdleTime = 12;
        private const float Gravity = 0.35f;
        private const float JumpTime = 42f;
        private const float MaxJumpRise = 13f;
        private const float MinJumpRise = 5.5f;
        private const float MaxJumpHorizontal = 16f;
        private const int ExplosionSize = 124;

        // 帧 0 只在这两个窗口出现：刚生成、爆炸前。
        private const int SpawnFrameTime = 18;
        private const int FuseFrameTime = 16;

        private const int GroundFrameInterval = 8;
        private const float ApexSpeed = 1.5f;

        // localAI[2] = 状态；localAI[0] = 待机计时；localAI[1] = 引信计时；frameCounter 当年龄用。
        private const float StateAir = 0f;
        private const float StateIdle = 1f;
        private const float StateFuse = 2f;

        private bool detonated;

        private float State
        {
            get => Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }

        private int Age => Projectile.frameCounter;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = Projectile.height = 30;
            Projectile.timeLeft = 2;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        // ai[0] = 起爆前的跳跃时长；ai[1] = 相位时间；ai[2] = 目标玩家。只有爆炸结算伤害。
        public override bool? CanDamage() => detonated;

        public override void AI()
        {
            Projectile.timeLeft = 2;

            Player target = GetTarget();
            if (target == null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.frameCounter++;

            float hopTime = Projectile.ai[0] > 0f ? Projectile.ai[0] : DefaultHopTime;
            Projectile.ai[1]++;

            if (Projectile.ai[1] > hopTime)
            {
                if (State != StateFuse)
                {
                    State = StateFuse;
                    Projectile.localAI[1] = 0f;
                    Projectile.netUpdate = true;
                }

                Fuse();
            }
            else if (State == StateIdle)
            {
                Idle(target);
            }
            else
            {
                Airborne();
            }

            if (!Projectile.active)
                return;

            Projectile.rotation = 0f;
            UpdateFrame();
        }

        private void Airborne()
        {
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > 15f)
                Projectile.velocity.Y = 15f;

            if (Math.Abs(Projectile.velocity.X) > 0.1f)
                Projectile.direction = Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;
        }

        private void Idle(Player target)
        {
            Projectile.velocity = Vector2.Zero;
            FaceTarget(target);

            if (!HasSupport(out bool onPlatform))
            {
                State = StateAir;
                return;
            }

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < IdleTime)
                return;

            Projectile.localAI[0] = 0f;

            if (onPlatform && target.Center.Y > Projectile.Bottom.Y)
            {
                // 玩家在下方：从平台面直接沉下去，交给 TileCollideStyle 的 fallThrough。
                State = StateAir;
                float direction = Math.Sign(target.Center.X - Projectile.Center.X);
                Projectile.velocity = new Vector2(direction * 2f, 0f);
            }
            else
            {
                Jump(target);
            }

            Projectile.netUpdate = true;
        }

        private void Jump(Player target)
        {
            Vector2 toTarget = target.Center - Projectile.Center;

            float vx = MathHelper.Clamp(1.5f * toTarget.X / JumpTime, -MaxJumpHorizontal, MaxJumpHorizontal);
            float vy = MathHelper.Clamp(1.5f * toTarget.Y / JumpTime - 0.5f * Gravity * JumpTime, -MaxJumpRise, -MinJumpRise);

            Projectile.velocity = new Vector2(vx, vy);
            State = StateAir;

            if (Math.Abs(vx) > 0.1f)
                Projectile.direction = Projectile.spriteDirection = vx > 0f ? 1 : -1;
        }

        private void Fuse()
        {
            Projectile.localAI[1]++;

            // 引信段按设计可以悬空：不加下落，整体速度衰减到静止。
            Projectile.velocity *= 0.86f;
            if (Projectile.velocity.LengthSquared() < 0.04f)
                Projectile.velocity = Vector2.Zero;

            if (Projectile.localAI[1] >= FuseTime)
                Projectile.Kill();
        }

        private void FaceTarget(Player target)
        {
            if (Math.Abs(target.Center.X - Projectile.Center.X) > 2f)
                Projectile.direction = Projectile.spriteDirection = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        private bool HasSupport(out bool onPlatform)
        {
            bool solid = false;
            bool platform = false;

            int tileY = (int)Math.Floor((Projectile.Bottom.Y + 2f) / 16f);

            for (int i = 0; i < 3; i++)
            {
                float x = Projectile.Center.X + (i - 1) * (Projectile.width / 2f - 4f);
                int tileX = (int)Math.Floor(x / 16f);
                if (!WorldGen.InWorld(tileX, tileY, 1))
                    continue;

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                if (!tile.HasUnactuatedTile)
                    continue;

                if (Main.tileSolid[tile.TileType])
                    solid = true;
                else if (Main.tileSolidTop[tile.TileType])
                    platform = true;
            }

            onPlatform = platform && !solid;
            return solid || platform;
        }

        private bool IsGrounded()
        {
            if (State == StateIdle)
                return true;

            if (State == StateFuse)
                return HasSupport(out _);

            return false;
        }

        private void UpdateFrame()
        {
            if (Age < SpawnFrameTime || (State == StateFuse && Projectile.localAI[1] >= FuseTime - FuseFrameTime))
            {
                Projectile.frame = 0;
                return;
            }

            if (IsGrounded())
            {
                Projectile.frame = (Age / GroundFrameInterval) % 2 == 0 ? 1 : 5;
                return;
            }

            float verticalSpeed = Projectile.velocity.Y;
            if (Math.Abs(verticalSpeed) < ApexSpeed)
                Projectile.frame = 3;
            else
                Projectile.frame = verticalSpeed < 0f ? 2 : 4;
        }

        private Player GetTarget()
        {
            int targetIndex = (int)Projectile.ai[2];
            if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
            {
                Player target = Main.player[targetIndex];
                if (target.active && !target.dead)
                    return target;
            }

            int closestIndex = Player.FindClosest(Projectile.Center, 0, 0);
            if (closestIndex >= 0 && closestIndex < Main.maxPlayers)
            {
                Player closest = Main.player[closestIndex];
                if (closest.active && !closest.dead)
                {
                    Projectile.ai[2] = closest.whoAmI;
                    Projectile.netUpdate = true;
                    return closest;
                }
            }
            return null;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.Y > 0f)
            {
                Projectile.velocity.Y = 0f;
                Projectile.velocity.X = 0f;

                if (State != StateFuse)
                {
                    State = StateIdle;
                    Projectile.localAI[0] = 0f;
                    Projectile.netUpdate = true;
                }
            }
            else if (oldVelocity.Y < 0f)
            {
                Projectile.velocity.Y = 0f;
            }

            if (oldVelocity.X != Projectile.velocity.X)
                Projectile.velocity.X = 0f;

            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            int targetIndex = (int)Projectile.ai[2];
            if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
            {
                Player target = Main.player[targetIndex];
                fallThrough = target.active && !target.dead && target.Center.Y > Projectile.Bottom.Y;
            }

            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override void OnKill(int timeLeft)
        {
            if (detonated)
                return;
            detonated = true;

            Vector2 explosionCenter = Projectile.Center;
            Projectile.Resize(ExplosionSize, ExplosionSize);
            Projectile.Center = explosionCenter;

            Projectile.Damage();

            if (Main.dedServ)
                return;

            for (int i = 0; i < 24; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.6f);
                Main.dust[dust].velocity *= 1.4f;
            }
            for (int i = 0; i < 12; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 4f;
            }

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);

            if (State == StateFuse)
            {
                Texture2D glow = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/MutantBoss/MutantSpearAimGlow", AssetRequestMode.ImmediateLoad).Value;
                float progress = MathHelper.Clamp(Projectile.localAI[1] / FuseTime, 0f, 1f);
                float radiusScale = 100f * progress / glow.Width;
                Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, glow.Bounds, Color.OrangeRed * (0.25f + 0.5f * progress), 0f, glow.Bounds.Size() * 0.5f, radiusScale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
