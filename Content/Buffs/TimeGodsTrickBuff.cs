using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry;
using FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Pure;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Items.Armor;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Core.ModPlayers;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs
{
    public class TimeGodsTrickBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<TimeGodsTrickPlayer>().timetrick = true;
        }
    }
    public class TimeGodsTrickPlayer : ModPlayer
    {
        public bool timetrick = false;
        public override void ResetEffects()
        {
            timetrick = false;
        }
        public override void PostUpdateBuffs()
        {
            FargoSoulsPlayer fp = Main.LocalPlayer.FargoSouls();
            Player py = Main.LocalPlayer;
            if (timetrick)
            {
                #region 套装充能
                //猫猫套
                if (fp.NekomiAttackReadyTimer <= 0)
                {
                    fp.NekomiMeter += (int)(NekomiHood.MAX_METER / 120f);
                    fp.NekomiTimer = Math.Clamp(fp.NekomiTimer + 60, 0, 420);
                }
                //冥河
                if (fp.StyxAttackReadyTimer <= 0)
                {
                    fp.StyxMeter += 125000 * 12 / 120;
                }
                #endregion
                //expert
                #region Maso饰品充能
                fp.AbomWandCD--;//手杖憎恶召唤
                fp.AgitatingLensCD++;//鲜血镰刀
                if (fp.SpecialDashCD > 3)
                    fp.SpecialDashCD -= 2;//特殊冲刺
                fp.AdditionalAttacksTimer--;//天界符文
                fp.DarkenedHeartCD--;//暗黑之心
                ModContent.GetInstance<FusedLensMechElectricOrbEffect>().Timer++;//熔融晶状体
                ModContent.GetInstance<GuttedHeartAura>().Timer++;//破碎心
                fp.CirnoGrazeCounter += IceQueensCrown.CIRNO_GRAZE_MAX / 120;//冰皇冠
                fp.NymphsPerfumeCD -= fp.MasochistSoul ? 10 : 1;//宁府香水
                if (fp.shieldCD > 2)
                    fp.shieldCD--;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ModContent.ProjectileType<CrystalSkull>())
                    {
                        if (Main.projectile[i].localAI[0] > 0)
                            Main.projectile[i].localAI[0] += 2;
                    }
                    if (Main.projectile[i].type == ModContent.ProjectileType<PungentEyeballMinion>())
                    {
                        Main.projectile[i].localAI[0] += 2;
                    }
                }//水晶头骨血肉团
                fp.DeviGrazeBonus += SparklingAdoration.GrazeCap(fp) / 120f;//闪心
                fp.DeviGrazeCounter = -60;
                fp.WretchedPouchCD += 2;//诅咒袋子
                fp.WyvernBallsCD++;//飞龙之羽
                #endregion
                #region 魔石充能
                fp.AdamantiteSpread += 1;//精金
                //远古神圣
                fp.AncientShadowFlameCooldown--;//远古暗影触手
                fp.ApprenticeItemCD--;//学徒
                fp.AshwoodCD--;//灰烬木火球
                fp.BeeCD--;//蜜蜂魔石花朵
                py.beetleCounter++;//甲虫魔石
                fp.BorealCD--;//针叶木雪球
                fp.CactusProcCD--;//仙人掌尖刺
                if (fp.CactusProcCD < 0)
                    fp.CactusProcCD = 0;
                //叶绿
                fp.CobaltJumpCooldown--;//钴蓝跳跃增强cd
                fp.CopperProcCD--;//铜闪电
                if (py.HasBuff(ModContent.BuffType<CrimsonRegenBuff>()))//猩红
                    fp.CrimsonRegenTime += 1;
                //水晶刺客
                //暗黑艺术炮台
                if (fp.EbonwoodCharge < (fp.ForceEffect<EbonwoodEnchant>() ? 500 : 250))
                    fp.EbonwoodCharge += fp.ForceEffect<EbonwoodEnchant>() ? 2 : 1;//乌木魔石充能
                //禁戒风（等2.0将召唤风与是否存在风独立再改）
                if (py.HasBuff(ModContent.BuffType<FossilReviveCDBuff>()))
                {
                    for (int i = 0; i < py.buffType.Length; i++)
                    {
                        if (py.buffType[i] == ModContent.BuffType<FossilReviveCDBuff>())
                        {
                            py.buffTime[i]--;
                        }
                    } 
                }//化石冷却
                //冰霜魔石（雪魔石处修改）
                fp.GladiatorStandardCD--;//角斗士长矛
                if (py.HasBuff(ModContent.BuffType<GoldenStasisCDBuff>()))
                {
                    for (int i = 0; i < py.buffType.Length; i++)
                    {
                        if (py.buffType[i] == ModContent.BuffType<GoldenStasisCDBuff>())
                        {
                            py.buffTime[i]--;
                        }
                    }
                }//金身冷却
                //神圣魔石（不知道能改什么。。。）
                fp.HuntressMissCD--;//女猎人叠层
                if (fp.HuntressMissCD < 0)
                    fp.HuntressMissCD = 0;
                //铁减伤时长不变
                //丛林魔石（能改啥）
                //铅魔石
                fp.MeteorCD--;//流星陨石
                //熔岩矿工武僧魔石
                fp.MythrilTimer += (float)fp.MythrilMaxTime / 250f;//秘银
                fp.NebulaEnchCD--;//星云射击
                fp.ObsidianCD--;//黑曜石
                if(fp.ObsidianCD < 0)
                    fp.ObsidianCD = 0;
                //山铜
                fp.PalladCounter++;//钯金球
                fp.PalmWoodForceCD--;//棕榈木
                //珍珠木无
                //粉色爱斯基摩（滚木魔石gun）
                //铂金
                fp.PumpkinSpawnCD--;//南瓜魔石（何意为）
                if (py.HasBuff(ModContent.BuffType<RainCDBuff>()))
                {
                    for (int i = 0; i < py.buffType.Length; i++)
                    {
                        if (py.buffType[i] == ModContent.BuffType<RainCDBuff>())
                        {
                            py.buffTime[i]--;
                        }
                    }
                }//雨伞CD，给伞修耐久有点复杂不做了
                fp.RedRidingArrowCD--;//红色骑术箭雨CD
                if (fp.RedRidingArrowCD < 0)
                    fp.RedRidingArrowCD = 0;
                //红木
                fp.ShadewoodCD--;//阴影木喷血
                if (fp.ShadewoodCD < 0)
                    fp.ShadewoodCD = 0;
                fp.ShadowOrbRespawnTimer--;//（远古）暗影球
                //渗透忍者
                fp.ShroomiteCD--;//蘑菇
                //银
                fp.icicleCD -= 2 ;//雪球（冰锥）
                fp.SolarEnchCharge += 1.5f;//耀斑
                //蜘蛛幽魂
                //阴森镰刀冷却麻烦。。
                if (py.HasBuff(ModContent.BuffType<TimeStopCDBuff>()))
                {
                    for (int i = 0; i < py.buffType.Length; i++)
                    {
                        if (py.buffType[i] == ModContent.BuffType<TimeStopCDBuff>())
                        {
                            py.buffTime[i]--;
                        }
                    }
                }//星辰时停cd
                //提基
                fp.TinProcCD--;//锡暴击
                //钛金？
                fp.TungstenCD--;//钨冲击波
                if (fp.TungstenCD < 0)
                    fp.TungstenCD = 0;
                if (py.HasBuff(ModContent.BuffType<BrokenShellBuff>()))
                {
                    for (int i = 0; i < py.buffType.Length; i++)
                    {
                        if (py.buffType[i] == ModContent.BuffType<BrokenShellBuff>())
                        {
                            py.buffTime[i]--;
                        }
                    }
                }//龟壳cd
                //英灵骑士
                fp.VortexCD--;//星璇
                #endregion
            }
        }
    }
}
