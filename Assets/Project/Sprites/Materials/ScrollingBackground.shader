Shader "Game/ScrollingBackground"
{
    Properties { _MainTex ("Background", 2D) = "white" {} _Opacity ("Transition", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _Opacity;
            struct Attributes { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                uv.x = frac(uv.x * _MainTex_ST.x + _MainTex_ST.z);
                // Mirror beyond the vertical texture edges without wrapping top into bottom.
                float vertical = uv.y * _MainTex_ST.y + _MainTex_ST.w;
                uv.y = 1.0 - abs(frac(vertical * 0.5) * 2.0 - 1.0);
                uv.y = clamp(uv.y, _MainTex_TexelSize.y * 0.5, 1.0 - _MainTex_TexelSize.y * 0.5);
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                color.a *= _Opacity;
                return color;
            }
            ENDHLSL
        }
    }
}
