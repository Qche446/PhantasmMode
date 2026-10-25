using FargosPhantasmMode.Common;
using Monochrome.Content.Worms;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EaterOfWorlds;
internal static class EowChain
{
    //检查这段的前后连接，断开时当场转换或收场。
    public static bool UpdateLinks(NPC npc, in MonoWormLayout layout)
    {
        if (!PhanUtil.HostCheck)
            return false;

        if (npc.type == NPCID.EaterofWorldsHead)
        {
            // ai[0] == 0 是建链窗口，这时还没有后节，不能判死
            if (npc.ai[0] == 0)
                return false;

            if (MonoWormNav.ValidateBehind(npc, layout, out _))
                return false;

            Kill(npc);
            return true;
        }

        if (npc.type == NPCID.EaterofWorldsTail)
        {
            if (MonoWormNav.ValidateAhead(npc, layout, out _))
                return false;

            Kill(npc);
            return true;
        }

        bool ahead = MonoWormNav.ValidateAhead(npc, layout, out _);
        bool behind = MonoWormNav.ValidateBehind(npc, layout, out _);

        if (!ahead && !behind)
        {
            Kill(npc);
            return true;
        }

        if (!ahead)
        {
            // tML 1.4.4 的 Transform 只有一个参数，ai 槽要自己接回去
            float behindIndex = npc.ai[0];
            npc.Transform(NPCID.EaterofWorldsHead);
            npc.ai[0] = behindIndex;
            EowPhase.AddNFollowTime(60); // 多一个头，蓄力阶段提前 1 秒
            npc.netUpdate = true; // 新的共享计时跟着这只头下发
            return true;
        }

        if (!behind)
        {
            float aheadIndex = npc.ai[1];
            npc.Transform(NPCID.EaterofWorldsTail);
            npc.ai[0] = 0f;
            npc.ai[1] = aheadIndex;
            npc.netUpdate = true;
            return true;
        }

        return false;
    }
    public static void Follow(NPC npc)
    {
        int aheadIndex = (int)npc.ai[1];
        if (aheadIndex <= 0 || aheadIndex >= Main.maxNPCs)
            return;

        NPC ahead = Main.npc[aheadIndex];
        if (!ahead.active)
            return;

        Vector2 offset = ahead.Center - npc.Center;
        float distance = offset.Length();
        if (distance < 1f)
            return; // 两段完全重合，方向未知，等下一帧

        npc.rotation = offset.ToRotation() + MathHelper.PiOver2;
        npc.position += offset * (1f - npc.width / distance);
        npc.velocity = Vector2.Zero;
    }
    public static void ManageAlpha(NPC npc)
    {
        if (npc.alpha > 0 && npc.life > 0)
        {
            for (int num75 = 0; num75 < 2; num75++)
            {
                int num76 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, DustID.Demonite, 0f, 0f, 100, default, 2f);
                Main.dust[num76].noGravity = true;
                Main.dust[num76].noLight = true;
            }
        }
        npc.alpha -= 42;
        if (npc.alpha < 0)
        {
            npc.alpha = 0;
        }
    }
    private static void Kill(NPC npc)
    {
        npc.life = 0;
        npc.HitEffect();
        npc.checkDead();
        npc.active = false;

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.DamageNPC, -1, -1, null, npc.whoAmI, -1f);
    }
}
