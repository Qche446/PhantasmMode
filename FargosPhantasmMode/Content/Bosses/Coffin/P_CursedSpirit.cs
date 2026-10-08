using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FargosPhantasmMode.Content.Bosses.Coffin;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.WorldGeneration;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;

public class P_CursedSpirit : PModeNPCBehaviour
{
	public CursedSpirit spirit;

	private int Frame = 0;

	public static readonly Color GlowColor = new(224, 196, 252, 0);

	private Vector2 LockVector1 = default;

	private readonly List<float> SlowChargeStates;

	public ref float Owner => ref spirit.Owner;

	public ref float Timer => ref spirit.Timer;

	public ref float State => ref spirit.State;

	public ref float AI3 => ref spirit.AI3;

	public ref float StartupFadein => ref spirit.StartupFadein;

	public ref int BiteTimer => ref spirit.BiteTimer;

	public ref int BittenPlayer => ref spirit.BittenPlayer;

	public ref bool RotateToVelocity => ref spirit.RotateToVelocity;

	public override int NPCType => ModContent.NPCType<CursedSpirit>();

	public override void SetDefaults(NPC npc)
	{
		if (!Main.getGoodWorld)
		{
			npc.damage *= (int)(1.1f * (float)npc.lifeMax);
			npc.lifeMax *= (int)(1.1f * (float)npc.lifeMax);
		}
	}

	public override void OnFirstTick(NPC npc)
	{
		spirit = Utilities.As<CursedSpirit>(npc);
	}

	/// <summary>
	/// 把"玩家挣脱咬击"结算到服务端的那只 CursedSpirit 上。
	/// <para>
	/// 挣脱的依据是客户端的连打计数，服务端推不出来，只能由被咬的客户端上报。
	/// 结算内容与本地解除那段一致，与 FSS 自己的 <c>SyncCursedSpiritRelease</c> 服务端分支同义。
	/// </para>
	/// </summary>
	/// <param name="spiritIndex">CursedSpirit 的 NPC 下标。</param>
	/// <param name="victimWhoAmI">被咬玩家的编号。</param>
	/// <returns>确实结算了才返回 true。</returns>
	public static bool TryApplyRelease(int spiritIndex, int victimWhoAmI)
	{
		if ((uint)spiritIndex >= Main.maxNPCs || (uint)victimWhoAmI >= Main.maxPlayers)
			return false;

		NPC npc = Main.npc[spiritIndex];
		if (!npc.active || npc.ModNPC is not CursedSpirit cursedSpirit)
			return false;

		Player victim = Main.player[victimWhoAmI];
		cursedSpirit.BittenPlayer = -1;
		cursedSpirit.BiteTimer = -90;
		npc.velocity = -Utilities.SafeDirectionTo((Entity)npc, victim.Center) * 12f;
		victim.immune = true;
		victim.immuneTime = Math.Max(victim.immuneTime, 30);
		victim.hurtCooldowns[0] = Math.Max(victim.hurtCooldowns[0], 30);
		victim.hurtCooldowns[1] = Math.Max(victim.hurtCooldowns[1], 30);
		npc.netUpdate = true;
		cursedSpirit.Timer = 0f;
		cursedSpirit.AI3 = 0f;
		return true;
	}

