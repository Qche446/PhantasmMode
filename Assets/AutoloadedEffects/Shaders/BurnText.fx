sampler uImage0 : register(s0);
float globalTime;
// 添加新参数
sampler uImage1 : register(s1); // 火焰噪声纹理
float burnIntensity; // 0~1, 0=无火焰, 1=完全烧毁
float2 windDirection; // 火焰飘散方向，例 (0.5, -1) 右上
float4 emberColor; // 暗烬颜色 (最暗处/烧焦边缘)
float4 flameColor; // 火焰主色 (中段)
float4 brightFlameColor; // 亮焰色 (接近焰尖)
float4 tipColor; // 焰尖/边缘强光色 (最亮处)

// ---------- 火焰像素着色器 ----------
float4 FireBurnShader(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // 1. 采样原始文字纹理
    float4 texColor = tex2D(uImage0, coords);
    
    // 2. 采样噪声（随时间向 windDirection 方向流动）
    float2 noiseUV = coords + globalTime * windDirection * 0.15;
    float noise = tex2D(uImage1, noiseUV).r;
    
    // 3. 热浪扭曲 UV
    float2 distortedUV = coords;
    distortedUV.x += (noise - 0.5) * burnIntensity * 0.06;
    distortedUV.y += (noise - 0.5) * burnIntensity * 0.06;
    float4 distortedTex = tex2D(uImage0, distortedUV);
    
    // 4. 文字亮度（用于驱动渐变位置）
    float brightness = (distortedTex.r + distortedTex.g + distortedTex.b) / 3.0;
    brightness *= distortedTex.a; // 考虑 alpha
    
    // 5. 四阶颜色渐变映射（暗→中→亮→尖）
    float3 gradientColor;
    if (brightness < 0.25)
        gradientColor = lerp(emberColor.rgb, flameColor.rgb, brightness / 0.25);
    else if (brightness < 0.55)
        gradientColor = lerp(flameColor.rgb, brightFlameColor.rgb, (brightness - 0.25) / 0.30);
    else if (brightness < 0.85)
        gradientColor = lerp(brightFlameColor.rgb, tipColor.rgb, (brightness - 0.55) / 0.30);
    else
        gradientColor = tipColor.rgb;
    
    // 6. 侵蚀 Alpha：噪声低于阈值处文字被"烧毁"
    float alphaThreshold = 1.0 - burnIntensity;
    float erodedAlpha = step(alphaThreshold, noise) * distortedTex.a;
    
    // 7. 灼烧边缘强光（在 alpha 临界处叠加亮色）
    float edge = smoothstep(alphaThreshold - 0.08, alphaThreshold, noise)
               - smoothstep(alphaThreshold, alphaThreshold + 0.08, noise);
    gradientColor += edge * tipColor.rgb * 1.8;
    
    // 8. 最终输出
    float4 finalColor = float4(gradientColor * texColor.a, erodedAlpha);
    return finalColor * sampleColor * 2.5;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 FireBurnShader();
    }
}