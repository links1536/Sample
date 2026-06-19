using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.Bloom
{
	partial class CharacterMaskPass : ScriptableRenderPass
	{
		public enum RenderingTiming
		{
			Opaque,
			Transparent,
		}


		const string TagName = "CharacterMask";
		static readonly ShaderTagId ShaderTag = new ShaderTagId(TagName);

		const string CharacterMaskTextureName = "_CharacterMaskTexture";
		public static readonly int CharacterMaskTextureId = Shader.PropertyToID(CharacterMaskTextureName);

		MaterialPropertyBlock m_Properties;

		public CharacterMaskPass()
		{
			m_Properties = new MaterialPropertyBlock();
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			// 別のカメラやアクティブ切ったとき用に、テクスチャに黒を割り当てておく
			cmd.SetGlobalTexture(CharacterMaskTextureId, Texture2D.blackTexture);

			base.OnCameraCleanup(cmd);
		}

	}
}
