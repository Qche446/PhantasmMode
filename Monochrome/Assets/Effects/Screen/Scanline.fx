// Monochrome 屏幕后处理：Scanline（扫描线 / CRT）。
// 契约见 Vignette.fx 顶部。

sampler uSource : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float fxOpacity;

// 暗线强度 0..1。
float scanlineStrength;
// 屏幕里有多少条暗线。屏幕高度的一半左右（例如 540）接近真实 CRT。
float scanlineCount;
// 滚动速度（条/秒）。0 = 静止。
float scanlineSpeed;

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

    // uv.y * count 个周期，每个周期 π 弧度 → 正好 count 条暗线。
    float phase = uv.y * scanlineCount * 3.14159265 + globalTime * scanlineSpeed * 3.14159265;
    // 0.5 - 0.5*cos ∈ [0,1]，用 smoothstep 感的余弦而不是硬阈值，缩放时不会闪烁。
    float mask = 1.0 - scanlineStrength * (0.5 - 0.5 * cos(phase));

    return float4(lerp(original.rgb, original.rgb * mask, saturate(fxOpacity)), original.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
