using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{
	public static class RenderTargetUtils
	{
		/// <summary>
		/// GraphicsFormat と RGBM フラグを取得する
		/// </summary>
		public static void GetFormat(out GraphicsFormat format, out bool useRGBM)
		{
			if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormatUsage.Linear | GraphicsFormatUsage.Render))
			{
				format = GraphicsFormat.B10G11R11_UFloatPack32;
				useRGBM = false;
			}
			else
			{
				format = QualitySettings.activeColorSpace == ColorSpace.Linear
					? GraphicsFormat.R8G8B8A8_SRGB
					: GraphicsFormat.R8G8B8A8_UNorm;
				useRGBM = true;
			}
		}

		/// <summary>
		/// 現在の設定に応じてRenderTarget用のフォーマットを取得する
		/// </summary>
		public static (RenderTextureFormat ColorFormat, GraphicsFormat GraphicsFormat) GetFormat(bool enableHDR, UniversalRenderPipelineAsset asset)
		{
			var colorFormat = enableHDR
				? asset.hdrColorBufferPrecision switch
				{
					HDRColorBufferPrecision._32Bits => RenderTextureFormat.RGB111110Float,
					HDRColorBufferPrecision._64Bits => RenderTextureFormat.ARGBHalf,
					_ => RenderTextureFormat.RGB111110Float,
				}
				: RenderTextureFormat.ARGB32;
			var graphicsFormat = enableHDR
				? asset.hdrColorBufferPrecision switch
				{
					HDRColorBufferPrecision._32Bits => GraphicsFormat.B10G11R11_UFloatPack32,
					HDRColorBufferPrecision._64Bits => GraphicsFormat.R16G16B16A16_SFloat,
					_ => GraphicsFormat.B10G11R11_UFloatPack32,
				}
				: GraphicsFormat.R8G8B8A8_UNorm;
			return (colorFormat, graphicsFormat);
		}

		/// <summary>
		/// カラーターゲット用RenderTextureDescriptorを作成する
		/// </summary>
		public static RenderTextureDescriptor CreateColorDescription(bool enableHDR, UniversalRenderPipelineAsset asset, in RenderTextureDescriptor desc)
		{
			var result = desc;
			result.depthStencilFormat = GraphicsFormat.None;
			result.depthBufferBits = 0;
			result.msaaSamples = 1;
			result.bindMS = false;
			result.memoryless = RenderTextureMemoryless.MSAA | RenderTextureMemoryless.Depth;

			var format = GetFormat(enableHDR, asset);
			result.colorFormat = format.ColorFormat;
			result.graphicsFormat = format.GraphicsFormat;

			return result;
		}

		/// <summary>
		/// デプスターゲット用RenderTextureDescriptorを作成する
		/// </summary>
		public static RenderTextureDescriptor CreateDepthStencilDescription(in RenderTextureDescriptor desc)
		{
			var result = desc;
			result.colorFormat = 0;
			result.graphicsFormat = GraphicsFormat.None;
			result.msaaSamples = 1;
			result.bindMS = false;
			result.memoryless = RenderTextureMemoryless.MSAA | RenderTextureMemoryless.Color;
			return result;
		}

		/// <summary>
		/// RenderTextureDescriptor → TextureDesc 変換処理
		/// </summary>
		public static void GetTextureDesc(in RenderTextureDescriptor desc, out TextureDesc rgDesc)
		{
			rgDesc = new TextureDesc(desc.width, desc.height)
			{
				dimension = desc.dimension,
				bindTextureMS = desc.bindMS,
				format = ((desc.depthStencilFormat != GraphicsFormat.None) ? desc.depthStencilFormat : desc.graphicsFormat),
				isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None && desc.depthStencilFormat != GraphicsFormat.None,
				slices = desc.volumeDepth,
				msaaSamples = (MSAASamples)desc.msaaSamples,
				enableRandomWrite = desc.enableRandomWrite,
				enableShadingRate = desc.enableShadingRate,
				useDynamicScale = desc.useDynamicScale,
				useDynamicScaleExplicit = desc.useDynamicScaleExplicit,
				vrUsage = desc.vrUsage
			};
		}

		/// <summary>
		/// 解像度を指定した段階落とす
		/// </summary>
		public static RenderTextureDescriptor GetDownsampling(in RenderTextureDescriptor desc, int downCount)
		{
			var result = desc;
			if (downCount >= 1)
			{
				result.width = Mathf.Max(1, desc.width >> downCount);
				result.height = Mathf.Max(1, desc.height >> downCount);
			}
			return result;
		}

		/// <summary>
		/// 解像度を指定した段階落とす
		/// </summary>
		public static TextureDesc GetDownsampling(in TextureDesc desc, int downCount)
		{
			var result = desc;
			if (downCount >= 1)
			{
				result.width = Mathf.Max(1, desc.width >> downCount);
				result.height = Mathf.Max(1, desc.height >> downCount);
			}
			return result;
		}

	}
}
