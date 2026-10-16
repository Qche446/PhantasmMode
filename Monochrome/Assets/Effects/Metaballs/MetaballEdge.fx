// 融合球合成着色器：把累积层按"融合体轮廓 + 一层滚动材质"画出来。
//
// 由 SpriteBatch 驱动（正在绘制的层目标就是 SpriteBatch 的源贴图）：
//   s0 = 融合场（累积分出来的层目标，SpriteBatch 自动绑定）
//   s1 = 层材质贴图（由 MonoMetaballType.PrepareShaderForTarget 绑上来）
//   coords = 0..1 的屏幕 UV
//
// 做法是从 Luminance 的 MetaballEdgeShader 搬过来的：不看阈值，而是看"这颗像素的四邻有没有空像素"——
// 四邻都非空就是在融合体内部（铺材质），只要有一侧是空的就说明自己在轮廓上（涂 edgeColor）。
// 材质按 (coords + layerOffset) 采样，于是花纹会随镜头视差滚动，而不是钉在屏幕上。
//
// 与 Luminance 原版的两点差异：
//   1) 原版是 ps_3_0，这里降到 ps_2_0（与 Monochrome 其余着色器一致，tML 的 FxcReader 走 fx_2_0）；
//   2) 原版还吃一个 singleFrameScreenOffset（补偿"累积与合成不在同一帧同一个镜头"），
//      我们的累积与合成都在同一帧、同一个 Main.screenPosition 下完成，那个补偿恒为零，所以没搬。

sampler metaballContents : register(s0);
sampler overlayTexture : register(s1);

float2 screenSize;    // 当前屏幕尺寸（像素）
float2 layerSize;     // 这一层材质贴图的尺寸（像素）
float2 layerOffset;   // 这一层材质的滚动偏移（0..1 的屏幕比例）
float4 edgeColor;     // 轮廓色

float2 convertToScreenCoords(float2 coords)
{
    return coords * screenSize;
}

float2 convertFromScreenCoords(float2 coords)
{
    return coords / screenSize;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseColor = tex2D(metaballContents, coords);

    // 空像素上让四邻判定全部失败：这样"没有内容的区域"不会被误判成轮廓。
    float alphaOffset = (1 - any(baseColor.a));

    float left = tex2D(metaballContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(-2, 0))).a + alphaOffset;
    float right = tex2D(metaballContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(2, 0))).a + alphaOffset;
    float top = tex2D(metaballContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(0, -2))).a + alphaOffset;
    float bottom = tex2D(metaballContents, convertFromScreenCoords(convertToScreenCoords(coords) + float2(0, 2))).a + alphaOffset;

    // 用 step 代替分支：邻域为空记 1，非空记 0。
    float conditionOpacityFactor = 1 - saturate(step(left, 0) + step(right, 0) + step(top, 0) + step(bottom, 0));

    // 材质按屏幕比例采样，所以分辨率与贴图尺寸无关；layerOffset 让它随镜头滚动。
    float4 layerColor = tex2D(overlayTexture, (coords + layerOffset) * screenSize / layerSize);
    float4 defaultColor = layerColor * baseColor * sampleColor;

    // 四邻都非空（factor = 1）画材质，否则画轮廓。
    return (defaultColor * conditionOpacityFactor) + (edgeColor * sampleColor * (1 - conditionOpacityFactor));
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
