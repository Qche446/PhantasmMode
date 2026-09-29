using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Global
{
    /// <summary>
    /// 作为EModeNPCBehaviour的copy版本(×
    /// </summary>
    public abstract class PModeNPCBehaviour : GlobalNPC
    {
        public abstract int NPCType { get; }
        public override bool InstancePerEntity => true;
        //public float AIState = 0;
        public bool RunPmodeAI = true;

        public sealed override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return lateInstantiation && entity.type == NPCType;
        }
        public override GlobalNPC NewInstance(NPC target)
        {
            //材质替换交给原法的globalnpc
            //TryLoadSprites(target);材质替换交给原法的globalnpc
            return PModeWorldSavingSystem.PhantasmMode && target.type == NPCType ? base.NewInstance(target) : null;
        }

        public bool FirstTick = true; 
        public virtual void OnFirstTick(NPC npc) { }
        //public virtual void StopEmodeAI(NPC npc) { }//字面意思
        public virtual bool SafePreAI(NPC npc) => base.PreAI(npc);
        public sealed override bool PreAI(NPC npc)
        {
            if (FirstTick)
            {
                FirstTick = false;
                //StopEmodeAI(npc);
                OnFirstTick(npc);
            }
            if (!RunPmodeAI)
            {
                return false;
            }
            return SafePreAI(npc);
        }
        public virtual void SafePostAI(NPC npc) => base.PostAI(npc);
        public sealed override void PostAI(NPC npc)
        {
            if (!RunPmodeAI)
            {
                return;
            }
            SafePostAI(npc);
            return;
        }
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);

            binaryWriter.Write(npc.localAI[0]);
            binaryWriter.Write(npc.localAI[1]);
            binaryWriter.Write(npc.localAI[2]);
            binaryWriter.Write(npc.localAI[3]);
            //binaryWriter.Write(AIState);
        }
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);

            npc.localAI[0] = binaryReader.ReadSingle();
            npc.localAI[1] = binaryReader.ReadSingle();
            npc.localAI[2] = binaryReader.ReadSingle();
            npc.localAI[3] = binaryReader.ReadSingle();
            //AIState = binaryReader.ReadSingle();
        }

        public virtual void ModifyHitByAnything(NPC npc, Player player, ref NPC.HitModifiers modifiers) { }

        public virtual void SafeModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) { }
        public sealed override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByItem(npc, player, item, ref modifiers);

            if (!WorldSavingSystem.EternityMode)
                return;

            SafeModifyHitByItem(npc, player, item, ref modifiers);
            ModifyHitByAnything(npc, player, ref modifiers);
        }

        public virtual void SafeModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) { }
        public sealed override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByProjectile(npc, projectile, ref modifiers);

            if (!WorldSavingSystem.EternityMode)
                return;

            SafeModifyHitByProjectile(npc, projectile, ref modifiers);
            ModifyHitByAnything(npc, Main.player[projectile.owner], ref modifiers);
        }


        public virtual void OnHitByAnything(NPC npc, Player player, NPC.HitInfo hit, int damageDone) { }

        public virtual void SafeOnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone) { }
        public sealed override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(npc, player, item, hit, damageDone);

            if (!WorldSavingSystem.EternityMode)
                return;

            SafeOnHitByItem(npc, player, item, hit, damageDone);
            // ModifyHitByAnything(npc, player, hit);
        }

        public virtual void SafeOnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) { }
        public sealed override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(npc, projectile, hit, damageDone);

            if (!WorldSavingSystem.EternityMode)
                return;

            SafeOnHitByProjectile(npc, projectile, hit, damageDone);
            // ModifyHitByAnything(npc, Main.player[projectile.owner], ref damage, ref knockback, ref crit);
        }


        protected static void NetSync(NPC npc, bool onlySendFromServer = true)
        {
            if (onlySendFromServer && Main.netMode != NetmodeID.Server)
                return;

            //npc.GetGlobalNPC<NewEModeGlobalNPC>().NetSync(npc);
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
        }
        /*
        /// <summary>
        /// Checks if loading sprites is necessary and does it if so.
        /// </summary>
        public void TryLoadSprites(NPC npc)
        {
            if (!Main.dedServ)
            {
                bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
                if (recolor || FargowiltasSouls.FargowiltasSouls.Instance.LoadedNewSprites)
                {
                    FargowiltasSouls.FargowiltasSouls.Instance.LoadedNewSprites = true;
                    LoadSprites(npc, recolor);
                }
            }
        }

        public virtual void LoadSprites(NPC npc, bool recolor) { }

        #region Sprite Loading
        protected static Asset<Texture2D> LoadSprite(string texture)
        {
            if (ModContent.RequestIfExists("FargowiltasSouls/Assets/ExtraTextures/Resprites/" + texture, out Asset<Texture2D> asset, AssetRequestMode.ImmediateLoad))
            {
                return asset;
            }
            return null;
        }

        protected static void LoadSpriteBuffered(bool recolor, int type, Asset<Texture2D>[] vanillaTexture, Dictionary<int, Asset<Texture2D>> fargoBuffer, string texturePrefix)
        {
            if (recolor)
            {
                if (!fargoBuffer.ContainsKey(type))
                {
                    fargoBuffer[type] = vanillaTexture[type];
                    vanillaTexture[type] = LoadSprite($"{texturePrefix}{type}") ?? vanillaTexture[type];
                }
            }
            else
            {
                if (fargoBuffer.ContainsKey(type))
                {
                    vanillaTexture[type] = fargoBuffer[type];
                    fargoBuffer.Remove(type);
                }
            }
        }

        protected static void LoadSpecial(bool recolor, ref Asset<Texture2D> vanillaResource, ref Asset<Texture2D> fargoSoulsBuffer, string name)
        {
            if (recolor)
            {
                if (fargoSoulsBuffer == null)
                {
                    fargoSoulsBuffer = vanillaResource;
                    vanillaResource = LoadSprite(name) ?? vanillaResource;
                }
            }
            else
            {
                if (fargoSoulsBuffer != null)
                {
                    vanillaResource = fargoSoulsBuffer;
                    fargoSoulsBuffer = null;
                }
            }
        }
        protected static void LoadNPCSprite(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.Npc, FargowiltasSouls.FargowiltasSouls.TextureBuffer.NPC, "NPC_");
        }

        protected static void LoadBossHeadSprite(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.NpcHeadBoss, FargowiltasSouls.FargowiltasSouls.TextureBuffer.NPCHeadBoss, "NPC_Head_Boss_");
        }

        protected static void LoadGore(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.Gore, FargowiltasSouls.FargowiltasSouls.TextureBuffer.Gore, "Gores/Gore_");
        }

        protected static void LoadGoreRange(bool recolor, int type, int lastType)
        {
            for (int i = type; i <= lastType; i++)
                LoadGore(recolor, i);
        }

        protected static void LoadExtra(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.Extra, FargowiltasSouls.FargowiltasSouls.TextureBuffer.Extra, "Extra_");
        }

        protected static void LoadGolem(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.Golem, FargowiltasSouls.FargowiltasSouls.TextureBuffer.Golem, "GolemLights");
        }

        protected static void LoadDest(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.Dest, FargowiltasSouls.FargowiltasSouls.TextureBuffer.Dest, "Dest");
        }
        protected static void LoadGlowMask(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.GlowMask, FargowiltasSouls.FargowiltasSouls.TextureBuffer.GlowMask, "Glow_");
        }
        protected static void LoadProjectile(bool recolor, int type)
        {
            LoadSpriteBuffered(recolor, type, TextureAssets.Projectile, FargowiltasSouls.FargowiltasSouls.TextureBuffer.Projectile, "Projectile_");
        }
        #endregion
        */
    }
    
}
