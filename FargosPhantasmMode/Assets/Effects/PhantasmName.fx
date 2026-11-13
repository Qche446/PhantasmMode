sampler uImage0 : register(s0);

float4 uSource; // xy = 文字屏幕尺寸，zw = 左上角屏幕坐标
float uTime;
float uHover;

float4 main(float2 coords : SV_POSITION, float2 texCoords : TEXCOORD0) : COLOR0
{
    float a = tex2D(uImage0, texCoords).a;

    float2 uv = saturate((coords - uSource.zw) / max(uSource.xy, float2(1.0, 1.0)));
    float wave = 0.5 + 0.5 * sin(uv.x * 5.0 - uTime * 2.5);

    float3 white = float3(0.96, 0.99, 1.0);
    float3 blue = float3(0.32, 0.60, 1.0);
    float3 color = lerp(white, blue, wave * 0.85);
    color = lerp(color, white, uHover * 0.3) * (1.0 + uHover * 0.25);

    return float4(color * a, a) * 1.2f;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 main();
    }
}
