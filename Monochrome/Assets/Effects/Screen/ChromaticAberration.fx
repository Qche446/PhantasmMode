// Monochrome 屏幕后处理：ChromaticAberration（径向色差）。
// 契约见 Vignette.fx 顶部。

sampler uSource : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float fxOpacity;

// 半径处的 UV 偏移量。0.004 约等于屏幕宽度的 0.4%，肉眼刚好能看出彩边。
float aberrationStrength;
// 0 = 全屏均匀偏移；1 = 只有边角才有色差（更自然）。
float aberrationFalloff;

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
    float4 mid = tex2D(uSource, uv);

    float2 dir = uv - 0.5;
    float r = length(dir) * 1.41421356;
    float amount = aberrationStrength * lerp(1.0, r, saturate(aberrationFalloff));
    float2 offset = dir * amount;

    float4 graded;
    graded.r = tex2D(uSource, uv + offset).r;
    graded.g = mid.g;
    graded.b = tex2D(uSource, uv - offset).b;
    graded.a = mid.a;

    return float4(lerp(mid.rgb, graded.rgb, saturate(fxOpacity)), mid.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
