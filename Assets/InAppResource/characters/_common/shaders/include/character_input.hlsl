#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


//========================================
// ライト

// 用途に応じて専用の変数にするため、defineで分けておく

// メインライト
#define _CharacterMainLightColor (_MainLightColor.rgb)
#define _CharacterMainLightDirection half3(_MainLightPosition.xyz)

// リムライト用の設定は分けておく
// 専用コンポーネントを用いてメインライトとリムライトを分けられるようにしておく
#define _CharacterRimLightColor (_MainLightColor.rgb)
#define _CharacterRimLightDirection half3(_MainLightPosition.xyz)
#define _CharacterRimLightDirectionalRate 0.8


TEXTURE2D(_MainTex);
TEXTURE2D(_ShadingMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_ControlMap1);
TEXTURE2D(_ControlMap2);
TEXTURE2D(_MatCapMap);
TEXTURE2D(_FaceShadowMaskTexture);

SAMPLER(sampler_MainTex);
SAMPLER(sampler_ShadingMap);
SAMPLER(sampler_NormalMap);
SAMPLER(sampler_ControlMap1);
SAMPLER(sampler_ControlMap2);
SAMPLER(sampler_MatCapMap);
SAMPLER(sampler_FaceShadowMaskTexture);

// 変数の並びがちょっと気持ち悪いけど
CBUFFER_START(UnityPerMaterial)
	float4 _MainTex_ST;

	float3 _RendererForwardWS;
	uint   _EnableMatCapMap;

	float3 _RendererUpWS;
	float  _NormalScale;

	float3 _RendererRightWS;
	float  _AlphaClip;

	half4  _Color;
	half4  _ShadingColor;

	half3  _HighLightColor;
	uint   _EnableHighLight;

	half3  _OutlineColor;
	uint   _EnableOutline;

	uint   _EnableLighting;
	uint   _EnableShadingMap;

	half   _ShadowSoftness;
	half   _ShadowThreshold;

	uint   _EnableControlMap1;

	uint   _EnableSpecular;
	half   _Smoothness;

	uint   _EnableRimLight;
	half   _RimIntensity;
	half   _RimPower;
	half   _RimSoftness;
	half   _RimThreshold;

	uint   _EnableControlMap2;

	float  _OutlineWidth;

	float  _DepthOffset;

CBUFFER_END

struct ControlTex
{
	half specularMask;
	half rimMask;
	half highlightMask;
	half shadowMask;
	half alphaMask;
};

ControlTex SampleControlTex(float2 uv)
{
	ControlTex control = (ControlTex)0;

	// R:Specular G:Rim B:Highlight
	control.specularMask = 0;
	control.rimMask = 1;
	control.highlightMask = 0;

	if (_EnableControlMap1 != 0) {
		half4 controlColor1 = SAMPLE_TEXTURE2D(_ControlMap1, sampler_ControlMap1, uv);
		control.specularMask = controlColor1.r;
		control.rimMask = controlColor1.g;
		control.highlightMask = controlColor1.b;
	}

	// R:Shadow mask G:Alpha B:Outline
	// アウトラインは役割上ここでとっても意味ないので、Outlineパスの頂点シェーダーで直接取る
	control.shadowMask = 1;
	control.alphaMask = 1;
	if (_EnableControlMap2 != 0) {
		half4 controlColor2 = SAMPLE_TEXTURE2D(_ControlMap2, sampler_ControlMap2, uv);
		control.shadowMask = controlColor2.r;
		control.alphaMask = controlColor2.g;
	}
	return control;
}

half SampleAlphaOnly(float2 uv)
{
#if defined(ALPHA_FROM_CONTROL)
	half alphaMask = 1;
	if (_EnableControlMap2 != 0) {
		alphaMask = SAMPLE_TEXTURE2D(_ControlMap2, sampler_ControlMap2, uv).g;
	}
	return alphaMask;
#else
	return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
#endif
}

inline void AlphaClip(float2 uv)
{
#if defined(ALPHACLIP_ON)
	half alpha = SampleAlphaOnly(uv);
	clip(alpha - _AlphaClip);
#endif
}

inline void AlphaClip(float2 uv, half colorAlpha)
{
#if defined(ALPHACLIP_ON)
	#if defined(ALPHA_FROM_CONTROL)
		half alphaMask = 1;
		if (_EnableControlMap2 != 0) {
			alphaMask = SAMPLE_TEXTURE2D(_ControlMap2, sampler_ControlMap2, uv).g;
		}
		clip(alphaMask - _AlphaClip);
	#else
		clip(colorAlpha - _AlphaClip);
	#endif
#endif
}
