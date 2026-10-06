// Monochrome 屏幕后处理模板：Vignette（暗角）。
//
// 屏幕 pass 的着色器契约（全部屏幕特效都按这个来）：
//   1. 顶点输入沿用图元顶点布局（见 StandardPrimitive.fx），UV 直接取 TextureCoordinates.xy：
//      全屏四边形上它是 0→1 的归一化屏幕坐标。
//   2. 输入贴图绑在**采样槽 1**（s1）。槽 0 留给 SpriteBatch，所以自定义图元一律从 1 开始。
//   3. uWorldViewProjection / globalTime / screenSize 由渲染路径自动写入，不要自己再声明别的变换。
//   4. **淡出统一走 fxOpacity**，不要把结果直接乘进去就返回：
//        return float4(lerp(原始, 结果, saturate(fxOpacity)), 原始.a);
//      这样每一条特效都天然支持淡入淡出，消费者不必去动强度参数。
//      （名字不叫 opacity 是有原因的：图元渲染路径每帧都会把 opacity 写成 1，见 README §5.13。）

sampler uSource : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float fxOpacity;

// 0 = 关；1 = 四角完全变成 vignetteColor。
float vignetteStrength;
// 过渡宽度：越小边角压得越硬。C# 侧钳到 [0.01, 1]。
float vignetteSoftness;
// 四角压到的颜色。通常接近黑。
float3 vignetteColor;

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

    // 椭圆暗角：把 [-1,1] 的方形坐标归一化，使**角落**恰好落在 r = 1。
    float2 d = (uv - 0.5) * 2.0;
    float r = length(d) * 0.70710678;

    float softness = max(vignetteSoftness, 0.01);
    float mask = smoothstep(1.0 - softness, 1.0, r) * saturate(vignetteStrength);

    float3 graded = lerp(original.rgb, vignetteColor, mask);
    return float4(lerp(original.rgb, graded, saturate(fxOpacity)), original.a);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
