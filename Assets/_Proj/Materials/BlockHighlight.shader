Shader "Custom/BlockHighlight"
{
    Properties
    {
        _Color ("Base Color", Color) = (0,0.7,1,0.3)
        _OutlineColor ("Outline Color", Color) = (0,1,1,0.8)
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.05)) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

           fixed4 frag (v2f i) : SV_Target
           {
                float2 uv = i.uv;

                // --------------------------
                // 수직 그라데이션 (하단->상단)
                // --------------------------
                // uv.y == 0 (아래) -> 1 (위)
                // 아래는 진하게, 위로 갈수록 투명해짐
                float vertical = smoothstep(-0.2, 1.0, uv.y);
                vertical = 1.0 - vertical;  // 아래쪽이 밝도록 반전
                vertical = pow(vertical, 0.6); // 값이 클수록 그라에디션 급격해짐(높이가 낮아보임)
                fixed4 baseCol = _Color * vertical;


                //--------------------------------
                // Wave: y로 흐르는 세로 물결
                //--------------------------------
                float time = _Time.y; // Unity 내장 시간
                float waveFreq = 10.0; // 물결 개수
                float waveSpeed = 1.5; // 물결이 올라가는 속도
                // 파형: sin → 0~1
                float wave = sin((uv.y * waveFreq) + (time * waveSpeed)) * 0.5 + 0.5;
                // 너무 강하면 지저분해지니 soft weight 적용
                float waveStrength = 0.25; // 전체 밝기 보정 정도
                fixed4 waveCol = _Color * wave * waveStrength;
                
                
                // --------------------------
                // 테두리 Outline
                // --------------------------
                float left = uv.x;
                float right = 1 - uv.x;
                float top = uv.y;
                float bottom = 1 - uv.y;

                float outline = step(left, _OutlineThickness) +
                                step(right, _OutlineThickness) +
                                step(top, _OutlineThickness) +
                                step(bottom, _OutlineThickness);

                fixed4 outlineCol = _OutlineColor * saturate(outline);
                
                // 최종 컬러는 아래에서 선택
                // return baseCol + outlineCol; // 아웃라인 주고 싶으면 이 주석 해제
                // return baseCol; // 기본 그라데이션 색만 주고 싶으면 이 주석 해제
                return baseCol + waveCol; // 웨이브 효과 들어간 색
                // return baseCol + waveCol + outlineCol; // 효과 모두 다 들어간 색
           }
           ENDCG
        }
    }
}