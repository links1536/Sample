using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.Distortion
{
	partial class DistortionPass : ScriptableRenderPass
	{
		class DistortionMarkPassData
		{
			internal TextureHandle SourceDepth;
			internal RendererListHandle RendererListHandle;
			internal Material Material;
			internal MaterialPropertyBlock Properties;
		}

		class DistortionApplyPassData
		{
			internal float DistortionPower;
			internal TextureHandle SourceColor;
			internal Material Material;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			var distortionVolume = VolumeManager.instance.stack.GetComponent<DistortionVolume>();
			if (distortionVolume == null || !distortionVolume.active || !distortionVolume.IsActive())
				return;

			// 各リソースデータ取得
			var lightData = frameData.Get<UniversalLightData>();
			var cameraData = frameData.Get<UniversalCameraData>();
			var resourceData = frameData.Get<UniversalResourceData>();
			var renderingData = frameData.Get<UniversalRenderingData>();
			var ariaProcessResourceData = frameData.Get<AriaPostProcessResourceData>();

			if (ariaProcessResourceData == null || ariaProcessResourceData.BloomUtilMaterial == null)
				return;

			if (ariaProcessResourceData.DistortionMaterial == null)
				return;

			if (!AriaPostProcessUtils.IsPostProcessTarget(cameraData))
				return;

			ConfigureInput(ScriptableRenderPassInput.Color);

			if (!resourceData.activeDepthTexture.IsValid())
				return;
			if (!resourceData.cameraOpaqueTexture.IsValid())
				return;

			// 描画モードが変わる
			using (var builder = renderGraph.AddRasterRenderPass<DistortionMarkPassData>("Distortion Mark", out var passData))
			{
				// 描画パラメータ
				var filterSettings = new FilteringSettings(RenderQueueRange.all, cameraData.camera.cullingMask);
				var sortFlags = SortingCriteria.CommonTransparent;
				var drawSettings = RenderingUtils.CreateDrawingSettings(ShaderTag, renderingData, cameraData, lightData, sortFlags);
				var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
				var rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
				if (!rendererListHandle.IsValid())
					return;

				// Color用設定
				var asset = UniversalRenderPipeline.asset;
				var colorDesc = RenderTargetUtils.CreateColorDescription(cameraData.isHdrEnabled, asset, cameraData.cameraTargetDescriptor);
				RenderTargetUtils.GetTextureDesc(colorDesc, out var rgColorDesc);
				rgColorDesc.name = DistortionNormalTextureName;
				rgColorDesc.clearBuffer = true;
				rgColorDesc.clearColor = new Color(0.5f, 0.5f, 1.0f);
				rgColorDesc.filterMode = FilterMode.Bilinear;
				rgColorDesc.wrapMode = TextureWrapMode.Clamp;

				// Depth用設定
				var depthDesc = RenderTargetUtils.CreateDepthStencilDescription(cameraData.cameraTargetDescriptor);
				RenderTargetUtils.GetTextureDesc(depthDesc, out var rgDepthDesc);
				rgDepthDesc.name = DistortionDepthTextureName;
				rgDepthDesc.clearBuffer = true;
				rgDepthDesc.clearColor = Color.clear;
				rgDepthDesc.filterMode = FilterMode.Point;
				rgDepthDesc.wrapMode = TextureWrapMode.Clamp;

				// Opaqueでは新しいテクスチャを Transparent では Opaque で描画したテクスチャに描く
				var distortionMarkColorTexture = renderGraph.CreateTexture(rgColorDesc);
				var distortionMarkDepthTexture = builder.CreateTransientTexture(rgDepthDesc);

				// PassData設定
				passData.SourceDepth = resourceData.activeDepthTexture;
				passData.RendererListHandle = rendererListHandle;
				passData.Material = ariaProcessResourceData.BloomUtilMaterial;
				passData.Properties = m_Properties;

				// RenderGraphへの設定
				builder.UseTexture(passData.SourceDepth, AccessFlags.Read);
				builder.UseRendererList(passData.RendererListHandle);
				builder.SetRenderAttachment(distortionMarkColorTexture, 0, AccessFlags.Write);
				builder.SetRenderAttachmentDepth(distortionMarkDepthTexture, AccessFlags.Write);
				builder.AllowPassCulling(false);
				builder.SetRenderFunc<DistortionMarkPassData>(static (passData, context) =>
				{
					// BlitDepthだとUnsafeでも出来なかったので、Depthのコピーを自前でやる
					passData.Properties.SetTexture("_SourceDepthTexture", passData.SourceDepth, RenderTextureSubElement.Depth);
					CoreUtils.DrawFullScreen(context.cmd, passData.Material, passData.Properties, 0);

					// 発光用の LightMode を描画
					context.cmd.DrawRendererList(passData.RendererListHandle);
				});
				builder.SetGlobalTextureAfterPass(distortionMarkColorTexture, DistortionNormalTextureId);
			}

			// SetRenderFunc 内でのグローバルの書き換えがあるので、一応パスを分けておく
			using (var builder = renderGraph.AddRasterRenderPass<DistortionApplyPassData>("Distortion Apply", out var passData))
			{
				var source = resourceData.cameraOpaqueTexture;
				var destination = resourceData.activeColorTexture;

				passData.SourceColor = source;
				passData.DistortionPower = distortionVolume.Intensity;
				passData.Material = ariaProcessResourceData.DistortionMaterial;

				builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);
				builder.SetRenderFunc<DistortionApplyPassData>(static (passData, context) =>
				{
					context.cmd.SetGlobalFloat(DistortionPowerId, passData.DistortionPower);
					Blitter.BlitTexture(context.cmd, passData.SourceColor, Vector2.one, passData.Material, 0);
				});
			}
		}
	}
}
