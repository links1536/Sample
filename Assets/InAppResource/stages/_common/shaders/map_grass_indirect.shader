Shader "Aria/Map/GrassIndirect"
{
	Properties
	{
		_ColorTop("Top Color", Color) = (1, 1, 1, 1)
		_ColorBottom("Bottom Color", Color) = (1, 1, 1, 1)
		

		[HideInInspector] _LodDistance ("Lod Distance", Vector) = (25, 50, 100, 0)
		[HideInInspector] _CurrentLodLevel("Lod Level", Int) = 0
		[HideInInspector] _LodFadeDistance("Lod Cross Fade Distance", Float) = 5
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

			struct Attributes
			{
				float4 positionOS	: POSITION;
				float2 uv			: TEXCOORD0;
				uint   instanceID	: SV_InstanceID;
			};

			struct Varyings
			{
				float4 positionCS	: SV_POSITION;
				float3 color		: TEXCOORD0;
				float3 positionWS	: TEXCOORD1;
			};
			
			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);

				half heightMask = input.uv.y * input.uv.y;

				uint grassId = _CurrentLodInstanceIdBuffer[input.instanceID];
				GrassInstanceData grassInstance = _GrassInstanceDataBuffer[grassId];
				float3 positionWS = TransformObjectToWorldCustom(input.positionOS, grassInstance);
				positionWS = SimulateWind(positionWS, heightMask, grassInstance);

				output.positionCS = TransformWorldToHClip(positionWS);
				output.color = lerp(_ColorBottom.rgb, _ColorTop.rgb, heightMask);
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

				return half4(input.color, 1);
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
				float3 positionWS = TransformObjectToWorldCustom(input.positionOS, grassInstance);
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
