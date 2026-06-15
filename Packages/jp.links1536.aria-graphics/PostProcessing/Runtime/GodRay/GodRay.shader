Shader "Hidden/Aria/PostProcessing/GodRay"
{
	Properties
	{
		_SampleCount("SampleCount", Int) = 100
		_GodRayMarkParams("God Ray Mark Param", Vector) = (0.9, 100, 0, 0)
		_GodRayBlurParams1("God Ray Blur Param1", Vector) = (10, 0.1, 1, 1)
		_GodRayBlurParams2("God Ray Blur Param2", Vector) = (1, 1, 0.9, 1)
		_GodRayApplyParams("God Ray Apply Param", Vector) = (0.5, 0, 0, 0)

		_ColorLight("Color Light", Color) = (1, 1, 1, 1)
		_ColorShadow("Color Shadow", Color) = (0, 0, 0, 1)

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
			#pragma multi_compile _ _USE_DRAW_PROCEDURAL
		ENDHLSL

		Pass
		{
			Name "GodRayMark"

			Blend Off
			ZWrite Off
			ZTest Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragMark
			#include "GodRay.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Blur"

			Blend Off
			ZWrite Off
			ZTest Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragBlur
			#include "GodRay.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Apply"

			Blend Off
			ZWrite Off
			ZTest Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragApply

			#pragma multi_compile_fragment _ ENABLE_UPSAMPLING

			#include "GodRay.hlsl"
			ENDHLSL
		}
	}
}
