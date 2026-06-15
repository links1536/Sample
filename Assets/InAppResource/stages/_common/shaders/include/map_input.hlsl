#pragma once

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

half4 _AmbientLightColor;

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_GlowTex);
SAMPLER(sampler_GlowTex);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_MatCapMap);
SAMPLER(sampler_MatCapMap);

TEXTURE2D(_ControlMap1);
SAMPLER(sampler_ControlMap1);

TEXTURE2D(_ReflectionTexture);
SAMPLER(sampler_ReflectionTexture);
TEXTURE2D(_MirrorMask);
SAMPLER(sampler_MirrorMask);

CBUFFER_START(UnityPerMaterial)
	float4 _MainTex_ST;
	half4 _Color;
	half3 _GlowColor;
	half  _AlphaClip;
	half  _NormalScale;

	half  _Metallic;
	half  _Smoothness;
	half  _Occlusion;

	half  _FogFactor;
CBUFFER_END

