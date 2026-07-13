sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);
sampler shapeTexture : register(s2);

float globalTime;
float lifeRatio;
float2 imageSize;
float4 sourceRectangle;
float omiga;

float3 color1;
float3 color2;
float3 color3;

float4 RoundSimpleDoubleColorPulse(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float2 framedCoords = (coords * imageSize - sourceRectangle.xy) / sourceRectangle.zw;
    float2 noiseUV = framedCoords;
    noiseUV.y *= 0.05;
    noiseUV.x *= lifeRatio;
    float mutiplex = cos(globalTime * omiga - framedCoords.x * 6) * 0.5 + 0.5;
    
    float2 flow = float2(1, 0.5);

    float mutipley = sin(3.141 * framedCoords.y) - 0.1;
    mutipley *= 0.8;
    /*
    float2 shapeCoords = framedCoords;
    shapeCoords.x *= 2;
    shapeCoords.y = 0.35 + 0.30 * shapeCoords.y;
    float4 shape = tex2D(shapeTexture, frac(shapeCoords + 0.1 * (globalTime * float2(12, 0))));
    shape.a = (shape.r + shape.g + shape.b) / 3;
    */
    float4 noise = 0.7 * tex2D(noiseTexture, frac(0.2 * globalTime * flow + noiseUV)) + 0.3;

    float4 resultcolor;

    if (framedCoords.x <= lifeRatio)
    {
        resultcolor = float4(lerp(color1, color2, mutiplex), 1) * sampleColor * 3 * noise;
    }
    else
    {
        resultcolor = 0.5 * float4(lerp(color1, color2, 0.5), 1) * sampleColor;
    }
    resultcolor *= 1 - dot(framedCoords - 0.5, framedCoords - 0.5) * 4;

    return resultcolor * mutipley;
}
float4 VerticalRolling(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float2 framedCoords = (coords * imageSize - sourceRectangle.xy) / sourceRectangle.zw;
    float2 noiseUV = framedCoords;
    noiseUV.y *= 0.05;
    noiseUV.x *= lifeRatio;
    float mutiplex = cos(globalTime * omiga - framedCoords.y * 6) * 0.5 + 0.5;
    
    float2 flow = float2(0.35, 1);
    float2 flow2 = float2(-0.35, 1);
    float mutipley = sin(3.141 * framedCoords.y) - 0.1;
    mutipley *= 0.8;
    /*
    float2 shapeCoords = framedCoords;
    shapeCoords.x *= 2;
    shapeCoords.y = 0.35 + 0.30 * shapeCoords.y;
    float4 shape = tex2D(shapeTexture, frac(shapeCoords + 0.1 * (globalTime * float2(12, 0))));
    shape.a = (shape.r + shape.g + shape.b) / 3;
    */
    float4 noise = 0.3 * tex2D(noiseTexture, frac(0.2 * globalTime * flow + noiseUV)) + 0.3;
    float4 noise2 = 0.3 * tex2D(noiseTexture, frac(0.2 * globalTime * flow2 + noiseUV)) + 0.3;

    float4 resultcolor;

    if (framedCoords.x <= lifeRatio)
    {
        resultcolor = float4(lerp(color1, color2, mutiplex), 1) * sampleColor * 3 * noise * noise2;
    }
    else
    {
        resultcolor = 0.5 * float4(lerp(color1, color2, 0.5), 1) * sampleColor;
    }
    float2 fc = framedCoords - 0.5;
    resultcolor *= 1 - dot(framedCoords - 0.5, framedCoords - 0.5) * 4;

    return resultcolor * mutipley;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 RoundSimpleDoubleColorPulse();
    }
    pass SimpleDoubleColorPulse
    {
        PixelShader = compile ps_3_0 RoundSimpleDoubleColorPulse();
    }
    pass VerticalRolling
    {
        PixelShader = compile ps_3_0 VerticalRolling();
    }

}