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

struct Attributes
{
	float4 positionOS			: POSITION;
	float3 normalOS				: NORMAL;
	float4 tangentOS			: TANGENT;
	float2 texcoord				: TEXCOORD0;
	float2 staticLightmapUV		: TEXCOORD1;
	float2 dynamicLightmapUV	: TEXCOORD2;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS		: SV_POSITION;
	float2 uv				: TEXCOORD0;
#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	float3 positionWS		: TEXCOORD1;
#endif
#if defined(REQUIRES_SCREEN_SPACE_POS_INTERPOLATOR)
	float4 positionSS		: TEXCOORD2;
#endif

	half3  normalWS			: TEXCOORD3;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	half4  tangentWS		: TEXCOORD4;
#endif
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	half3  viewDirTS		: TEXCOORD5;
#endif

	half   fogFactor		: TEXCOORD6;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord		: TEXCOORD7;
#endif

	// ライティング
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
#if defined(ENABLE_DYNAMIC_LIGHTMAP)
	float2  dynamicLightmapUV : TEXCOORD9;
#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

//========================================================================================================
//	Emission
//========================================================================================================

Varyings vertEmission(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
#endif
#if defined(REQUIRES_SCREEN_SPACE_POS_INTERPOLATOR)
	output.positionSS = ComputeScreenPos(output.positionCS);
#endif
	output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

	output.fogFactor = ComputeFogFactor(output.positionCS.z);

	return output;
}

half4 fragEmission(Varyings input) : SV_Target
{
#if !defined(GLOWMAP_ON)
	return 0;
#else

	half4 glowCol = SAMPLE_TEXTURE2D(_GlowTex, sampler_GlowTex, input.uv);
#if defined(ALPHACLIP_ON)
	clip(glowCol.a - _AlphaClip);
#endif

	half4 finalCol = 0;
	finalCol.rgb += glowCol.rgb * glowCol.a * _GlowColor.rgb;
	finalCol.a = Luminance(finalCol.rgb);

	// apply fog
	finalCol.rgb *= ComputeFogMaskWithAlpha(input.fogFactor, _FogFactor);
	return finalCol;
#endif
}
