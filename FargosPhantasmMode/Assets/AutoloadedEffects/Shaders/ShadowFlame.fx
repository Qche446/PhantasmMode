sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float3 color;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 c = tex2D(uImage0, coords);
    float a = max(c.r, max(c.g, c.b));
    return float4(color, c.w) * a;
    if (a > 0.01)//明度超过m的部分被替换
    {
        return float4(color, c.w) * a;
    }
    else
        return float4(color, c.w) * a * a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
   
}