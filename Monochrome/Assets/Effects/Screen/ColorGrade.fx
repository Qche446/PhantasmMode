// Monochrome 屏幕后处理：ColorGrade（色调 / 饱和度 / 对比度 / 亮度）。
// 契约见 Vignette.fx 顶部。

sampler uSource : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float fxOpacity;

// 乘性色调。白 (1,1,1) = 不变；暖调可用 (1.0, 0.92, 0.82)。
float3 gradeTint;
// 1 = 不变；0 = 灰度；>1 = 加饱和。
float gradeSaturation;
// 1 = 不变；围绕 0.5 中灰拉伸。
float gradeContrast;
// 直接加上去的偏移，0 = 不变。
float gradeBrightness;

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

    float3 c = original.rgb * gradeTint;

    // Rec.601 亮度权重：比 (r+g+b)/3 更符合人眼，黑白转换不会让红蓝变成同一种灰。
    float luma = dot(c, float3(0.299, 0.587, 0.114));
    c = lerp(luma.xxx, c, gradeSaturation);

    c = (c - 0.5) * gradeContrast + 0.5 + gradeBrightness;
    c = saturate(c);

    return float4(lerp(original.rgb, c, saturate(fxOpacity)), original.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
