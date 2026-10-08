using System.Linq;

namespace Monochrome.Common.MonoUtil
{
    public static partial class MonoUtil
    {
        /// <summary>
        /// 查找玩家是否装备某个饰品（只查找原版饰品栏，其他模组添加的额外饰品栏不会查找）。返回饰品所在饰品栏的序号
        /// </summary>
        public static int FindAccessorySlot(this Player player, params int[] ItemType)
        {
            int num = 5;
            if (Main.masterMode)
                num++;
            if (player.extraAccessory && Main.expertMode)
                num++;
            for (int i = 0; i < num; i++)
            {
                if (ItemType.Contains(player.armor[3 + i].type))
                    return i;
            }
            return -1;
        }
    }
}
