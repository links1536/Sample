using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.Bloom
{
	partial class BloomEmissionPass : ScriptableRenderPass
	{
		class BloomResourceData : ContextItem
		{
			public TextureHandle BloomMarkTexture;

			public override void Reset()
			{
				BloomMarkTexture = TextureHandle.nullHandle;
			}
		}

		class BloomMarkPassData
		{
			internal TextureHandle SourceDepth;
			internal RendererListHandle RendererListHandle;
			internal Material Material;
			internal MaterialPropertyBlock Properties;
		}

		class SetGlobalValuePassData
		{
			internal BloomEmissionVolume BloomEmissionVolume;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			var bloomVolume = VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();
			if (bloomVolume == null || !bloomVolume.active || !bloomVolume.IsActive())
				return;

			var bloomEmissionVolume = VolumeManager.instance.stack.GetComponent<BloomEmissionVolume>();
			if (bloomEmissionVolume == null || !bloomEmissionVolume.active || !bloomEmissionVolume.IsActive())
				return;

			// 各リソースデータ取得
			var lightData = frameData.Get<UniversalLightData>();
			var cameraData = frameData.Get<UniversalCameraData>();
			var resourceData = frameData.Get<UniversalResourceData>();
			var renderingData = frameData.Get<UniversalRenderingData>();
			var ariaProcessResourceData = frameData.Get<AriaPostProcessResourceData>();

			if (ariaProcessResourceData == null || ariaProcessResourceData.BloomUtilMaterial == null)
				return;

			if (!AriaPostProcessUtils.IsPostProcessTarget(cameraData))
				return;

			// Bloom用リソースは新規作成の可能性もある
			var bloomResourceData = frameData.GetOrCreate<BloomResourceData>();

			// 描画モードが変わる
			bool opaqueMode = m_RenderingTiming == RenderingTiming.Opaque;

			using (var builder = renderGraph.AddRasterRenderPass<BloomMarkPassData>(passName, out var passData, profilingSampler))
			{
				// 描画パラメータ
				var filterSettings = new FilteringSettings(m_RenderQueueRange, cameraData.camera.cullingMask);
				var sortFlags = opaqueMode ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent;
				var drawSettings = RenderingUtils.CreateDrawingSettings(ShaderTag, renderingData, cameraData, lightData, sortFlags);
				var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
				var rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
				if (!rendererListHandle.IsValid())
					return;

				// Color用設定
				var asset = UniversalRenderPipeline.asset;
				var colorDesc = RenderTargetUtils.CreateColorDescription(cameraData.isHdrEnabled, asset, cameraData.cameraTargetDescriptor);
				RenderTargetUtils.GetTextureDesc(colorDesc, out var rgColorDesc);
				rgColorDesc.name = BloomMarkColorTextureName;
				rgColorDesc.clearBuffer = true;
				rgColorDesc.clearColor = Color.clear;
				rgColorDesc.filterMode = FilterMode.Bilinear;
				rgColorDesc.wrapMode = TextureWrapMode.Clamp;

				// Depth用設定
				var depthDesc = RenderTargetUtils.CreateDepthStencilDescription(cameraData.cameraTargetDescriptor);
				RenderTargetUtils.GetTextureDesc(depthDesc, out var rgDepthDesc);
				rgDepthDesc.name = BloomMarkDepthTextureName;
				rgDepthDesc.clearBuffer = true;
				rgDepthDesc.clearColor = Color.clear;
				rgDepthDesc.filterMode = FilterMode.Point;
				rgDepthDesc.wrapMode = TextureWrapMode.Clamp;

				// Opaqueでは新しいテクスチャを Transparent では Opaque で描画したテクスチャに描く
				var distortionMarkColorTexture = opaqueMode
					? renderGraph.CreateTexture(rgColorDesc)
					: bloomResourceData.BloomMarkTexture;
				var distortionMarkDepthTexture = builder.CreateTransientTexture(rgDepthDesc);

				// PassData設定
				passData.SourceDepth = resourceData.activeDepthTexture;
				passData.RendererListHandle = rendererListHandle;
				passData.Material = ariaProcessResourceData.BloomUtilMaterial;
				passData.Properties = m_Properties;

				// RenderGraphへの設定
				builder.UseTexture(passData.SourceDepth, AccessFlags.Read);
				builder.UseRendererList(passData.RendererListHandle);
				builder.SetRenderAttachment(distortionMarkColorTexture, 0, opaqueMode ? AccessFlags.Write : AccessFlags.ReadWrite);
				builder.SetRenderAttachmentDepth(distortionMarkDepthTexture, AccessFlags.Write);
				builder.AllowPassCulling(false);
				builder.SetRenderFunc<BloomMarkPassData>(static (passData, context) =>
				{
					// BlitDepthだとUnsafeでも出来なかったので、Depthのコピーを自前でやる
					passData.Properties.SetTexture("_SourceDepthTexture", passData.SourceDepth, RenderTextureSubElement.Depth);
					CoreUtils.DrawFullScreen(context.cmd, passData.Material, passData.Properties, 0);

					// 発光用の LightMode を描画
					context.cmd.DrawRendererList(passData.RendererListHandle);
				});
				builder.SetGlobalTextureAfterPass(distortionMarkColorTexture, BloomMarkColorTextureId);

				// 描画命令を予約したので、BloomMarkを覚えておく
				bloomResourceData.BloomMarkTexture = distortionMarkColorTexture;
			}

			// SetRenderFunc 内でのグローバルの書き換えがあるので、一応パスを分けておく
			using (var builder = renderGraph.AddUnsafePass<SetGlobalValuePassData>(passName, out var passData, profilingSampler))
			{
				passData.BloomEmissionVolume = bloomEmissionVolume;

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);
				builder.SetRenderFunc<SetGlobalValuePassData>(static (passData, context) =>
				{
					var bloomEmissionVolume = passData.BloomEmissionVolume;
					float clamp = bloomEmissionVolume.Clamp;
					float threshold = bloomEmissionVolume.Threshold;
					float thresholdKnee = threshold * 0.5f;
					float intensity = bloomEmissionVolume.Intensity;
					var bloomParams = new Vector4(intensity, clamp, threshold, thresholdKnee);
					context.cmd.SetGlobalVector(BloomEmissionParamsId, bloomParams);
				});
			}
		}
	}
}
