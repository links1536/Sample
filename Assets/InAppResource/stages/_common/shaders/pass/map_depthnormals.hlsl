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
//	DepthNormal
//========================================================================================================

struct AttributesNormal
{
	float4 positionOS		: POSITION;
	float3 normalOS			: NORMAL;
	float4 tangentOS		: TANGENT;
	float2 texcoord			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsNormal
{
	float4 positionCS		: SV_POSITION;
	float2 uv				: TEXCOORD0;
	float3 normalWS			: TEXCOORD3;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	float4 tangentWS		: TEXCOORD4;
#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsNormal vertNormal(AttributesNormal input)
{
	VaryingsNormal output = (VaryingsNormal)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

	// 座標系
	output.positionCS = vertexInput.positionCS;

	// 法線など
	float3 normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	real   sign = input.tangentOS.w * GetOddNegativeScale();
	float4 tangentWS = float4(normalInput.tangentWS.xyz, sign);
#endif
	output.normalWS = normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	output.tangentWS = tangentWS;
#endif

	// UV
	output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

	return output;
}

// フラグメントシェーダー
half4 fragNormal(VaryingsNormal input, FRONT_FACE_TYPE faceType : FRONT_FACE_SEMANTIC) : SV_Target
{
	// 表裏の判定
	half facing = IS_FRONT_VFACE(faceType, 1, -1);

	// sample the texture
	half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

#if defined(ALPHACLIP_ON)
	clip(albedo.a - _AlphaClip);
#endif

	// 法線
#if defined(NORMALMAP_ON)
	float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);

	float   sgn = input.tangentWS.w;      // should be either +1 or -1
	float3  bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
	half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
	float3  normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
	float3 normalWS = input.normalWS;
#endif

	// 裏面の時は法線が逆なので元に戻す
	normalWS *= facing;
	normalWS = NormalizeNormalPerPixel(normalWS);

#if defined(_GBUFFER_NORMALS_OCT)
	float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
	float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
	half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
	return half4(packedNormalWS, 0.0);
#else
	return half4(normalWS, 0.0);
#endif
}

