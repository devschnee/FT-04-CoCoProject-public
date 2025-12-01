Shader "Custom/JoystickHighlight"
{
    Properties
    {
        _MainTex ("Mask Texture (Alpha Only)", 2D) = "white" {}

        _HighlightColor ("Highlight Color", Color) = (0,1,1,1)
        _Intensity ("Highlight Intensity", Float) = 1

        _DirectionAngle ("Direction Angle", Float) = 90
        _Range ("Angle Range", Float) = 45

        _BloomIntensity ("Bloom Intensity", Float) = 1.2
        _BloomSoftness ("Bloom Softness", Float) = 0.2

        _Alpha ("Final Alpha", Range(0,1)) = 1 // 최종 알파 조절
    }

    SubShader
    {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
            };

            struct v2f {
                float4 vertex:SV_POSITION;
                float2 uv:TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _HighlightColor;
            float _Intensity;

            float _DirectionAngle;
            float _Range;

            float _BloomIntensity;
            float _BloomSoftness;

            float _Alpha;   // 이 값으로 최종 투명도 조절


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float angle360(float2 p)
            {
                float a = degrees(atan2(p.y,p.x));
                if(a < 0) a += 360;
                return a;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float alphaMask = tex2D(_MainTex, i.uv).a;
                if(alphaMask <= 0.001)
                    return float4(0,0,0,0);

                float2 centered = i.uv - float2(0.5, 0.5);
                float ang = angle360(centered);

                // Up 기준
                ang -= 90;
                if(ang < 0) ang += 360;

                float d = abs(ang - _DirectionAngle);
                d = min(d, 360 - d);

                float mask = step(d, _Range);


                //------------------------------------------------
                // Base highlight
                //------------------------------------------------
                float baseA = alphaMask * mask * _HighlightColor.a * _Intensity;
                float3 baseRGB = _HighlightColor.rgb * baseA;


                //------------------------------------------------
                // Radial bloom
                //------------------------------------------------
                float r = length(centered) * 2.0;
                float bloomMask = 1.0 - smoothstep(1.0 - _BloomSoftness, 1.0, r);

                float bloomA = bloomMask * alphaMask * _BloomIntensity * mask;
                float3 bloomRGB = _HighlightColor.rgb * bloomA;


                //------------------------------------------------
                // Final combine + Alpha control
                //------------------------------------------------
                float3 rgb = baseRGB + bloomRGB;
                float a = (baseA + bloomA) * _Alpha;   // 최종 알파 스케일링

                if(a > 0.0001)
                    rgb /= a;

                return float4(rgb, a);
            }

            ENDCG
        }
    }
}
