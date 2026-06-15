using Aria.Rendering.Universal.PostProcessing.Bloom;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{

	public partial class PostProcessFeature
	{
		class LatePostProcessPass : ScriptableRenderPass
		{
			class PassData
			{
			}

			public LatePostProcessPass()
			{
				profilingSampler = new ProfilingSampler(nameof(LatePostProcessPass));
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				using (var builder = renderGraph.AddUnsafePass<PassData>("Late Post Process Pass", out var passData))
				{
					builder.AllowPassCulling(false);
					builder.AllowGlobalStateModification(true);
					builder.UseAllGlobalTextures(true);

					//var bloomVolume = VolumeManager.instance.stack.GetComponent<BloomEmissionVolume>();
					//var emissionVolume = VolumeManager.instance.stack.GetComponent<BloomEmissionVolume>();
					//if (emissionVolume != null && emissionVolume.IsActive())
					//	builder.UseGlobalTexture(BloomEmissionPass.BloomMarkColorTextureId, AccessFlags.Read);

					// PrePostprocessを実行
					builder.SetRenderFunc<PassData>(static (data, context) =>
					{
						//context.cmd.ClearRenderTarget(false, true, Color.clear);
					});
				}
			}
		}
	}
}
