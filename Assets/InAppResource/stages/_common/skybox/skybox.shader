Shader "Aria/Map/Skybox"
{
	Properties
	{
		_UpColor("Up", Color) = (1, 1, 1, 1)
		_HorizonColor("Horizon", Color) = (1, 1, 1, 1)
		_Height("Height", Range(0, 1)) = 0
		_Threshold("Threshold", Range(0, 1)) = 0.1

		_SunColor("Sun Color", Color) = (1, 1, 1, 1)
		_SunSize("Sun Size", Range(0, 1)) = 1
		_SunCoverage("Sun Coverage", Range(0, 10)) = 0.3
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Background"
			"RenderType" = "Background"
			"PreviewType" = "Skybox"
		}
		Cull Off
		ZWrite Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#include "Packages/jp.links1536.aria-graphics/ShaderLibrary/SkyCommon.hlsl"

			struct Attributes
			{
				float4 positionOS	: POSITION;
				float2 uv			: TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS	: SV_POSITION;
				float2 uv			: TEXCOORD0;
				float3 positionOS	: TEXCOORD1;
				float3 positionWS	: TEXCOORD2;
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _UpColor;
				half4 _HorizonColor;
				float _Height;
				float _Threshold;

				half3 _SunColor;
				float _SunSize;
				float _SunCoverage;
			CBUFFER_END

			Varyings vert (Attributes input)
			{
				Varyings output = (Varyings)0;
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(positionWS);
				output.positionOS = input.positionOS.xyz;
				output.positionWS = positionWS;
				output.uv = input.uv;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half3 horizonColor = _HorizonColor.rgb;
				half3 upColor = _UpColor.rgb;
#if UNITY_COLORSPACE_GAMMA
				horizonColor.rgb = FastSRGBToLinear(horizonColor.rgb);
				upColor.rgb = FastSRGBToLinear(upColor.rgb);
#endif

				// 天辺と水平方向の色を高さに合わせて変化させる
				float t = smoothstep(_Height - _Threshold, _Height + _Threshold, input.positionOS.y);
				half3 color = lerp(horizonColor, upColor, t);

				// 太陽表現
				half3 ray = normalize(input.positionOS.xyz);
				half3 lightRay = normalize(_MainLightPosition.xyz);
				half sunAttenuation = SunAttenuation(lightRay, ray, _SunSize, _SunCoverage);
				
				half lightColorIntensity = clamp(length(_MainLightColor.rgb), 0.25, 1);
				half3 lightColor = _MainLightColor.rgb / lightColorIntensity;
				half3 sunColor = _SunColor.rgb * lightColor * sunAttenuation;
				color += sunColor;

				return half4(color, 1);
			}
			ENDHLSL
		}
	}
}
