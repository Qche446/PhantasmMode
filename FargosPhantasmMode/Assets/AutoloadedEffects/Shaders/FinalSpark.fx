// FPM 的对比度滤镜（自 FSS 的 FinalSpark 改写，用于 Mutant 的终结阶段）。
//
// 屏幕 pass 的着色器契约（与 Monochrome 的屏幕特效一致，模板见 Assets/Effects/Screen/）：
//   1. 顶点输入沿用图元顶点布局，UV 直接取 TextureCoordinates.xy；
//   2. 输入贴图绑在采样槽 1；
//   3. uWorldViewProjection / globalTime / screenSize 由渲染路径自动写入；
//   4. 淡入淡出走 fxOpacity——这里它是唯一的强度参数，0 时结果与原始画面完全一致。

sampler uSource : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float fxOpacity;

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

    // 对比度：把每个通道推离 0.5。位移量就是 fxOpacity，所以淡出过程中画面自然回到原样。
    float amount = saturate(fxOpacity);
    float3 contrasted = original.rgb + (original.rgb - 0.5) * amount;
    return float4(contrasted, original.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
