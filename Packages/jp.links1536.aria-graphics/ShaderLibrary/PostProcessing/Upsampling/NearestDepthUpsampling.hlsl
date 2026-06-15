#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define DEPTH_THRESHOLD 0.5

inline float2 ComputeUV(float2 uv, float2 offset, float2 texelSize)
{
	return uv + (offset * texelSize) - (0.5 * texelSize);
}

inline float SampleLinearEyeDepth(TEXTURE2D_FLOAT(depthTexture), float2 uv)
{
	float depth = SAMPLE_TEXTURE2D_X(depthTexture, sampler_PointClamp, uv).r;
	return LinearEyeDepth(depth, _ZBufferParams);
}

inline void GetNearDepthUV(float distance, float2 uv, inout float minDistance, inout float2 nearestUV)
{
	if (distance < minDistance)
	{
		minDistance = distance;
		nearestUV = uv;
	}
}

float4 SampleNearestDepthFiltered(float2 uv, float2 lowResTexelSize, TEXTURE2D_X_FLOAT(colorTexture), TEXTURE2D_X_FLOAT(lowResDepthTexture), TEXTURE2D_X_FLOAT(highResDepthTexture))
{
	float highResDepth = SampleLinearEyeDepth(highResDepthTexture, uv);

	float2 lowResUV00 = ComputeUV(uv, float2(0, 0), lowResTexelSize);
	float2 lowResUV01 = ComputeUV(uv, float2(0, 1), lowResTexelSize);
	float2 lowResUV10 = ComputeUV(uv, float2(1, 0), lowResTexelSize);
	float2 lowResUV11 = ComputeUV(uv, float2(1, 1), lowResTexelSize);

	float lowResDepth00 = SampleLinearEyeDepth(lowResDepthTexture, lowResUV00);
	float lowResDepth01 = SampleLinearEyeDepth(lowResDepthTexture, lowResUV01);
	float lowResDepth10 = SampleLinearEyeDepth(lowResDepthTexture, lowResUV10);
	float lowResDepth11 = SampleLinearEyeDepth(lowResDepthTexture, lowResUV11);

	float distance00 = abs(lowResDepth00 - highResDepth);
	float distance01 = abs(lowResDepth01 - highResDepth);
	float distance10 = abs(lowResDepth10 - highResDepth);
	float distance11 = abs(lowResDepth11 - highResDepth);

	float4 result = 0;
	if (distance00 < DEPTH_THRESHOLD
		&& distance01 < DEPTH_THRESHOLD
		&& distance10 < DEPTH_THRESHOLD
		&& distance11 < DEPTH_THRESHOLD)
	{
		// なにもフィルタリングしていない線形サンプリング
		result = SAMPLE_TEXTURE2D_X(colorTexture, sampler_LinearClamp, uv);
	}
	else
	{
		float minDistance = 1e9;
		float2 nearestUV = uv;
		GetNearDepthUV(distance00, lowResUV00, minDistance, nearestUV);
		GetNearDepthUV(distance01, lowResUV01, minDistance, nearestUV);
		GetNearDepthUV(distance10, lowResUV10, minDistance, nearestUV);
		GetNearDepthUV(distance11, lowResUV11, minDistance, nearestUV);

		// Nearest-Depth Filterによるサンプリング
		result = SAMPLE_TEXTURE2D_X(colorTexture, sampler_PointClamp, nearestUV);
	}
	return result;
}
