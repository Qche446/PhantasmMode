using FargosPhantasmMode.Content.Items.Global;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Items.Weapons;

public class Monochrome : ModItem, IPhantomRender
{
    private const string TexturePath = "FargosPhantasmMode/Content/Items/Weapons/Monochrome/";
    public override string Texture => TexturePath + "Monochrome";

    public Color Color1 => Color.Black;

    public Color Color2 => Color.White;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 60;
        Item.SetWeaponValues(32, 5, 0);
        Item.DamageType = DamageClass.Ranged;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = 30;
        Item.noMelee = true;
        Item.UseSound = SoundID.Item11;
        Item.autoReuse = true;
        Item.shoot = ProjectileID.Bullet;
        Item.shootSpeed = 12f;
        Item.value = Item.sellPrice(0, 20, 0, 0);
        Item.useAmmo = AmmoID.Bullet;
        Item.GetGlobalItem<PModeGlobalItem>().Phantasm = true;
        Item.scale = 0.75f;
    }
    public override Vector2? HoldoutOffset() => new Vector2(-30, 0);
    public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 20;
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse != 2)
        {
            Item.shoot = ModContent.ProjectileType<HunterMark_Black>();
            Item.shootSpeed = 30;
            Item.useTime = 15;
            Item.useAnimation = 15;
        }
        else
        {
            Item.shoot = ModContent.ProjectileType<RendingSpiral_White>();
            Item.shootSpeed = 15;
            Item.useTime = 30;
            Item.useAnimation = 30;
        }
        return true;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI);
        return false;
    }
    public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
    {
        string texturePath;
        if (drawInfo.drawPlayer.altFunctionUse == 2)
            texturePath = TexturePath + "Monochrome_White";
        else
            texturePath = TexturePath + "Monochrome_Black";
        var texture = ModContent.Request<Texture2D>(texturePath).Value;
        drawData.texture = texture;
        drawData.sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
        drawData.origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
        drawData.scale *= 1f;
        drawData.position += new Vector2(32, 0).RotatedBy(drawData.rotation) * drawInfo.drawPlayer.direction;
        return true;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        var texture = ModContent.Request<Texture2D>(Texture).Value;
        spriteBatch.Draw(texture, Item.Center - Main.screenPosition, null, Color.White, rotation, texture.Size() * 0.5f, Item.scale, SpriteEffects.None, 0f);
        return false;
    }
}
