using ReLogic.Content;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Common.VisualEquipType
{
    public class RibbonDrawLayer : PlayerDrawLayer
    {
        public override bool IsHeadLayer => true;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.GetModPlayer<VisualEquipPlayer>().ribbonChanged;
        }

        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.Head);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            VisualEquipPlayer modPlayer = drawPlayer.GetModPlayer<VisualEquipPlayer>();

            if (modPlayer.ribbonTarget is not null && !modPlayer.ribbonTarget.IsDisposed)
            {
                DrawData ribbon = new(modPlayer.ribbonTarget, modPlayer.ribbonTargetOrigin - Main.screenPosition, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None)
                {
                    shader = modPlayer.ribbonShader
                };
                drawInfo.DrawDataCache.Add(ribbon);
            }
            /*
            Asset<Texture2D> texture = ModContent.Request<Texture2D>("FargosPhantasmMode/Common/VisualEquipType/WhiteRibbon");
            Vector2 drawPosition = RibbonPos(drawPlayer);

            Rectangle drawFrame = drawPlayer.bodyFrame;
            drawFrame.Y = 0;
            DrawData knot = new DrawData(texture.Value, drawPosition - Main.screenPosition, drawFrame, drawInfo.colorArmorHead, drawPlayer.headRotation, drawInfo.headVect, 0.45f, drawInfo.playerEffect)
            {
                shader = modPlayer.ribbonShader
            };
            drawInfo.DrawDataCache.Add(knot);
            */
        }

        public static Vector2 RibbonPos(Player drawPlayer)
        {
            return drawPlayer.Center + Main.OffsetsPlayerHeadgear[drawPlayer.bodyFrame.Y / drawPlayer.bodyFrame.Height] + new Vector2(-10 * drawPlayer.direction, -18f);
        }
    }
}
