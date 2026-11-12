sampler uImage0 : register(s0);

// uSource：xy 是文字包围盒的屏幕尺寸，zw 是左上角的屏幕坐标。
float4 uSource;
float uTime;
float uHover;

float4 main(float2 coords : SV_POSITION, float2 texCoords : TEXCOORD0) : COLOR0
{
    float alpha = tex2D(uImage0, texCoords).a;
    if (alpha < 0.01)
        return 0;

    float2 uv = saturate((coords - uSource.zw) / max(uSource.xy, 1.0));

    // 一道很慢的波横向扫过，灰度在灰与浅灰之间小幅起伏。
    float wave = 0.5 + 0.5 * sin(uv.x * 3.0 - uTime * 1.1);
    float gray = lerp(0.58, 0.84, wave);
    float3 color = float3(gray * 0.97, gray, gray * 1.05);
    color *= 1.0 + uHover * 0.12;

    return float4(color * alpha, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 main();
    }
}
