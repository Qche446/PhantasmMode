// 融合场合成着色器：把半分辨率累积场按阈值切出等值面，并加一圈边缘带。
//
// 由 MonoPrimitiveRenderer.RenderQuad 驱动（整屏四边形）：
//   Coord.x = u、Coord.y = v（都是 0..1）
//   累积场贴图绑在 s1
//
// 为什么走图元路径而不是 SpriteBatch.Begin(effect)：后者要求自定义 effect 自带
// 匹配 SpriteBatch 顶点格式的顶点着色器（SpriteBatch 只会自动填一个名叫 MatrixTransform
// 的参数），而 Monochrome 里没有这类已验证的样本。图元路径的"顶点契约 + 自定义 shader +
// s1 贴图"已经在上机测试里跑通了，风险低得多。

sampler uField : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float uThreshold;    // 等值面阈值：Σinfluence 超过它算"在体内"
float uSoftness;     // 阈值过渡宽度，决定边缘软硬
float uEdgeWidth;    // 边缘带宽度
float4 uEdgeColor;   // 边缘色，其 alpha 是边缘带的不透明程度
float uOpacity;      // 整体不透明度

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
    float4 field = tex2D(uField, float2(input.Coord.x, input.Coord.y));

    float total = field.a;                              // Σ influence
    float3 baseColor = field.rgb / max(total, 0.0001);  // RGB 存的是 color*influence 的累积，除回去还原平均色

    float soft = max(uSoftness, 0.0001);
    float inside = smoothstep(uThreshold - soft, uThreshold + soft, total);
    // 再往内推一个 edgeWidth，两者相减就得到一圈贴着等值面的带。
    float inner = smoothstep(uThreshold + uEdgeWidth - soft, uThreshold + uEdgeWidth + soft, total);
    float edgeBand = saturate(inside - inner);

    float3 color = lerp(baseColor, uEdgeColor.rgb, edgeBand * uEdgeColor.a);
    float alpha = inside * uOpacity;

    // FNA 的 BlendState.AlphaBlend 是 (One, One, InverseSourceAlpha, InverseSourceAlpha)，
    // 也就是**预乘**语义，所以这里必须先把颜色乘上 alpha。
    return float4(color * alpha, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
