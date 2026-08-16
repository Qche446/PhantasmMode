// ShadowveilHeartRings.fx
// 程序化（无贴图）物品图标 shader：深紫黑 3D 球体 + 3 条相互独立的三维倾斜环带（土星风格）。
// 三条环带各自拥有不同的旋转轴（轴方位 az0 与倾角 inc 均不同），并且：
//   - 旋转轴各自以不同方向、不同速度进动（内环 + 、中环 − 、外环 +）；
//   - 环带沿各自轴向的旋转方向不同（亮弧与扭曲凸起以各自 spinDir 公转）；
//   - 环带半径不再是正圆：带有随环面角 theta 移动的局部扭曲凸起 + 轻微涟漪。
// 前弧（yr<0）越过球体，后弧（yr>0）被球盘遮挡。像素级 ps_3_0。
// 参数为普通全局变量，由 C# 经 ManagedShader.TrySetParameter 注入。
float globalTime;        // 动画时钟 —— Luminance 自动注入 Main.GlobalTimeWrappedHourly，无需 C# 设置
float2 screenPosition;   // 绘制矩形左上角
float2 screenSize;       // 绘制矩形尺寸
float2 anchorPoint;      // 效果中心
float radius;            // 参考半径（像素），球/环按此缩放
float ringCount;         // 保留（C# 仍传入；环带现在无条件绘制）
float spinSpeed;         // 公转角速度（弧度/秒）
float inclination;       // 环面基准倾角（弧度，0 = 正侧立 / edge-on）
float4 coreColor;        // 球体颜色 + 不透明度
float4 ringColor;        // 环颜色 + 不透明度
float4 glowColor;        // 光晕颜色 + 强度

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 worldUV = screenPosition + screenSize * uv;
    float2 p = worldUV - anchorPoint;
    float r = length(p);

    float safeRadius = max(radius, 0.001);
    float nr = r / safeRadius;

    // 每条环带独立累加“后弧/前弧”的预乘贡献（front 因轴不同而各异，不能先求和再分前后）
    float3 backPremulSum = 0;
    float  backASum = 0;
    float3 frontPremulSum = 0;
    float  frontASum = 0;

    // ================= 内环带：轴方位 0.6rad、略侧立；轴正向进动；环正向旋转；凸起 0.15 =================
    {
        float azSpeed = spinSpeed * 0.06;                  // 轴进动速度（慢）
        float az = 0.6 + azSpeed * globalTime;             // + ：轴向一个方向进动
        float ca = cos(az); float sa = sin(az);
        float xr =  p.x * ca + p.y * sa;
        float yr = -p.x * sa + p.y * ca;
        float front = step(0.0, -yr);

        float spinDir = 1.0;                               // 环内旋转方向（亮弧与凸起随行）
        float inc = inclination - 0.18 + 0.10 * sin(globalTime * spinSpeed * 0.3);
        float sinInc = clamp(sin(inc), 0.30, 1.0);         // 防除零；下限 0.30 保证环带不会 edge-on 坍缩
        float u = yr / sinInc;                             // 环面内（被倾角压缩的）轴坐标
        float R = sqrt(xr * xr + u * u);                   // 反投影回环面半径
        float theta = atan2(u, xr);                        // 环面内角度（公转相位）

        float bandC = 0.55, bandW = 0.05;
        float warpAng = theta + spinDir * globalTime * spinSpeed * 0.5;   // 扭曲凸起随环公转
        warpAng = atan2(sin(warpAng), cos(warpAng));       // 包回 [-π, π]
        float warp = 0.18 * exp(-warpAng * warpAng / 0.18) + 0.03 * cos(theta * 3.0 + globalTime * spinSpeed * 0.6);
        float Rtarget = safeRadius * bandC * (1.0 + warp); // 非正圆：带一处移动凸起的环面半径
        float t = (R - Rtarget) / (safeRadius * bandW);
        float band = exp(-t * t);                          // 高斯环带
        float bright = 0.6 + 0.4 * sin(theta * 2.0 + spinDir * globalTime * spinSpeed * 1.3) + warp * 0.5;
        float ringA = band * bright * ringColor.a;

        backPremulSum  += ringColor.rgb * ringA * (1.0 - front);
        backASum       += ringA * (1.0 - front);
        frontPremulSum += ringColor.rgb * ringA * front;
        frontASum      += ringA * front;
    }

    // ================= 中环带：轴方位 2.8rad、基准倾角；轴反向进动；环反向旋转；凸起 0.13 =================
    {
        float azSpeed = spinSpeed * 0.07;
        float az = 2.8 - azSpeed * globalTime;             // − ：进动方向相反
        float ca = cos(az); float sa = sin(az);
        float xr =  p.x * ca + p.y * sa;
        float yr = -p.x * sa + p.y * ca;
        float front = step(0.0, -yr);

        float spinDir = -1.0;
        float inc = inclination + 0.09 * sin(globalTime * spinSpeed * 0.3 + 2.1);
        float sinInc = clamp(sin(inc), 0.30, 1.0);
        float u = yr / sinInc;
        float R = sqrt(xr * xr + u * u);
        float theta = atan2(u, xr);

        float bandC = 0.80, bandW = 0.06;
        float warpAng = theta + spinDir * globalTime * spinSpeed * 0.55 + 2.1;
        warpAng = atan2(sin(warpAng), cos(warpAng));
        float warp = 0.17 * exp(-warpAng * warpAng / 0.18) + 0.03 * cos(theta * 3.0 + globalTime * spinSpeed * 0.8 + 1.3);
        float Rtarget = safeRadius * bandC * (1.0 + warp);
        float t = (R - Rtarget) / (safeRadius * bandW);
        float band = exp(-t * t);
        float bright = 0.6 + 0.4 * sin(theta * 2.0 + spinDir * globalTime * spinSpeed * 1.5 + 1.1) + warp * 0.5;
        float ringA = band * bright * ringColor.a * 0.92;

        backPremulSum  += ringColor.rgb * ringA * (1.0 - front);
        backASum       += ringA * (1.0 - front);
        frontPremulSum += ringColor.rgb * ringA * front;
        frontASum      += ringA * front;
    }

    // ================= 外环带：轴方位 4.6rad、更面向观察者；轴正向进动（更快）；环反向旋转；凸起 0.11 =================
    {
        float azSpeed = spinSpeed * 0.09;
        float az = 4.6 + azSpeed * globalTime;             // + ：进动方向
        float ca = cos(az); float sa = sin(az);
        float xr =  p.x * ca + p.y * sa;
        float yr = -p.x * sa + p.y * ca;
        float front = step(0.0, -yr);

        float spinDir = -1.0;
        float inc = inclination + 0.22 + 0.08 * sin(globalTime * spinSpeed * 0.3 + 4.2);
        float sinInc = clamp(sin(inc), 0.30, 1.0);
        float u = yr / sinInc;
        float R = sqrt(xr * xr + u * u);
        float theta = atan2(u, xr);

        float bandC = 1.10, bandW = 0.07;
        float warpAng = theta + spinDir * globalTime * spinSpeed * 0.60 + 4.2;
        warpAng = atan2(sin(warpAng), cos(warpAng));
        float warp = 0.16 * exp(-warpAng * warpAng / 0.18) + 0.03 * cos(theta * 3.0 + globalTime * spinSpeed * 1.0 + 2.6);
        float Rtarget = safeRadius * bandC * (1.0 + warp);
        float t = (R - Rtarget) / (safeRadius * bandW);
        float band = exp(-t * t);
        float bright = 0.6 + 0.4 * sin(theta * 2.0 + spinDir * globalTime * spinSpeed * 1.7 + 2.2) + warp * 0.5;
        float ringA = band * bright * ringColor.a * 0.84;

        backPremulSum  += ringColor.rgb * ringA * (1.0 - front);
        backASum       += ringA * (1.0 - front);
        frontPremulSum += ringColor.rgb * ringA * front;
        frontASum      += ringA * front;
    }

    // ================= 实心球体（假 3D 光照，与现版本一致） =================
    float coreR = safeRadius * 0.30;
    float edgeW = safeRadius * 0.04;
    float coreMask = 1.0 - smoothstep(coreR - edgeW, coreR + edgeW, r);   // 球盘覆盖（带软边）

    float2 d = p / max(coreR, 0.001);
    float dd = dot(d, d);
    float z = sqrt(max(1.0 - dd, 0.0));                                   // 球高，用于边缘压暗
    float2 lightDir = normalize(float2(-0.55, -0.55));                    // 屏幕 +y 向下，光源左上
    float diff = max(dot(d, lightDir), 0.0);
    diff = smoothstep(0.0, 0.65, diff) * (0.35 + 0.65 * z);
    float rim = pow(max(dot(d, -lightDir), 0.0), 2.0) * 0.5;              // 右下边缘反光
    float bloom = exp(-dd * 2.5) * 0.6;                                   // 中心内发光
    float3 N = normalize(float3(d, z));
    float3 L = normalize(float3(-0.62, -0.62, 0.55));                     // 光源略偏向观察者
    float3 H = normalize(L + float3(0.0, 0.0, 1.0));
    float spec = pow(max(dot(N, H), 0.0), 24.0) * 0.9;                    // 光泽高光

    float3 coreCol = coreColor.rgb * (0.55 + 0.9 * diff)
                   + float3(0.12, 0.07, 0.20) * rim
                   + float3(0.14, 0.10, 0.22) * bloom
                   + float3(0.92, 0.86, 1.0) * spec;
    float coreA = coreColor.a * coreMask;

    // ================= 从后到前预乘合成（后弧 < 不透明球 < 前弧） =================
    float3 backPremul  = backPremulSum * (1.0 - coreMask);
    float  backA       = backASum * (1.0 - coreMask);
    float3 frontPremul = frontPremulSum;
    float  frontA      = frontASum;

    float3 premul = 0;
    float accA = 0;

    // 第 0 层：环境光晕（最底）
    {
        float halo = glowColor.a * exp(-nr * nr * 1.5);
        float haloA = saturate(halo * 1.2);
        float3 haloCol = glowColor.rgb * (halo * 3.0 + 0.05);
        premul = haloCol * haloA + premul * (1.0 - haloA);
        accA = haloA + accA * (1.0 - haloA);
    }
    // 第 1 层：三条环的后弧（被球盘遮挡）
    premul = backPremul + premul * (1.0 - backA);
    accA = backA + accA * (1.0 - backA);
    // 第 2 层：不透明球体
    premul = coreCol * coreA + premul * (1.0 - coreA);
    accA = coreA + accA * (1.0 - coreA);
    // 第 3 层：三条环的前弧（覆盖球体）
    premul = frontPremul + premul * (1.0 - frontA);
    accA = frontA + accA * (1.0 - frontA);

    return float4(premul, saturate(accA));               // 预乘输出，配合 BlendState.AlphaBlend
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
