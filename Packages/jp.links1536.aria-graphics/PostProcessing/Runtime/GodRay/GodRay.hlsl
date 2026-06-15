#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/PostProcessing/EncodeHDR.hlsl"
#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/PostProcessing/Upsampling/NearestDepthUpsampling.hlsl"
#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/Noise.hlsl"

#define MAX_RAYMARCH_STEP 128

struct RayVaryings
{
	float4 positionCS	: SV_POSITION;
	float2 texcoord		: TEXCOORD0;
	float2 ray			: TEXCOORD1;
};

TEXTURE2D(_GodRayTexture);
TEXTURE2D(_LowResolutionDepthTexture);

CBUFFER_START(UnityPerMaterial)
	float4  _GodRayMarkParams;
	float4  _GodRayBlurParams1;
	float4  _GodRayBlurParams2;
	float4  _GodRayApplyParams;
	
	half4   _ColorLight;
	half4   _ColorShadow;

	float3 _FogNoiseDirection;
	float  _FogNoiseSpeed;
	float  _FogNoiseScale;
	float2 _FogNoiseMinMax;

	float4 _GodRayTexture_TexelSize;
CBUFFER_END

#define GODRAY_THRESHOLD _GodRayMarkParams.x
#define GODRAY_DISTANCE _GodRayMarkParams.y
#define GODRAY_INTENSITY _GodRayMarkParams.z

#define GODRAY_SAMPLE_COUNT _GodRayBlurParams1.x
#define GODRAY_RCP_SAMPLE_COUNT _GodRayBlurParams1.y
#define GODRAY_NOISE_SCALE _GodRayBlurParams1.z

#define GODRAY_DENSITY _GodRayBlurParams2.x
#define GODRAY_WEIGHT _GodRayBlurParams2.y
#define GODRAY_DECAY _GodRayBlurParams2.z



RayVaryings vert(Attributes input)
{
	RayVaryings output = (RayVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
	float4 pos = input.positionOS;
	float2 uv = input.uv;
#else
	float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
	float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
#endif

	output.positionCS = pos;
	output.texcoord = DYNAMIC_SCALING_APPLY_SCALEBIAS(uv);

	Light mainLight = GetMainLight();
	float3 lightDirection = mainLight.direction;
	float3 lightPositionWS = GetCameraPositionWS() - lightDirection * _ProjectionParams.z;
	float3 lightPositionVS = TransformWorldToView(lightPositionWS);
	float4 lightPositionCS = TransformWorldToHClip(lightPositionWS);
	float2 lightPositionUV = lightPositionCS.xy / lightPositionCS.w * 0.5 + 0.5;
	lightPositionUV.y = 1 - lightPositionUV.y;
	output.ray = (uv - lightPositionUV) * sign(lightPositionVS.z);

	return output;
}

half4 fragMark(RayVaryings input) : SV_Target
{
	float2 uv = input.texcoord;
	float depth = LinearEyeDepth(SampleSceneDepth(input.texcoord), _ZBufferParams);

	//half distanceMask = step(min(GODRAY_DISTANCE, _ProjectionParams.z - 1), depth);
	half distanceMask = step(GODRAY_DISTANCE, depth);
	half colorMask = distanceMask;

	half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb * colorMask;
	color = saturate(color);
	color = max(0, (color) - GODRAY_THRESHOLD) * GODRAY_INTENSITY;

	return half4(color, 1);
}

half4 fragBlur(RayVaryings input) : SV_Target
{
	float2 uv = input.texcoord;
	float2 direction = normalize(input.ray) * min(1, length(input.ray));

	half dither = InterleavedGradientNoise(input.positionCS.xy, 0) * GODRAY_NOISE_SCALE * GODRAY_RCP_SAMPLE_COUNT;
	//if (GODRAY_NOISE_SCALE >0)
	//	dither -= abs(RandomNoise_2_1_Float(uv, _Time.y)) * GODRAY_NOISE_SCALE;

	// UVサンプル
	float2 start = uv - direction * dither;
	float2 end = start - direction * GODRAY_DENSITY;

	// サンプルする
	half illuminationDecay = 1.0f;
	half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
	for (int i = 0; i < GODRAY_SAMPLE_COUNT; i++)
	{
		uv = lerp(start, end, i * GODRAY_RCP_SAMPLE_COUNT);
		half3 sampleColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
		sampleColor *= illuminationDecay * GODRAY_WEIGHT;
		color += sampleColor;
		illuminationDecay *= GODRAY_DECAY;
	}
	color *= GODRAY_RCP_SAMPLE_COUNT;

	return half4(color, 1);
}

half4 fragApply(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
	half3 color = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));
#if defined(ENABLE_UPSAMPLING)
	half4 godray = SampleNearestDepthFiltered(uv, _GodRayTexture_TexelSize.xy, _GodRayTexture, _LowResolutionDepthTexture, _CameraDepthTexture);
#else
	half4 godray = SAMPLE_TEXTURE2D_X(_GodRayTexture, sampler_LinearClamp, uv);
#endif
	half3 finalColor = (color * godray.a) + (lerp(_ColorShadow.rgb, _ColorLight.rgb, godray.rgb));
	return EncodeHDR(finalColor);
}
