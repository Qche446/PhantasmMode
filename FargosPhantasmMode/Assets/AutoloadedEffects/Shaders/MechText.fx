sampler uGlyph : register(s1);   // 字形掩码

matrix uWorldViewProjection;

float3 uColorA;        // 渐变第一站：红
float3 uColorB;        // 第二站：蓝
float3 uColorC;        // 第三站：绿
float3 uColorD;        // 第四站：黄
float uColorPhase;     // 四色沿横向流动的相位 0..1
float uTextLeft;       // 文字在四边形 UV 上的左边界
float uTextRight;      // 右边界
float uScrollPhase;    // 扫描相位 0..1
float2 uQuadSize;      // 四边形的像素尺寸
float2 uBandRadius;    // 扫描高光的横向 / 纵向半径（像素）
float uHighlight;      // 扫描线叠在字形上的高光强度
float uErosion;        // 边缘烧蚀强度
float uEdgeWidth;      // 字形边缘带宽度（掩码值）
float uHaloOpacity;    // 暗底（描边那圈）的不透明度

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.Coord = input.Coord;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.Coord.xy;

    // 文字在四边形上的归一化横向位置：0 = 最左、1 = 最右。
    float tx = saturate((uv.x - uTextLeft) / max(uTextRight - uTextLeft, 0.0001));

    // 红 → 蓝 → 绿 → 黄 沿横向铺满，整体随时间流动（相位推进时颜色往左走）。
    float g = frac(tx + uColorPhase) * 4.0;
    float3 c01 = lerp(uColorA, uColorB, saturate(g));
    float3 c12 = lerp(uColorB, uColorC, saturate(g - 1.0));
    float3 c23 = lerp(uColorC, uColorD, saturate(g - 2.0));
    float3 c30 = lerp(uColorD, uColorA, saturate(g - 3.0));
    float3 gradient = lerp(lerp(c01, c12, step(1.0, g)), lerp(c23, c30, step(3.0, g)), step(2.0, g));

    // 扫描线：位置在 frontX，亮度按"到扫描线的椭圆像素距离"衰减，上下左右一起淡出。
    float frontX = 1.0 - frac(uScrollPhase);
    float dx = frac(uv.x - frontX + 0.5) - 0.5;
    float2 delta = float2(dx * uQuadSize.x / max(uBandRadius.x, 1.0),
                          (uv.y - 0.5) * uQuadSize.y / max(uBandRadius.y, 1.0));
    float band = 1.0 - smoothstep(0.0, 1.0, length(delta));

    float4 texel = tex2D(uGlyph, uv);
    // 白字覆盖率：取 rgb 的亮度再与 alpha 取小，预乘与非预乘两种字形纹理都得到同一结果。
    float ink = min(max(max(texel.r, texel.g), texel.b), texel.a);
    float halo = saturate(texel.a - ink);   // 黑描边那圈

    // 高光只加在扫描线与字形重叠的地方：底色是渐变，扫到哪儿哪儿亮起来（并略微偏白）。
    float boost = band * uHighlight;
    float3 glyphColor = gradient * (1.0 + boost) + boost * 0.35;

    // 边缘烧蚀：只在字形边缘动，咬合形状用两个正弦叠出来，不需要贴图。
    float bite = 0.5 + 0.5 * sin(uv.x * 97.0 + uv.y * 61.0) * sin(uv.x * 41.0 - uv.y * 113.0);
    float edge = saturate(1.0 - ink / max(uEdgeWidth, 0.0001));
    float burn = band * bite * uErosion;
    float glyphAlpha = saturate(ink * (1.0 - burn * edge));

    // 合成：暗底 → 字形。
    float3 rgb = float3(0.02, 0.03, 0.05) * (halo * uHaloOpacity);
    float alpha = halo * uHaloOpacity;

    rgb = glyphColor * glyphAlpha + rgb * (1.0 - glyphAlpha);
    alpha = glyphAlpha + alpha * (1.0 - glyphAlpha);

    return float4(rgb, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
