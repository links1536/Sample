Shader "Hidden/Aria/PostProcessing/VolumetricFog"
{
	Properties
	{
		_SampleCount("SampleCount", Int) = 100
		_ColorLight("Color Light", Color) = (1, 1, 1, 1)
		_ColorShadow("Color Shadow", Color) = (1, 1, 1, 1)
		_Intensity("Intensity", Float) = 1
		_Extinction("Extinction", Float) = 1
		_NearFar("x...Near y...Far", Vector) = (0, 50, 0, 0)
		_NoiseScale("Noise Scale", Float) = 0.1

		_FogNoiseDirection("Fog Noise Speed", Vector) = (0, 0, 0, 0)
		_FogNoiseSpeed("Fog Noise Speed", Float) = 1
		_FogNoiseScale("Fog Noise Scale", Float) = 1
		_FogNoiseMinMax("Fog Noise Min/Max", Vector) = (0, 1, 0, 0)
	}
	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}
		LOD 100

		HLSLINCLUDE
			#pragma multi_compile_local _ _USE_RGBM
			#pragma multi_compile _ _USE_DRAW_PROCEDURAL
		ENDHLSL

		Pass
		{
			Name "VolumetricFog"

			Tags
			{
				"LightMode"="UniversalForward"
			}

			Blend Off
			ZWrite Off
			ZTest Always
			Cull Off

			HLSLPROGRAM
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX

			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			//#pragma multi_compile_fragment _ _SHADOWS_SOFT
			//#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			//#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

			#pragma multi_compile_local_fragment _ ENABLE_FOG_NOISE

			#pragma vertex vert
			#pragma fragment frag
			#include "VolumetricFog.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Apply"

			Blend Off
			ZWrite Off
			ZTest Always
			Cull Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment fragApply

			#pragma multi_compile_fragment _ ENABLE_UPSAMPLING
			
			#include "VolumetricFog.hlsl"
			ENDHLSL
		}
	}
}
