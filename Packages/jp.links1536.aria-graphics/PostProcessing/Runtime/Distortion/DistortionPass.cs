using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.Distortion
{
	partial class DistortionPass : ScriptableRenderPass
	{
		enum RenderPass
		{
			Distortion,
			DistortionNormal,
		}


		const string TagName = "DistortionNormal";
		static readonly ShaderTagId ShaderTag = new ShaderTagId(TagName);

		const string DistortionNormalTextureName = "_DistortionNormalTexture";
		public static readonly int DistortionNormalTextureId = Shader.PropertyToID(DistortionNormalTextureName);

		const string DistortionDepthTextureName = "_DistortionDepthTexture";
		static readonly int DistoritonDepthTextureId = Shader.PropertyToID(DistortionDepthTextureName);

		static readonly int DistortionPowerId = Shader.PropertyToID("_DistortionPower");

		MaterialPropertyBlock m_Properties;

		public DistortionPass()
		{
			m_Properties = new MaterialPropertyBlock();
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			// 別のカメラやアクティブ切ったとき用に、テクスチャにデフォルトを割り当てておく
			cmd.SetGlobalTexture(DistortionNormalTextureId, Texture2D.normalTexture);

			base.OnCameraCleanup(cmd);
		}

	}
}
