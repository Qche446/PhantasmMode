// ShadowveilHeart.fx
// 纯程序化（无贴图）物品图标 shader：深紫黑球体 + 2~3 条淡紫扭曲光环绕转 + 柔和光晕。
// 像素级，ps_3_0。参数为普通全局变量，由 C# 经 ManagedShader.TrySetParameter 注入。
float globalTime;        // 动画时钟 —— Luminance 自动注入 Main.GlobalTimeWrappedHourly，无需 C# 设置
float2 screenPosition;   // 绘制矩形左上角
float2 screenSize;       // 绘制矩形尺寸
float2 anchorPoint;      // 效果中心
float radius;            // 参考半径（像素），球/环按此缩放
float ringCount;         // 2 或 3
float spinSpeed;         // 公转角速度（弧度/秒）
float ringWobble;        // 环半径扭曲幅度（0.12 = 12%）
float4 coreColor;        // 球体颜色 + 不透明度
float4 ringColor;        // 环颜色 + 不透明度
float4 glowColor;        // 光晕颜色 + 强度

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 worldUV = screenPosition + screenSize * uv;
    float2 p = worldUV - anchorPoint;
    float r = length(p);
    float ang = atan2(p.y, p.x);

    float safeRadius = max(radius, 0.001);
    float nr = r / safeRadius;

    // 从后到前做预乘合成（不透明球体最后叠加，正确遮住经过球心的环）。
    float3 premul = 0;
    float accA = 0;

    // ---- 第 0 层：环境光晕（最底） ----
    {
        float halo = glowColor.a * exp(-nr * nr * 1.5);
        float haloA = saturate(halo * 1.2);
        float3 haloCol = glowColor.rgb * (halo * 3.0 + 0.05);
        premul = haloCol * haloA + premul * (1.0 - haloA);
        accA = haloA + accA * (1.0 - haloA);
    }

    // ---- 第 1 层：2~3 条淡紫扭曲光环 ----
    {
        float ringA = 0;

        if (ringCount > 0)  // 内环
        {
            float baseR = safeRadius * 0.55;
            float effectiveR = baseR * (1.0 + ringWobble * sin(ang * 3.0 + globalTime * spinSpeed * 1.00));
            float ringWidth = safeRadius * 0.05;
            float d2 = (r - effectiveR) * (r - effectiveR);
            float band = exp(-d2 / (ringWidth * ringWidth));
            float bright = 0.55 + 0.45 * cos(ang - globalTime * spinSpeed * 1.6);   // 旋转亮段，体现方向
            ringA += band * bright * ringColor.a;
        }
        if (ringCount > 1)  // 中环
        {
            float baseR = safeRadius * 0.83;
            float effectiveR = baseR * (1.0 + ringWobble * sin(ang * 4.0 + globalTime * spinSpeed * 1.35 + 1.7));
            float ringWidth = safeRadius * 0.062;
            float d2 = (r - effectiveR) * (r - effectiveR);
            float band = exp(-d2 / (ringWidth * ringWidth));
            float bright = 0.55 + 0.45 * cos(ang - globalTime * spinSpeed * 2.0 - 1.1);
            ringA += band * bright * ringColor.a * 0.92;
        }
        if (ringCount > 2)  // 外环
        {
            float baseR = safeRadius * 1.11;
            float effectiveR = baseR * (1.0 + ringWobble * sin(ang * 5.0 + globalTime * spinSpeed * 1.70 + 3.4));
            float ringWidth = safeRadius * 0.074;
            float d2 = (r - effectiveR) * (r - effectiveR);
            float band = exp(-d2 / (ringWidth * ringWidth));
            float bright = 0.55 + 0.45 * cos(ang - globalTime * spinSpeed * 2.4 - 2.2);
            ringA += band * bright * ringColor.a * 0.84;
        }

        // 预乘合成：ringCol 已是 color*alpha，over 时不可再乘 alpha（否则 alpha 平方、环偏暗）。
        float3 ringCol = ringColor.rgb * ringA;
        premul = ringCol + premul * (1.0 - ringA);
        accA = ringA + accA * (1.0 - ringA);
    }

    // ---- 第 2 层：实心球体（假 3D 光照，最前，遮挡环） ----
    {
        float coreR = safeRadius * 0.30;
        float edgeW = safeRadius * 0.04;
        float coreMask = 1.0 - smoothstep(coreR - edgeW, coreR + edgeW, r);

        float2 d = p / max(coreR, 0.001);
        float dd = dot(d, d);
        float z = sqrt(max(1.0 - dd, 0.0));              // 球高，用于边缘压暗
        float2 lightDir = normalize(float2(-0.55, -0.55)); // 屏幕 +y 向下，光源左上
        float diff = max(dot(d, lightDir), 0.0);
        diff = smoothstep(0.0, 0.65, diff) * (0.35 + 0.65 * z);
        float rim = pow(max(dot(d, -lightDir), 0.0), 2.0) * 0.45; // 右下边缘反光
        float bloom = exp(-dd * 2.5) * 0.55;              // 中心内发光

        float3 coreCol = coreColor.rgb * (0.55 + 0.9 * diff)
                       + float3(0.10, 0.06, 0.17) * rim
                       + float3(0.13, 0.09, 0.20) * bloom;
        float coreA = coreColor.a * coreMask;

        premul = coreCol * coreA + premul * (1.0 - coreA);
        accA = coreA + accA * (1.0 - coreA);
    }

    return float4(premul, saturate(accA));  // 预乘输出，配合 BlendState.AlphaBlend
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
