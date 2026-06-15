#pragma once

#include "../include/character_common.hlsl"

struct Attributes
{
	float4 positionOS	: POSITION;
	half3  normalOS		: NORMAL;
	half4  tangentOS	: TANGENT;
	half4  color		: COLOR;
	float2 uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS	: SV_POSITION;
	float2 uv			: TEXCOORD0;
	half3  normalWS		: TEXCOORD1;
	float3 positionWS	: TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vertOutline(Attributes input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	Varyings output;
	ZERO_INITIALIZE(Varyings, output);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);


	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

	// 法線情報
	half3   normalWS  = TransformObjectToWorldNormal(input.normalOS);
	half3   tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
	half3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, input.tangentOS.w);

	// R:Shadow mask G:Alpha B:Outline
	half outlineMask = SAMPLE_TEXTURE2D_LOD(_ControlMap2, sampler_ControlMap2, input.uv, 0).b;

	// アウトラインの太さを距離で変える
	outlineMask *= lerp(0.5, 10, saturate(abs(min(0, TransformWorldToView(positionWS).z / 10))));

	// アウトライン用法線はタンジェントスペースで格納されている
	half3  outlineNormalTS   = normalize(input.color.xyz * 2 - 1);
	half   outlineWeight     = input.color.a;
	half3  outlineNormalWS   = TransformTangentToWorldDir(outlineNormalTS, tangentToWorld);
	half   outlineWidth      = _OutlineWidth / 1000 * outlineWeight * outlineMask;
	float3 outlinePositionWS = positionWS + outlineNormalWS * outlineWidth;

	output.positionCS = TransformWorldToHClip(outlinePositionWS);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	output.normalWS = normalWS;
	output.positionWS = positionWS;

	return output;
}

half4 fragOutline(Varyings input) : SV_Target
{
	half3 directLightColor = SampleMainLight(input.positionWS, input.normalWS);

	half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
	col.rgb *= directLightColor * _OutlineColor;

	AlphaClip(input.uv, col.a);

	return col;
}
