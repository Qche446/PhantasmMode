using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Buffs;
using FargosPhantasmMode.Content.Items.Global;
using FargowiltasSouls.Content.Items.Weapons.Challengers;
using Monochrome.Common.MonoUtil;
using Terraria.DataStructures;

namespace FargosPhantasmMode.Content.Items.Weapons
{
    public class FleshBayonet : ModItem, IPhantomRender
    {
        public const string TexturePath = "FargosPhantasmMode/Content/Items/Weapons/FleshBayonet/";
        public override string Texture => TexturePath + "FleshBayonet";

        public Color Color1 => Color.Red;

        public Color Color2 => Color.Yellow;

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 80;
            Item.height = 80;
            Item.SetWeaponValues(33, 4, 0);
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item40;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BloodBullets>();
            //Item.useAmmo = AmmoID.Bullet;
            Item.shootSpeed = 36f;
            Item.value = Item.sellPrice(0, 10, 0, 0);
            Item.GetGlobalItem<PModeGlobalItem>().Phantasm = true;
            Item.scale = 0.75f;
        }
        public override void HoldItem(Player player) => player.AddBuff(ModContent.BuffType<BulletReload>(), 2);
        public override Vector2? HoldoutOffset() => new Vector2(-20, 0);
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2)
            {
                Item.shoot = ModContent.ProjectileType<BloodBullets>();
                Item.shootSpeed = 36;
                Item.noUseGraphic = false;
                Item.UseSound = NavalRustrifle.PowerShotSound;
                var pp = player.Phantasm();
                switch (pp.BulletReloadTime)
                {
                    case 0:
                        Item.useTime = Item.useAnimation = 15;
                        break;
                    case 1:
                        Item.useTime = Item.useAnimation = 40;
                        break;
                    case 2:
                        Item.useTime = Item.useAnimation = 70;
                        break;
                    case 3:
                        Item.noUseGraphic = true;
                        Item.useTime = Item.useAnimation = 30;
                        return player.ownedProjectileCounts[ModContent.ProjectileType<FleshBayonet_Dash>()] < 1;
                        //break;
                }
                return true;
            }
            else
            {
                Item.shoot = ModContent.ProjectileType<FleshBayonet_Reload>();
                Item.UseSound = SoundID.Item1;
                Item.shootSpeed = 5;
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.noUseGraphic = true;
            }
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var pp = player.Phantasm();
            if (player.altFunctionUse == 2)
            {
                SoundEngine.PlaySound(SoundID.Unlock with { Pitch = 0f }, player.Center);
                Projectile.NewProjectileDirect(source, position, velocity, Item.shoot, 0, knockback, player.whoAmI, player.whoAmI);
                player.AddBuff(ModContent.BuffType<PrayerofTaintedBlood>(), 5 * 60);
                pp.BulletReloadTime++;
                if (pp.BulletReloadTime > 3)
                    pp.BulletReloadTime = 0;
                return false;
            }
            bool hyper = pp.PrayerofTaintedBlood;
            float length = (Main.MouseWorld - player.Center).Length() / 600f;
            switch (pp.BulletReloadTime)
            {
                case 0:
                    {
                        ShootBloodBullet(1, 0.6f, Main.MouseWorld + Main.rand.NextVector2Unit() * length * Main.rand.NextFloat(0, 20), BloodBullets.Single, Main.rand.Next(3, 8));
                        if (hyper)
                        {
                            ShootBloodBullet(1, 0.6f, Main.MouseWorld + Main.rand.NextVector2Unit() * length * Main.rand.NextFloat(0, 20), BloodBullets.Single, Main.rand.Next(3, 8));
                        }
                    }
                    break;
                case 1:
                    {
                        ShootBloodBullet(2f, 1.6f, Main.MouseWorld, BloodBullets.Throughout, Main.rand.Next(7, 12));
                        if (hyper)
                        {
                            ShootBloodBullet(2f, 1.6f, Main.MouseWorld + Main.rand.NextVector2Unit() * length * Main.rand.NextFloat(0, 10), BloodBullets.Throughout, Main.rand.Next(7, 12));
                        }
                    }
                    break;
                case 2:
                    {
                        int max = Main.rand.Next(12, 15);
                        if (hyper)
                            max = (int)(max * 2f);
                        for (int i = 0; i < max; i++)
                        {
                            ShootBloodBullet(1, 0.4f, Main.MouseWorld + Main.rand.NextVector2Unit() * Main.rand.NextFloat(0, 200), BloodBullets.Diffusion, Main.rand.Next(1, 3));
                        }
                    }
                    break;
                case 3:
                    {
                        Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<FleshBayonet_Dash>(), (int)(damage * (hyper ? 8 : 4)), knockback, player.whoAmI, ai2: hyper ? 1 : 0);
                    }
                    break;
            };
            Projectile ShootBloodBullet(float speedmul, float damagemul, Vector2 targetPos, float MoveType, int crush)
            {
                float speed = velocity.Length() * speedmul;
                var p = Projectile.NewProjectileDirect(source, position, speed * position.SafeDirectionTo(targetPos), Item.shoot, (int)(damage * damagemul), knockback, player.whoAmI, targetPos.X, targetPos.Y, MoveType);
                if (p.active && p != null)
                    p.Phantasm().CaseCrush += crush;
                return p;
            }
            return false;
        }
        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            var texture = ModContent.Request<Texture2D>(Texture).Value;
            drawData.texture = texture;
            drawData.sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
            drawData.origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            float aimRotation = (drawData.rotation);
            if (drawInfo.drawPlayer.direction > 0)
            {
                drawData.position += new Vector2(26, 0).RotatedBy(drawData.rotation);
                drawData.rotation = aimRotation + MathHelper.PiOver4 * 5;
                drawData.effect = SpriteEffects.FlipVertically;
            }
            else
            {
                drawData.position -= new Vector2(26, 0).RotatedBy(drawData.rotation);
                drawData.rotation = aimRotation - MathHelper.PiOver4;
                drawData.effect = SpriteEffects.None;
            }
            return true;
        }
    }
}
