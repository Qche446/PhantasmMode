// Monochrome 屏幕后处理：Bloom 的亮部提取（阈值 + 强度预乘）。
//
// 只做第一步：把超过阈值的亮度挑出来、乘上 strength，输出到半分辨率的中间目标。
// 模糊交给 Monochrome.Blur（复用同一份 5 抽头），合成交给调用方的加性绘制，
// 所以这个 pass 自己不做 lerp(fxOpacity)——它不直接上台面，淡出由 Strength 表达。
//
// 输出 alpha 恒为 0：加性混合时它不改变目标 alpha。
//
// 基本契约（采样槽 1、UV 取 TextureCoordinates.xy、顶点着色器逐字沿用图元写法）
// 见 Vignette.fx 顶部。

sampler uSource : register(s1);

matrix uWorldViewProjection;
float threshold;
float strength;

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
    float3 color = tex2D(uSource, input.TextureCoordinates.xy).rgb;

    // Rec.601 亮度权重：与"看起来有多亮"吻合得够好，且不需要查表。
    float luma = dot(color, float3(0.299, 0.587, 0.114));

    // 只把超出阈值的部分留在颜色里：亮部保留自己的色相，暗部权重为 0。
    // threshold = 0 时权重恒为 1，也就是整屏发光——那是参数用错，不是彩蛋。
    float weight = max(luma - threshold, 0.0) / max(luma, 0.0001);

    return float4(color * weight * strength, 0.0);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
