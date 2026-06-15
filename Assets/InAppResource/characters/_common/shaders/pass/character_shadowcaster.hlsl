#pragma once

struct Attributes
{
	float4 positionOS	: POSITION;
	float3 normalOS		: NORMAL;
	float2 uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS	: SV_POSITION;
	float2 uv			: TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vertShadow (Attributes input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	Varyings output;
	ZERO_INITIALIZE(Varyings, output);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

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
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	return output;
}

half4 fragShadow (Varyings input) : SV_Target
{
	AlphaClip(input.uv);

	return 0;
}
