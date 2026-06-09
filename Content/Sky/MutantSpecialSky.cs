using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Sky
{
    public class MutantSpecialSky : CustomSky
    {
        private bool isActive = false;
        private float intensity = 0f;
        private float lifeIntensity = 0f;
        private float specialColorLerp = 0f;
        private Color? specialColor = null;
        private int delay = 0;
        private readonly int[] xPos = new int[50];
        private readonly int[] yPos = new int[50];

        // 冰面裂缝相关
        private float crack1Alpha = 0f;  // 第一道裂缝透明度
        private float crack2Alpha = 0f;  // 第二道裂缝透明度
        private bool crack1Shown = false;
        private bool crack2Shown = false;

        // 纹理缓存（只加载一次）
        private static Texture2D iceBackgroundTex;
        private static Texture2D crack1Tex;
        private static Texture2D crack2Tex;
        private static bool texturesLoaded = false;

        private static void LoadTextures()
        {
            if (texturesLoaded) return;
            texturesLoaded = true;

            iceBackgroundTex = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/IceBackground",
                ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            crack1Tex = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/Crack1",
                ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            crack2Tex = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/Crack2",
                ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Update(GameTime gameTime)
        {
            const float increment = 0.01f;

            bool useSpecialColor = false;

            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>())
                && (Main.npc[EModeGlobalNPC.mutantBoss].ai[0] < 0 || Main.npc[EModeGlobalNPC.mutantBoss].ai[0] >= 10))
            {
                NPC mutant = Main.npc[EModeGlobalNPC.mutantBoss];
                intensity += increment;
                lifeIntensity = mutant.ai[0] < 0 ? 1f : 1f - (float)mutant.life / mutant.lifeMax;

                // === 裂缝逻辑：根据 Boss 血量比例 ===
                float lifeRatio = (float)mutant.life / mutant.lifeMax;

                // 65% 血量：显示第一道裂缝（缓慢渐入）
                if (lifeRatio <= 0.65f && !crack1Shown)
                {
                    crack1Shown = true;
                }
                if (crack1Shown && crack1Alpha < 1f)
                {
                    crack1Alpha += 0.01f; // 缓慢出现
                    if (crack1Alpha > 1f)
                        crack1Alpha = 1f;
                }

                // 33.3% 血量：显示第二道裂缝
                if (lifeRatio <= 0.333f && !crack2Shown)
                {
                    crack2Shown = true;
                }
                if (crack2Shown && crack2Alpha < 1f)
                {
                    crack2Alpha += 0.01f;
                    if (crack2Alpha > 1f)
                        crack2Alpha = 1f;
                }

                void ChangeColorIfDefault(Color color)
                {
                    if (specialColor == null)
                        specialColor = color;
                    if (specialColor != null && specialColor == color)
                        useSpecialColor = true;
                }

                switch ((int)mutant.ai[0])
                {
                    case -5:
                        if (mutant.ai[2] >= 420)
                            ChangeColorIfDefault(FargoSoulsUtil.AprilFools ? new Color(255, 180, 50) : Color.Cyan);
                        break;
                    case 10:
                        useSpecialColor = true;
                        specialColor = Color.Black;
                        specialColorLerp = 1f;
                        break;
                    case 19:
                        ChangeColorIfDefault(Color.Gray);
                        break;
                    case 27:
                        ChangeColorIfDefault(Color.Red);
                        break;
                    case 28:
                        ChangeColorIfDefault(Color.Gold);
                        break;
                    case 36:
                        if (WorldSavingSystem.MasochistModeReal && mutant.ai[2] > 180 * 3 - 60)
                            ChangeColorIfDefault(Color.Blue);
                        break;
                    case 44:
                        ChangeColorIfDefault(Color.DeepPink);
                        break;
                    case 48:
                        ChangeColorIfDefault(Color.Purple);
                        break;
                    case 49:
                        ChangeColorIfDefault(Color.Black);
                        break;
                    case 50:
                        if (mutant.ai[1] < (WorldSavingSystem.MasochistModeReal ? 60 : 90))
                            ChangeColorIfDefault(Color.OrangeRed);
                        else
                            ChangeColorIfDefault(new Color(32, 247, 32));
                        break;
                    default:
                        break;
                }

                if (intensity > 1f)
                    intensity = 1f;
            }
            else
            {
                lifeIntensity -= increment;
                if (lifeIntensity < 0f)
                    lifeIntensity = 0f;

                specialColorLerp -= increment * 2;
                if (specialColorLerp < 0)
                    specialColorLerp = 0;

                intensity -= increment;
                if (intensity < 0f)
                {
                    intensity = 0f;
                    lifeIntensity = 0f;
                    specialColorLerp = 0f;
                    specialColor = null;
                    delay = 0;
                    crack1Alpha = 0f;
                    crack2Alpha = 0f;
                    crack1Shown = false;
                    crack2Shown = false;
                    Deactivate();
                    return;
                }
            }

            if (useSpecialColor)
            {
                specialColorLerp += increment * 2;
                if (specialColorLerp > 1)
                    specialColorLerp = 1;
            }
            else
            {
                specialColorLerp -= increment * 2;
                if (specialColorLerp < 0)
                {
                    specialColorLerp = 0;
                    specialColor = null;
                }
            }
            
        }

        private Color ColorToUse(ref float opacity)
        {
            // 冰面颜色改为淡蓝/白色调
            Color color = Color.Lerp(new Color(180, 220, 255), new Color(220, 240, 255), 0.5f);
            opacity = intensity * 0.7f + lifeIntensity * 0.3f;

            if (specialColorLerp > 0 && specialColor != null)
            {
                color = Color.Lerp(color, (Color)specialColor, specialColorLerp);
                if (specialColor == Color.Black)
                    opacity = Math.Min(1f, opacity + Math.Min(intensity, lifeIntensity) * 0.5f);
            }

            return color;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                LoadTextures();

                float opacity = 0.8f;
                Color color = ColorToUse(ref opacity);

                // ========== 1. 绘制冰面背景 ==========
                spriteBatch.Draw(iceBackgroundTex,
                    new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    color * opacity);

                // ========== 2. 绘制裂缝 ==========
                // 第一道裂缝（66.7%血量出现）
                if (crack1Alpha > 0f && crack1Tex != null)
                {
                    spriteBatch.Draw(crack1Tex,
                        new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                        Color.White * crack1Alpha * opacity);
                }

                // 第二道裂缝（33.3%血量出现）
                if (crack2Alpha > 0f && crack2Tex != null)
                {
                    spriteBatch.Draw(crack2Tex,
                        new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                        Color.White * crack2Alpha * opacity);
                }

                // ========== 3. 保留原静态噪点（可选，但可以削弱或去掉） ==========
                // 如果你想让画面更干净，可以注释掉下面的噪点部分
                if (--delay < 0)
                {
                    delay = Main.rand.Next(5 + (int)(85f * (1f - lifeIntensity)));
                    for (int i = 0; i < 50; i++)
                    {
                        xPos[i] = Main.rand.Next(Main.screenWidth);
                        yPos[i] = Main.rand.Next(Main.screenHeight);
                    }
                }

                for (int i = 0; i < 50; i++)
                {
                    int width = Main.rand.Next(3, 251);
                    spriteBatch.Draw(ModContent.Request<Texture2D>("FargowiltasSouls/Content/Sky/MutantStatic",
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value,
                    new Rectangle(xPos[i] - width / 2, yPos[i], width, 3),
                    color * lifeIntensity * 0.75f * 0.3f); // 削弱噪点强度
                }
            }
        }

        public override float GetCloudAlpha()
        {
            return 1f - intensity;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
            crack1Alpha = 0f;
            crack2Alpha = 0f;
            crack1Shown = false;
            crack2Shown = false;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
            crack1Alpha = 0f;
            crack2Alpha = 0f;
            crack1Shown = false;
            crack2Shown = false;
        }

        public override bool IsActive()
        {
            return isActive;
        }

        public override Color OnTileColor(Color inColor)
        {
            float dummy = 0f;
            Color skyColor = Color.Lerp(Color.White, ColorToUse(ref dummy), 0.5f);
            return Color.Lerp(skyColor, inColor, 1f - intensity);
        }
    }
}