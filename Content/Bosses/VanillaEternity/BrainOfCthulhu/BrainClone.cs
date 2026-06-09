using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;


namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.BrainOfCthulhu
{
    public class BrainClone : ModNPC
    {
        public override string Texture => "Terraria/Images/NPC_266";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Brain of Cthulhu");
            //isplayName.AddTranslation((int)GameCulture.CultureName.Chinese, "克苏鲁之脑");
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.BrainofCthulhu];
            NPCID.Sets.CantTakeLunchMoney[Type] = true;

            this.ExcludeFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.width = 160;
            NPC.height = 110;
            NPC.scale += 0.25f;
            NPC.damage = 30;
            NPC.defense = 14;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit9;
            NPC.DeathSound = SoundID.NPCDeath11;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;

            NPC.Opacity = 0;
        }

        int trueAlpha;

        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            return trueAlpha == 0;
        }

        public override void AI()
        {
            if (EModeGlobalNPC.brainBoss < 0f || EModeGlobalNPC.brainBoss >= Main.maxNPCs)
            {
                NPC.SimpleStrikeNPC(int.MaxValue, 0, false, 0, null, false, 0, true);
                NPC.active = false;
                return;
            }
            NPC brain = Main.npc[EModeGlobalNPC.brainBoss];
            if (!brain.active || brain.type != NPCID.BrainofCthulhu)
            {
                NPC.SimpleStrikeNPC(int.MaxValue, 0, false, 0, null, false, 0, true);
                NPC.active = false;
                return;
            }

            if (NPC.buffType[0] != 0) //constant debuff cleanse
            {
                NPC.buffImmune[NPC.buffType[0]] = true;
                NPC.DelBuff(0);
            }

            NPC.target = brain.target;
            NPC.damage = brain.damage;
            NPC.defDamage = brain.defDamage;
            NPC.defense = brain.defense;
            NPC.defDefense = brain.defDefense;
            NPC.life = brain.life;
            NPC.lifeMax = brain.lifeMax;
            NPC.knockBackResist = 0;

            NPC.ai[2]++;
            int time = Main.getGoodWorld ? 110 : 80;
            if (Main.npc.Any(n => n.TypeAlive<BrainIllusionAttack>() && n.localAI[0] < BrainIllusionAttack.attackDelay))
                NPC.ai[2] = 9999;
            if (NPC.ai[2] < time && NPC.HasPlayerTarget)
            {
                if (NPC.Opacity < 1)
                    NPC.Opacity += 0.05f;
                Player player = Main.player[NPC.target];
                if (brain.GetGlobalNPC<PHBrainOfCthulhu>().ClonefadeDashTimer <= 0 && NPC.ai[1] == 0) // ai1: if has not yet dashed
                {
                    NPC.velocity *= 0;
                    NPC.Center = player.Center + (player.Center - brain.Center);
                }
                else
                {
                    NPC.velocity += NPC.DirectionTo(player.Center) * 0.28f;
                    NPC.ai[1] = 1; // lock to "dashing state" for rest of duration
                }
                   
            }
            else
            {
                NPC.Opacity -= 0.05f;
                NPC.damage = 0;
                if (NPC.Opacity <= 0.05f)
                {
                    NPC.SimpleStrikeNPC(int.MaxValue, 0, false, 0, null, false, 0, true);
                    NPC.active = false;
                    return;
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.brainBoss, NPCID.BrainofCthulhu))
            {
                NPC.frame.Y = Main.npc[EModeGlobalNPC.brainBoss].frame.Y;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (WorldSavingSystem.MasochistModeReal)
            {
                target.AddBuff(BuffID.Poisoned, 120);
                target.AddBuff(BuffID.Darkness, 120);
                target.AddBuff(BuffID.Bleeding, 120);
                target.AddBuff(BuffID.Slow, 120);
                target.AddBuff(BuffID.Weak, 120);
                target.AddBuff(BuffID.BrokenArmor, 120);
            }
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.Null();
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                //SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);
                for (int i = 0; i < 40; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood);
                    Main.dust[d].velocity *= 2.5f;
                    Main.dust[d].scale += 0.5f;
                }
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool CheckDead()
        {
            NPC.FargoSouls().Needled = false;

            NPC brain = FargoSoulsUtil.NPCExists(EModeGlobalNPC.brainBoss, NPCID.BrainofCthulhu);
            if (brain.Alive())
            {
                NPC.active = true;
                NPC.life = brain.life;
            }
            return false;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!Terraria.GameContent.TextureAssets.Npc[NPCID.BrainofCthulhu].IsLoaded)
                return false;

            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPCID.BrainofCthulhu].Value;
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = drawColor;
            color26 = NPC.GetAlpha(color26);

            SpriteEffects effects = SpriteEffects.None;

            Main.EntitySpriteDraw(texture2D13, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, NPC.rotation, origin2, NPC.scale, effects, 0);

            return false;
        }
    }
}