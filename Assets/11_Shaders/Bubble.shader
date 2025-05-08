Shader "Custom/Bubble2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        _Distortion ("Distortion", Range(0, 0.1)) = 0.02
        _DistortionSpeed ("Distortion Speed", Range(0, 5)) = 1
        _ColorShift ("Color Shift", Range(0, 1)) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.8
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 2
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _Distortion;
            float _DistortionSpeed;
            float _ColorShift;
            float _Glossiness;
            float _FresnelPower;
            
            // 원형 UV 계산
            float2 getRadialUV(float2 uv)
            {
                float2 centered = uv - 0.5;
                float dist = length(centered);
                float2 radial = float2(dist, atan2(centered.y, centered.x));
                return radial;
            }
            
            // 왜곡 효과 계산
            float2 distort(float2 uv, float time)
            {
                float2 radialUV = getRadialUV(uv);
                
                // 시간에 따라 변화하는 왜곡 효과
                float distortionX = sin(radialUV.y * 6 + time * _DistortionSpeed) * _Distortion;
                float distortionY = cos(radialUV.y * 7 + time * _DistortionSpeed * 0.8) * _Distortion;
                
                return uv + float2(distortionX, distortionY);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 왜곡된 UV 좌표
                float2 distortedUV = distort(i.uv, _Time.y);
                
                // 원본 텍스처 색상
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                // 가장자리 효과 (프레넬)
                float2 centered = i.uv - 0.5;
                float dist = length(centered);
                float rim = pow(dist * 2, _FresnelPower);
                rim = saturate(rim);
                
                // 무지개 색상 효과
                float3 rainbow = 0.5 + 0.5 * cos(6.28318 * (_Time.y * 0.2 + _ColorShift + float3(0, 0.33, 0.67) + dist));
                rainbow = lerp(rainbow, 1, 0.2); // 밝게 조정
                
                // 외곽선 효과
                float outline = smoothstep(0.5 - _OutlineWidth, 0.5, dist);
                
                // 최종 색상 조합
                col.rgb *= rainbow;
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, outline * _OutlineColor.a);
                
                // 투명도 조정 (가장자리로 갈수록 투명해짐)
                col.a *= col.a * i.color.a * (1.0 - outline * 0.5);
                
                // 반사광 효과 (하이라이트)
                float highlight = pow(1 - abs(dist - 0.3), 10) * _Glossiness;
                col.rgb += highlight;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}