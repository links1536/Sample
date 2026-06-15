#pragma once

#include "../include/character_common.hlsl"

struct Attributes
{
	float4 positionOS	: POSITION;
	half3	normalOS	: NORMAL;
	half4	tangentOS	: TANGENT;
	float2 uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS	: SV_POSITION;
	float2 uv			: TEXCOORD0;
	half3	normalWS	: TEXCOORD1;
	half4	tangentWS	: TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vertDepthNormal(Attributes input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	Varyings output;
	ZERO_INITIALIZE(Varyings, output);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.positionCS = TransformWorldToHClip(positionWS);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);

	// 法線情報
	half3   normalWS = TransformObjectToWorldNormal(input.normalOS);
	half3   tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
	output.normalWS = normalWS;
	output.tangentWS = half4(tangentWS, input.tangentOS.w);

	return output;
}

half4 fragDepthNormal(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	AlphaClip(input.uv);

	half3 normalWS = SampleNormalWS(input.uv, input.normalWS, input.tangentWS.xyz, input.tangentWS.w, 1);
	normalWS = NormalizeNormalPerPixel(normalWS);
	return half4(normalWS, 0.0);
}
