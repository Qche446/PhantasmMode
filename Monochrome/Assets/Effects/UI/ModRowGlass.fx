sampler uImage0 : register(s0);
sampler uIcon : register(s1);

// uSource：xy 是面板的屏幕尺寸，zw 是左上角的屏幕坐标。
// uBorder、uCorner 都是屏幕像素，由调用方乘过 UI 缩放。
float4 uSource;
float uTime;
float uHover;
float uBorder;
float uCorner;
float uIconStrength;

float hash21(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float hash11(float x)
{
    return frac(sin(x * 12.9898) * 43758.5453);
}

// 从右上角光源射进玻璃内部的光束：每条的张角、长度、脉动各不相同，方向上带一点慢速摆动。
float3 glass_beams(float2 p, float2 size, float t)
{
    float2 light = float2(size.x, 0.0);
    float2 toPixel = p - light;
    float dist = length(toPixel);
    float2 dir = toPixel / max(dist, 1.0);
    float3 sum = 0.0;

    [unroll]
    for (int i = 0; i < 7; i++)
    {
        float seed = (float)i * 1.618 + 0.35;

        // 均匀铺开再抖动，保证几条光束散在整个扇形里，而不是挤成一束。
        float spread = ((float)i + 0.5) / 7.0 + (hash11(seed) - 0.5) * 0.10;
        float angle = lerp(1.20, 5.08, spread);
        angle += (hash11(seed + 4.7) * 2.0 - 1.0) * 0.06 * sin(t * (0.18 + 0.24 * hash11(seed + 9.1)) + seed * 5.3);

        // 光束很窄，用叉积近似角差，省掉逐光束的 atan2。
        float2 beamDir = float2(cos(angle), sin(angle));
        float offset = abs(dir.x * beamDir.y - dir.y * beamDir.x);
        float halfWidth = 0.010 + 0.022 * hash11(seed + 2.3);
        halfWidth *= 5;
        float ray = pow(saturate(1.0 - offset / halfWidth), 1.8) + pow(saturate(1.0 - offset / (halfWidth * 3.5)), 2.0) * 0.35;

        float reach = size.x * (0.30 + 0.75 * hash11(seed + 6.1));
        float falloff = saturate(1.0 - dist / reach);
        falloff *= falloff;

        float pulse = 0.55 + 0.45 * sin(t * (0.6 + 0.7 * hash11(seed + 8.3)) + seed * 3.7);
        sum += ray * falloff * smoothstep(4.0, 30.0, dist) * pulse;
    }

    return sum;
}

// 圆角矩形的有符号距离：内部为负，可以把边框贴着圆角画出来。
float rounded_rect_distance(float2 p, float2 halfSize, float radius)
{
    float2 q = abs(p) - halfSize + radius;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

float4 main(float2 coords : SV_POSITION, float2 texCoords : TEXCOORD0) : COLOR0
{
    float4 shape = tex2D(uImage0, texCoords);
    float2 size = max(uSource.xy, 1.0);
    float2 local = coords - uSource.zw - size * 0.5;
    float2 uv = saturate((local + size * 0.5) / size);

    float distanceToEdge = -rounded_rect_distance(local, size * 0.5, uCorner);

    // 隔着玻璃看图标：UV 轻微起伏，图标被拉伸铺满整行。
    float2 warp = float2(sin(uv.y * 7.0 + uTime * 0.5) * 0.0035, cos(uv.x * 5.5 - uTime * 0.4) * 0.0035);
    float4 icon = tex2D(uIcon, saturate(uv + warp));

    // 图标压暗、去饱和并偏冷，作为玻璃后面的底图。
    float3 iconColor = lerp(icon.rgb, dot(icon.rgb, float3(0.299, 0.587, 0.114)) * float3(0.85, 0.92, 1.05), 0.35);
    iconColor *= lerp(0.45, 0.75, saturate(1.0 - uv.y));

    float3 color = lerp(float3(0.14, 0.17, 0.24), iconColor, icon.a * uIconStrength);

    // 右上角一个光源：辉光加一点来自角落的冷白。
    float corner = saturate(1.0 - length(float2(1.0 - uv.x, uv.y * 1.6)) * 1.15);
    color += pow(corner, 3.0) * 0.38 * float3(0.85, 0.92, 1.0);
    color += exp(-length(float2(1.0 - uv.x, uv.y * 1.8)) * 3.2) * 0.22;

    // 光束从这个角落射进玻璃内部。
    color += glass_beams(local + size * 0.5, size, uTime) * 0.42 * float3(0.76, 0.88, 1.0);

    // 名字那一带压暗一点，字压在图标上也看得清。
    color *= 1.0 - saturate(1.0 - pow(abs(uv.y - 0.5) * 2.6, 2.0)) * 0.22;

    // 边缘冷蓝折射。
    float fresnel = saturate(1.0 - distanceToEdge / (uBorder * 5.0)) * smoothstep(uBorder, uBorder * 2.5, distanceToEdge);
    color = lerp(color, float3(0.55, 0.72, 1.0), fresnel * 0.18);

    // 顶边细高光与极轻的磨砂颗粒。
    color += smoothstep(uBorder * 2.5, 0.0, uv.y * size.y) * 0.10;
    color += (hash21(floor(local / max(uBorder * 0.9, 1.0))) - 0.5) * 0.004;

    // 内侧阴影：越靠边越暗，玻璃才有厚度。
    color *= lerp(0.60, 1.0, saturate(distanceToEdge / (uBorder * 4.0)));

    // 灰色边框沿圆角走，往内一条亮线做出厚度。
    float edge = saturate(1.0 - distanceToEdge / uBorder);
    float3 borderColor = lerp(float3(0.42, 0.44, 0.48), float3(0.76, 0.78, 0.82), saturate(uv.y + 0.2));
    color = lerp(color, borderColor, smoothstep(0.0, 0.6, edge));
    color += saturate(1.0 - abs(distanceToEdge - uBorder * 1.8) / (uBorder * 0.7)) * 0.14;

    color *= 1.0 + uHover * 0.16;

    // 图标本身要看得清，透明度留给边缘一点点过渡。
    float alpha = smoothstep(0.02, 0.6, shape.a) * 0.94;
    return float4(color, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 main();
    }
}
