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
    float _UseDoorway;
    float4 _Exit, _HallAxis, _Outward, _RoomLimits, _HallLimits;
    float _DoorHalfWidth;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
   float wave(float2 p) { float t=_Time.y*_FlowSpeed; return sin(p.x*2.8+p.y*1.7-t*3.2)*0.55+sin(p.y*5.1-p.x*1.2-t*4.3)*0.3+sin(p.x*9+p.y*7-t*5)*0.15; }
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p)
   {
    float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
   }
   float2 doorwayPoint(float2 p) { float2 d=p-_Exit.xz;return float2(dot(d,_HallAxis.xz),dot(d,_Outward.xz)); }
   float arrivalDistance(float2 p)
   {
    float2 q=doorwayPoint(p);
    float inside=distance(p,_FloodSource.xz);
    // Beyond the wall, water must travel through the doorway before spreading along the hall.
    float hall=distance(_Exit.xz,_FloodSource.xz)+length(float2(q.x*0.72,q.y));
    return _UseDoorway>.5 && q.y>0 ? hall : inside;
   }
   float boundsMask(float2 q,float4 bounds)
   { return min(min(q.x-bounds.x,bounds.y-q.x),min(q.y-bounds.z,bounds.w-q.y)); }
   V vert(A i)
   {
    V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.world=TransformObjectToWorld(i.positionOS.xyz);
    float edge=saturate((_FloodRadius-arrivalDistance(o.world.xz))*.5);
    o.world.y+=wave(o.world.xz)*_WaveHeight*edge-_FloodDepth*(1-edge);
    o.positionCS=TransformWorldToHClip(o.world); return o;
   }
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 p=i.world.xz;
    float fingers=(noise(p*1.1)-.5)*.65+(noise(p*3.8)-.5)*.2+(noise(p*11)-.5)*.045;
    float wetness=saturate((_FloodRadius-arrivalDistance(p)+fingers)*5);
    if(_UseDoorway>.5)
    {
     float2 q=doorwayPoint(p);
     float mask=q.y<0 ? boundsMask(q,_RoomLimits) : boundsMask(q,_HallLimits);
     // The thin wall band permits flow only through the actual door opening.
     if(abs(q.y)<.10) mask=min(mask,_DoorHalfWidth-abs(q.x));
     wetness*=saturate(mask*18);
    }
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
