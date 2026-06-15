Shader "Aria/Character/BasicDepthOffset"
{
	Properties
	{
		_MainTex("Base Map", 2D) = "white" {}
		_Color("Base Color", Color) = (1, 1, 1, 1)

		_EnableShadingMap("Enable Shading Map", Int) = 0
		[NoScaleOffset] _ShadingMap("Shading Map", 2D) = "white" {}
		_ShadingColor("Shading Color", Color) = (1, 1, 1, 1)

		[NoScaleOffset] _NormalMap("Nomral Map", 2D) = "bump" {}
		_NormalScale("Normal Scale", Float) = 1

		_EnableLighting("Enable Lighting", Int) = 1
		_EnableMatCapMap("Enable MatCap", Int) = 0
		_MatCapMap("MatCap", 2D) = "white" {}
		[NoScaleOffset] _FaceShadowMaskTexture("Face Shadow Map", 2D) = "black" {}

		_ShadowSoftness("Shadow Softness", Range(0, 1)) = 0.01
		_ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.63

		_EnableControlMap1("Enable ControlMap1", Float) = 0
		[NoScaleOffset] _ControlMap1("R:Specular G:Rim B:Highlight", 2D) = "black" {}

		_EnableSpecular("Enable Specular", Float) = 1
		_Smoothness("Smoothness Power", Range(0, 1)) = 1

		_EnableRimLight("Enable Rim", Float) = 1
		_RimIntensity("Rim Intensity", Float) = 1000
		_RimPower("Rim Power", Float) = 50
		_RimSoftness("Rim Softness", Range(0, 1)) = 0.5
		_RimThreshold("Rim Threshold", Range(0, 1)) = 0.5

		_EnableHighLight("Enable High Light", Float) = 1
		_HighLightColor("High Light Color", Color) = (1, 1, 1, 1)

		_EnableControlMap2("Enable ControlMap2", Float) = 0
		[NoScaleOffset] _ControlMap2("R:Shadow G:Alpha B:Outline", 2D) = "white" {}
		_AlphaClip("Alpha Clip", Range(0, 1)) = 0.5
		_EnableOutline("EnableOutline", Int) = 1
		_OutlineWidth("Outline Width", Float) = 0.5
		_OutlineColor("Outline Color", Color) = (0.5, 0.5, 0.5, 1)

		_RenderingMode("Rendering Mode", Int) = 0

		_SrcBlend("SrcBlend", Int) = 1
		_DstBlend("DstBlend", Int) = 0
		
		_ZWrite("ZWrite", Int) = 1

		_DepthOffset("Depth Offset", Float) = 0
	}
	SubShader
	{
		Tags {
			"RenderPipeline"="UniversalPipeline"
			"RenderType"="Opaque"
		}
		LOD 100

		HLSLINCLUDE
			#pragma shader_feature_local_fragment _ ALPHA_FROM_CONTROL
			#pragma shader_feature_local_fragment _ ALPHACLIP_ON
			#include "include/character_input.hlsl"
		ENDHLSL

		Pass
		{
			Name "BaseColor"

			Tags
			{
				"LightMode" = "UniversalForward"
			}

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			AlphaToMask On

			HLSLPROGRAM
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment frag

			//-----------------------------------
			//    Unity
			//-----------------------------------
			#pragma multi_compile_instancing

			//-----------------------------------
			//    自作
			//-----------------------------------
			#include_with_pragmas "Packages/jp.links1536.aria-graphics/ShaderLibrary/Keywords/Fog.hlsl"
			#pragma multi_compile _ _MIRROR_BAKING

			#pragma shader_feature_local_fragment _ FACELIGHTING_ON
			#pragma shader_feature_local_fragment _ NORMALMAP_ON
			#pragma shader_feature_local_fragment _ TRANSPARENT_ON

			//#pragma shader_feature_local _ DEPTH_OFFSET_ON

			#include "pass/character_forward.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthOffset"

			Tags
			{
				"LightMode" = "DepthOffset"
			}

			Blend SrcAlpha OneMinusSrcAlpha
			AlphaToMask On

			HLSLPROGRAM
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment frag

			//-----------------------------------
			//    Unity
			//-----------------------------------
			#pragma multi_compile_instancing

			//-----------------------------------
			//    自作
			//-----------------------------------
			#include_with_pragmas "Packages/jp.links1536.aria-graphics/ShaderLibrary/Keywords/Fog.hlsl"
			#pragma multi_compile _ _MIRROR_BAKING

			#pragma shader_feature_local_fragment _ FACELIGHTING_ON
			#pragma shader_feature_local_fragment _ NORMALMAP_ON
			#pragma shader_feature_local_fragment _ TRANSPARENT_ON

			#pragma shader_feature_local _ DEPTH_OFFSET_ON

			#include "pass/character_forward.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Outline"

			Tags
			{
				"LightMode" = "Outline"
			}
			Blend Off
			Cull Front

			HLSLPROGRAM
			#pragma vertex vertOutline
			#pragma fragment fragOutline

			#pragma shader_feature_local_fragment _ TRANSPARENT_ON

			#include "pass/character_outline.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"

			Tags
			{
				"LightMode" = "DepthOnly"
			}
			
			ColorMask 0

			HLSLPROGRAM
			#pragma vertex vertDepth
			#pragma fragment fragDepth

			#include "pass/character_depthonly.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthNormals"

			Tags
			{
				"LightMode" = "DepthNormals"
			}

			HLSLPROGRAM
			#pragma vertex vertDepthNormal
			#pragma fragment fragDepthNormal

			#pragma shader_feature_local_fragment _ NORMALMAP_ON

			#include "pass/character_depthnormals.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"

			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow

			#include "pass/character_shadowcaster.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "CharacterMask"

			Tags
			{
				"LightMode" = "CharacterMask"
			}

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "pass/character_mask.hlsl"
			ENDHLSL
		}
	}

	CustomEditor "AriaEditor.Shaders.Characters.ToonShaderEditor"
}
