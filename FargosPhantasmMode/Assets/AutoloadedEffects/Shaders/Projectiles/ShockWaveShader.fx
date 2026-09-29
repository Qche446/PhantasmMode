sampler uImage1 : register(s1);

float globalTime;
float2 screenPosition;
float2 screenSize;
float3 color1;
float3 color2;
float maxOpacity;
float2 Center;
float Radius;
float FadedWidth;

float2 PolarFloat2(float R, float Angle)
{
    return float2(R * cos(Angle), R * sin(Angle));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 worldUV = screenPosition + screenSize * uv;
    float2 pixelatedUV = worldUV / screenSize;
    float2 noiseUV = pixelatedUV - (Center / screenSize);
    float r = distance(Center, worldUV);
    float angle = atan2(worldUV.y - Center.y, worldUV.x - Center.x);
    float4 noiseColor1 = tex2D(uImage1, PolarFloat2(r, angle + 2 * globalTime));
    float4 noiseColor2 = tex2D(uImage1, PolarFloat2(r + 6 * globalTime, angle));
    float fadedprog = 1;
    if (r - Radius > 0)
        fadedprog = clamp((r - Radius) / FadedWidth, 0, 1);
    else
        fadedprog = clamp((Radius - r) / FadedWidth, 0, 1);
    float opacity = maxOpacity * (1 - fadedprog);
    float3 color = lerp(color1, color2, 0.5 + 0.5 * sin(0.5 * globalTime));
    return float4(color, 1) * opacity * noiseColor1 * noiseColor2 * 3;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}