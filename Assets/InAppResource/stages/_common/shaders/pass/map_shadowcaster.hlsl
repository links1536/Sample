#pragma once

#if defined(_MIRROR_RENDERING)
	#define MIRROR_ON
#endif

#if (defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)) && !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	//#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
#endif

#if defined(LIGHTMAP_ON)
	#define ENABLE_STATIC_LIGHTMAP
#endif

#if defined(DYNAMICLIGHTMAP_ON)
	#define ENABLE_DYNAMIC_LIGHTMAP
#endif

#if defined(MIRROR_ON)
	#define REQUIRES_SCREEN_SPACE_POS_INTERPOLATOR
#endif

#if defined(NORMALMAP_ON)
	#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

//#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
//#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
//#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/Common.hlsl"

#include "include/map_surface_data.hlsl"
#include "include/map_brdf.hlsl"
#include "include/map_lighting.hlsl"

//========================================================================================================
//	ShadowCaster
//========================================================================================================
struct ShadowAttributes
{
	float4 positionOS	: POSITION;
	float3 normalOS		: NORMAL;
#if defined(ALPHACLIP_ON)
	float2 texcoord		: TEXCOORD0;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ShadowVaryings
{
	float4 positionCS	: SV_POSITION;
#if defined(ALPHACLIP_ON)
	float2 uv			: TEXCOORD0;
#endif
};

ShadowVaryings vertShadow (ShadowAttributes input)
{
	ShadowVaryings output = (ShadowVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);

	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_MainLightPosition.xyz - positionWS);
#else
	float3 lightDirectionWS = _MainLightPosition.xyz;
#endif

	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

	// ニアクリップを起こさないようにする
#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
	positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

	output.positionCS = positionCS;
#if defined(ALPHACLIP_ON)
	output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
#endif
	return output;
}

float4 fragShadow (ShadowVaryings input) : SV_Target
{
	#if defined(ALPHACLIP_ON)
		half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
		clip(col.a - _AlphaClip);
	#endif

#ifdef LOD_FADE_CROSSFADE
	LODFadeCrossFade(i.positionCS);
#endif
	return 0;
}

