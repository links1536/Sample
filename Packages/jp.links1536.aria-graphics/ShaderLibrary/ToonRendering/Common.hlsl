#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

half ToonStep(half value, half threshold, half softness)
{
	half valueMin = saturate(threshold - softness * 0.5);
	half valueMax = saturate(threshold + softness * 0.5);
	return saturate((value - valueMin) / max(HALF_EPS, valueMax - valueMin));
}

float ToonStep(float value, float threshold, float softness)
{
	float valueMin = saturate(threshold - softness * 0.5);
	float valueMax = saturate(threshold + softness * 0.5);
	return saturate((value - valueMin) / max(FLT_EPS, valueMax - valueMin));
}
