Shader "Hidden/Aria/PostProcessing/BloomUtil"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		Pass
		{
			ZClip False
			Cull Off
			ZWrite On
			ZTest Always
			ColorMask 0

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			TEXTURE2D_X_FLOAT(_SourceDepthTexture);

			float DeviceDepth(float eyeDepth, float4 zBufferParam)
			{
				return (1.0 / eyeDepth - zBufferParam.w) / zBufferParam.z;
			}

			float OffsetByLinearEyeDepth(float z, float offset)
			{
				float eyeDepth = LinearEyeDepth(z, _ZBufferParams);
				eyeDepth += offset;
				return DeviceDepth(eyeDepth, _ZBufferParams);
			}

			Varyings vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
				float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);

				output.positionCS = pos;
				output.texcoord = uv;

				return output;
			}

			float frag(Varyings input) : SV_Depth
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
				float depth = SAMPLE_TEXTURE2D_X_LOD(_SourceDepthTexture, sampler_PointClamp, uv, 0).r;
				return OffsetByLinearEyeDepth(depth, 0.001);
			}
			ENDHLSL
		}
	}
}
