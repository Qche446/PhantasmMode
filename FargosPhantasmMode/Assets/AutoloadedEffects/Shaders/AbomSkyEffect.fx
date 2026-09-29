sampler noise : register(s1);

float maxOpacity;
float time;
float verticalPower; 
float2 screenPosition;
float2 screenSize;
float2 anchorPoint;
float2 playerPosition; 

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

    float2 pixelatedUV = worldUV / screenSize;
    pixelatedUV.x -= worldUV.x % (0.5 / screenSize.x);
    pixelatedUV.y -= worldUV.y % (0.5 / (screenSize.y / 2) * 2);

    float2 noiseUV = pixelatedUV;

    float n1 = tex2D(noise, fracX(noiseUV * 1.47 + flowDir1 * time * 0.003)).r;
    float n2 = tex2D(noise, fracX(noiseUV * 1.57 + flowDir2 * time * 0.003)).r;
    float n3 = tex2D(noise, fracX(noiseUV * 1.37 + flowDir3 * time * 0.003)).r;
    if (n1 < 0.3)
        n1 = 0.3;
    if (n2 < 0.3)
        n2 = 0.3;
    if (n3 < 0.3)
        n3 = 0.3;
    float4 color1 = float4(0.7, 0, 1.0, 1.0); // ×Ï
    float4 color2 = float4(1.0, 0.5, 0.1, 1.0); // »ÆÉ«
    float4 color3 = float4(1, 0.4, 0, 1.0); // ºì

    float4 textureMesh = (n1 * color1 + n2 * color2 + n3 * color3) * 0.7 * 1.5;

    return textureMesh * maxOpacity * colorMult * 0.7;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}