	public override bool SafePreAI(NPC npc)
	{
		NPC owner = FargoSoulsUtil.NPCExists(Owner, new int[1] { ModContent.NPCType<CursedCoffin>() });
		if (!FargoExtensionMethods.TypeAlive<CursedCoffin>(owner))
		{
			if (FargoSoulsUtil.HostCheck)
			{
				npc.StrikeInstantKill();
			}
			return false;
		}
		if (StartupFadein < 10f)
		{
			StartupFadein += 1f;
			npc.Opacity = 0f;
		}
		else if (StartupFadein == 10f)
		{
			npc.Opacity = 1f;
			StartupFadein += 1f;
		}
		if (FargoSoulsUtil.HostCheck)
		{
			npc.lifeMax = (owner.lifeMax = Math.Min(npc.lifeMax, owner.lifeMax));
			npc.life = (owner.life = Math.Min(npc.life, owner.life));
		}
		RotateToVelocity = true;
		npc.dontTakeDamage = npc.scale < 0.5f;
		npc.noTileCollide = true;
		if (FargoExtensionMethods.IsWithinBounds(owner.target, 255))
		{
			Player player = Main.player[owner.target];
			if (player != null && FargoExtensionMethods.Alive(player))
			{
				CursedCoffin coffin = Utilities.As<CursedCoffin>(owner);
				if (BittenPlayer != -1)
				{
					Player victim = Main.player[BittenPlayer];
					if (BiteTimer > 0 && victim.active && !victim.ghost && !victim.dead && npc.Distance(victim.Center) < 160f && FargoExtensionMethods.FargoSouls(victim).MashCounter < 20f)
					{
						victim.AddBuff(ModContent.BuffType<GrabbedBuff>(), 2);
						npc.velocity *= 0.2f;
						victim.velocity = Vector2.Zero;
						victim.Center = Vector2.Lerp(victim.Center, npc.Center, 0.1f);
					}
					else
					{
						BittenPlayer = -1;
						BiteTimer = -90;
						npc.velocity = -Utilities.SafeDirectionTo((Entity)npc, victim.Center) * 12f;
						victim.immune = true;
						victim.immuneTime = Math.Max(victim.immuneTime, 30);
						victim.hurtCooldowns[0] = Math.Max(victim.hurtCooldowns[0], 30);
						victim.hurtCooldowns[1] = Math.Max(victim.hurtCooldowns[1], 30);
						npc.netUpdate = true;
						Timer = 0f;
						AI3 = 0f;
						// 挣脱的依据是客户端连打计数，服务端推不出来，所以由客户端上报、服务端复核后结算。
						// 服务端自己走到这里时上面已经把状态改完了，不必再发。
						if (Main.netMode == NetmodeID.MultiplayerClient)
							FargosPhantasmMode.Core.Systems.PModeNet.ReleaseCursedSpirit.Send((byte)npc.whoAmI);
					}
					return false;
				}
				if (coffin.StateMachine.StateStack.Count == 0)
				{
					return false;
				}
				P_CursedCoffin.BehaviorStates currentState = ((ModNPC)(object)coffin).NPC.GetGlobalNPC<P_CursedCoffin>().StateMachine.CurrentState.Identifier;
				bool newState = (float)(int)currentState != State;
				P_CursedCoffin.BehaviorStates behaviorStates = currentState;
				P_CursedCoffin.BehaviorStates behaviorStates2 = behaviorStates;
				switch (behaviorStates2)
				{
				case P_CursedCoffin.BehaviorStates.StunPunish:
					if (newState)
					{
						Timer = 0f;
						AI3 = 0f;
						npc.netUpdate = true;
					}
					Movement(npc, player.Center + Utilities.SafeDirectionTo(player.Center, npc.Center) * 300f, 0.1f, 10f, 5f, 0.08f, 20f);
					break;
				case P_CursedCoffin.BehaviorStates.HoveringForSlam:
					if (newState)
					{
						Timer = 0f;
						AI3 = 0f;
						npc.netUpdate = true;
					}
					Artillery(npc, owner);
					break;
				case P_CursedCoffin.BehaviorStates.SlamWShockwave:
					if (newState)
					{
						Timer = 0f;
						AI3 = 0f;
						npc.netUpdate = true;
					}
					SlamSupport(npc, owner);
					break;
				default:
					if (!SlowChargeStates.Contains((int)currentState))
					{
						if (behaviorStates2 == P_CursedCoffin.BehaviorStates.PhaseTransition)
						{
							npc.Center = owner.Center;
							npc.scale = 0.2f;
						}
						break;
					}
					if (!SlowChargeStates.Contains(State))
					{
						Timer = 0f;
						AI3 = 0f;
						npc.netUpdate = true;
					}
					SlowCharges(npc, owner, currentState);
					break;
				}
				State = (int)currentState;
				if (RotateToVelocity)
				{
					npc.rotation = npc.velocity.ToRotation() + (float)Math.PI / 2f;
				}
				return false;
			}
		}
		return false;
	}

