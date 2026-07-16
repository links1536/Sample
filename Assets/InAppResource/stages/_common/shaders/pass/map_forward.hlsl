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
	float4 positionOS		: POSITION;
	float3 normalOS			: NORMAL;
	float4 tangentOS		: TANGENT;
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

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half4  fogFactorAndVertexLight : TEXCOORD6; // x: fogFactor, yzw: vertex light
#else
	half   fogFactor		: TEXCOORD6;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord		: TEXCOORD7;
#endif

	// ライティング
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
#if defined(ENABLE_DYNAMIC_LIGHTMAP)
	float2  dynamicLightmapUV : TEXCOORD9;
#endif

#ifdef USE_APV_PROBE_OCCLUSION
	float4 probeOcclusion : TEXCOORD10;
#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert (Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

	// 頂点ライティング
	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

	// フォグ
	half fogFactor = 0;
	#if !defined(_FOG_FRAGMENT)
		fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
	#endif

	// 座標系
	output.positionCS = vertexInput.positionCS;
#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	output.positionWS = vertexInput.positionWS;
#endif
#if defined(REQUIRES_SCREEN_SPACE_POS_INTERPOLATOR)
	output.positionSS = ComputeScreenPos(vertexInput.positionCS);
#endif

	// 法線など
	half3   normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	real   sign = input.tangentOS.w * GetOddNegativeScale();
	half4  tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
	output.normalWS = normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	output.tangentWS = tangentWS;
#endif

	// 視線
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
	half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
	output.viewDirTS = viewDirTS;
#endif

	// UV
	output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

	// ライティング
	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#if defined(ENABLE_DYNAMIC_LIGHTMAP)
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

	OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
	output.fogFactor = fogFactor;
#endif

	// 影
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	output.shadowCoord = GetShadowCoord(vertexInput);
#endif

	return output;
}

inline half3 SampleMatCap(TEXTURE2D_PARAM(matcapTexture, matcapSampler), float3 normalWS)
{
	float2 uv = mul((float3x3)UNITY_MATRIX_V, normalWS).xy * 0.5 + 0.5;
	return SAMPLE_TEXTURE2D(matcapTexture, matcapSampler, uv).rgb;
}


void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	inputData.positionWS = input.positionWS;
#endif

#if defined(NORMALMAP_ON)
	float sgn = input.tangentWS.w;      // should be either +1 or -1
	float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
	half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
	inputData.tangentToWorld = tangentToWorld;
	inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
	inputData.normalWS = input.normalWS;
#endif

	inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
	inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

	// 影
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

	// 焼き付けられたGI
#if defined(ENABLE_DYNAMIC_LIGHTMAP)
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#else
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
	float2 preRotatedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	switch (UNITY_DISPLAY_ORIENTATION_PRETRANSFORM)
	{
		default:
		case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0: inputData.normalizedScreenSpaceUV = preRotatedScreenSpaceUV; break;
		case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90: inputData.normalizedScreenSpaceUV = float2(1 - preRotatedScreenSpaceUV.y, preRotatedScreenSpaceUV.x); break;
		case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180: inputData.normalizedScreenSpaceUV = float2(1 - preRotatedScreenSpaceUV.x, 1 - preRotatedScreenSpaceUV.y); break;
		case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270: inputData.normalizedScreenSpaceUV = float2(preRotatedScreenSpaceUV.y, 1 - preRotatedScreenSpaceUV.x); break;
	}
#else
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
#endif

	// デバッグ用情報？
#if defined(DEBUG_DISPLAY)
	#if defined(ENABLE_DYNAMIC_LIGHTMAP)
		inputData.dynamicLightmapUV = input.dynamicLightmapUV;
	#endif
	#if defined(ENABLE_STATIC_LIGHTMAP)
		inputData.staticLightmapUV = input.staticLightmapUV;
	#else
		inputData.vertexSH = input.vertexSH;
	#endif
#endif
}

void InitializeMapSurfaceData(float2 uv, out MapSurfaceData surfaceData)
{
	half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;

#if defined(ALPHACLIP_ON)
	clip(albedo.a - _AlphaClip);
#endif

	// 法線
#if defined(NORMALMAP_ON)
	half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), _NormalScale);
#else
	half3 normalTS = half3(0, 0, 1);
#endif

	// 発光
#if defined(GLOWMAP_ON)
	half4 glowTexture = SAMPLE_TEXTURE2D(_GlowTex, sampler_GlowTex, uv);
	half3 glowColor = glowTexture.rgb * glowTexture.a * _GlowColor.rgb;
#else
	half3 glowColor = 0;
#endif

#if defined(CONTROLMAP_1_ON)
	half4 controlTexture1 = SAMPLE_TEXTURE2D(_ControlMap1, sampler_ControlMap1, uv);
	half metallic = controlTexture1.r * _Metallic;
	half smoothness = controlTexture1.g * _Smoothness;
	half occlusion = controlTexture1.b * _Occlusion;
#else
	half metallic = _Metallic;
	half smoothness = _Smoothness;
	half occlusion = _Occlusion;
#endif

	surfaceData = (MapSurfaceData)0;

	surfaceData.albedo = albedo.rgb;
	surfaceData.normalTS = normalTS;
	surfaceData.emission = glowColor;
	surfaceData.metallic = metallic;
	surfaceData.smoothness = smoothness;
	surfaceData.occlusion = occlusion;
	surfaceData.alpha = albedo.a;

	surfaceData.fogIntensity = _FogFactor;
}

// フラグメントシェーダー
half4 frag(Varyings input, FRONT_FACE_TYPE faceType : FRONT_FACE_SEMANTIC) : SV_Target
{
	// 表裏の判定
	half facing = IS_FRONT_VFACE(faceType, 1, -1);

	// SurfaceData
	MapSurfaceData surfaceData;
	InitializeMapSurfaceData(input.uv, surfaceData);

	// InputData
	InputData inputData;
	InitializeInputData(input, surfaceData.normalTS, inputData);

	// 裏面の時は法線が逆なので元に戻す
	inputData.normalWS *= facing;

#if defined(MIRROR_ON)
	half mirrorMask = SAMPLE_TEXTURE2D(_MirrorMask, sampler_MirrorMask, input.uv).r;
	half3 mirrorCol = SAMPLE_TEXTURE2D(_ReflectionTexture, sampler_ReflectionTexture, inputData.normalizedScreenSpaceUV).rgb;
	surfaceData.albedo.rgb *= lerp(1, mirrorCol, surfaceData.metallic * surfaceData.smoothness * mirrorMask);
#endif

#if defined(MATCAP_ON)
	surfaceData.albedo.rgb *= SampleMatCap(TEXTURE2D_ARGS(_MatCapMap, sampler_MatCapMap), inputData.normalWS);
#endif

#if defined(LIGHTING_OFF)
	half4 finalColor = FragmentUnlighting(inputData, surfaceData);
#else
	half4 finalColor = FragmentLighting(inputData, surfaceData);
#endif
	return max(0, finalColor);
}
