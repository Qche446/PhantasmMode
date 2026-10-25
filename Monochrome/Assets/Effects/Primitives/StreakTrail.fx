// Monochrome 拖尾着色器：沿条带滚动一张条纹贴图，让拖尾看起来像流动的光。
//
// 顶点契约与 StandardPrimitive.fx 相同（与 MonoPrimitiveVertex.Declaration 一致）：
//   TextureCoordinates.x = 沿路径的长度参数 t，取值 [0,1]，0 为头、1 为尾（与 Luminance 的 completionRatio 相同）
//   TextureCoordinates.y = 截面坐标，已按半宽偏移：两侧是 0.5 ∓ 半宽/2，中线是 0.5
//   TextureCoordinates.z = 截面半宽（像素）
// 像素着色器先用 (y - 0.5) / z + 0.5 把截面还原成 [0,1]，之后的取样与淡出都基于这个值。
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

    // 还原归一化截面：0 / 1 是条带两侧、0.5 是中线。
    float section = (coord.y - 0.5) / max(coord.z, 0.0001) + 0.5;

    // 本效果自己按"0 为尾"排版，而顶点给的 X 是"0 为头"，所以先翻一次；
    // 之后长度、滚动、两端淡出都用 along，与顶点契约换成 lumi 方向之前完全一样。
    float along = 1.0 - coord.x;

    // 贴图取样：X 沿长度滚动，Y 铺满整个截面。
    float2 uv = float2(frac(along * uScrollScale - globalTime * uScrollSpeed), section * uSectionTiles);
    float streak = tex2D(uImage1, uv).r;

    // 截面两侧的淡出（贴图自己的剖面之外再加一层保险）。uEdgeFade = 0 时除数兜底让 saturate 恒为 1，
    // 于是这一项自动变成"不淡出"——ps_2_0 没有动态分支，所以用 max 兜而不是写 if。
    float edge = 0.5 - abs(section - 0.5);
    float edgeFade = pow(saturate(edge / max(uEdgeFade, 0.0001)), 2.0);

    // 尾端一小段、头端一小段淡出——只是为了让两头不出现硬切边，不是拿来调形状的。
    float tailFade = pow(saturate(along / 0.05), 2.0);
    float headSpan = max(1.0 - uHeadFadeStart, 0.0001);
    float headFade = pow(saturate(1.0 - (along - uHeadFadeStart) / headSpan), 2.0);

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
