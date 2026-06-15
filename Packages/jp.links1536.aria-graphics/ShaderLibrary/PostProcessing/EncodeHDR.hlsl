#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

half4 EncodeHDR(half3 color)
{
#if _USE_RGBM
	half4 outColor = EncodeRGBM(color);
#else
	half4 outColor = half4(color, 1.0);
#endif

#if UNITY_COLORSPACE_GAMMA
	return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
#else
	return outColor;
#endif
}

half3 DecodeHDR(half4 color)
{
#if UNITY_COLORSPACE_GAMMA
	color.xyz *= color.xyz; // γ to linear
#endif

#if _USE_RGBM
	return DecodeRGBM(color);
#else
	return color.xyz;
#endif
}
