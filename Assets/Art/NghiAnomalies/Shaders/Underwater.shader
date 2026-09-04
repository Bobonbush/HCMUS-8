Shader "Nghi/Underwater"
{
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" }
  Pass
  {
   ZTest Always ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
   V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(i.positionOS.xyz); return o; }
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
    uv+=float2(sin(uv.y*31+_Time.y*2.3),cos(uv.x*26-_Time.y*1.8))*0.009;
    float3 color=SampleSceneColor(saturate(uv));
    float vignette=saturate(1-length(uv-0.5)*0.65);
    return half4(lerp(color*float3(0.48,0.74,0.72),float3(0.025,0.12,0.13),0.3)*vignette,1);
   }
   ENDHLSL
  }
 }
}
