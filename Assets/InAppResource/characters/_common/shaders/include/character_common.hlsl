#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/ToonRendering/Common.hlsl"

#define BLEND_BURN_COLOR(base, mix) (1 - (1 - base) / mix)
#define BLEND_BURN_LINEAR(base, mix) (1 - ((1 - base) +  (1 - mix)))
#define BLEND_SCREEN(base, mix) (1 - (1 - base) * (1 - mix))
#define BLEND_DODGE_COLOR(base, mix) (base / (1 - mix))

//#define VIEW_BASE_LIGHTING

struct VectorData
{
	half3 halfDirectionWS;

	half NdotL;
	half NdotV;
	half NdotH;
	half LdotH;
};

VectorData ComputeVectorData(half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
	VectorData vectorData = (VectorData)0;

	vectorData.halfDirectionWS = SafeNormalize(viewDirectionWS + lightDirectionWS);

	vectorData.NdotL = saturate(dot(normalWS, lightDirectionWS));
	vectorData.NdotV = saturate(dot(normalWS, viewDirectionWS));
	vectorData.NdotH = saturate(dot(normalWS, vectorData.halfDirectionWS));
	vectorData.LdotH = saturate(dot(lightDirectionWS, vectorData.halfDirectionWS));

	return vectorData;
}


//========================================
// 関数



half3 SampleNormalWS(float2 uv, half3 normalWS, half3 tangentWS, half flipSign, half facing)
{
#if defined(NORMALMAP_ON)
	half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), _NormalScale);
	normalTS = normalize(normalTS);

	half3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, flipSign);
	normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#endif
	normalWS *= facing;

	return normalWS;
}

inline half3 GetLightDirectionWS(float3 positionWS)
{
#if defined(VIEW_BASE_LIGHTING)
	// 視点を基準にしたライティング
	half3 lightDir = TransformViewToWorldDir(half3(-1, 1, 1), true);
#else
	// キャラ専用変数を用いたライティング
	// 現状はマクロを用いたメインライトのラッパー
	half3 lightDir = _CharacterMainLightDirection;
#endif
	return lightDir;
}

float3 GetForwardDirectionWS()
{
#if CHARACTER_FACE
	return _RendererForwardWS;
#else
	return GetObjectToWorldMatrix()[2].xyz;
#endif
}

float3 GetUpDirectionWS()
{
#if CHARACTER_FACE
	return _RendererUpWS;
#else
	return GetObjectToWorldMatrix()[1].xyz;
#endif
}

float3 GetRightDirectionWS()
{
#if CHARACTER_FACE
	return _RendererRightWS;
#else
	return GetObjectToWorldMatrix()[0].xyz;
#endif
}

float GetForwardFade(float3 positionWS)
{
	// 正面からみたときに眉が前髪を貫通する表現とかに使う
	// 横に回り込んだ時は通常通りになるようにする
	half3 viewWS = GetWorldSpaceNormalizeViewDir(positionWS);

	half3 forwardWS = GetForwardDirectionWS();
	half  VdotF = dot(viewWS, forwardWS);
	VdotF = saturate(abs(VdotF));

	half3 upWS = GetUpDirectionWS();
	half  VdotU = saturate(abs(dot(viewWS, upWS)));
	return clamp((1 - VdotU) * (VdotF), 0.3, 1);
}

inline half HighLightMapping(half halfLambert)
{
	half softness = 0.01 / 2;
	half threshold = 0.999;
	return ToonStep(halfLambert, threshold, softness);
}

inline half3 RimLight(float3 positionWS, half3 normalWS, half lightAtten)
{
#if defined(_MIRROR_BAKING)
	// 鏡の中はいろんな兼ね合いでバグるので
	return 0;
#else
	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
	half  rim = abs(1.0 - max(0, dot(viewDirWS, normalWS)));
	rim = max(HALF_EPS, pow(rim, _RimPower));

	// リムライト2値化 × 発光力
	rim = ToonStep(rim, _RimThreshold, _RimSoftness) * _RimIntensity;

	// 光の向きを考慮するか
	half directionalMask = saturate(dot(normalWS, _CharacterRimLightDirection));
	rim = lerp(rim, rim * directionalMask, _CharacterRimLightDirectionalRate);

	// メインライトで影になる箇所はリムライトを乗せない
	// やりたい表現次第では lightAtten をかけない方がいい
	return rim * _CharacterRimLightColor.rgb * lightAtten;
#endif
}

inline half3 SampleMatCap(TEXTURE2D_PARAM(matcapTexture, matcapSampler), half3 normalWS)
{
	float2 uv = mul((float3x3)UNITY_MATRIX_V, normalWS).xy * 0.5 + 0.5;
	return SAMPLE_TEXTURE2D(matcapTexture, matcapSampler, uv).rgb;
}

half SampleFaceSDF(float2 uv, half3 lightDirectionWS, half NdotL)
{
	half2 lightWS = normalize(lightDirectionWS.xz);
	half2 forwardWS = normalize(GetForwardDirectionWS().xz);
	half2 rightWS = normalize(GetRightDirectionWS().xz);
	forwardWS.y *= -1;

	half forwardMask = saturate(dot(forwardWS, lightWS) * 0.5 + 0.5);
	half side = saturate(dot(rightWS, lightWS) * 0.5 + 0.5);

	// R・Gが光の当たり方、Bは適用度のマスク
	half3 sdfMask = SAMPLE_TEXTURE2D(_FaceShadowMaskTexture, sampler_FaceShadowMaskTexture, uv).rgb;
	half sdf = lerp(sdfMask.r, sdfMask.g, side);

	//step(y, x)
	//smoothstep(min, max, x)
	half minValue = saturate(sdf - 0.5);
	half maxValue = saturate(sdf + 0.5);
	half sdfLit = 1 - smoothstep(minValue, maxValue, forwardMask);
	half lit = saturate(NdotL);
	return lerp(lit, sdfLit, sdfMask.b);
}

half3 SampleMainLight(float3 positionWS, half3 normalWS)
{
	return _CharacterMainLightColor.rgb;
}

half3 SampleIndirectLight(float3 positionWS, half3 normalWS)
{
	return SampleSH(normalWS);
}