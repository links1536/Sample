Shader "Hidden/Aria/PostProcessing/Distortion"
{
	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}
		LOD 100

		Pass
		{
			Name "Distortion"

			Blend One Zero
			ZWrite Off
			ZTest Always

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

			TEXTURE2D(_DistortionNormalTexture);
			SAMPLER(sampler_DistortionNormalTexture);
			float4 _DistortionNormalTexture_TexelSize;

			TEXTURE2D_X(_CameraColorTexture);
			SAMPLER(sampler_CameraColorTexture);

			float _DistortionPower;

			half4 frag(Varyings i) : SV_Target
			{
				float2 distNormal = SAMPLE_TEXTURE2D_LOD(_DistortionNormalTexture, sampler_DistortionNormalTexture, i.texcoord, 0).xy * 2 - 1;
				float2 uv = i.texcoord + (distNormal * _DistortionPower);
				half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
				return color;
			}
			ENDHLSL
		}
	}
}
