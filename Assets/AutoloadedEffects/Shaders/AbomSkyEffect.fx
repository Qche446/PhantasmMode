sampler noise : register(s1);

float maxOpacity;
float time;
float verticalPower; // 控制竖直渐变曲线，值越大，上部越集中
float2 screenPosition;
float2 screenSize;
float2 anchorPoint; // 保留但不再使用
float2 playerPosition; // 未使用

// 三层噪声的流动方向，由外部每帧随机传入
float2 flowDir1;
float2 flowDir2;
float2 flowDir3;

float InverseLerp(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
}
float2 fracX(float2 value)
{
    float2 result;
    float2 floorValue = floor(value);
    if (floorValue.x % 2 == 0)
    {
        result.x = value.x - floorValue.x;
    }
    else
    {
        result.x = 1 - (value.x - floorValue.x);
    }
    
    if (floorValue.y % 2 == 0)
    {
        result.y = value.y - floorValue.y;
    }
    else
    {
        
        result.y = 1 - (value.y - floorValue.y);
    }
    
    return result;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 worldUV = screenPosition + screenSize * uv;

    float verticalGradient = saturate((worldUV.y - screenPosition.y) / screenSize.y);
    float colorMult = pow(verticalGradient, verticalPower);

    // ---- 像素化（0.5 像素步长，保留原有细马赛克风格） ----
    float2 pixelatedUV = worldUV / screenSize;
    pixelatedUV.x -= worldUV.x % (0.5 / screenSize.x);
    pixelatedUV.y -= worldUV.y % (0.5 / (screenSize.y / 2) * 2);

    float2 noiseUV = pixelatedUV/* - (screenPosition / screenSize) * 0.99*/;

    // ---- 三层独立流动噪声 ----
    // 每层采样一次，方向由 flowDir 控制，速度可通过系数微调
    float n1 = tex2D(noise, fracX(noiseUV * 1.47 + flowDir1 * time * 0.003)).r;
    float n2 = tex2D(noise, fracX(noiseUV * 1.57 + flowDir2 * time * 0.003)).r;
    float n3 = tex2D(noise, fracX(noiseUV * 1.37 + flowDir3 * time * 0.003)).r;
    if (n1 < 0.3)
        n1 = 0.3;
    if (n2 < 0.3)
        n2 = 0.3;
    if (n3 < 0.3)
        n3 = 0.3;
    // 各层颜色（沿用原来的色调）
    float4 color1 = float4(0.7, 0, 1.0, 1.0); // 紫
    float4 color2 = float4(1.0, 0.5, 0.1, 1.0); // 黄色
    float4 color3 = float4(1, 0.4, 0, 1.0); // 红

    // 混合
    float4 textureMesh = (n1 * color1 + n2 * color2 + n3 * color3) * 0.7 * 1.5;

    // ---- 最终合成 ----
    return textureMesh * maxOpacity * colorMult * 0.7;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}