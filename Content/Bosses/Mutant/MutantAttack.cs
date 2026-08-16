namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    /// <summary>
    /// Mutant 的 AI 状态机枚举，对应 <see cref="MutantBossOverride.MutantAI"/> 中
    /// <c>switch ((int)npc.ai[0])</c> 的各个 case 值（枚举值即 ai[0] 的数值）。
    /// 注意：因 P3（绝望阶段）使用负值 -1~-7，底层类型由 byte 改为 sbyte。
    /// </summary>
    public enum MuAt : sbyte
    {
        // ──── 阶段 1（P1） 0~9 ────
        /// <summary>0 蠕虫预判投矛</summary>
        /// 
        SpearTossDirectP1AndChecks = 0,
        /// <summary>1 P1阿空圆环</summary>
        OkuuSpheresP1 = 1,
        /// <summary>2 准备真眼俯冲</summary>
        PrepareTrueEyeDiveP1 = 2,
        /// <summary>3 真眼俯冲</summary>
        TrueEyeDive = 3,
        /// <summary>4 P1青冲预备</summary>
        PrepareSpearDashDirectP1 = 4,
        /// <summary>5 P1青冲</summary>
        SpearDashDirectP1 = 5,
        /// <summary>6 冲刺中</summary>
        WhileDashingP1 = 6,
        /// <summary>7 靠近</summary>
        ApproachForNextAttackP1 = 7,
        /// <summary>8 P1虚无射线</summary>
        VoidRaysP1 = 8,
        /// <summary>9 P1波粒+突变剑（内部按 localAI[2] 细分：0 波粒 / 1 准备剑 / 2 剑阵）</summary>
        BoundaryBulletHellAndSwordP1 = 9,

        // ──── 阶段 2（P2） 10~52 ────

        /// <summary>10 P2转换</summary>
        Phase2Transition = 10,
        /// <summary>11 P2靠近（挂机）</summary>
        ApproachForNextAttackP2 = 11,
        /// <summary>12 唐飞炸弹</summary>
        LieFlightBomb = 12,
        /// <summary>13 蓝冲预备</summary>
        PrepareSpearDashPredictiveP2 = 13,
        /// <summary>14 蓝冲</summary>
        SpearDashPredictiveP2 = 14,
        /// <summary>15 冲刺中（蓝冲后）</summary>
        WhileDashingP2 = 15,
        /// <summary>16 goto case 11：靠近（为波粒准备）</summary>
        ApproachForBulletHellP2 = 16,
        /// <summary>17 P2波粒</summary>
        BoundaryBulletHellP2 = 17,
        /// <summary>18 P2虚无射线（概率由唐飞炸弹置换）</summary>
        VoidRaysP2 = 18,
        /// <summary>19 天界柱投掷</summary>
        PillarDunk = 19,
        /// <summary>20 克苏鲁星镰</summary>
        EOCStarSickles = 20,
        /// <summary>21 青冲预备</summary>
        PrepareSpearDashDirectP2 = 21,
        /// <summary>22 青冲</summary>
        SpearDashDirectP2 = 22,
        /// <summary>23 goto case 15：青冲后冲刺中</summary>
        WhileDashingDirectP2 = 23,
        /// <summary>24 蠕虫预判投矛准备</summary>
        SpawnDestroyersForPredictiveThrow = 24,
        /// <summary>25 蠕虫预判投矛</summary>
        SpearTossPredictiveP2 = 25,
        /// <summary>26 准备机械光扇</summary>
        PrepareMechRayFan = 26,
        /// <summary>27 机械光扇</summary>
        MechRayFan = 27,
        /// <summary>28 棺材波动+意志金雷</summary>
        CoffinWave = 28,
        /// <summary>29 准备猪鲨夹击</summary>
        PrepareFishron1 = 29,
        /// <summary>30 生成猪鲨夹击</summary>
        SpawnFishrons = 30,
        /// <summary>31 准备真眼俯冲P2</summary>
        PrepareTrueEyeDiveP2 = 31,
        /// <summary>32 goto case 3：生成真眼（俯冲）</summary>
        TrueEyeDiveFollowup = 32,
        /// <summary>33 准备核弹</summary>
        PrepareNuke = 33,
        /// <summary>34 核弹</summary>
        Nuke = 34,
        /// <summary>35 准备史莱姆雨</summary>
        PrepareSlimeRain = 35,
        /// <summary>36 史莱姆雨</summary>
        SlimeRain = 36,
        /// <summary>37 准备猪鲨2</summary>
        PrepareFishron2 = 37,
        /// <summary>38 goto case 30：生成猪鲨（夹击）</summary>
        SpawnFishronsFollowup = 38,
        /// <summary>39 阿空圆环准备</summary>
        PrepareOkuuSpheresP2 = 39,
        /// <summary>40 阿空圆环</summary>
        OkuuSpheresP2 = 40,
        /// <summary>41 环绕投矛</summary>
        SpearTossDirectP2 = 41,
        /// <summary>42 准备双子水晶</summary>
        PrepareTwinRangsAndCrystals = 42,
        /// <summary>43 双子水晶</summary>
        TwinRangsAndCrystals = 43,
        /// <summary>44 女皇剑阵</summary>
        EmpressSwordWave = 44,
        /// <summary>45 准备突变剑</summary>
        PrepareMutantSword = 45,
        /// <summary>46 突变剑</summary>
        MutantSword = 46,
        /// <summary>47 goto case 35：准备史莱姆雨（连击）</summary>
        PrepareSlimeRainFollowup = 47,
        /// <summary>48 皇后史莱姆雨</summary>
        QueenSlimeRain = 48,
        /// <summary>49 鳝丝石巨人</summary>
        SANSGOLEM = 49,
        /// <summary>50 铁处女（肉山+世纪花）</summary>
        IronVirgin = 50,
        /// <summary>52 下一招前的喘息（ChooseNextAttack 的中转站）</summary>
        P2NextAttackPause = 52,

        // ──── 阶段 3（P3 / 绝望阶段） -1~-7 ────

        /// <summary>-1 P3转阶段</summary>
        Phase3Transition = -1,
        /// <summary>-2 P3虚无射线</summary>
        VoidRaysP3 = -2,
        /// <summary>-3 P3阿空圆环</summary>
        OkuuSpheresP3 = -3,
        /// <summary>-4 P3波粒</summary>
        BoundaryBulletHellP3 = -4,
        /// <summary>-5 最终火花</summary>
        FinalSpark = -5,
        /// <summary>-6 死亡剧情暂停</summary>
        DyingDramaticPause = -6,
        /// <summary>-7 死亡动画</summary>
        DyingAnimationAndHandling = -7,
    }
}
