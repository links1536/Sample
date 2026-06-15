#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

static float4 _BilinearWeights[4] =
{
	float4(9.0, 3.0, 3.0, 1.0) / 16,
	float4(3.0, 9.0, 1.0, 3.0) / 16,
	float4(3.0, 1.0, 9.0, 3.0) / 16,
	float4(1.0, 3.0, 3.0, 9.0) / 16,
};

static float4 _Weight = ( 0.22702702702, 0.19459459459, 0.12162162162, 0.05405405405);

float ComputeBlendFactor(float eyeDepth, float fogDepth)
{
	return saturate(fogDepth == 0 ? 0 : abs(eyeDepth / fogDepth));
}

half4 fragUpsamplingH(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
	half4 blured = fragH(input);
	half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

	float depth = SampleSceneDepth(input.texcoord);
	float eyeDepth = Linear01Depth(depth, _ZBufferParams);
	float fogDepth = Linear01Depth(color.a, _ZBufferParams);

	half4 finalColor = 1;
	finalColor.rgb = lerp(color.rgb, blured.rgb, ComputeBlendFactor(eyeDepth, fogDepth));
	return finalColor;
}

half4 fragUpsamplingV(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
	half4 blured = fragV(input);
	half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

	float depth = SampleSceneDepth(input.texcoord);
	float eyeDepth = Linear01Depth(depth, _ZBufferParams);
	float fogDepth = Linear01Depth(color.a, _ZBufferParams);

	half4 finalColor = 1;
	finalColor.rgb = lerp(color.rgb, blured.rgb, ComputeBlendFactor(eyeDepth, fogDepth));
	return finalColor;
}
