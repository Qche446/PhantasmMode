sampler uField : register(s1);
sampler uNoise : register(s2);

matrix uWorldViewProjection;
float globalTime;
float2 screenSize;

float3 color;
float m;
float n;

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
    float2 uv = float2(input.Coord.x, input.Coord.y);
    float4 field = tex2D(uField, uv);

    // 融合场是加法累积出来的，可以超过 1。先夹回 [0,1]，m 才保持"接近饱和"的原意。
    float a = saturate(field.a);

    float4 result;
    if (a > m)
        result = tex2D(uNoise, uv);
    else if (abs(a - m) < n)
        result = float4(color, 1.0);
    else
        result = float4(color, field.a) * a * a;

    return result;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
