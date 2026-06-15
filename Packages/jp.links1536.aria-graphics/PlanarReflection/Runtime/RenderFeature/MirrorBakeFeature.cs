using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PlanarReflection
{
	/// <summary>
	/// 反射を焼き付けるためのRendererFeature
	/// </summary>
	[DisallowMultipleRendererFeature]
	class MirrorBakeFeature : ScriptableRendererFeature
	{
		class MirrorBakePass : ScriptableRenderPass
		{
			public MirrorBakePass()
			{
				profilingSampler = new ProfilingSampler(nameof(MirrorBakePass));
			}

			class PassData
			{

			}

			public override void OnCameraCleanup(CommandBuffer cmd)
			{
				cmd.DisableKeyword(MirrorUtils.KeyworldMirrorBaking);
				cmd.SetInvertCulling(false);
				base.OnCameraCleanup(cmd);
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				using var builder = renderGraph.AddUnsafePass<PassData>("MirrorBakePass", out var passData);
				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);
				builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
					ExecutePass(cmd);
				});
			}

			static void ExecutePass(CommandBuffer cmd)
			{
				cmd.SetInvertCulling(true);
				cmd.EnableKeyword(MirrorUtils.KeyworldMirrorBaking);
			}
		}

		MirrorBakePass m_Pass = default;

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (m_Pass == null)
				return;
			renderer.EnqueuePass(m_Pass);
		}

		public override void Create()
		{
			m_Pass = new MirrorBakePass();
			m_Pass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
		}
	}
}
