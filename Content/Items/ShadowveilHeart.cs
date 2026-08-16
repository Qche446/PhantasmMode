using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items
{
    public class ShadowveilHeart : SoulsItem
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        private static Matrix GetCurrentMatrix(SpriteBatch sb)
        {
            if (PhanUtil._sbMatrixField != null)
                return (Matrix)PhanUtil._sbMatrixField.GetValue(sb);
            return Main.UIScaleMatrix; // 兜底
        }
        public override void SetStaticDefaults()
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.ItemNoGravity[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.accessory = true;
            Item.rare = ItemRarityID.Gray;
            Item.value = Item.sellPrice(0, 0, 70, 0);
        }
        public static float GrazeCap => 0.25f;
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var fp = player.FargoSouls();
            var Itemplayer = player.GetModPlayer<ShadowveilHeartPlayer>();
            player.buffImmune[BuffID.BrokenArmor] = true;
            AddGrazeSkill(player);
            if (player.AddEffect<ShadowveilGrazeEffect>(Item))
            {
                Itemplayer.VeilGraze = true;
            }
        }
        private void AddGrazeSkill(Player player)
        {
            var fp = player.FargoSouls();
            fp.Graze = true;
            player.AddEffect<MasoGrazeRing>(Item);
            if (fp.Graze && player.whoAmI == Main.myPlayer && player.HasEffect<MasoGrazeRing>() && player.ownedProjectileCounts[ModContent.ProjectileType<GrazeRing>()] < 1)
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<GrazeRing>(), 0, 0f, Main.myPlayer);
        }
        public override void UpdateVanity(Player player)
        {
            AddGrazeSkill(player);
        }
        public static void OnGraze(Player player)
        {
            //Main.NewText(player.FargoSouls().DeviGraze);
            var vp = player.GetModPlayer<ShadowveilHeartPlayer>();
            float grazecap = GrazeCap;
            double grazeGain = 0.01;
            if (vp.VeilGraze)
                vp.VeilGrazeBonus += grazeGain;
            if (vp.VeilGrazeBonus > grazecap)
                vp.VeilGrazeBonus = grazecap;
            vp.GrazeConsumTime = 120;
            if (player.whoAmI == Main.myPlayer)
                CooldownBarManager.Activate("ShadowveilHeart", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/SparklingAdoration").Value, Color.Purple, () => (float)(vp.VeilGrazeBonus / GrazeCap), true, 0, () => vp.VeilGraze, 11);
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Accessories/Graze") { Volume = 0.5f }, Main.LocalPlayer.Center);
            }
            Vector2 baseVel = Vector2.UnitX.RotatedByRandom(2 * Math.PI);
            const int max = 64; //make some indicator dusts
            for (int i = 0; i < max; i++)
            {
                Vector2 vector6 = baseVel * 3f;
                vector6 = vector6.RotatedBy((i - (max / 2 - 1)) * 6.28318548f / max) + Main.LocalPlayer.Center;
                Vector2 vector7 = vector6 - Main.LocalPlayer.Center;
                //changes color when bonus is maxed
                int d = Dust.NewDust(vector6 + vector7, 0, 0, vp.VeilGrazeBonus >= grazecap ? DustID.Shadowflame : DustID.GemSapphire, 0f, 0f, 0, default);
                Main.dust[d].scale = vp.VeilGrazeBonus >= grazecap ? 1f : 0.75f;
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = vector7;
            }
        }
        public override void AddRecipes()
        {
            CreateRecipe()
            .AddIngredient(ModContent.ItemType<Masochist>(), 1)
            .AddIngredient(ItemID.Shadewood, 10)
            .AddIngredient(ItemID.FallenStar, 3)
            .AddIngredient(ItemID.DemoniteBar, 5)
            .AddTile(TileID.DemonAltar)
            .Register();
        }
        private static void DrawShadowveil(SpriteBatch sb, Vector2 center, float radius, Matrix transform, Matrix transform_end)
        {
            if (Main.dedServ)
                return;
            Vector2 rectSize = new Vector2(radius * 2.8f);
            Vector2 rectTopLeft = center;
            Rectangle drawRect = new Rectangle((int)rectTopLeft.X, (int)rectTopLeft.Y, (int)rectSize.X, (int)rectSize.Y);

            ManagedShader shader = ShaderManager.GetShader("FargosPhantasmMode.ShadowveilHeartRings");
            // globalTime 由 Luminance 自动注入，无需（也不应）手动设置
            shader.TrySetParameter("screenPosition", rectTopLeft);
            shader.TrySetParameter("screenSize", rectSize);
            shader.TrySetParameter("anchorPoint", center + rectSize * 0.5f);
            shader.TrySetParameter("radius", radius);
            shader.TrySetParameter("ringCount", 3f);
            shader.TrySetParameter("spinSpeed", 1.8f);
            shader.TrySetParameter("inclination", 0.6f);
            shader.TrySetParameter("coreColor", new Vector4(0.11f, 0.05f, 0.22f, 0.95f));
            shader.TrySetParameter("ringColor", new Vector4(0.74f, 0.62f, 1f, 0.9f));
            shader.TrySetParameter("glowColor", new Vector4(0.40f, 0.25f, 0.62f, 0.7f));

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, transform);
            shader.Apply();
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel, drawRect, null, Color.White, 0f, pixel.Size() * 0.5f, SpriteEffects.None, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, transform_end);
        }

        public override bool PreDrawInInventory(SpriteBatch sb, Vector2 pos, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float radius = Math.Clamp(18f * scale, 10f, 28f);
            Matrix matrix = GetCurrentMatrix(sb);
            DrawShadowveil(sb, pos, radius, matrix, matrix);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            float radius = Math.Max(Item.width, Item.height) * 0.65f * Math.Max(scale, 0.1f);
            DrawShadowveil(spriteBatch, Item.Center - Main.screenPosition, radius, Main.GameViewMatrix.TransformationMatrix, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }
    }
    // 标记“当前帧是否处于世界绘制阶段”：世界 pass 开头（Main.Draw 构建变换矩阵时）置 true，
    // 瓦片/tile entity（物品框等）绘制完成后、UI 绘制前置 false。供 PreDrawInInventory 区分
    // 背包 UI（UIScaleMatrix）与物品框/展示假人（GameViewMatrix）两种坐标空间。
    public class ShadowveilGrazeEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<DeviEnergyHeader>();
        public override int ToggleItemType => ModContent.ItemType<ShadowveilHeart>();
    }
    public class ShadowveilHeartPlayer : ModPlayer
    {
        public bool VeilGraze = false;
        public double VeilGrazeBonus = 0;
        public int GrazeConsumTime = 120;
        public override void ResetEffects()
        {
            VeilGraze = false;
            if (VeilGrazeBonus < 0)
                VeilGrazeBonus = 0;
            if (VeilGrazeBonus > ShadowveilHeart.GrazeCap)
                VeilGrazeBonus = ShadowveilHeart.GrazeCap;
            var fp = Player.FargoSouls();
            if (fp.DeviGrazeBonus > SparklingAdoration.GrazeCap(fp))
                fp.DeviGrazeBonus = SparklingAdoration.GrazeCap(fp);
        }
        public override void PostUpdateEquips()
        {
            if (--GrazeConsumTime <= 0 || !VeilGraze)
            {
                GrazeConsumTime = 40;
                if (VeilGrazeBonus > 0)
                    VeilGrazeBonus -= 0.01;
            }
        }
        public override void OnHurt(Player.HurtInfo info)
        {
            if (VeilGrazeBonus >= 0.12)
                VeilGrazeBonus -= 0.12;
            else
                VeilGrazeBonus = 0;
        }
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (VeilGraze)
            {
                modifiers.FinalDamage *= 1 - (float)VeilGrazeBonus;
            }
        }
        public override void UpdateDead()
        {
            VeilGrazeBonus = 0;
            GrazeConsumTime = 0;
        }
    }
}
