Shader "Hidden/Aria/PostProcessing/Bloom"
{
	HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
		#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

		// 通常のBloom用
		TEXTURE2D_X(_SourceTexLowMip);
		float4 _SourceTexLowMip_TexelSize;
		float4 _Params;					// x: scatter, y: clamp, z: threshold (linear), w: threshold knee

		// Bloom Thresholdの影響を受けずに発光させたいもの
		TEXTURE2D_X(_BloomEmissionTexture);
		float4 _BloomEmissionTexture_TexelSize;
		float4 _BloomEmissionParams;	// x: boost, y: clamp, z: threshold (linear), w: threshold knee

		#define Scatter				_Params.x

		float4 _Params2;				// x: kawaseDistance y: kawaseScatter z: dualScatter w: dualScatter * 0.5
		#define KawaseDistance		_Params2.x
		#define KawaseScatter		_Params2.y
		#define DualScatter			_Params2.z
		#define DualHalfScatter		_Params2.w

		half4 EncodeHDR(half3 color)
		{
			// 意図的にRGBMの処理は抜いている

		#if UNITY_COLORSPACE_GAMMA
			color = sqrt(color); // linear to γ
		#endif

			return half4(color, 1.0);
		}

		half3 SampleHDR(half4 data)
		{
			half3 color = data.xyz;

		#if UNITY_COLORSPACE_GAMMA
			color *= color; // γ to linear
		#endif
		
			// 意図的にRGBMの処理は抜いている

			return color;
		}

		half3 SampleHDR(float2 uv,  float2 offset)
		{
			float2 texelSize = _BlitTexture_TexelSize.xy;
			return SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - offset * texelSize, texelSize)));
		}

		half3 SamplePrefilter(TEXTURE2D_X_PARAM(textureMap, textureSampler), float2 texelSize, float2 uv,  float2 offset)
		{
			half4 color = SAMPLE_TEXTURE2D_X(textureMap, textureSampler, uv + texelSize * offset);
			#if _ENABLE_ALPHA_OUTPUT
				// When alpha is enabled, regions with zero alpha should not generate any bloom / glow. Therefore we pre-multipy the color with the alpha channel here and the rest
				// of the computations remain float3. Still, when bloom is applied to the final image, bloom will still be spread on regions with zero alpha (see UberPost.compute)
				color.xyz *= color.w;
			#endif
			return color.xyz;
		}

		half3 SampleColor(TEXTURE2D_X_PARAM(textureMap, textureSampler), float2 texelSize, float2 uv, float4 params)
		{
		#if _BLOOM_HQ
			half3 A = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2(-1.0, -1.0));
			half3 B = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 0.0, -1.0));
			half3 C = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 1.0, -1.0));
			half3 D = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2(-0.5, -0.5));
			half3 E = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 0.5, -0.5));
			half3 F = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2(-1.0,  0.0));
			half3 G = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 0.0,  0.0));
			half3 H = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 1.0,  0.0));
			half3 I = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2(-0.5,  0.5));
			half3 J = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 0.5,  0.5));
			half3 K = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2(-1.0,  1.0));
			half3 L = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 0.0,  1.0));
			half3 M = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2( 1.0,  1.0));

			half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

			half3 color = (D + E + I + J) * div.x;
			color += (A + B + G + F) * div.y;
			color += (B + C + H + G) * div.y;
			color += (F + G + L + K) * div.y;
			color += (G + H + M + L) * div.y;
		#else
			half3 color = SamplePrefilter(TEXTURE2D_X_ARGS(textureMap, textureSampler), texelSize, uv, float2(0,0));
		#endif
		
			float ClampMax = params.y;
			float Threshold = params.z;
			float ThresholdKnee = params.w;

			// User controlled clamp to limit crazy high broken spec
			color = min(ClampMax, color);

			// Thresholding
			half brightness = Max3(color.r, color.g, color.b);
			half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
			softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
			half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
			color *= multiplier;
			
			// Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
			color = max(color, 0);

			return color;
		}


		half4 FragPrefilter(Varyings input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
			UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
			{
				uv = RemapFoveatedRenderingLinearToNonUniform(uv);
			}
#endif
			half3 color = 0;

			color += SampleColor(TEXTURE2D_X_ARGS(_BlitTexture, sampler_LinearClamp), _BlitTexture_TexelSize.xy, uv, _Params);
			color += SampleColor(TEXTURE2D_X_ARGS(_BloomEmissionTexture, sampler_LinearClamp), _BloomEmissionTexture_TexelSize.xy, uv, _BloomEmissionParams) * _BloomEmissionParams.x;

			return EncodeHDR(color);
		}

		half4 FragBlurH(Varyings input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			float2 texelSize = _BlitTexture_TexelSize.xy * 2.0;
			float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

			// 9-tap gaussian blur on the downsampled source
			half3 c0 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - float2(texelSize.x * 4.0, 0.0), texelSize)));
			half3 c1 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - float2(texelSize.x * 3.0, 0.0), texelSize)));
			half3 c2 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - float2(texelSize.x * 2.0, 0.0), texelSize)));
			half3 c3 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - float2(texelSize.x * 1.0, 0.0), texelSize)));
			half3 c4 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv                                 , texelSize)));
			half3 c5 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + float2(texelSize.x * 1.0, 0.0), texelSize)));
			half3 c6 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + float2(texelSize.x * 2.0, 0.0), texelSize)));
			half3 c7 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + float2(texelSize.x * 3.0, 0.0), texelSize)));
			half3 c8 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + float2(texelSize.x * 4.0, 0.0), texelSize)));

			half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
						+ c4 * 0.22702703
						+ c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

			return EncodeHDR(color);
		}

		half4 FragBlurV(Varyings input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			float2 texelSize = _BlitTexture_TexelSize.xy;
			float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

			// Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
			half3 c0 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - float2(0.0, texelSize.y * 3.23076923), texelSize)));
			half3 c1 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - float2(0.0, texelSize.y * 1.38461538), texelSize)));
			half3 c2 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv                                        , texelSize)));
			half3 c3 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + float2(0.0, texelSize.y * 1.38461538), texelSize)));
			half3 c4 = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + float2(0.0, texelSize.y * 3.23076923), texelSize)));

			half3 color = c0 * 0.07027027 + c1 * 0.31621622
						+ c2 * 0.22702703
						+ c3 * 0.31621622 + c4 * 0.07027027;

			return EncodeHDR(color);
		}

		half3 Upsample(float2 uv)
		{
			half3 highMip = SampleHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));

		#if _BLOOM_HQ
			half3 lowMip = SampleHDR(SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_LinearClamp), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));
		#else
			half3 lowMip = SampleHDR(SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_LinearClamp, uv));
		#endif

			return lerp(highMip, lowMip, Scatter);
		}

		half4 FragUpsample(Varyings input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			half3 color = Upsample(UnityStereoTransformScreenSpaceTex(input.texcoord));
			return EncodeHDR(color);
		}

		half4 FragKawase(Varyings input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

			const float d = KawaseDistance;

			half3 c0 = SampleHDR(uv, float2( d,  d));
			half3 c1 = SampleHDR(uv, float2(-d,  d));
			half3 c2 = SampleHDR(uv, float2(-d, -d));
			half3 c3 = SampleHDR(uv, float2( d, -d));

			half3 color = (c0 + c1 + c2 + c3) * 0.25;

			if (KawaseScatter < 0.999)
				color = lerp(SampleHDR(uv, float2( 0,  0)), color, Scatter);

			return EncodeHDR(color);
		}

		half4 FragDualDownsample(Varyings input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

			half3 c0 = SampleHDR(uv, float2(0, 0));

			half3 c1 = SampleHDR(uv, float2( 0.5,  0.5));
			half3 c2 = SampleHDR(uv, float2(-0.5,  0.5));
			half3 c3 = SampleHDR(uv, float2(-0.5, -0.5));
			half3 c4 = SampleHDR(uv, float2( 0.5, -0.5));

			half3 color = (1.0 / 8.0) * (c0 * 4.0 + c1 + c2 + c3 + c4);

			return EncodeHDR(color);
		}

		half4 FragDualUpsample(Varyings input) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
			float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

			const float hs = DualHalfScatter;
			half3 c1 = SampleHDR(uv, float2( hs,  hs));
			half3 c2 = SampleHDR(uv, float2(-hs,  hs));
			half3 c3 = SampleHDR(uv, float2(-hs, -hs));
			half3 c4 = SampleHDR(uv, float2( hs, -hs));

			const float s = DualScatter;
			half3 c5 = SampleHDR(uv, float2(-s, 0.0));
			half3 c6 = SampleHDR(uv, float2( s, 0.0));
			half3 c7 = SampleHDR(uv, float2( 0.0,  s));
			half3 c8 = SampleHDR(uv, float2( 0.0, -s));

			half3 color = (1.0 / 12.0) *
				((c1 + c2 + c3 + c4) * 2.0 +
				  c5 + c6 + c7 + c8);

			return EncodeHDR(color);
		}


	ENDHLSL

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}
		LOD 100
		ZTest Always
		ZWrite Off
		Cull Off

		Pass // 0
		{
			Name "Bloom Prefilter"

			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FragPrefilter
				#pragma multi_compile_local_fragment _ _BLOOM_HQ
				#pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT
			ENDHLSL
		}

		Pass // 1
		{
			Name "Bloom Blur Horizontal"

			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FragBlurH
			ENDHLSL
		}

		Pass // 2
		{
			Name "Bloom Blur Vertical"

			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FragBlurV
			ENDHLSL
		}

		Pass // 3
		{
			Name "Bloom Upsample"

			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FragUpsample
				#pragma multi_compile_local_fragment _ _BLOOM_HQ
			ENDHLSL
		}

		Pass // 4
		{
			Name "Bloom Kawase"

			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FragKawase
			ENDHLSL
		}

		Pass // 5
		{
			Name "Bloom Dual Downsample"

			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FragDualDownsample
			ENDHLSL
		}

		Pass // 6
		{
			Name "Bloom Dual Upsample"

			HLSLPROGRAM
				#pragma vertex Vert
				#pragma fragment FragDualUpsample
			ENDHLSL
		}
	}
}
