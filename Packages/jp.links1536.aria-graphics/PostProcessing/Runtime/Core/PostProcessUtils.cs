using UnityEngine;

namespace Aria.Rendering.Universal.PostProcessing
{
	static class PrePostProcessUtils
	{
		public const string LowResolutionColorTextureName = "_LowResolutionColorTexture";
		public static readonly int LowResolutionColorTextureId = Shader.PropertyToID(LowResolutionColorTextureName);

		public const string LowResolutionDepthTextureName = "_LowResolutionDepthTexture";
		public static readonly int LowResolutionDepthTextureId = Shader.PropertyToID(LowResolutionDepthTextureName);
	}
}
