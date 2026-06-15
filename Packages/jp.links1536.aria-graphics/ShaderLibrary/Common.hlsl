#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

half3 MixFogWithAlpha(half3 color, float fogFactor)
{
	half3 tempColor = MixFogColor(color, unity_FogColor.rgb, fogFactor);
	return lerp(color, tempColor, unity_FogColor.a);
}

half3 MixFogWithAlpha(half3 color, float fogFactor, half fogIntensity)
{
	half3 tempColor = MixFogColor(color, unity_FogColor.rgb, fogFactor);
	return lerp(color, tempColor, unity_FogColor.a * fogIntensity);
}

half ComputeFogMask(float fogFactor)
{
	return ComputeFogIntensity(fogFactor);
}

half ComputeFogMask(float fogFactor, half fogIntensity)
{
	return half(1.0) - (half(1.0) - ComputeFogIntensity(fogFactor)) * fogIntensity;
}

half ComputeFogMaskWithAlpha(float fogFactor)
{
	return half(1.0) - (half(1.0) - ComputeFogIntensity(fogFactor)) * unity_FogColor.a;
}

half ComputeFogMaskWithAlpha(float fogFactor, half fogIntensity)
{
	return half(1.0) - (half(1.0) - ComputeFogIntensity(fogFactor)) * unity_FogColor.a * fogIntensity;
}