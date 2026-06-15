Shader "Aria/Map/Mirror/Opaque"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_AlphaClip("Alpha Clip", Range(0, 1)) = 0.5

		[Normal][NoScaleOffset] _NormalMap("Nomral Map", 2D) = "bump" {}
		_NormalScale("Normal Scale", Range(-1, 1)) = 1

		[HideInInspector] _ReflectionTexture("", 2D) = "white" {}
		_MirrorMask("Mirror Mask", 2D) = "white" {}

		_MatCapMap("MatCap", 2D) = "white" {}
		
		_ControlMap1("R:Metallic G:Smoothness B:Occlusion", 2D) = "white" {}
		_Metallic("Metallic", Range(0, 1)) = 1
		_Smoothness("Smoothness", Range(0, 1)) = 1
		_Occlusion("Occlusion", Range(0, 1)) = 1
		
		[NoScaleOffset] _GlowTex("Glow Texture", 2D) = "white" {}
		[HDR] _GlowColor("Glow Color", Color) = (0, 0, 0, 1)

		_FogFactor("Fog Factor", Range(0, 1)) = 1

		[HideInInspector]_RenderingMode("Rendering Mode", Int) = 0
		_CullMode("Cull Mode", Int) = 2
		[HideInInspector]_SrcBlend("SrcBlend", Int) = 1
		[HideInInspector]_DstBlend("DstBlend", Int) = 0
	}
	SubShader
	{
		Tags
		{
			"RendererPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
		}
		Cull[_CullMode]

		HLSLINCLUDE
			#include "include/map_input.hlsl"
		ENDHLSL

		Pass
		{
			Name "MapForward"
			Tags
			{
				"LightMode" = "UniversalForward"
			}
			Blend[_SrcBlend][_DstBlend]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_instancing
			#pragma multi_compile _ _MIRROR_RENDERING

			#include_with_pragmas "Packages/jp.links1536.aria-graphics/ShaderLibrary/Keywords/Fog.hlsl"
			#include_with_pragmas "keywords/map_unity_keywords.hlsl"

			//----------------------------------
			// Shader Keywords
			#pragma shader_feature_local _ NORMALMAP_ON

			#pragma shader_feature_local_fragment _ SHADINGMAP_ON
			#pragma shader_feature_local_fragment _ LIGHTING_OFF
			#pragma shader_feature_local_fragment _ GLOWMAP_ON
			#pragma shader_feature_local_fragment _ CONTROLMAP_1_ON
			#pragma shader_feature_local_fragment _ CONTROLMAP_2_ON
			#pragma shader_feature_local_fragment _ MATCAP_ON
			#pragma shader_feature_local_fragment _ SPECULAR_OFF
			#pragma shader_feature_local_fragment _ ALPHACLIP_ON
			#pragma shader_feature_local_fragment _ TRANSPARENT_ON

			#include "pass/map_forward.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "MapBloom"
			Tags
			{
				"LightMode" = "BloomEmission"
			}

			HLSLPROGRAM
			#pragma vertex vertEmission
			#pragma fragment fragEmission

			#pragma multi_compile_fog

			//----------------------------------
			// Shader Keywords
			#pragma shader_feature_local_fragment _ GLOWMAP_ON
			#pragma shader_feature_local_fragment _ ALPHACLIP_ON
			
			#include "pass/map_emission.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "MapDepth"
			Tags { "LightMode" = "DepthOnly" }

			ColorMask 0

			HLSLPROGRAM
			#pragma vertex vertDepth
			#pragma fragment fragDepth
			
			//----------------------------------
			// Shader Keywords
			#pragma shader_feature_local _ ALPHACLIP_ON

			#include "pass/map_depthonly.hlsl"
			ENDHLSL
		}
		
		Pass
		{
			Name "MapDepthNormals"
			Tags
			{
				"LightMode" = "DepthNormals"
			}

			HLSLPROGRAM
			#pragma vertex vertNormal
			#pragma fragment fragNormal

			//----------------------------------
			// Shader Keywords
			#pragma shader_feature_local _ NORMALMAP_ON
			#pragma shader_feature_local _ ALPHACLIP_ON

			#include "pass/map_depthnormals.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "MapShadow"
			Tags { "LightMode" = "ShadowCaster" }

			ColorMask 0

			HLSLPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow
			
			//----------------------------------
			// Shader Keywords
			#pragma shader_feature_local _ ALPHACLIP_ON

			#include "pass/map_shadowcaster.hlsl"
			ENDHLSL
		}
	}
	
	CustomEditor "AriaEditor.Shaders.Map.MapShaderEditor"
}