using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PlanarReflection
{
	/// <summary>
	/// 反射を適用するためのやつ
	/// </summary>
	[DisallowMultipleRendererFeature]
	class MirrorRenderingFeature : ScriptableRendererFeature
	{
		class MirrorRenderingPass : ScriptableRenderPass
		{
			public MirrorRenderingPass()
			{
				profilingSampler = new ProfilingSampler(nameof(MirrorRenderingPass));
			}

			class PassData
			{
			}

			public override void OnCameraCleanup(CommandBuffer cmd)
			{
				cmd.DisableKeyword(MirrorUtils.KeyworldMirrorRendering);
				base.OnCameraCleanup(cmd);
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				using (var builder = renderGraph.AddUnsafePass<PassData>("Setup Mirror Rendering", out var passData))
				{
					builder.AllowGlobalStateModification(true);
					builder.AllowPassCulling(false);
					builder.SetRenderFunc((PassData passData, UnsafeGraphContext context) =>
					{
						var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
						ExecutePass(passData, cmd);
					});
				}
			}

			static void ExecutePass(PassData passData, CommandBuffer cmd)
			{
				cmd.EnableKeyword(MirrorUtils.KeyworldMirrorRendering);
			}
		}

		[SerializeField] int m_BakeRendererIndex;

		MirrorRenderingPass m_Pass = default;

		public int BakeRendererIndex
			=> m_BakeRendererIndex;

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (m_Pass == null)
				return;
			renderer.EnqueuePass(m_Pass);
		}

		public override void Create()
		{
			m_Pass = new MirrorRenderingPass();
			m_Pass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
		}
	}
}
