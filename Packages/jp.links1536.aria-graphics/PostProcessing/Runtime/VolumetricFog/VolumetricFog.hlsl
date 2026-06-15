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
	float3 ray			: TEXCOORD1;
};

TEXTURE2D(_VolumetricFogTexture);
TEXTURE2D(_LowResolutionDepthTexture);

float4 _VolumetricFogTexture_TexelSize;

CBUFFER_START(UnityPerMaterial)

	half3  _ColorLight;
	float  _Intensity;

	half3  _ColorShadow;
	float  _Extinction;

	float2 _NearFar;
	float  _NoiseScale;
	uint   _SampleCount;

	float3 _FogNoiseDirection;
	float  _FogNoiseSpeed;
	float2 _FogNoiseMinMax;
	float  _FogNoiseScale;

CBUFFER_END

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
	output.texcoord = uv;

	// Get view vector using UV
	float3 viewVector = mul(unity_CameraInvProjection, float4(uv * 2 - 1, 0, -1)).xyz;
	viewVector = mul(unity_CameraToWorld, float4(viewVector, 0)).xyz;
	output.ray = viewVector;

	return output;
}

void SampleShadow(float3 positionWS, out float3 shadowPositionWS, out half shadowAttenuation)
{
#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
	half cascadeIndex = ComputeCascadeIndex(positionWS);
#else
	half cascadeIndex = half(0.0);
#endif

	// ShadowCoordを計算する
	half4 shadowParams = GetMainLightShadowParams();
	float4x4 shadowMatrix = _MainLightWorldToShadow[cascadeIndex];
	float4 shadowCoord = mul(shadowMatrix, float4(positionWS, 1.0));

	// シャドウマップから影情報を取得
	half fade = half(1.0) - GetMainLightShadowFade(positionWS);
	float shadowDepth = SAMPLE_TEXTURE2D_LOD(_MainLightShadowmapTexture, sampler_LinearClamp, shadowCoord.xy, 0).r;
	shadowAttenuation = saturate(lerp(0, step(shadowDepth, shadowCoord.z), shadowParams.x * fade));

	// ワールド座標に変換
	float2 uv = shadowCoord.xy;
	shadowPositionWS = mul(shadowMatrix, float4(uv.x * 2 - 1, uv.y * 2 - 1, shadowDepth, 1)).xyz;
}

half3 CalculateLightColor(Light light)
{
	half attenuation = saturate(light.distanceAttenuation) * saturate(light.shadowAttenuation);
	return max(0, light.color * attenuation);
}

half3 SampleLight(float3 positionWS)
{
	// 影・座標を取得する
	float3 shadowPositionWS = 0;
	half shadowAttenuation = 0;
	SampleShadow(positionWS, shadowPositionWS, shadowAttenuation);

	Light mainLight = GetMainLight();
	half3 lightColor = CalculateLightColor(mainLight);

#if defined(_ADDITIONAL_LIGHTS)
	uint pixelLightCount = GetAdditionalLightsCount();

	// Forward+
	#if USE_CLUSTER_LIGHT_LOOP
		[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
		{
			CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
			Light additionalLight = GetAdditionalLight(lightIndex, positionWS);
			lightColor += CalculateLightColor(additionalLight);
		}
	#endif

	// 通常の加算ライト
	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light additionalLight = GetAdditionalLight(lightIndex, positionWS);
		lightColor += CalculateLightColor(additionalLight);
	LIGHT_LOOP_END
#endif

	half3 color = lerp(_ColorShadow, lightColor * _ColorLight, shadowAttenuation) * _Intensity;

	//float3 lightPositionWS = shadowPositionWS + normalize(_MainLightPosition.xyz) * 1000;
	//// 現在の位置のフォグの色を取得
	//half3 fogColorFade = (positionWS - lightPositionWS) / (shadowPositionWS - lightPositionWS);
	//half3 fogColor = lerp(_ColorStart.rgb, _ColorEnd.rgb, FastSRGBToLinear(saturate(positionWS.y / 100)));
	return max(0, color);
}

float GetDensity(float3 positionWS)
{
	// 3Dフォグ
	// TODO: 画面上で直接やるのは重いので、3DテクスチャをComputeShaderで生成してサンプリングする方式にしたい
	float density = 1;
#if defined(ENABLE_FOG_NOISE)
	float3 fogNoiseUV = positionWS;
	fogNoiseUV += _FogNoiseDirection * _FogNoiseSpeed * _Time.y;
	fogNoiseUV *= _FogNoiseScale;
	float fogNoise = PerlinNoise_3_1_Float(fogNoiseUV);
	density = lerp(_FogNoiseMinMax.x, _FogNoiseMinMax.y, fogNoise);
#endif
	return max(0, density);
}

half4 frag(RayVaryings input) : SV_Target
{
	float2 uv = input.texcoord;

	float len = length(input.ray);
	float3 rayDir = normalize(input.ray);

	// 深度値を求める
	float depth = SampleSceneDepth(input.texcoord);
	float linearDepth = LinearEyeDepth(depth, _ZBufferParams);

	float near = _NearFar.x;
	float far = _NearFar.y;
	//far = min(linearDepth, far);

	float3 startPositionWS = GetCameraPositionWS();
	float3 endPositionWS = startPositionWS + rayDir * linearDepth;

	if (_SampleCount <= 0)
		return 0;

	uint stepCount = clamp(_SampleCount, 0, MAX_RAYMARCH_STEP);
	float stepDepth = (far - near) / stepCount;

	float currentDepth = near + stepDepth;
	currentDepth += RandomNoise_2_1_Float(uv, 0) * _NoiseScale * stepDepth;

	uint actualStepCount = 0;
	float3 accumulatedColor = 0;
	float accumulatedTransmittance = 1.0;
	float extinction = max(FLT_EPS, _Extinction);
	
	[loop]
	for (uint i = 0; i < stepCount; ++i)
	{
		if (currentDepth > far || currentDepth >= linearDepth)
			break;

		actualStepCount++;

		// レイの現在位置
		float3 currentPositionWS = startPositionWS + rayDir * currentDepth;

		// 密度と透過率の計算
		float density = GetDensity(currentPositionWS);
		float currentExtinction = extinction * density;

		// このステップにおける透過率の減衰量
		float localTransmittance = exp(-currentExtinction * stepDepth);

		// ライトの色を計算
		half3 lightColor = SampleLight(currentPositionWS);

		// 光の影響度
		half3 scattering = lightColor * (1.0 - localTransmittance);
		accumulatedColor += scattering * accumulatedTransmittance;

		// 累積透過率の更新
		accumulatedTransmittance *= localTransmittance;

		// 次のループの距離を計算
		currentDepth += stepDepth;
	}

	// ライトのカラーを決定する
	//color = FastSRGBToLinear(color);
	half4 fogColor = half4(accumulatedColor, accumulatedTransmittance);
	return fogColor;
}

half4 fragApply(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
	half3 color = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));
#if defined(ENABLE_UPSAMPLING)
	half4 fog = SampleNearestDepthFiltered(uv, _VolumetricFogTexture_TexelSize.xy, _VolumetricFogTexture, _LowResolutionDepthTexture, _CameraDepthTexture);
#else
	half4 fog = SAMPLE_TEXTURE2D_X(_VolumetricFogTexture, sampler_LinearClamp, uv);
#endif
	half3 finalColor = color + fog.rgb;
	return EncodeHDR(finalColor);
}
