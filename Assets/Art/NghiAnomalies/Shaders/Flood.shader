Shader "Nghi/Flood"
{
 Properties { _BaseColor("Water",Color)=(0.045,0.15,0.17,0.8) _WaveHeight("Wave height",Float)=0.0015 _FloodSource("Source",Vector)=(0,0,0,0) _FloodRadius("Spread",Float)=0 _FloodDepth("Depth",Float)=0 _FlowSpeed("Flow",Float)=0.22 _FoamStrength("Foam",Float)=0.025 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float _WaveHeight;
    float4 _FloodSource;
    float _FloodRadius;
    float _FloodDepth;
    float _FlowSpeed;
    float _FoamStrength;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   float wave(float2 p) { float t=_Time.y*_FlowSpeed; return sin(p.x*2.8+p.y*1.7-t*3.2)*0.55+sin(p.y*5.1-p.x*1.2-t*4.3)*0.3+sin(p.x*9+p.y*7-t*5)*0.15; }
   V vert(A i)
   {
    V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.world=TransformObjectToWorld(i.positionOS.xyz);
    float edge=saturate(1-distance(o.world.xz,_FloodSource.xz)/max(_FloodRadius,0.001));
    o.world.y+=wave(o.world.xz)*_WaveHeight*edge-_FloodDepth*(1-edge);
    o.positionCS=TransformWorldToHClip(o.world); return o;
   }
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 p=i.world.xz;
    float wetness=saturate((_FloodRadius-distance(p,_FloodSource.xz)+sin(p.x*9)*sin(p.y*7)*0.05)*5);
    clip(wetness-0.01);
    float h=wave(p);
    float3 n=normalize(float3((h-wave(p+float2(0.035,0)))*_WaveHeight/0.035,1,(h-wave(p+float2(0,0.035)))*_WaveHeight/0.035));
    float3 view=normalize(GetWorldSpaceViewDir(i.world));
    float fresnel=pow(1-saturate(abs(dot(view,n))),4);
    Light light=GetMainLight();
    float spec=pow(saturate(dot(n,normalize(light.direction+view))),95)*1.4;
    float foam=smoothstep(0.53,0.88,h+sin(p.x*23+p.y*19+_Time.y*4)*0.13);
    float2 uv=GetNormalizedScreenSpaceUV(i.positionCS)+n.xz*0.012;
    float3 refractColor=SampleSceneColor(saturate(uv));
    float3 color=lerp(refractColor,_BaseColor.rgb,0.55)+fresnel*float3(0.17,0.24,0.25)+spec*light.color;
    return half4(lerp(color,float3(0.65,0.76,0.72),foam*_FoamStrength),0.72*wetness);
   }
   ENDHLSL
  }
 }
}
