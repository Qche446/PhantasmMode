using Monochrome.Common.MonoUtil.Physics;
using Monochrome.Core.Physics;

namespace FargosPhantasmMode.Common.VisualEquipType
{
    public class WhiteRibbon : ModItem
    {
        private const int SegmentCount = 21;
        private const float SegmentLength = 2.5f;
        private const int AnchorIndex = 7;

        public override void SetDefaults()
        {
            Item.DefaultToAccessory(16, 16);
            Item.rare = ItemRarityID.White;
            Item.value = Item.sellPrice(0, 5);
            Item.maxStack = 1;
            Item.vanity = true;
        }
        public override void UpdateVisibleAccessory(Player player, bool hideVisual)
        {
            if (hideVisual)
            {
                return;
            }
            VisualEquipPlayer modPlayer = player.GetModPlayer<VisualEquipPlayer>();
            Vector2 anchor = RibbonDrawLayer.RibbonPos(player);
            if (modPlayer.ribbonRope is null)
            {
                if (Main.dedServ)
                {
                    return;
                }

                modPlayer.ribbonRope = new MonoRope(SegmentCount, SegmentLength, anchor, anchorIndex: AnchorIndex)
                {
                    Damping = 0.94f,
                    Gravity = new Vector2(0f, 0.3f),
                    TeleportThreshold = 24
                };
                MonoPhysicsSystem.Register(modPlayer.ribbonRope);
            }

            modPlayer.ribbonRope.SetAnchor(anchor);
            player.fullRotation = 0;
            player.fullRotationOrigin = player.Center - player.position;
            modPlayer.ribbonChanged = true;
        }

        public override void UpdateItemDye(Player player, int dye, bool hideVisual)
        {
            if (!hideVisual)
            {
                player.GetModPlayer<VisualEquipPlayer>().ribbonShader = dye;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Silk, 5).AddIngredient(ItemID.Cloud, 2).AddTile(TileID.Loom).Register();
        }
    }
}
