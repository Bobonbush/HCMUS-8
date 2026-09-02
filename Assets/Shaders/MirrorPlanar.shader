Shader "HCMUS/MirrorPlanar"
{
    // Planar mirror glass for URP. A MirrorReflection component renders the scene
    // from a mirrored camera into _ReflectionTex; because that camera uses the main
    // camera's (mirrored) frustum, sampling by screen position lines the image up
    // with the glass exactly. With no reflection bound (_HasReflection 0) the glass
    // falls back to the dark tint it had before - distant mirrors stay cheap.
    Properties
    {
        _Tint ("Reflection Tint", Color) = (0.72, 0.78, 0.82, 1)
        _BaseColor ("Fallback / Depth Color", Color) = (0.02, 0.024, 0.028, 1)
        _Strength ("Reflection Strength", Range(0, 1)) = 0.85
        [HideInInspector] _ReflectionTex ("Reflection", 2D) = "black" {}
        [HideInInspector] _HasReflection ("Has Reflection", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "MirrorForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _BaseColor;
                half _Strength;
                half _HasReflection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionHCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                half3 reflection = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv).rgb;
                half3 color = lerp(_BaseColor.rgb, reflection * _Tint.rgb, _Strength * _HasReflection);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
