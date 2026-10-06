// Monochrome 屏幕后处理：Blur（可分离高斯模糊的一个方向）。
//
// **一条模糊链要两个实例**：横向一次、纵向一次，而且必须相邻（见 MonoBlurPass.CreateChain）。
// 只跑一个方向不是模糊，是拖影。
//
// 5 抽头二项式权重 (1,4,6,4,1)/16：一次只能扩到约 ±2·radius，
// 想要更宽就得叠更多 pass（每多一次翻倍量级的代价）。刻意不做循环：
// ps_2_0 不允许对常量数组做动态索引，展开写反而更省心也更快。
//
// 契约见 Vignette.fx 顶部。

sampler uSource : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float fxOpacity;

// 方向：横向用 (1,0)、纵向用 (0,1)。
float2 blurDirection;
// 单步半径（像素）。总模糊半径约为 2 * blurRadius。
float blurRadius;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates.xy;
    float4 original = tex2D(uSource, uv);

    // 在着色器里除 screenSize：窗口尺寸一变就自动跟上，C# 侧不需要每帧重算。
    // 用 _TexelSize 之类的常量缓冲不行——那是 Unity 的概念，这里只有我们自己写的参数。
    float2 step = blurDirection * blurRadius / max(screenSize, float2(1.0, 1.0));

    float4 sum = original * 0.375;
    sum += (tex2D(uSource, uv - step) + tex2D(uSource, uv + step)) * 0.25;
    sum += (tex2D(uSource, uv - step * 2.0) + tex2D(uSource, uv + step * 2.0)) * 0.0625;

    return float4(lerp(original.rgb, sum.rgb, saturate(fxOpacity)), original.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
