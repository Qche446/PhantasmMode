// Monochrome 图元默认着色器。
//
// 顶点契约（与 MonoPrimitiveVertex.Declaration 一致）：
//   TextureCoordinates.x = 沿路径的长度 / 角度参数 t，取值 [0,1]
//   TextureCoordinates.y = 归一化截面坐标，取值 [0,1]，0.5 恒为截面中线
//   TextureCoordinates.z = 截面 0→1 所跨越的像素距离
//
// 顶点输入签名逐字沿用 Luminance 的 StandardPrimitiveShader.fx：两边的顶点声明
// (0:Vector2/Position, 8:Color/Color, 12:Vector3/TextureCoordinate) 逐字段相同，
// 所以签名不需要试错。
//
// 参数：
//   useTexture (默认 0) : 0 = 只用顶点色（线 / 圆 / 拖尾）；1 = 再乘 s1 上的贴图（RenderQuad）
//   debugMode  (默认 0) : >=0.5 时输出契约可视化，用来肉眼验证上面那三个分量
//
// 本文件必须编译成**完整效果**（fxc /T fx_2_0），不能只编译像素着色器：
// tML 的 FxcReader 走的是 new Effect(device, bytes)，喂裸 PS 字节码会在解析阶段失败。

sampler uTexture : register(s1);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;
float useTexture;
float debugMode;

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
    // 原样传出：截面展开放在像素着色器里，debugMode 才看得到未加工的契约值。
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 coord = input.TextureCoordinates;

    // 截面展开：把归一化截面映射到贴图的截面位置，coord.z 是这一整段的像素跨度。
    float2 uv = float2(coord.x, (coord.y - 0.5) / coord.z + 0.5);

    // ps_2_0 没有动态分支，所以三态一律用 lerp/saturate 表达。
    // 未绑定采样器时 D3D9 返回常量色（不是 NaN），因此系数为 0 的那一项不会污染结果。
    float4 solid = input.Color;
    float4 textured = tex2D(uTexture, uv) * input.Color;
    float4 result = lerp(solid, textured, saturate(useTexture));

    // 契约可视化：R = 截面（应沿法线 0→1 线性变化），G = 像素跨度 / 64，B = 长度参数
    float4 debug = float4(coord.y, saturate(coord.z / 64.0), coord.x, 1.0);
    return lerp(result, debug, step(0.5, debugMode));
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
