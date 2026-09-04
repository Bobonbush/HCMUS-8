Shader "Nghi/BillboardCutout"
{
 Properties { _BaseMap("Photo",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1) _Cutoff("Cutoff",Range(0,1))=0.12 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  Pass
  {
   Cull Off ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST; float4 _BaseColor; float _Cutoff;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
   half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(c.a-_Cutoff);return half4(c.rgb,1);}
   ENDHLSL
  }
 }
}
