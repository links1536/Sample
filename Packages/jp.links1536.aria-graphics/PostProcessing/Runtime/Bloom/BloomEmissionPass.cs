using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.Bloom
{
	partial class BloomEmissionPass : ScriptableRenderPass
	{
		public enum RenderingTiming
		{
			Opaque,
			Transparent,
		}


		const string TagName = "BloomEmission";
		static readonly ShaderTagId ShaderTag = new ShaderTagId(TagName);

		const string BloomMarkColorTextureName = "_BloomEmissionTexture";
		public static readonly int BloomMarkColorTextureId = Shader.PropertyToID(BloomMarkColorTextureName);

		const string BloomMarkDepthTextureName = "_BloomEmissionDepthTexture";
		static readonly int BloomMarkDepthTextureId = Shader.PropertyToID(BloomMarkDepthTextureName);

		static readonly int BloomEmissionParamsId = Shader.PropertyToID("_BloomEmissionParams");

		readonly RenderQueueRange m_RenderQueueRange;
		readonly RenderingTiming m_RenderingTiming;

		MaterialPropertyBlock m_Properties;

		public BloomEmissionPass(RenderQueueRange renderQueueRange, RenderingTiming renderingTiming)
		{
			m_RenderQueueRange = renderQueueRange;
			m_RenderingTiming = renderingTiming;

			m_Properties = new MaterialPropertyBlock();
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			// 別のカメラやアクティブ切ったとき用に、テクスチャに黒を割り当てておく
			cmd.SetGlobalTexture(BloomMarkColorTextureId, Texture2D.blackTexture);

			base.OnCameraCleanup(cmd);
		}

	}
}
