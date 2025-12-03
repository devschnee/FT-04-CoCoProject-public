Shader "Custom/StarSoftGlow_Yellow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 0.94, 0.70, 1)
        _GlowStrength ("Glow Strength", Float) = 1.0
        _BaseColor ("SpriteRenderer Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One     // Additive
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _GlowColor;
            float _GlowStrength;
            float4 _BaseColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // 중심 정규화용
                float2 centered = uv - 0.5;

                // 거리 기반 소프트 마스크 (Glow Mask)
                float dist = length(centered);
                float glowMask = saturate(1.0 - dist * 2.0);

                // 부드럽게 (Pow가 Feather 역할)
                glowMask = pow(glowMask, 2.5);

                // Glow = GlowColor * Mask * Strength * BaseColor.a
                float3 glow = _GlowColor.rgb * glowMask * _GlowStrength * _BaseColor.a;

                return float4(glow, glowMask);
            }

            ENDHLSL
        }
    }
}
