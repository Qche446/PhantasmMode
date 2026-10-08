using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Physics;

namespace FargosPhantasmMode.Common.VisualEquipType
{
    public class VisualEquipLoader : ModSystem
    {
        public const int RibbonTargetSize = 124;
        private const float AnchorRatio = 5f / 15f;
        private static readonly MonoPrimitiveSettings RibbonSettings = new(
            t => 2.5f + 1.2f * Math.Abs(t - AnchorRatio) / (1f - AnchorRatio),
            _ => new(255, 255, 255, 200));

        public override void ResizeArrays()
        {
        }
        public override void PostDrawTiles()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player is null || !player.active)
                {
                    continue;
                }

                VisualEquipPlayer modPlayer = player.GetModPlayer<VisualEquipPlayer>();
                if (modPlayer.ribbonRope is null || !modPlayer.ribbonChanged)
                {
                    continue;
                }

                RenderRibbon(modPlayer);
            }
        }
        private static void RenderRibbon(VisualEquipPlayer modPlayer)
        {
            RenderTarget2D target = EnsureTarget(modPlayer);
            modPlayer.ribbonTargetOrigin = BoundsCenter(modPlayer.ribbonRope.Points) - new Vector2(RibbonTargetSize * 0.5f);

            GraphicsDevice device = Main.instance.GraphicsDevice;
            RenderTargetBinding[] previous = device.GetRenderTargets();
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            MonoRopeRenderer.DrawToTarget(modPlayer.ribbonRope, target, modPlayer.ribbonTargetOrigin, RibbonSettings);
            device.SetRenderTargets(previous);
        }
        private static RenderTarget2D EnsureTarget(VisualEquipPlayer modPlayer)
        {
            RenderTarget2D target = modPlayer.ribbonTarget;
            if (target is null || target.IsDisposed)
            {
                target = new RenderTarget2D(Main.instance.GraphicsDevice, RibbonTargetSize, RibbonTargetSize, false, SurfaceFormat.Color, DepthFormat.None);
                modPlayer.ribbonTarget = target;
            }

            return target;
        }
        private static Vector2 BoundsCenter(Vector2[] points)
        {
            Vector2 min = points[0];
            Vector2 max = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return (min + max) * 0.5f;
        }
    }
}
