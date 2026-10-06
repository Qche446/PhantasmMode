// Monochrome 屏幕后处理：Grain（胶片噪点）。
// 契约见 Vignette.fx 顶部。

sampler uSource : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float fxOpacity;

// 0..1 的叠加幅度。0.08 左右是"有质感但不脏"。
float grainStrength;
// 噪声格子密度。1 = 全屏一个格子（几乎看不出噪点），600 左右是细密颗粒。
float grainScale;
// 1 = 单色噪点（推荐，像胶片）；0 = 三通道独立（彩色噪点，更脏）。
float grainMonochrome;

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

// 便宜的 hash 噪声。用 floor 把坐标量化成格子，否则是连续渐变而不是颗粒。
float Hash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates.xy;
    float4 original = tex2D(uSource, uv);

    // 每帧换种子：globalTime 是秒，乘一个与屏幕尺寸无关的常数即可。
    // 不做每帧独立的随机数是因为着色器里拿不到 CPU 的随机源，而"每帧不同"才是噪点的关键。
    float seed = floor(globalTime * 30.0);
    float2 cell = floor(uv * grainScale);

    float n1 = Hash(cell + seed);
    float n2 = Hash(cell + seed + 19.19);
    float n3 = Hash(cell + seed + 7.77);

    float3 noise = lerp(float3(n1, n2, n3), n1.xxx, saturate(grainMonochrome));

    // 以 0.5 为中心 → 噪点有正有负，整体亮度不漂。
    float3 graded = original.rgb + (noise - 0.5) * 2.0 * grainStrength;

    return float4(lerp(original.rgb, saturate(graded), saturate(fxOpacity)), original.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
