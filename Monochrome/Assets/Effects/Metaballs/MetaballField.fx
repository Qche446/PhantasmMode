// 融合场累积着色器：把一颗球的影响力写进半分辨率目标（纯加法混合）。
//
// 顶点契约（与 MonoPrimitiveVertex 一致）：
//   Coord.x = 角度参数 t
//   Coord.y = 0 圆心 → 1 边缘
//   Coord.z = 半径（像素）
//
// 关键性质：Coord.y 在扇形三角形上**线性插值**，因此三角形内部恒有 Y = d / r
// （d 为到圆心的像素距离）。于是 (1 - Y) 与 CPU 侧 MonoMetaball.Influence 的线性衰减
// 完全同式——power = 1 时，GPU 累积出来的场在数值上等于 MonoMetaballManager.Sample()，
// 于是"GPU 画得不对"可以直接用 CPU 采样去对照定位。
//
// 本 effect 由 MonoPrimitiveRenderer.RenderCircle（扇形）驱动，每个球一次绘制。
// 每次绘制前由 C# 侧写入 uStrength；uInfluencePower 是全局参数写一次即可。

sampler uTexture : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float uStrength;         // 单颗球的影响力缩放（逐球写入）
float uInfluencePower;   // 影响力手感：1 = 线性（Influence1）、2/3 = 更黏

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.Coord = input.Coord;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float t = saturate(1.0 - input.Coord.y);
    float influence = pow(t, max(uInfluencePower, 0.0001)) * uStrength;

    // 预乘输出：A 累积 Σinfluence，RGB 累积 Σ(color * influence)，
    // 合成阶段再用 RGB / A 还原平均颜色。
    //
    // 累积目标必须配 (One, One) 的纯加法混合，**不能用 BlendState.Additive**：
    // FNA 的 Additive 是 (SourceAlpha, SourceAlpha, One, One)，
    // 会把这里已经乘过的影响力再乘一次 alpha，结果变成影响力的平方。
    return float4(input.Color.rgb * influence, influence);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
