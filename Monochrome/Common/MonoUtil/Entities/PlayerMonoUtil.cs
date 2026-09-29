using System.Linq;

namespace Monochrome.Common.MonoUtil
{
    public static partial class MonoUtil
    {
        /// <summary>
        /// 检查player是否有某个buff集合中的buff
        /// </summary>
        /// <returns>out一个索引,如果没有buff则out-1。这个索引是这几个buff按顺序的索引，不是player.BuffType</returns>
        public static bool HasBuffList(this Player player, out int FirstBuffIndex, params int[] buffList)
        {
            for (int i = 0; i < buffList.Length; i++)
            {
                if (player.HasBuff(buffList[i]))
                {
                    FirstBuffIndex = i;
                    return true;
                }
            }
            FirstBuffIndex = -1;
            return false;
        }
        /// <summary>
        /// 获取某个buff类型在玩家buff数组里的索引。若没有这个buff，则返回-1
        /// </summary>
        public static int FindBuffIndex(this Player py, int type)
        {
            for (int i = 0; i < py.buffType.Length; i++)
            {
                if (py.buffType[i] == type)
                {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// 获取玩家身上某个buff的持续时间。无buff则返回-1
        /// </summary>
        public static int BuffTime(this Player player, int type)
        {
            int index = player.FindBuffIndex(type);
            if (index < 0)
                return -1;
            return player.buffTime[index];
        }
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
