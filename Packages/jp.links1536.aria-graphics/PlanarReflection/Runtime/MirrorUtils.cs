using UnityEngine;
using UnityEngine.Rendering;

namespace Aria.Rendering.Universal.PlanarReflection
{
	static class MirrorUtils
	{
		public static GlobalKeyword KeyworldMirrorRendering = GlobalKeyword.Create("_MIRROR_RENDERING");
		public static GlobalKeyword KeyworldMirrorBaking = GlobalKeyword.Create("_MIRROR_BAKING");

		public static int ReflectionTextureId = Shader.PropertyToID("_ReflectionTexture");
	}
}
