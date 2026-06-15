using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PlanarReflection
{
	[ExecuteAlways]
	class MirrorObjectManager : MonoBehaviour
	{
		static MirrorObjectManager m_Instance;
		public static bool TryGetInstance(out MirrorObjectManager instance)
		{
			instance = m_Instance;
			return instance != null;
		}

		[RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		static void RuntimeInitialize()
		{
			var obj = new GameObject("MirrorObjectManager", typeof(MirrorObjectManager));
			obj.hideFlags = HideFlags.HideAndDontSave;
			m_Instance = obj.GetComponent<MirrorObjectManager>();
			if (Application.isPlaying)
				DontDestroyOnLoad(obj);
		}

		HashSet<MirrorObject> m_MirrorObjects = new HashSet<MirrorObject>();

		public void Register(MirrorObject mirrorObject)
			=> m_MirrorObjects.Add(mirrorObject);
		public void Unregister(MirrorObject mirrorObject)
			=> m_MirrorObjects.Remove(mirrorObject);

		void OnEnable()
		{
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		void OnDisable()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (camera == null)
				return;
			if (!TryGetRenderFeature<MirrorRenderingFeature>(camera, out var mirrorRenderingFeature)
			 || !mirrorRenderingFeature.isActive)
				return;

			if (!TryGetRenderFeature(mirrorRenderingFeature.BakeRendererIndex, out MirrorBakeFeature _))
			{
				Debug.LogError($"RendererIndex:{mirrorRenderingFeature.BakeRendererIndex} は {nameof(MirrorBakeFeature)} が設定されていません");
				return;
			}

			foreach (var mirror in m_MirrorObjects)
			{
				if (mirror == null)
					continue;
				mirror.Render(context, camera, mirrorRenderingFeature.BakeRendererIndex);
			}
		}

		bool TryGetRenderFeature<T>(Camera camera, out T renderFeature)
			where T : ScriptableRendererFeature
		{
			renderFeature = default;
			if (camera == null)
				return false;
			var additional = camera.GetUniversalAdditionalCameraData();
			if (additional == null)
				return false;
			var renderer = additional.scriptableRenderer;
			if (renderer == null)
				return false;
			if (!URPPropertyAccessor.TryGetRenderFeatures(renderer, out var featureList))
				return false;
			if (featureList == null)
				return false;
			renderFeature = featureList.OfType<T>().FirstOrDefault();
			return renderFeature != null;
		}

		bool TryGetRenderFeature<T>(int index, out T renderFeature)
			where T : ScriptableRendererFeature
		{
			renderFeature = default;
			if (!URPPropertyAccessor.TryGetRenderFeatures(index, out var featureList))
				return false;
			if (featureList == null)
				return false;
			renderFeature = featureList.OfType<T>().FirstOrDefault();
			return renderFeature != null;
		}
	}
}
