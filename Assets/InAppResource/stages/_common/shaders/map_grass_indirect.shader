Shader "Aria/Map/GrassIndirect"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1, 1, 1, 1)
		_ColorBottom("Bottom Color", Color) = (1, 1, 1, 1)

		_Metallic("Metallic", Range(0, 1)) = 1
		_Smoothness("Smoothness", Range(0, 1)) = 1
		_Occlusion("Occlusion", Range(0, 1)) = 1

		_FogFactor("Fog Factor", Range(0, 1)) = 1

		[Toggle(LIGHTING_OFF)] _LightingOff("Lighting Off", Int) = 0
		[PerRendererData] _LodDistance("Lod Distance", Vector) = (25, 50, 100, 0)
		[PerRendererData] _CurrentLodLevel("Lod Level", Int) = 0
		[PerRendererData] _LodFadeDistance("Lod Cross Fade Distance", Float) = 5
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "TransparentCutout"
			"Queue" = "AlphaTest"
		}

		Cull Off

		HLSLINCLUDE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct GrassInstanceData
			{
				float positionX;
				float positionY;
				float positionZ;

				float sinY;
				float cosY;

				float scaleXZ;
				float scaleY;

				float animationTimeOffset;

				// ライトプローブからサンプリングした色をもたせるとか？
			};

			// TODO: 影響度をパラメーターに出したり、風をスクリプトから設定できるようにすると自由度が上がる
			static half3 _WindDirection = half3(1, 0, 1) * 0.1;
			static half  _WindPower = half(1);
			static half  _WindSpeed = half(3);

			StructuredBuffer<GrassInstanceData> _GrassInstanceDataBuffer;
			StructuredBuffer<uint> _CurrentLodInstanceIdBuffer;
			TEXTURE2D(_DitherMaskLOD2D);
			SAMPLER(sampler_DitherMaskLOD2D);

			CBUFFER_START(UnityPerMaterial)
				half4 _ColorTop;
				half4 _ColorBottom;

				float3 _LodDistance;
				int _CurrentLodLevel;
				float _LodFadeDistance;

				half  _Metallic;
				half  _Smoothness;
				half  _Occlusion;
				half  _FogFactor;
			CBUFFER_END

			float3 TransformObjectToWorldCustom(float3 positionOS, GrassInstanceData grassInstance)
			{
				float3 positionWS = float3(
					grassInstance.positionX + (positionOS.x * grassInstance.cosY + positionOS.z * grassInstance.sinY) * grassInstance.scaleXZ,
					grassInstance.positionY + (positionOS.y) * grassInstance.scaleY,
					grassInstance.positionZ + (-positionOS.x * grassInstance.sinY + positionOS.z * grassInstance.cosY) * grassInstance.scaleXZ
				);
				
				return positionWS;
			}

			half3 TransformObjectToWorldNormalCustom(half3 normalOS, GrassInstanceData grassInstance)
			{
				half cosY = half(grassInstance.cosY);
				half sinY = half(grassInstance.sinY);
				half rcpScaleXZ = rcp(max(HALF_EPS, half(grassInstance.scaleXZ)));
				half rcpScaleY = rcp(max(HALF_EPS, half(grassInstance.scaleY)));
				half3 normalWS = half3(
					(normalOS.x * cosY + normalOS.z * sinY) * rcpScaleXZ,
					normalOS.y * rcpScaleY,
					(-normalOS.x * sinY + normalOS.z * cosY) * rcpScaleXZ
				);
				return normalize(normalWS);
			}

			float3 SimulateWind(float3 positionWS, half heightMask, GrassInstanceData grassInstance)
			{
				half time = sin((_Time.y + grassInstance.animationTimeOffset) * _WindSpeed) * 0.7 + 0.3;
				return positionWS + (_WindDirection * _WindPower * time * heightMask);
			}

			VertexPositionInputs GetVertexPositionInputsCustom(float3 positionOS, half heightMask, GrassInstanceData grassInstance)
			{
				VertexPositionInputs input = (VertexPositionInputs)0;

				float3 positionWS = TransformObjectToWorldCustom(positionOS, grassInstance);
				positionWS = SimulateWind(positionWS, heightMask, grassInstance);

				input.positionWS = positionWS;
				input.positionVS = TransformWorldToView(input.positionWS);
				input.positionCS = TransformWorldToHClip(input.positionWS);

				float4 ndc = input.positionCS * 0.5f;
				input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
				input.positionNDC.zw = input.positionCS.zw;

				return input;
			}

			VertexNormalInputs GetVertexNormalInputsCustom(float3 normalOS, GrassInstanceData grassInstance)
			{
				VertexNormalInputs tbn;
				tbn.tangentWS = real3(1.0, 0.0, 0.0);
				tbn.bitangentWS = real3(0.0, 1.0, 0.0);
				tbn.normalWS = TransformObjectToWorldNormalCustom(normalOS, grassInstance);
				return tbn;
			}

			void DitherClip(half fade, float4 positionCS)
			{
				float2 uv = positionCS.xy;
				uv *= 0.25;
				uv.y = frac(uv.y) * 0.0625;
				uv.y += fade * 0.9375;

				half dither = SAMPLE_TEXTURE2D(_DitherMaskLOD2D, sampler_DitherMaskLOD2D, uv).a;
				clip(dither - 0.5);
			}

		ENDHLSL

		Pass
		{
			Name "GrassForward"

			Tags
			{
				"LightMode" = "UniversalForward"
			}

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR

			#include_with_pragmas "Packages/jp.links1536.aria-graphics/ShaderLibrary/Keywords/Fog.hlsl"
			#include_with_pragmas "keywords/map_unity_keywords.hlsl"
			
			#pragma skip_variants LIGHTMAP_SHADOW_MIXING
			#pragma skip_variants SHADOWS_SHADOWMASK
			#pragma skip_variants DIRLIGHTMAP_COMBINED
			#pragma skip_variants LIGHTMAP_ON

			#pragma shader_feature_local_fragment _ LIGHTING_OFF

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/Common.hlsl"

			#include "include/map_surface_data.hlsl"
			#include "include/map_brdf.hlsl"
			#include "include/map_lighting.hlsl"

			struct Attributes
			{
				float4 positionOS	: POSITION;
				float3 normalOS		: NORMAL;
				float2 uv			: TEXCOORD0;
				uint   instanceID	: SV_InstanceID;
			};

			struct Varyings
			{
				float4 positionCS	: SV_POSITION;
				float3 color		: TEXCOORD0;
				float3 positionWS	: TEXCOORD1;
				
				half3  normalWS			: TEXCOORD3;

			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				half4  fogFactorAndVertexLight : TEXCOORD6; // x: fogFactor, yzw: vertex light
			#else
				half   fogFactor		: TEXCOORD6;
			#endif
			};
			
			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);

				half heightMask = input.uv.y * input.uv.y;

				uint grassId = _CurrentLodInstanceIdBuffer[input.instanceID];
				GrassInstanceData grassInstance = _GrassInstanceDataBuffer[grassId];
				VertexPositionInputs vertexInput = GetVertexPositionInputsCustom(input.positionOS.xyz, heightMask, grassInstance);
				
				half3 normalWS = half3(0, 1, 0);

				// 頂点ライティング
				half3 vertexLight = VertexLighting(vertexInput.positionWS, normalWS);

				// フォグ
				half fogFactor = 0;
				#if !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = vertexInput.positionCS;
				output.color = lerp(_ColorBottom.rgb, _ColorTop.rgb, heightMask);
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalWS;
				
				// ライティング
			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
			#else
				output.fogFactor = fogFactor;
			#endif

				return output;
			}


			void InitializeInputData(Varyings input, out InputData inputData)
			{
				inputData = (InputData)0;

			#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
				inputData.positionWS = input.positionWS;
			#endif

				inputData.normalWS = input.normalWS;
				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

				// 影
				inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
				inputData.shadowMask = 1;
				
				// AmbientColorを単純に取り出す
				inputData.bakedGI =  half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
				inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
			#else
				inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
			#endif

			#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
				float2 preRotatedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
				switch (UNITY_DISPLAY_ORIENTATION_PRETRANSFORM)
				{
					default:
					case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0: inputData.normalizedScreenSpaceUV = preRotatedScreenSpaceUV; break;
					case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90: inputData.normalizedScreenSpaceUV = float2(1 - preRotatedScreenSpaceUV.y, preRotatedScreenSpaceUV.x); break;
					case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180: inputData.normalizedScreenSpaceUV = float2(1 - preRotatedScreenSpaceUV.x, 1 - preRotatedScreenSpaceUV.y); break;
					case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270: inputData.normalizedScreenSpaceUV = float2(preRotatedScreenSpaceUV.y, 1 - preRotatedScreenSpaceUV.x); break;
				}
			#else
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			#endif
			}

			void InitializeMapSurfaceData(half3 color, out MapSurfaceData surfaceData)
			{
				half3 albedo = color;

				// 法線
				half3 normalTS = half3(0, 0, 1);

				// 発光
				half3 glowColor = 0;

				half metallic = _Metallic;
				half smoothness = _Smoothness;
				half occlusion = _Occlusion;

				surfaceData = (MapSurfaceData)0;

				surfaceData.albedo = albedo;
				surfaceData.normalTS = normalTS;
				surfaceData.emission = glowColor;
				surfaceData.metallic = metallic;
				surfaceData.smoothness = smoothness;
				surfaceData.occlusion = occlusion;
				surfaceData.alpha = 1;

				surfaceData.fogIntensity = _FogFactor;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// SurfaceData
				MapSurfaceData surfaceData;
				InitializeMapSurfaceData(input.color, surfaceData);

				// InputData
				InputData inputData;
				InitializeInputData(input, inputData);

				float3 cameraPositionWS = GetCameraPositionWS();
				float cameraDistance = distance(input.positionWS, cameraPositionWS);

				// TODO: 距離をそのままフェードに使うのではなく、距離に入るとフェードイン・出るとフェードアウトする形に改修する
				float fadeFar = _LodDistance[(uint)_CurrentLodLevel];
				float fadeNear = max(0, fadeFar - _LodFadeDistance);
				half fade = 1 - saturate((cameraDistance - fadeNear) / (fadeFar - fadeNear));
				DitherClip(fade, input.positionCS);

			#if defined(LIGHTING_OFF)
				half4 finalColor = FragmentUnlighting(inputData, surfaceData);
			#else
				half4 finalColor = FragmentLighting(inputData, surfaceData);
			#endif
				return finalColor;
			}
			ENDHLSL
		}
		
		Pass
		{
			Name "GrassShadowCaster"

			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS	: POSITION;
				float3 normalOS		: NORMAL;
				float2 uv			: TEXCOORD0;
				uint   instanceID	: SV_InstanceID;
			};

			struct Varyings
			{
				float4 positionCS	: SV_POSITION;
				float3 positionWS	: TEXCOORD1;
			};
			
			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);

				half heightMask = input.uv.y * input.uv.y;

				uint grassId = _CurrentLodInstanceIdBuffer[input.instanceID];
				GrassInstanceData grassInstance = _GrassInstanceDataBuffer[grassId];
				float3 positionWS = TransformObjectToWorldCustom(input.positionOS.xyz, grassInstance);
				positionWS = SimulateWind(positionWS, heightMask, grassInstance);
				
			#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 lightDirectionWS = normalize(_MainLightPosition.xyz - positionWS);
			#else
				float3 lightDirectionWS = _MainLightPosition.xyz;
			#endif

				half3 normalWS = TransformObjectToWorldNormalCustom(input.normalOS, grassInstance);
				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				output.positionCS = positionCS;
				output.positionWS = positionWS;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				float3 cameraPositionWS = GetCameraPositionWS();
				float cameraDistance = distance(input.positionWS, cameraPositionWS);

				float fadeFar = _LodDistance[(uint)_CurrentLodLevel];
				float fadeNear = max(0, fadeFar - _LodFadeDistance);
				half fade = 1 - saturate((cameraDistance - fadeNear) / (fadeFar - fadeNear));
				DitherClip(fade, input.positionCS);

				return 1;
			}
			ENDHLSL
		}
	}
}
