#pragma once

struct Attributes
{
	float4 positionOS	: POSITION;
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

Varyings vertDepth (Attributes input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	Varyings output;
	ZERO_INITIALIZE(Varyings, output);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.positionCS = TransformWorldToHClip(positionWS);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	return output;
}

half4 fragDepth(Varyings input) : SV_Target
{
	AlphaClip(input.uv);

	return 0;
}
