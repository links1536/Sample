using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.CharacterMask
{
	partial class CharacterMaskPass : ScriptableRenderPass
	{
		class BloomMarkPassData
		{
			internal TextureHandle SourceDepth;
			internal RendererListHandle RendererListHandle;
			internal Material Material;
			internal MaterialPropertyBlock Properties;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			// 各リソースデータ取得
			var lightData = frameData.Get<UniversalLightData>();
			var cameraData = frameData.Get<UniversalCameraData>();
			var resourceData = frameData.Get<UniversalResourceData>();
			var renderingData = frameData.Get<UniversalRenderingData>();
			var extraInputsData = frameData.GetOrCreate<ExtraInputsResourceData>();
			var ariaProcessResourceData = frameData.Get<AriaPostProcessResourceData>();

			if (!AriaPostProcessUtils.IsPostProcessTarget(cameraData))
				return;

			using (var builder = renderGraph.AddRasterRenderPass<BloomMarkPassData>(passName, out var passData, profilingSampler))
			{
				// 描画パラメータ
				var filterSettings = new FilteringSettings(RenderQueueRange.all, cameraData.camera.cullingMask);
				var sortFlags = cameraData.defaultOpaqueSortFlags;
				var drawSettings = RenderingUtils.CreateDrawingSettings(ShaderTag, renderingData, cameraData, lightData, sortFlags);
				var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
				var rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
				if (!rendererListHandle.IsValid())
					return;

				// Color用設定
				var asset = UniversalRenderPipeline.asset;
				var colorDesc = RenderTargetUtils.CreateColorDescription(cameraData.isHdrEnabled, asset, cameraData.cameraTargetDescriptor);
				RenderTargetUtils.GetTextureDesc(colorDesc, out var markColorTextureDesc);
				markColorTextureDesc.name = CharacterMaskTextureName;
				markColorTextureDesc.clearBuffer = true;
				markColorTextureDesc.clearColor = Color.clear;
				markColorTextureDesc.filterMode = FilterMode.Bilinear;
				markColorTextureDesc.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
				markColorTextureDesc.wrapMode = TextureWrapMode.Clamp;

				// Depth用設定
				var depthDesc = RenderTargetUtils.CreateDepthStencilDescription(cameraData.cameraTargetDescriptor);
				RenderTargetUtils.GetTextureDesc(depthDesc, out var markDepthTextureDesc);
				markDepthTextureDesc.clearBuffer = true;
				markDepthTextureDesc.clearColor = Color.clear;
				markDepthTextureDesc.filterMode = FilterMode.Point;
				markDepthTextureDesc.wrapMode = TextureWrapMode.Clamp;


				// Opaqueでは新しいテクスチャを Transparent では Opaque で描画したテクスチャに描く
				var colorTexture = renderGraph.CreateTexture(markColorTextureDesc);
				var depthTexture = builder.CreateTransientTexture(markDepthTextureDesc);

				// PassData設定
				passData.SourceDepth = resourceData.activeDepthTexture;
				passData.RendererListHandle = rendererListHandle;
				passData.Material = ariaProcessResourceData.BloomUtilMaterial;
				passData.Properties = m_Properties;

				// RenderGraphへの設定
				builder.UseTexture(passData.SourceDepth, AccessFlags.Read);
				builder.UseRendererList(passData.RendererListHandle);
				builder.SetRenderAttachment(colorTexture, 0, AccessFlags.Write);
				builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.Write);
				builder.AllowPassCulling(false);
				builder.SetRenderFunc<BloomMarkPassData>(static (passData, context) =>
				{
					// BlitDepthだとUnsafeでも出来なかったので、Depthのコピーを自前でやる
					passData.Properties.SetTexture("_SourceDepthTexture", passData.SourceDepth, RenderTextureSubElement.Depth);
					CoreUtils.DrawFullScreen(context.cmd, passData.Material, passData.Properties, 0);

					// 発光用の LightMode を描画
					context.cmd.DrawRendererList(passData.RendererListHandle);
				});
				builder.SetGlobalTextureAfterPass(colorTexture, CharacterMaskTextureId);

				// 描画命令を予約したので、BloomMarkを覚えておく
				extraInputsData.CharacterMask = colorTexture;
			}
		}
	}
}
