using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal
{
	public static class URPPropertyAccessor
	{
		static PropertyInfo m_RendererToRendererFeatures;

		static URPPropertyAccessor()
		{
			// RendererFeatureは接続したいことが多いけど秘匿されている
			var rendererType = typeof(ScriptableRenderer);
			m_RendererToRendererFeatures = rendererType.GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		static bool TryGetPipelineAsset(out UniversalRenderPipelineAsset asset)
			=> (asset = UniversalRenderPipeline.asset) != null;

		public static bool TryGetRenderer(int index, out UniversalRenderer renderer)
		{
			if (!TryGetPipelineAsset(out var asset))
			{
				renderer = null;
				return false;
			}
			renderer = asset.GetRenderer(index) as UniversalRenderer;
			return renderer != null;
		}

		public static bool TryGetRendererData(int index, out UniversalRendererData rendererData)
		{
			if (!TryGetPipelineAsset(out var asset)
			 || asset.rendererDataList.IsEmpty
			 || index < 0 || asset.rendererDataList.Length <= index)
			{
				rendererData = null;
				return false;
			}
			rendererData = asset.rendererDataList[index] as UniversalRendererData;
			return rendererData != null;
		}

		public static bool TryGetRenderFeatures(int index, out List<ScriptableRendererFeature> rendererFeatureList)
		{
			if (!TryGetRenderer(index, out var renderer))
			{
				rendererFeatureList = null;
				return false;
			}
			rendererFeatureList = m_RendererToRendererFeatures?.GetValue(renderer) as List<ScriptableRendererFeature>;
			return rendererFeatureList != null;
		}

		public static bool TryGetRenderFeatures(ScriptableRenderer renderer, out List<ScriptableRendererFeature> rendererFeatureList)
		{
			if (renderer == null)
			{
				rendererFeatureList = null;
				return false;
			}
			rendererFeatureList = m_RendererToRendererFeatures?.GetValue(renderer) as List<ScriptableRendererFeature>;
			return rendererFeatureList != null;
		}
	}
}
