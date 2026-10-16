// Monochrome 拖尾着色器：沿条带滚动一张条纹贴图，让拖尾看起来像流动的光。
//
// 顶点契约与 StandardPrimitive.fx 相同（与 MonoPrimitiveVertex.Declaration 一致）：
//   TextureCoordinates.x = 沿路径的长度参数 t，取值 [0,1]，0 为尾、1 为头
//   TextureCoordinates.y = 归一化截面坐标，取值 [0,1]，0.5 恒为截面中线
//   TextureCoordinates.z = 截面 0→1 所跨越的像素距离
// 所以它可以直接喂给 MonoPrim.RenderTrail：把 MonoPrimitiveSettings.Shader 指向本效果即可。

sampler uImage1 : register(s1);

matrix uWorldViewProjection;
float globalTime;
float uScrollSpeed = 0.35;
float uScrollScale = 1.0;
float uSectionTiles = 1.0;
float uTextureAmount = 0.35;
float uBrightness = 1.25;
float uEdgeFade = 0.25;
float uHeadFadeStart = 0.95;

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
    // 原样传出：截面展开放在像素着色器里。
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 coord = input.TextureCoordinates;

    // 贴图取样：X 沿长度滚动，Y 铺满整个截面。
    float2 uv = float2(frac(coord.x * uScrollScale - globalTime * uScrollSpeed), coord.y * uSectionTiles);
    float streak = tex2D(uImage1, uv).r;

    // 截面两侧的淡出（贴图自己的剖面之外再加一层保险）。uEdgeFade = 0 时除数兜底让 saturate 恒为 1，
    // 于是这一项自动变成"不淡出"——ps_2_0 没有动态分支，所以用 max 兜而不是写 if。
    float edge = 0.5 - abs(coord.y - 0.5);
    float edgeFade = pow(saturate(edge / max(uEdgeFade, 0.0001)), 2.0);

    // 尾端一小段、头端一小段淡出——只是为了让两头不出现硬切边，不是拿来调形状的。
    float tailFade = pow(saturate(coord.x / 0.05), 2.0);
    float headSpan = max(1.0 - uHeadFadeStart, 0.0001);
    float headFade = pow(saturate(1.0 - (coord.x - uHeadFadeStart) / headSpan), 2.0);

    // 顶点色（消费者给的 ColorFunction）是主角，贴图只做 ±uTextureAmount 的明暗调制。
    float modulate = lerp(1.0 - uTextureAmount, 1.0 + uTextureAmount, streak);
    return input.Color * edgeFade * tailFade * headFade * modulate * uBrightness;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