	private void SlamSupport(NPC npc, NPC owner)
	{
		P_CursedCoffin coffin = owner.GetGlobalNPC<P_CursedCoffin>();
		Player player = Main.player[owner.target];
		if (AI3 == 0f)
		{
			if (coffin.Timer < 0f || owner.velocity.Y == 0f)
			{
				AI3 = 1f;
			}
			npc.velocity = Vector2.Lerp(npc.velocity, Utilities.SafeDirectionTo((Entity)npc, owner.Center) * Math.Min(Math.Max(20f, owner.velocity.Length()), npc.Distance(owner.Center)), 0.2f);
			LerpOpacity(npc, 0.15f);
			LerpScale(npc, 0.4f);
		}
		else if (AI3 == 1f)
		{
			if (npc.Distance(owner.Center) > 50f)
			{
				for (int i = 0; i < 20; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.Shadowflame, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f));
				}
				npc.Center = owner.Center;
				npc.netUpdate = true;
			}
			LerpOpacity(npc, 1f, 0.4f);
			LerpScale(npc, 1f, 0.4f);
			AI3 = 2f;
			npc.velocity = Vector2.UnitY * 1f;
			SoundEngine.PlaySound(in CursedCoffin.SoulShotSFX, npc.Center);
			if (!FargoSoulsUtil.HostCheck)
			{
				return;
			}
			int cap = (WorldSavingSystem.MasochistModeReal ? 3 : 2);
			for (int j = -cap; j <= cap; j++)
			{
				if (j != 0 && (WorldSavingSystem.EternityMode || (j != 1 && j != -1)))
				{
					Vector2 vel = Vector2.UnitY.RotatedBy((float)j * ((float)Math.PI * 2f) * (0.041f + Main.rand.NextFloat(-0.02f, 0.01f))) * (6 + Math.Abs(j));
					Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Bottom + npc.velocity, vel, ModContent.ProjectileType<CoffinDarkSouls>(), FargoSoulsUtil.ScaledProjectileDamage(npc.damage, 1f, 2), 1f, Main.myPlayer, npc.whoAmI, -0.135f);
				}
			}
		}
		else
		{
			if (coffin.Timer > 3f)
			{
				AI3 = 0f;
			}
			npc.velocity *= 0.97f;
			LerpOpacity(npc, 1f, 0.4f);
			LerpScale(npc, 1f, 0.4f);
		}
	}

	private void SlowCharges(NPC npc, NPC owner, P_CursedCoffin.BehaviorStates state)
	{
		if (Timer < 80f)
		{
			LerpScale(npc, 0.8f, 0.4f);
			LerpOpacity(npc, 0.5f, 0.4f);
		}
		else
		{
			LerpScale(npc, 1f, 0.4f);
			LerpOpacity(npc, 1f, 0.4f);
		}
		Player player = Main.player[owner.target];
		if (Timer <= 1f)
		{
			AI3 = Utilities.SafeDirectionTo((Entity)npc, player.Center).ToRotation() + Main.rand.NextFloat(-0.9424779f, 0.9424779f);
			npc.netUpdate = true;
			List<Vector2> corners = CoffinArena.TopArenaCorners((Entity)npc);
			P_CursedCoffin coffin = owner.GetGlobalNPC<P_CursedCoffin>();
			if (coffin.StateMachine.StateStack.Count != 0 && coffin.StateMachine.CurrentState.Identifier == P_CursedCoffin.BehaviorStates.WavyShotSlam)
			{
				LockVector1 = corners.OrderByDescending((Vector2 x) => x.Distance(player.Center)).First();
			}
			else
			{
				LockVector1 = corners.OrderByDescending((Vector2 x) => Math.Abs(CrossProduct(npc.DirectionTo(x), npc.DirectionTo(player.Center)))).First();
			}
		}
		else if (Timer < 80f)
		{
			Movement(npc, LockVector1, 0.2f, 20f, 10f, 0.1f, 20f);
			npc.rotation = MathHelper.Lerp(npc.rotation, npc.DirectionTo(player.Center).ToRotation() + (float)Math.PI / 2f, 0.1f);
			RotateToVelocity = false;
		}
		else if (Timer < 90f)
		{
			npc.velocity = Vector2.Lerp(Vector2.Zero, npc.DirectionTo(player.Center) * 3f, 0.2f);
		}
		else
		{
			SoundEngine.PlaySound(in CursedCoffin.SpiritDroneSFX, npc.Center);
			if (state != P_CursedCoffin.BehaviorStates.RandomStuff)
			{
				if (Timer > 110f)
				{
					npc.noTileCollide = false;
				}
				if (Timer > 180f || (Timer > 110f && Collision.SolidTiles(npc.position + npc.velocity, npc.width, npc.height)))
				{
					Timer = 0f;
					return;
				}
				if (npc.velocity.LengthSquared() < 100f)
				{
					npc.velocity += npc.velocity.SafeNormalize(Vector2.Zero) * 0.5f;
				}
				npc.velocity = Utilities.ClampLength(npc.velocity, 0f, 10f);
			}
			else
			{
				Vector2 vectorToIdlePosition = player.Center - npc.Center;
				float speed = (WorldSavingSystem.MasochistModeReal ? 6.5f : (WorldSavingSystem.EternityMode ? 5.5f : 3f));
				float inertia = 20f;
				if (!WorldSavingSystem.MasochistModeReal)
				{
					inertia *= 1.5f;
				}
				vectorToIdlePosition.Normalize();
				vectorToIdlePosition *= speed;
				npc.velocity = (npc.velocity * (inertia - 1f) + vectorToIdlePosition) / inertia;
				if (npc.velocity == Vector2.Zero)
				{
					npc.velocity.X = -0.15f;
					npc.velocity.Y = -0.05f;
				}
				if (npc.velocity.Length() > 6.5f)
				{
					npc.velocity *= 0.97f;
				}
			}
		}
		Timer += 1f;
		static float CrossProduct(Vector2 v1, Vector2 v2)
		{
			return v1.X * v2.Y - v1.Y * v2.X;
		}
	}

	private void Artillery(NPC npc, NPC owner)
	{
		if (npc.Opacity > 0.9f)
		{
			npc.Opacity = 0.9f;
		}
		LerpOpacity(npc, 0.4f);
		LerpScale(npc, 0.6f);
		Vector2 desiredPos = owner.Center - Vector2.UnitY * owner.height;
		Movement(npc, desiredPos, 0.1f, Math.Max(25f, owner.velocity.Length()), owner.velocity.Length(), 0.08f, 20f);
		if (!(npc.Distance(desiredPos) < (float)owner.height * 0.75f))
		{
			return;
		}
		if (Timer % 20f == 19f)
		{
			SoundEngine.PlaySound(in CursedCoffin.SoulShotSFX, npc.Center);
			if (FargoSoulsUtil.HostCheck)
			{
				Vector2 vel = -Vector2.UnitY.RotatedBy(0.8796459436416626 * Math.Sin((float)Math.PI * 2f * (Timer + (float)Main.rand.Next(20)) / 53f)) * 4f;
				Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<CoffinDarkSouls>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 1f, 2), 1f, Main.myPlayer, npc.whoAmI, 0.18f);
			}
		}
		Timer += 1f;
	}

	private void GrabbyHands(NPC npc, NPC owner)
	{
		CursedCoffin coffin = Utilities.As<CursedCoffin>(owner);
		Player player = Main.player[owner.target];
		if (coffin.Timer < 40f)
		{
			Vector2 offset = -Vector2.UnitY * 300f - Vector2.UnitX * Math.Sign(owner.Center.X - player.Center.X) * 200f;
			Vector2 desiredPos = player.Center + offset;
			Movement(npc, desiredPos, 0.1f, 10f, 5f, 0.08f, 20f);
		}
		else
		{
			npc.velocity *= 0.97f;
		}
		if (coffin.Timer < 40f)
		{
			LerpOpacity(npc, 0.15f);
			LerpScale(npc, 0.4f);
		}
		else
		{
			LerpOpacity(npc, 1f, 0.3f);
			LerpScale(npc, 1f, 0.3f);
		}
		if (coffin.Timer == 40f && FargoSoulsUtil.HostCheck)
		{
			Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, (npc.rotation + (float)Math.PI / 2f).ToRotationVector2() * 4f, ModContent.ProjectileType<CoffinHand>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.1f, 2), 1f, Main.myPlayer, owner.whoAmI, 1f, 1f);
		}
	}

	private static void Movement(NPC npc, Vector2 pos, float accel = 0.03f, float maxSpeed = 20f, float lowspeed = 5f, float decel = 0.03f, float slowdown = 30f)
	{
		if (npc.Distance(pos) > slowdown)
		{
			npc.velocity = Vector2.Lerp(npc.velocity, (pos - npc.Center).SafeNormalize(Vector2.Zero) * maxSpeed, accel);
		}
		else
		{
			npc.velocity = Vector2.Lerp(npc.velocity, (pos - npc.Center).SafeNormalize(Vector2.Zero) * lowspeed, decel);
		}
	}

	private static void LerpOpacity(NPC npc, float target, float speed = 0.15f)
	{
		npc.Opacity = (float)Utils.Lerp(npc.Opacity, target, speed);
	}

	private static void LerpScale(NPC npc, float target, float speed = 0.15f)
	{
		npc.scale = (float)Utils.Lerp(npc.scale, target, speed);
	}

	public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D bodytexture = TextureAssets.Npc[npc.type].Value;
		Vector2 drawPos = npc.Center - screenPos;
		SpriteEffects spriteEffects = ((npc.direction != 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
		Color glowColor = CursedCoffin.GlowColor;
		int trailLength = NPCID.Sets.TrailCacheLength[npc.type];
		if (npc.scale < 0.5f)
		{
			trailLength /= 2;
		}
		for (int i = 0; i < trailLength; i++)
		{
			Vector2 oldPos = npc.oldPos[i];
			DrawData oldGlow = new DrawData(bodytexture, oldPos + npc.Size / 2f - screenPos + new Vector2(0f, npc.gfxOffY), npc.frame, npc.GetAlpha(glowColor * (0.8f / (float)i)), npc.oldRot[i], npc.Size / 2f, npc.scale, spriteEffects);
			GameShaders.Misc["LCWingShader"].UseColor(Color.Blue).UseSecondaryColor(Color.Black);
			GameShaders.Misc["LCWingShader"].Apply(oldGlow);
			oldGlow.Draw(spriteBatch);
		}
		for (int j = 0; j < 12; j++)
		{
			float spinOffset = (float)Main.GameUpdateCount * 0.001f * (float)j % 12f;
			float magnitude = 1f + (float)(j % 5) * 2f * MathF.Sin((float)Main.GameUpdateCount * ((float)Math.PI * 2f) / (10f + ((float)j - 6f) * 28f));
			Vector2 afterimageOffset = ((float)Math.PI * 2f * ((float)j + spinOffset) / 12f).ToRotationVector2() * magnitude * npc.scale;
			spriteBatch.Draw(bodytexture, drawPos + afterimageOffset, npc.frame, npc.GetAlpha(glowColor * npc.Opacity), npc.rotation, npc.Size / 2f, npc.scale, spriteEffects, 0f);
		}
		spriteBatch.Draw(bodytexture, drawPos, npc.frame, npc.GetAlpha(drawColor), npc.rotation, npc.Size / 2f, npc.scale, spriteEffects, 0f);
		return false;
	}

	public override void FindFrame(NPC npc, int frameHeight)
	{
		if ((npc.frameCounter += 1.0) > 4.0)
		{
			if (++Frame >= Main.npcFrameCount[npc.type] - 1)
			{
				Frame = 0;
			}
			npc.frameCounter = 0.0;
		}
		npc.spriteDirection = npc.direction;
		npc.frame.Y = frameHeight * Frame;
		npc.frame.Width = 120;
		if (SlowChargeStates.Contains(State))
		{
			npc.frame.X = 120;
		}
		else
		{
			npc.frame.X = 0;
		}
	}
}
