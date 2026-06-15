using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.GodRay
{
	partial class GodRayPass : ScriptableRenderPass
	{
		private class GodRayBlitPassData
		{
			internal bool IsBlitTexture;
			internal Material GodRayMaterial;
			internal TextureHandle ColorTargetTexture;
			internal TextureHandle GodRayMarkTexture;
			internal TextureHandle GodRayTexture;
			internal TextureHandle GodRayApplyTexture;
		}

		TextureDesc ToTextureDesc(RenderTextureDescriptor desc, string name)
			=> new TextureDesc(desc)
			{
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				name = name,
				// DontCareで読むのでわざわざ消すのはやらない
				clearBuffer = false,
			};

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			var cameraData = frameData.Get<UniversalCameraData>();
			if (!AriaPostProcessUtils.IsPostProcessTarget(cameraData))
				return;

			if (!TryGetVolume(out var godRayVolume))
				return;

			var ariaProcessResourceData = frameData.Get<AriaPostProcessResourceData>();
			var godRayMaterial = ariaProcessResourceData.GodRayMaterial;

			SetupMaterial(godRayMaterial, godRayVolume);
			ConfigureInput(ScriptableRenderPassInput.Depth);

			var renderingData = frameData.Get<UniversalRenderingData>();
			var lightData = frameData.Get<UniversalLightData>();
			var resourceData = frameData.Get<UniversalResourceData>();

			// レンダーターゲット作成
			var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
			var downScaleDescriptor = RenderTargetUtils.GetDownsampling(cameraTargetDescriptor, m_DownScaleCount);

			var asset = UniversalRenderPipeline.asset;
			var godrayTextureDescriptor = RenderTargetUtils.CreateColorDescription(cameraData.isHdrEnabled, asset, downScaleDescriptor);
			var godrayApplyDescriptor = RenderTargetUtils.CreateColorDescription(cameraData.isHdrEnabled, asset, cameraTargetDescriptor);

			using (var builder = renderGraph.AddUnsafePass<GodRayBlitPassData>("God Ray", out var passData))
			{
				var godRayMarkTexture = builder.CreateTransientTexture(ToTextureDesc(godrayTextureDescriptor, GodRayMarkTextureName));
				var godRayTexture = builder.CreateTransientTexture(ToTextureDesc(godrayTextureDescriptor, GodRayTextureName));
				var godRayApplyTexture = builder.CreateTransientTexture(ToTextureDesc(godrayApplyDescriptor, GodRayApplyTextureName));

				var colorTargetDescriptor = resourceData.activeColorTexture.GetDescriptor(renderGraph);
				bool supportsCopyTexture = colorTargetDescriptor.format == ToTextureDesc(godrayApplyDescriptor, GodRayApplyTextureName).format;

				// PassDataをセットアップ
				passData.IsBlitTexture = !supportsCopyTexture || cameraData.isSceneViewCamera;
				passData.GodRayMaterial = godRayMaterial;
				passData.ColorTargetTexture = resourceData.activeColorTexture;
				passData.GodRayMarkTexture = godRayMarkTexture;
				passData.GodRayTexture = godRayTexture;
				passData.GodRayApplyTexture = godRayApplyTexture;

				// URPへテクスチャの使用申請
				builder.UseTexture(passData.ColorTargetTexture, AccessFlags.ReadWrite);
				builder.UseTexture(passData.GodRayMarkTexture, AccessFlags.ReadWrite);
				builder.UseTexture(passData.GodRayTexture, AccessFlags.ReadWrite);
				builder.UseTexture(passData.GodRayApplyTexture, AccessFlags.ReadWrite);

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);

				// ゴッドレイの元をマークする
				builder.SetRenderFunc<GodRayBlitPassData>(static (passData, context) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
					cmd.SetGlobalTexture(GodRayTextureId, passData.GodRayTexture);
					Blitter.BlitCameraTexture(cmd,
						source: passData.ColorTargetTexture, destination: passData.GodRayMarkTexture,
						loadAction: RenderBufferLoadAction.DontCare, storeAction: RenderBufferStoreAction.Store,
						material: passData.GodRayMaterial, pass: (int)RenderPassId.Mark
					);
					Blitter.BlitCameraTexture(cmd,
						source: passData.GodRayMarkTexture, destination: passData.GodRayTexture,
						loadAction: RenderBufferLoadAction.DontCare, storeAction: RenderBufferStoreAction.Store,
						material: passData.GodRayMaterial, pass: (int)RenderPassId.Blur
					);
					Blitter.BlitCameraTexture(cmd,
						source: passData.ColorTargetTexture, destination: passData.GodRayApplyTexture,
						loadAction: RenderBufferLoadAction.DontCare, storeAction: RenderBufferStoreAction.Store,
						material: passData.GodRayMaterial, pass: (int)RenderPassId.Apply
					);

					RenderGraphUtils.CopyTexture(cmd, passData.GodRayApplyTexture, passData.ColorTargetTexture, passData.IsBlitTexture);
					cmd.SetGlobalTexture(GodRayTextureId, Texture2D.blackTexture);
				});
			}
		}
	}
}
