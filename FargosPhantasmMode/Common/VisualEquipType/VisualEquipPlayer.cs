using Monochrome.Common.MonoUtil.Physics;
using Monochrome.Core.Physics;

namespace FargosPhantasmMode.Common.VisualEquipType
{
    public class VisualEquipPlayer : ModPlayer
    {
        public bool ribbon;
        public bool ribbonChanged;

        public int ribbonShader;
        public MonoRope ribbonRope;
        public RenderTarget2D ribbonTarget;
        public Vector2 ribbonTargetOrigin;

        public override void UpdateVisibleVanityAccessories()
        {
            base.UpdateVisibleVanityAccessories();
        }

        public override void ResetEffects()
        {
            if (!ribbonChanged && ribbonRope is not null)
            {
                MonoPhysicsSystem.Unregister(ribbonRope);
                ribbonRope = null;
                ribbonTarget?.Dispose();
                ribbonTarget = null;
            }

            ribbon = ribbonChanged;
            ribbonChanged = false;
            ribbonShader = 0;
        }
    }
}
