Shader "Aria/Effect/Dissolve"
{
	Properties
	{
		_Scroll("Scroll", Vector) = (0,0,0,0)
		
		[Header(Mask)]
		[NoScaleOffset] _GlobalMaskTexture("Global Mask Texture", 2D) = "white" {}
		_DissolveMaskTexture("Dissolve Mask", 2D) = "white" {}
		
		[Header(Flow Map)]
		[NoScaleOffset]_FlowTexture("Flow Map", 2D) = "white" {}
		_FlowPower("Flow Power", Float) = 1
		_FlowSpeed("Flow Speed", Float) = 1

		[Header(Main)]
		[Space]
		[NoScaleOffset]_ColorMaskTexture("Color Mask Texture", 2D) = "white" {}
		_MainColor("Main Color", Color) = (1,1,1,1)
		_AddPower("Add Power", Range(0, 8)) = 0

		[Toggle(ENABLE_SUB_COLOR)]_EnableSubColor("Enable Sub Color", Int) = 0
		_SubColor("Sub Color", Color) = (0,0,0,0)

		[Toggle(ENABLE_EMISSIVE)]_EnableEmissive("Enable Emission", Int) = 0
		[HDR]_EmissiveColor("Emission Color", Color) = (0,0,0,0)

		_Dissolve("Dissolve", Range(0, 1)) = 0
		_EdgeSize("Edge Size", Range(0, 1)) = 0.1

		[Header(Distortion)]
		[Toggle(ENABLE_DISTORTION)]_EnableDistortion("Enable Distortion", Int) = 0
		[NoScaleOffset][Normal]_DistortionTexture("Distortion Texture", 2D) = "bump" {}

		[Toggle(ENABLE_DISTORTION_MASK)]_EnableDistortionMask("Enable Distortion Mask", Int) = 0
		[NoScaleOffset]_DistortionMaskTexture("Distortion Mask", 2D) = "white" {}

		_DistortionDissolve("Dissolve", Range(0, 1)) = 0

		_DistortionPower("Distortion Power", Range(0, 1)) = 0
		_DistortionScale("Distortion Scale", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}

		LOD 100
		Cull Off

		HLSLINCLUDE

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/SamplingTexture.hlsl"

			//====================================
			// グローバル変数
			TEXTURE2D(_CameraOpaqueTexture);
			SAMPLER(sampler_CameraOpaqueTexture);

			TEXTURE2D(_GlobalMaskTexture);
			SAMPLER(sampler_GlobalMaskTexture);

			//====================================
			// マテリアル変数
			TEXTURE2D(_DissolveMaskTexture);
			SAMPLER(sampler_DissolveMaskTexture);

			TEXTURE2D(_FlowTexture);
			SAMPLER(sampler_FlowTexture);

			TEXTURE2D(_ColorMaskTexture);
			SAMPLER(sampler_ColorMaskTexture);

			TEXTURE2D(_DistortionTexture);
			SAMPLER(sampler_DistortionTexture);

			TEXTURE2D(_DistortionMaskTexture);
			SAMPLER(sampler_DistortionMaskTexture);

			CBUFFER_START(UnityPerMaterial)
				float4 _DissolveMaskTexture_ST;

				float4 _Scroll;

				half4 _MainColor;
				half4 _SubColor;
				half4 _EmissiveColor;

				half _AddPower;

				half _Dissolve;
				half _EdgeSize;

				half _FlowPower;
				half _FlowSpeed;

				half _DistortionDissolve;
				half _DistortionPower;
				half _DistortionScale;
			CBUFFER_END


			float SampleMask(TEXTURE2D_PARAM(maskTexture, maskSampler), float2 uv)
			{
				float globalMask = SAMPLE_TEXTURE2D(_GlobalMaskTexture, sampler_GlobalMaskTexture, uv).r;
				float mask = SAMPLE_TEXTURE2D(maskTexture, maskSampler, uv * _Scroll.xy + _Scroll.zw).r * globalMask;
				return mask;
			}

		ENDHLSL

		Pass
		{
			Tags
			{
				"LightMode" = "UniversalForward"
			}

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_local_fragment _ ENABLE_SUB_COLOR
			#pragma multi_compile_local_fragment _ ENABLE_EMISSIVE

			#include_with_pragmas "Packages/jp.links1536.aria-graphics/ShaderLibrary/Keywords/Fog.hlsl"

			struct Attributes
			{
				float4 vertex	: POSITION0;
				float2 uv		: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 vertex		: SV_POSITION;
				float2 uv			: TEXCOORD0;
				half fogFactor		: TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				output.vertex	= TransformObjectToHClip(input.vertex.xyz);
				output.uv		= TRANSFORM_TEX(input.uv, _DissolveMaskTexture);
				output.fogFactor	= ComputeFogFactor(output.vertex.z);

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float mask = SampleMask(TEXTURE2D_ARGS(_ColorMaskTexture, sampler_ColorMaskTexture), input.uv);
				half4 texCol = SAMPLE_FLOWMAP(_DissolveMaskTexture, sampler_DissolveMaskTexture, input.uv, _FlowTexture, sampler_FlowTexture, input.uv, _FlowPower, _FlowSpeed) * mask;

				half dissolve = _Dissolve;
				float alpha = step(dissolve, texCol.r);
				clip(alpha - 0.001);

#if defined(ENABLE_SUB_COLOR)
				half col = step(dissolve , texCol.r - _EdgeSize);
				float4 base = lerp(_SubColor, _MainColor, col);
				float4 emissive = col * base * (texCol.r * _AddPower);
#else
				half col = step(dissolve, texCol.r);
				float4 base = _MainColor;
				float4 emissive = col * base * (texCol.r * _AddPower);
#endif

				half4 finalColor = base;
//#if defined(ENABLE_EMISSIVE)
//				finalColor += emissive + _EmissiveColor;
//#endif
				finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
				finalColor.a = lerp(texCol.r, 1, col);

				return finalColor;
			}

			ENDHLSL
		}

		Pass
		{
			Tags
			{
				"LightMode" = "BloomEmission"
			}

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_local_fragment _ ENABLE_SUB_COLOR
			#pragma multi_compile_local_fragment _ ENABLE_EMISSIVE

			#include_with_pragmas "Packages/jp.links1536.aria-graphics/ShaderLibrary/Keywords/Fog.hlsl"

			struct Attributes
			{
				float4 vertex	: POSITION0;
				float2 uv		: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 vertex		: SV_POSITION;
				float2 uv			: TEXCOORD0;
				half fogFactor		: TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				output.vertex	= TransformObjectToHClip(input.vertex.xyz);
				output.uv		= TRANSFORM_TEX(input.uv, _DissolveMaskTexture);
				output.fogFactor	= ComputeFogFactor(output.vertex.z);

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

#if defined(ENABLE_EMISSIVE)

				float mask = SampleMask(TEXTURE2D_ARGS(_ColorMaskTexture, sampler_ColorMaskTexture), input.uv);
				half4 texCol = SAMPLE_FLOWMAP(_DissolveMaskTexture, sampler_DissolveMaskTexture, input.uv, _FlowTexture, sampler_FlowTexture, input.uv, _FlowPower, _FlowSpeed) * mask;

				half dissolve = _Dissolve;
				float alpha = step(dissolve, texCol.r);
				clip(alpha - 0.001);

#if defined(ENABLE_SUB_COLOR)
				half col = step(dissolve , texCol.r - _EdgeSize);
				float4 base = lerp(_SubColor, _EmissiveColor, col);
				float4 emissive = col * base * (texCol.r * _AddPower);
#else
				half col = step(dissolve, texCol.r);
				float4 base = _EmissiveColor;
				float4 emissive = col * base * (texCol.r * _AddPower);
#endif

				half4 finalColor = base;
				finalColor += emissive;
				finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
				finalColor.a = lerp(texCol.r, 1, col);

				return finalColor;
#else
				return 0;
#endif
			}

			ENDHLSL
		}

		Pass
		{
			Name "DistortionNormal"

			Tags
			{
				"LightMode" = "DistortionNormal"
			}

			Blend One One
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature_local_fragment _ ENABLE_DISTORTION
			#pragma shader_feature_local_fragment _ ENABLE_DISTORTION_MASK

			struct Attributes
			{
				float4 vertex	: POSITION0;
				float2 uv		: TEXCOORD0;
				half3 normalOS	: NORMAL;
				half4 tangent	: TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 vertex			: SV_POSITION;
				float2 uv				: TEXCOORD0;
				half3x3 tangentToWorld	: TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				output.vertex = TransformObjectToHClip(input.vertex.xyz);
				output.uv = input.uv;

				half3 normalWS = TransformObjectToWorldNormal(input.normalOS);
				half3 tangentWS = TransformObjectToWorldDir(input.tangent.xyz);

				half tangentSign = input.tangent.w * unity_WorldTransformParams.w;
				half3 bitangentWS = cross(normalWS, tangentWS) * tangentSign;
				output.tangentToWorld = half3x3(
					half3(tangentWS.x, bitangentWS.x, normalWS.x),
					half3(tangentWS.y, bitangentWS.y, normalWS.y),
					half3(tangentWS.z, bitangentWS.z, normalWS.z)
				);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

#if defined(ENABLE_DISTORTION)

				float mask = SampleMask(TEXTURE2D_ARGS(_DistortionMaskTexture, sampler_DistortionMaskTexture), input.uv);
				half4 texCol = SAMPLE_FLOWMAP(_DissolveMaskTexture, sampler_DissolveMaskTexture, input.uv, _FlowTexture, sampler_FlowTexture, input.uv, _FlowPower, _FlowSpeed) * mask;

				half fade = (texCol.r - _DistortionDissolve) / max(HALF_EPS, _DistortionDissolve);
				clip(fade - 0.01);

				float4 normalTex = SAMPLE_FLOWMAP(_DistortionTexture, sampler_DistortionTexture, input.uv, _FlowTexture, sampler_FlowTexture, input.uv, _FlowPower, _FlowSpeed);
				float2 distortionOffset = UnpackNormal(normalTex).xy * mask * fade;
				half3 worldNormal = mul(input.tangentToWorld, float3(distortionOffset, 0));
				half2 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal).xy;

				float distortionPower = _DistortionPower * _FlowPower * _DistortionScale;
				return float4(viewNormal * distortionPower, 0.0, 0.0);
#else
				return float4(0.0, 0.0, 0.0, 0.0);
#endif
			}

			ENDHLSL
		}
	}
}
