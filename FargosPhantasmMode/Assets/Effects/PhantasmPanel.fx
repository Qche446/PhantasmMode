sampler uImage0 : register(s0);

float4 uSource;   // xy = 面板的屏幕尺寸，zw = 面板左上角的屏幕坐标
float uTime;
float uHover;     // 0..1，悬停强度
float uPixel;     // 像素块边长（屏幕像素）

float hash11(float x)
{
    return frac(sin(x * 12.9898) * 43758.5453);
}

// 一条从左向右吹的线：head 是风头，尾巴拖在左边，走出右边界就绕回左边。
float streak(float2 p, float seed)
{
    float span = uSource.x + 260.0;
    float trackY = 6.0 + hash11(seed * 1.71) * (uSource.y - 12.0);
    float speed = 90.0 + 150.0 * hash11(seed * 3.13);
    float length = 60.0 + 90.0 * hash11(seed * 5.37);
    float thick = 1.0 + 2.0 * hash11(seed * 7.91);

    float head = frac(uTime * speed / span + hash11(seed * 9.17)) * span - length;
    float tail = head - p.x;

    float along = tail < 0.0 ? 0.0 : saturate(1.0 - tail / length);
    float wave = sin(p.x * 0.02 + uTime * 2.0 + seed * 4.0) * 4.0;
    float across = 1.0 - smoothstep(thick * 0.5, thick, abs(p.y - trackY - wave));

    return along * along * across;
}

float4 main(float2 coords : SV_POSITION, float2 texCoords : TEXCOORD0) : COLOR0
{
    float4 shape = tex2D(uImage0, texCoords);

    float2 size = uSource.xy;
    // 像素化：先落到 uPixel 的格子上，再归一化。
    float2 p = floor((coords - uSource.zw) / uPixel) * uPixel;
    float2 uv = saturate(p / size);

    const float3 deep = float3(0.055, 0.075, 0.135);
    const float3 mid = float3(0.16, 0.24, 0.46);
    const float3 glow = float3(0.62, 0.82, 1.0);

    // 底层本身在深浅之间缓慢游走，整体偏冷。
    float drift = 0.5 + 0.5 * sin(uv.x * 2.4 - uTime * 0.35 + sin(uv.y * 3.0 + uTime * 0.6) * 1.2);
    float3 color = lerp(deep, mid, drift * 0.55 + uv.y * 0.15);

    // 越靠边框越蓝，边界本身随正弦慢慢游走，所以蓝边不是一条直尺。
    float2 edge = min(p, size - p);
    float dist = min(edge.x, edge.y) + sin(p.y * 0.08 + uTime * 1.9) * 3.0 + sin(p.x * 0.05 - uTime * 1.3) * 2.0;
    float rim = saturate(1.0 - dist / 26.0);
    rim *= rim;
    color += glow * rim * 0.5;

    // 一条斜着缓慢横移的蓝带，让底色不是死的。
    float band = saturate(sin(uv.x * 6.0 - uTime * 0.8 + sin(uv.y * 4.0 + uTime * 0.5)) - 0.35);
    color += float3(0.05, 0.12, 0.30) * band;

    // 悬停整体提亮，靠蓝而不是靠白，免得底板盖过名字。
    color = color * (1.0 + uHover * 0.5) + glow * (uHover * 0.10);

    float wind = streak(p, 1.0);
    wind = max(wind, streak(p, 2.0));
    wind = max(wind, streak(p, 3.0));
    wind = max(wind, streak(p, 4.0));
    wind = max(wind, streak(p, 5.0));
    wind = max(wind, streak(p, 6.0));
    wind = max(wind, streak(p, 7.0));
    wind = max(wind, streak(p, 8.0));
    wind = max(wind, streak(p, 9.0));
    wind = max(wind, streak(p, 10.0));
    wind = max(wind, streak(p, 11.0));
    color = lerp(color, float3(0.90, 0.96, 1.0), wind * 0.9);

    // 底板贴图只取轮廓（圆角与边框的 alpha），颜色与明暗全部由上面算。
    return float4(color, smoothstep(0.08, 0.6, shape.a));
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 main();
    }
}
