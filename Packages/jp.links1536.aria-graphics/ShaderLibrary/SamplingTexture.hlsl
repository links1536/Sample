#pragma once

half3 SampleTexture2DBox(TEXTURE2D_PARAM(textureMap, textureSample), float2 uv, float2 texelSize, float sampleScale)
{
	float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);
	half3 color = 0;
	color += SAMPLE_TEXTURE2D(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.xy)).xyz;
	color += SAMPLE_TEXTURE2D(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.zy)).xyz;
	color += SAMPLE_TEXTURE2D(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.xw)).xyz;
	color += SAMPLE_TEXTURE2D(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.zw)).xyz;
	return color * 0.25;
}

half3 SampleTexture2DBoxX(TEXTURE2D_X_PARAM(textureMap, textureSample), float2 uv, float2 texelSize, float sampleScale)
{
	float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);
	half3 color = 0;
	color += SAMPLE_TEXTURE2D_X(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.xy)).xyz;
	color += SAMPLE_TEXTURE2D_X(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.zy)).xyz;
	color += SAMPLE_TEXTURE2D_X(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.xw)).xyz;
	color += SAMPLE_TEXTURE2D_X(textureMap, textureSample, UnityStereoTransformScreenSpaceTex(uv + d.zw)).xyz;
	return color * 0.25;
}

half4 SampleFlowMap(TEXTURE2D_PARAM(textureMap, textureSample), float2 uv, TEXTURE2D_PARAM(flowMap, flowSampler), float2 flowUV, float flowPower, float flowSpeed)
{
	float2 flowDir = SAMPLE_TEXTURE2D(flowMap, flowSampler, flowUV).xy;
	float2 flowOffset = (flowDir - 0.5) * flowPower;
	half4 color1 = SAMPLE_TEXTURE2D(textureMap, textureSample, uv + (flowOffset * frac(_Time.x * flowSpeed)));
	half4 color2 = SAMPLE_TEXTURE2D(textureMap, textureSample, uv + (flowOffset * frac(_Time.x * flowSpeed + 0.5)));

	float blend = abs((0.5 - frac(_Time.x * flowSpeed)) / 0.5);
	return lerp(color1, color2, blend);
}

#define SAMPLE_TEXTURE2D_BOX(textureName, samplerName, uv, texelSize, sampleScale) \
	SampleTexture2DBox(TEXTURE2D_ARGS(textureName, samplerName), uv, texelSize, sampleScale)
	
#define SAMPLE_TEXTURE2D_BOX_X(textureName, samplerName, uv, texelSize, sampleScale) \
	SampleTexture2DBoxX(TEXTURE2D_X_ARGS(textureName, samplerName), uv, texelSize, sampleScale)

#define SAMPLE_FLOWMAP(textureName, samplerName, uv, flowTexName, flowSamplerName, flowUV, flowPower, flowSpeed) \
	SampleFlowMap(TEXTURE2D_ARGS(textureName, samplerName), uv, TEXTURE2D_ARGS(flowTexName, flowSamplerName), flowUV, flowPower, flowSpeed)
