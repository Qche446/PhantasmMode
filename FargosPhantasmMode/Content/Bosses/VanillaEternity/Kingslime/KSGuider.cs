using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monochrome.Core.Net;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Kingslime
{
    /// <summary>
    /// <para>targetPos = new(ai[0], ai[1]); ai[2] is the remaining travel time in ticks.</para>
    /// <para>Projectile.velocity supplies an initial <b>direction</b> only. Its magnitude is ignored.</para>
    /// <para>localAI: [0] = total turn angle, [1] = constant speed, [2] = initialized, [3] = total travel ticks.</para>
    /// </summary>
    public class KSGuider : ModProjectile
    {
        private const int TurnSamples = 720;
        private const float MaximumTurn = MathHelper.TwoPi - 0.001f;
        private static readonly Dictionary<int, Vector2[]> TurnProfiles = [];
        private static readonly Dictionary<ulong, TurnSolution> TurnCache = [];

        private readonly struct TurnSolution(float turn, float unitDisplacementLength)
        {
            public readonly float Turn = turn;
            public readonly float UnitDisplacementLength = unitDisplacementLength;
        }

        public float localAI3;
        public override string Texture => "FargowiltasSouls/Content/Bosses/MutantBoss/MutantSlimeBall_2";
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;

            // King Slime fires nine guides with these durations and direction
            // offsets. Calculate their immutable path data during mod loading,
            // rather than blocking its teleport frame with trig-heavy searches.
            PrewarmKingSlimePaths();
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.hostile = true;
            Projectile.scale = 1f;
        }
        public override void AI()
        {
            if (++Projectile.frameCounter > 6)
            {
                if (++Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
                Projectile.frameCounter = 0;
            }

            Vector2 targetPos = new(Projectile.ai[0], Projectile.ai[1]);
            int remainingTicks = Math.Max(1, (int)MathF.Ceiling(Projectile.ai[2]));
            if (Projectile.ai[2] <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[2] == 0f)
            {
                InitializePath(targetPos, remainingTicks);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.netUpdate = true;
            }

            MoveAlongFieldLine(targetPos, remainingTicks);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathF.PI / 2f;
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Vector2 offset = new Vector2(0, -20).RotatedBy(Projectile.rotation);
            offset = offset.RotatedByRandom(MathHelper.Pi / 6);

            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.GemSapphire, 0f, 0f, 150);
                Main.dust[d].position += offset;
                float velrando = Main.rand.Next(20, 31) / 10;
                Main.dust[d].velocity = Projectile.velocity / velrando;
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 1.2f;
            }
            if (!Collision.SolidCollision(base.Projectile.position, base.Projectile.width, base.Projectile.height))
            {
                Lighting.AddLight(base.Projectile.Center, 0f, 0f, 0.8f);
            }
        }

        private void InitializePath(Vector2 targetPos, int travelTicks)
        {
            Vector2 offsetToTarget = targetPos - Projectile.Center;
            Vector2 targetDirection = offsetToTarget.SafeNormalize(Vector2.UnitX);
            Vector2 initialDirection = Projectile.velocity.SafeNormalize(targetDirection);
            float distanceToTarget = offsetToTarget.Length();

            TurnSolution path = FindSmoothTurn(initialDirection.ToRotation(), targetDirection.ToRotation(), travelTicks);
            Projectile.localAI[0] = path.Turn;
            Projectile.localAI[1] = distanceToTarget / path.UnitDisplacementLength;
            Projectile.localAI[2] = 1f;
            localAI3 = travelTicks;
        }

        private void MoveAlongFieldLine(Vector2 targetPos, int remainingTicks)
        {
            Vector2 offsetToTarget = targetPos - Projectile.Center;
            float speed = Projectile.localAI[1];

            if (offsetToTarget.LengthSquared() <= 0.0001f || Projectile.localAI[1] <= 0.0001f)
            {
                Projectile.Center = targetPos;
                Projectile.velocity = Vector2.Zero;
                Projectile.ai[2] = 0f;
                return;
            }

            int totalTicks = (int)localAI3;
            int stepIndex = totalTicks - remainingTicks;
            float initialAngle;
            if (stepIndex == 0)
            {
                initialAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
            }
            else
            {
                float previousProgress = (stepIndex - 1f) / (totalTicks - 1f);
                initialAngle = Projectile.velocity.ToRotation() - Projectile.localAI[0] * SmoothTurnProgress(previousProgress);
            }

            float progress = totalTicks <= 1 ? 0f : stepIndex / (totalTicks - 1f);
            float velocityAngle = initialAngle + Projectile.localAI[0] * SmoothTurnProgress(progress);
            Projectile.velocity = velocityAngle.ToRotationVector2() * Projectile.localAI[1];
            Projectile.ai[2]--;
        }

        // A smooth angular distribution has a zero turn rate at both ends. It
        // resembles an electric field line more closely than a sharp homing turn.
        private static float SmoothTurnProgress(float progress) => MathHelper.SmoothStep(0f, 1f, progress);

        private static Vector2 SumUnitVelocityDirections(float initialAngle, float totalTurn, int totalTicks)
        {
            Vector2 sum = Vector2.Zero;
            for (int step = 0; step < totalTicks; step++)
            {
                float progress = totalTicks <= 1 ? 0f : step / (totalTicks - 1f);
                sum += (initialAngle + totalTurn * SmoothTurnProgress(progress)).ToRotationVector2();
            }
            return sum;
        }

        private static TurnSolution FindSmoothTurn(float initialAngle, float targetAngle, int totalTicks)
        {
            float shortestAngle = MathHelper.WrapAngle(targetAngle - initialAngle);
            if (totalTicks <= 1 || Math.Abs(shortestAngle) < 0.0001f)
                return new TurnSolution(0f, totalTicks);

            ulong cacheKey = GetTurnCacheKey(totalTicks, shortestAngle);
            if (TurnCache.TryGetValue(cacheKey, out TurnSolution cachedPath))
                return cachedPath;

            Vector2 desiredDirection = targetAngle.ToRotationVector2();
            float bestTurn = 0f;
            float bestAbsoluteTurn = float.MaxValue;
            float bestDirectionalError = float.MaxValue;
            Vector2[] profile = GetTurnProfile(totalTicks);
            float previousTurn = -MaximumTurn;
            Vector2 previousSum = profile[0].RotatedBy(initialAngle);
            float previousCross = Cross(previousSum, desiredDirection);

            for (int sample = 1; sample <= TurnSamples; sample++)
            {
                float currentTurn = MathHelper.Lerp(-MaximumTurn, MaximumTurn, sample / (float)TurnSamples);
                Vector2 currentSum = profile[sample].RotatedBy(initialAngle);
                float currentCross = Cross(currentSum, desiredDirection);

                if ((previousCross < 0f && currentCross > 0f) || (previousCross > 0f && currentCross < 0f))
                {
                    float low = previousTurn;
                    float high = currentTurn;
                    for (int iteration = 0; iteration < 18; iteration++)
                    {
                        float middle = (low + high) * 0.5f;
                        float middleCross = Cross(SumUnitVelocityDirections(initialAngle, middle, totalTicks), targetAngle.ToRotationVector2());
                        if ((previousCross < 0f && middleCross < 0f) || (previousCross > 0f && middleCross > 0f))
                            low = middle;
                        else
                            high = middle;
                    }

                    float candidateTurn = (low + high) * 0.5f;
                    Vector2 candidateSum = SumUnitVelocityDirections(initialAngle, candidateTurn, totalTicks);
                    float candidateAbsoluteTurn = Math.Abs(candidateTurn);
                    if (Vector2.Dot(candidateSum, desiredDirection) > 0f && candidateAbsoluteTurn < bestAbsoluteTurn)
                    {
                        bestTurn = candidateTurn;
                        bestAbsoluteTurn = candidateAbsoluteTurn;
                        bestDirectionalError = 0f;
                    }
                }

                float directionalError = Math.Abs(MathHelper.WrapAngle(currentSum.ToRotation() - targetAngle));
                if (Vector2.Dot(currentSum, desiredDirection) > 0f
                    && (directionalError < bestDirectionalError
                        || directionalError == bestDirectionalError && Math.Abs(currentTurn) < bestAbsoluteTurn))
                {
                    bestTurn = currentTurn;
                    bestAbsoluteTurn = Math.Abs(currentTurn);
                    bestDirectionalError = directionalError;
                }

                previousTurn = currentTurn;
                previousCross = currentCross;
            }

            float unitDisplacementLength = SumUnitVelocityDirections(0f, bestTurn, totalTicks).Length();
            TurnSolution path = new(bestTurn, unitDisplacementLength);
            TurnCache[cacheKey] = path;
            return path;
        }

        private static Vector2[] GetTurnProfile(int totalTicks)
        {
            if (TurnProfiles.TryGetValue(totalTicks, out Vector2[] profile))
                return profile;

            profile = new Vector2[TurnSamples + 1];
            for (int sample = 0; sample <= TurnSamples; sample++)
            {
                float turn = MathHelper.Lerp(-MaximumTurn, MaximumTurn, sample / (float)TurnSamples);
                profile[sample] = SumUnitVelocityDirections(0f, turn, totalTicks);
            }

            TurnProfiles[totalTicks] = profile;
            return profile;
        }

        private static ulong GetTurnCacheKey(int totalTicks, float relativeTargetAngle)
        {
            uint quantizedAngle = unchecked((uint)MathF.Round(relativeTargetAngle * 100000f));
            return ((ulong)(uint)totalTicks << 32) | quantizedAngle;
        }

        private static void PrewarmKingSlimePaths()
        {
            foreach (int travelTicks in new[] { 100, 110, 130, 140 })
            {
                GetTurnProfile(travelTicks);
                for (int i = -4; i <= 4; i++)
                {
                    float launchOffset = i * MathHelper.Pi / 5f;
                    if (Math.Abs(i) <= 1)
                        launchOffset *= 1.2f;
                    else if (Math.Abs(i) >= 3)
                        launchOffset *= 0.85f;

                    FindSmoothTurn(launchOffset, 0f, travelTicks);
                }
            }
        }

        private static float Cross(Vector2 first, Vector2 second) => first.X * second.Y - first.Y * second.X;

        /// <summary>
        /// 三个 <c>localAI</c> 格加本类自己的 <see cref="localAI3"/>，写读由字段表统一。
        /// </summary>
        private static readonly MonoNetFields<Projectile> Fields =
            MonoNet.Fields<Projectile>("fpm.ksGuider")
                .Float("localAI0", static p => p.localAI[0], static (p, value) => p.localAI[0] = value)
                .Float("localAI1", static p => p.localAI[1], static (p, value) => p.localAI[1] = value)
                .Float("localAI2", static p => p.localAI[2], static (p, value) => p.localAI[2] = value)
                .Float("localAI3", static p => Self(p).localAI3, static (p, value) => Self(p).localAI3 = value);

        private static KSGuider Self(Projectile projectile) => (KSGuider)projectile.ModProjectile;

        public override void SendExtraAI(BinaryWriter writer) => Fields.Write(Projectile, writer);

        public override void ReceiveExtraAI(BinaryReader reader) => Fields.Read(Projectile, reader);
        public override void OnHitPlayer(Player py, Player.HurtInfo info)
        {
            py.AddBuff(ModContent.BuffType<FractureBuff>(), 60 * 10);
            py.AddBuff(BuffID.Slimed, 60);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int sizeY = texture.Height / Main.projFrames[Type]; //ypos of lower right corner of sprite to draw
            int sizeX = texture.Width;
            int frameY = Projectile.frame * sizeY;
            int frameX = 0;
            Rectangle rectangle = new(frameX, frameY, sizeX, sizeY);
            Vector2 origin = rectangle.Size() / 2f;
            SpriteEffects spriteEffects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;


            //Main.spriteBatch.End();
            //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (float i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i += 1)
            {
                Color oldColor = Color.BlueViolet;
                oldColor.A = 50;
                float modifier = (float)(ProjectileID.Sets.TrailCacheLength[Type] - i) / ProjectileID.Sets.TrailCacheLength[Type];
                oldColor *= modifier;
                float scale = (Projectile.scale / 2) + (Projectile.scale * modifier / 2);
                int max0 = (int)i - 1;//Math.Max((int)i - 1, 0);
                if (max0 < 0)
                    continue;
                Vector2 oldPos = Vector2.Lerp(Projectile.oldPos[(int)i], Projectile.oldPos[max0], 1 - i % 1) + (origin / 2);
                float oldRot = Projectile.oldRot[max0];
                Main.EntitySpriteDraw(texture, oldPos - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, oldColor,
                    oldRot, origin, scale, spriteEffects, 0);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, Color.White,
                    Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            return false;
        }
    }
}
