#pragma once

#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/Common.hlsl"

#include "../include/character_common.hlsl"

struct Attributes
{
	float4	positionOS	: POSITION;
	half3	normalOS	: NORMAL;
	half4	tangentOS	: TANGENT;
	float2	uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4	positionCS	: SV_POSITION;
	float2	uv			: TEXCOORD0;
	half3	normalWS	: TEXCOORD1;
	half4	tangentWS	: TEXCOORD2;
	float3	positionWS	: TEXCOORD3;
	float	fogFactor	: TEXCOORD4;
	float	depthFade	: TEXCOORD5;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert (Attributes input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	Varyings output;
	ZERO_INITIALIZE(Varyings, output);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);

	// 法線情報
	half3   normalWS  = TransformObjectToWorldNormal(input.normalOS);
	half3   tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
	output.normalWS = normalWS;
	output.tangentWS = half4(tangentWS, input.tangentOS.w);

	output.fogFactor = ComputeFogFactor(output.positionCS.z);

#if defined(DEPTH_OFFSET_ON)
	// 正面を向いているかでフェードさせる
	float fade = GetForwardFade(output.positionWS);
	output.depthFade = fade;

	// View空間上で深度値をオフセットする
	float3 positionVS = TransformWorldToView(output.positionWS);
	positionVS.z += _DepthOffset;// *fade;
	float4 depthPositionCS = TransformWViewToHClip(positionVS);
	float depth = depthPositionCS.z / depthPositionCS.w;
	output.positionCS.z = depth * output.positionCS.w;
#endif

	return output;
}

half4 frag(Varyings input, half facing : VFACE) : SV_Target
{
	// R:Specular G:Rim B:Highlight
	half specularMask = 1;
	half rimMask = 1;
	half highlightMask = 0;
	half skinMask = 0;
	if (_EnableControlMap1 != 0) {
		half4 controlColor1 = SAMPLE_TEXTURE2D(_ControlMap1, sampler_ControlMap1, input.uv);
		specularMask = controlColor1.r;
		rimMask = controlColor1.g;
		highlightMask = controlColor1.b;
		skinMask = 1.0 - controlColor1.a;
	}

	// アルファチャンネルのみ使う
	half alpha = SampleAlphaOnly(input.uv);

	// αクリップ
#if defined(ALPHACLIP_ON)
	clip(alpha - _AlphaClip);
#endif

	// 法線取得
	half3 normalWS = SampleNormalWS(input.uv, input.normalWS, input.tangentWS.xyz, input.tangentWS.w, facing);

	// スペキュラ―
	half specularIntensity = 0;
	if (_EnableSpecular != 0) {
		half3 indirectLighting = SampleSH(normalWS);
		half3 specLightDir = normalize(GetLightDirectionWS(input.positionWS));
		half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
		half3 lightView = normalize(specLightDir + view);
		half  specular = pow(saturate(dot(lightView, normalWS)), _Smoothness);
		specularIntensity += specular * Luminance(indirectLighting) * specularMask;
	}


	// リムライト
	half limLightIntensity = 0;
	if (_EnableRimLight != 0) {
		// 元の色を乗せて加算する
		// 発光範囲かだけ取りたいので
		limLightIntensity += Luminance(RimLight(input.positionWS.xyz, normalWS, 1) * rimMask);
	}

#if defined(DEPTH_OFFSET_ON)
	//finalColor.a *= GetForwardFade(input.positionWS);
	alpha *= input.depthFade;
#endif

	// R・・・キャラ
	// G・・・Bloom対象
	// B・・・肌
	return half4(1, specularIntensity + limLightIntensity, skinMask, alpha);
}
