using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{

	public partial class PostProcessFeature
	{
		class PrePostProcessPass : ScriptableRenderPass
		{
			class PassData
			{
				internal PassInput Input;

				internal TextureHandle Color;
				internal TextureHandle Depth;

				internal TextureHandle DownScaleColor;
				internal TextureHandle DownScaleDepth;

				internal IToneMapping ToneMapping;
				internal IAnimeDiffusion AnimeDiffusion;
			}

			int m_DownScaleCount;

			public PrePostProcessPass(int downScaleCount)
			{
				profilingSampler = new ProfilingSampler(nameof(PrePostProcessPass));
				m_DownScaleCount = downScaleCount;
			}

			public override void OnCameraCleanup(CommandBuffer cmd)
			{
				// 別のカメラやアクティブ切ったときようにキーワードをオフにする
				ToneMappingUtils.Cleanup(cmd);
				AnimeDiffusionUtils.Cleanup(cmd);

				base.OnCameraCleanup(cmd);
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				var lightData = frameData.Get<UniversalLightData>();
				var cameraData = frameData.Get<UniversalCameraData>();
				var resourceData = frameData.Get<UniversalResourceData>();
				var renderingData = frameData.Get<UniversalRenderingData>();

				if (!AriaPostProcessUtils.IsPostProcessTarget(cameraData))
					return;

				var tonemappingVolume = VolumeManager.instance.stack.GetComponent<ToneMappingVolume>();
				var animeDiffusionVolume = VolumeManager.instance.stack.GetComponent<AnimeDiffusitionVolume>();

				// Input作成フラグ
				PassInput input = PassInput.None;

				if (m_DownScaleCount > 0 || animeDiffusionVolume.IsActive())
					input |= PassInput.DownScale;

				// 低解像度用Descriptor作成
				var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
				var downScaleDesc = RenderTargetUtils.GetDownsampling(cameraTargetDescriptor, m_DownScaleCount);

				using (var builder = renderGraph.AddUnsafePass<PassData>("Pre Post Process Pass", out var passData))
				{
					passData.Input = input;
					passData.ToneMapping = tonemappingVolume;
					passData.AnimeDiffusion = animeDiffusionVolume;

					builder.AllowPassCulling(false);
					builder.AllowGlobalStateModification(true);

					// 解像度を落としたカラー・デプステクスチャを作成する
					if ((passData.Input & PassInput.DownScale) != 0)
					{
						// レンダーターゲット作成
						var asset = UniversalRenderPipeline.asset;
						var downScaleColorDesc = RenderTargetUtils.CreateColorDescription(cameraData.isHdrEnabled, asset, downScaleDesc);
						var downScaleDepthDesc = RenderTargetUtils.CreateDepthStencilDescription(downScaleDesc);

						passData.Color = resourceData.activeColorTexture;
						passData.Depth = resourceData.activeDepthTexture;

						passData.DownScaleColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, downScaleColorDesc, PrePostProcessUtils.LowResolutionColorTextureName, false, FilterMode.Bilinear, TextureWrapMode.Clamp);
						passData.DownScaleDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, downScaleDepthDesc, PrePostProcessUtils.LowResolutionDepthTextureName, false, FilterMode.Bilinear, TextureWrapMode.Clamp);

						builder.UseTexture(passData.Color, AccessFlags.Read);
						builder.UseTexture(passData.Depth, AccessFlags.Read);

						builder.UseTexture(passData.DownScaleColor, AccessFlags.Write);
						builder.UseTexture(passData.DownScaleDepth, AccessFlags.Write);

						builder.SetGlobalTextureAfterPass(passData.DownScaleColor, PrePostProcessUtils.LowResolutionColorTextureId);
						builder.SetGlobalTextureAfterPass(passData.DownScaleDepth, PrePostProcessUtils.LowResolutionDepthTextureId);
					}

					// PrePostprocessを実行
					builder.SetRenderFunc<PassData>(static (data, context) =>
					{
						var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

						// カラーとデプスの縮小バッファを作成
						if ((data.Input & PassInput.DownScale) != 0)
						{
							CoreUtils.SetRenderTarget(cmd,
								data.DownScaleColor, colorLoadAction: RenderBufferLoadAction.DontCare, colorStoreAction: RenderBufferStoreAction.Store,
								data.DownScaleDepth, depthLoadAction: RenderBufferLoadAction.DontCare, depthStoreAction: RenderBufferStoreAction.Store,
								clearFlag: ClearFlag.None
							);

							RTHandle source = data.Color;
							Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
							Blitter.BlitColorAndDepth(cmd, data.Color, data.Depth, viewportScale, 1, true);
						}

						// ポストエフェクトの準備パスを実行
						ToneMappingUtils.ExecutePass(cmd, data.ToneMapping);
						AnimeDiffusionUtils.ExecutePass(cmd, data.AnimeDiffusion);
					});
				}
			}
		}
	}
}
