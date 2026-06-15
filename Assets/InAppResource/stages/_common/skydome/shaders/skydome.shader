Shader "Aria/Map/Skydome"
{
	Properties
	{
		[Header(Textures)][Space]
		[NoScaleOffset] _MainTex ("Main Pattern", 2D) = "white" {}
		_CloudTexA ("Cloud Mask A", 2D) = "white" {}
		_CloudTexB ("Cloud Mask B", 2D) = "gray" {}

		[Header(Cloud)][Space]
		_CloudColorLight ("Light Color", Color) = (1, 1, 1, 1)
		_CloudColorShadow ("Shadow Color", Color) = (0.55, 0.6, 0.7, 1)
		_CloudCoverage ("Coverage", Range(0, 1)) = 0.3
		_CloudEdgeSoftness ("Edge Softness", Range(0.1, 1)) = 0.2
		_CloudLayerBlend ("Layer Blend", Range(0, 1)) = 0.35

		[Header(Animation)][Space]
		_CloudScrollA ("Cloud A Scroll", Vector) = (0.01, 0.0, -0.015, 0.0)
		_CloudScrollB ("Cloud B Scroll", Vector) = (0.004, 0.0, -0.006, 0.0)

		[Header(Atmosphere)][Space]
		_Intensity ("Intensity", Range(0, 8)) = 1
		_SkyTopColor ("Top Color", Color) = (0.42, 0.55, 0.8, 1)
		_SkyHorizonColor ("Horizon Color", Color) = (0.82, 0.85, 0.9, 1)
		_HorizonFade ("Horizon Fade", Range(0, 2)) = 0.65
		_FogBlend ("Fog Blend", Range(0, 1)) = 0.35

		[Header(Sun)][Space]
		_SunColor ("Sun Color", Color) = (1, 1, 1, 1)
		_SunSize ("Sun Size", Range(0, 0.1)) = 0.015
		_SunCoverage ("Sun Coverage", Range(0, 10)) = 5
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Background"
			"Queue" = "Background"
		}

		Pass
		{
			Name "SkyForward"

			Tags
			{
				"LightMode" = "UniversalForward"
			}

			Blend Off
			Cull Front

			ZClip False
			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/SkyCommon.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			TEXTURE2D(_CloudTexA);
			SAMPLER(sampler_CloudTexA);

			TEXTURE2D(_CloudTexB);
			SAMPLER(sampler_CloudTexB);

			CBUFFER_START(UnityPerMaterial)
				float4 _CloudTexA_ST;
				float4 _CloudTexB_ST;
				
				float4 _CloudScrollA;
				float4 _CloudScrollB;

				// 雲
				half4 _CloudColorLight;
				half4 _CloudColorShadow;

				// 空
				half4 _SkyTopColor;
				half4 _SkyHorizonColor;

				// 太陽
				half4 _SunColor;


				// 雲
				half _Intensity;
				half _CloudEdgeSoftness;
				half _CloudLayerBlend;
				half _CloudCoverage;

				half _HorizonFade;
				half _FogBlend;

				half _SunSize;
				half _SunCoverage;

			CBUFFER_END

			struct Attributes
			{
				float4 positionOS	: POSITION;
				float3 normalOS		: NORMAL;
				float2 uv			: TEXCOORD0;
				float2 mask			: TEXCOORD1;
			};

			struct Varyings
			{
				float4 positionCS	: SV_POSITION;
				float4 uv			: TEXCOORD0;
				float4 mask			: TEXCOORD1;
				float3 positionWS	: TEXCOORD2;
				float3 normalWS		: TEXCOORD3;
			};
			
			#define _CloudLightFactor 8

			static half CLOUD_VOLUME_STEPS = 10;
			static half CLOUD_SHADOW_STEPS = 2;
			
			static half RCP_CLOUD_VOLUME_STEP = half(1.0 / CLOUD_VOLUME_STEPS);
			static half RCP_CLOUD_SHADOW_STEP = half(1.0 / CLOUD_SHADOW_STEPS);

			half SampleCloudDensity(float2 cloudAUV, float2 cloudBUV)
			{
				// 複数のテクスチャを組みあわせて、雲のノイズ表現を行う
				half cloudMaskA = SAMPLE_TEXTURE2D(_CloudTexA, sampler_CloudTexA, cloudAUV).r;
				half cloudMaskB = SAMPLE_TEXTURE2D(_CloudTexB, sampler_CloudTexB, cloudBUV).r;

				half layeredCloud = lerp(cloudMaskA, cloudMaskA + cloudMaskB, _CloudLayerBlend);
				half erosion = _CloudCoverage * 0.5;
				return saturate(layeredCloud - erosion) * 0.75;
			}

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = positionInputs.positionCS;
				output.positionWS = positionInputs.positionWS;
				output.normalWS = normalize(normalInputs.normalWS);

				output.mask = float4(input.uv, input.mask);
				output.uv = float4(
					TRANSFORM_TEX(input.uv, _CloudTexA) + _CloudScrollA.xy * _Time.y,
					TRANSFORM_TEX(input.uv, _CloudTexB) + _CloudScrollB.xy * _Time.y
				);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half3 normalWS = normalize(input.normalWS);
				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

				Light mainLight = GetMainLight();
				half3 lightDirWS = mainLight.direction;

				// ドーム形状に対して、雲を表示する領域を決める
				half skyCloudMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.mask.xy).r;

				// 高さ情報
				float2 mask = input.mask.zw;
				half vertical = saturate(mask.y);

				// 雲を視差表現で疑似的に厚みを見せる
				// Volumetricの方がキレイだけど、重いので一旦こうする
				half horizonDepth = pow(saturate(half(1.0) - vertical), 0.65);
				float2 viewParallax = (viewDirWS.xz * (0.02 + horizonDepth * 0.12));
				float2 lightParallax = (lightDirWS.xz * 0.045);

				half transmittance = 1.0;
				half integratedDensity = 0.0;
				half shadowDensity = 0.0;

				float4 cloudUV = input.uv;

				[unroll]
				for (int volumeStepIndex = 0; volumeStepIndex < CLOUD_VOLUME_STEPS; volumeStepIndex++)
				{
					half t = (half(volumeStepIndex) + half(0.5)) * RCP_CLOUD_VOLUME_STEP;
					half sampleHeight = saturate(half(1.0) - abs(t * half(2.0) - half(1.0)));
					half heightMask = sampleHeight;

					// ステップに合わせて視差オフセットを増やしていく
					float2 stepOffset = viewParallax * t;
					float2 cloudAUV = cloudUV.xy - stepOffset;
					float2 cloudBUV = cloudUV.zw - stepOffset * 1.15;

					half density = SampleCloudDensity(cloudAUV, cloudBUV) * heightMask * skyCloudMask;

					// この層の密度
					float sliceAlpha = density * float(0.42);
					integratedDensity += sliceAlpha * transmittance;
					transmittance *= (1.0 - sliceAlpha);
				}

				[unroll]
				for (int shadowStepIndex = 0; shadowStepIndex < CLOUD_SHADOW_STEPS; shadowStepIndex++)
				{
					half t = (half(shadowStepIndex) + half(1.0)) * RCP_CLOUD_SHADOW_STEP;
					float2 lightOffset = lightParallax * t;
					float2 cloudAUV = cloudUV.xy - lightOffset;
					float2 cloudBUV = cloudUV.zw - lightOffset * 1.15;
					shadowDensity += SampleCloudDensity(cloudAUV, cloudBUV);
				}
				shadowDensity *= RCP_CLOUD_SHADOW_STEP * skyCloudMask;

				// 雲
				half selfShadow = exp2(-shadowDensity * _CloudLightFactor);
				half NdotL = saturate(dot(normalWS, lightDirWS));
				half lightingMask = saturate((vertical + NdotL) * half(0.5));
				half cloudLit = saturate(lightingMask * selfShadow);
				half3 cloudColor = lerp(_CloudColorShadow.rgb, _CloudColorLight.rgb, cloudLit);

				// 空の色
				half3 skyColor = lerp(_SkyHorizonColor.rgb, _SkyTopColor.rgb, vertical);

				// 雲と空を合成
				half cloudMask = smoothstep(half(0.0), max(_CloudEdgeSoftness, HALF_EPS), integratedDensity);
				half3 color = lerp(skyColor, cloudColor, cloudMask);

				// 水平方向にシーンのフォグを乗せる処理
				half horizonMask = pow(saturate(half(1.0) - vertical), max(_HorizonFade, HALF_EPS));
				half fogIntensity = saturate(horizonMask * _FogBlend * unity_FogColor.a);
				color = lerp(color, unity_FogColor.rgb, fogIntensity);

				// 太陽
				half sunAttenuation = SunAttenuation(lightDirWS, -viewDirWS, _SunSize, _SunCoverage);

				// 太陽は雲で隠す
				half lightColorIntensity = clamp(length(mainLight.color), 0.25, 1);
				half3 lightColor = mainLight.color / lightColorIntensity;
				half sunMask = saturate(1 - cloudMask);
				half3 sunColor = _SunColor.rgb * lightColor * sunAttenuation * sunMask;
				color += sunColor;

#if defined(UNITY_COLORSPACE_GAMMA)
				color = FastLinearToSRGB(color);
#endif

				return half4(color * _Intensity, 1);
			}
			ENDHLSL
		}
	}
}
