sampler uImage1 : register(s1);

float globalTime;
float3 color;
float2 screenPosition;
float2 screenSize;
float2 Center;
float R;
float Direct;
float AngleRange;
float Opacticy;

static const float PI = 3.14159265359;

float InverseLerp(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 worldUV = screenPosition + screenSize * uv;
    float2 fromCenter = worldUV - Center;
    float worldDistance = length(fromCenter);
    float radius = max(R, 0.0001);
    float halfAngle = max(AngleRange * 0.5, 0.0001);


    float2 direction = fromCenter / max(worldDistance, 0.0001);
    float2 arcDirection = float2(cos(Direct), sin(Direct));
    float angleDiff = acos(clamp(dot(direction, arcDirection), -1.0, 1.0));


    float2 noiseUV = worldUV / screenSize - Center / screenSize;
    float adjustedTime = globalTime * 0.6;
    float noise1 = tex2D(uImage1, frac(noiseUV * 1.46 + float2(0.56, 1.2) * adjustedTime)).g;
    float noise2 = tex2D(uImage1, frac(noiseUV * 1.57 + float2(-0.3, -0.9) * adjustedTime)).g;
    float noise3 = tex2D(uImage1, frac(noiseUV * 1.57 + float2(0.8, 0.3) * adjustedTime)).g;
    float textureMesh = max(noise1 * 0.3 + noise2 * 0.3 + noise3 * 0.3, 0.01);


    float radialMask = InverseLerp(radius, radius * 0.6, worldDistance);
    float angularMask = InverseLerp(halfAngle, halfAngle * 0.8, angleDiff);
    float mask = radialMask * angularMask;
    if (mask <= 0.0 || Opacticy <= 0.0)
        return float4(0, 0, 0, 0);

    float opacity = saturate(Opacticy) * mask / sqrt(textureMesh);
    return float4(color * opacity, opacity);
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
