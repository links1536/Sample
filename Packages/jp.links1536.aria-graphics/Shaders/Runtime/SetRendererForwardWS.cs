using UnityEngine;
using UnityEngine.Pool;

namespace Links
{
	public class SetRendererForwardWS : MonoBehaviour
	{
		static int ForwardId = Shader.PropertyToID("_RendererForwardWS");
		static int UpId = Shader.PropertyToID("_RendererUpWS");
		static int RightId = Shader.PropertyToID("_RendererRightWS");

		Renderer[] m_Renderers;
		MaterialPropertyBlock m_Properties;

		void OnEnable()
		{
			m_Renderers = GetComponentsInChildren<Renderer>(true);
			m_Properties = new MaterialPropertyBlock();
		}

		void LateUpdate()
		{
			if (m_Renderers == null)
				return;

			bool isPlaying = Application.isPlaying;

			foreach (var renderer in m_Renderers)
			{

				if (renderer == null)
					continue;

				var transform = renderer.transform;
				var forward = transform.forward;
				var up = transform.up;
				var right = transform.right;

				if (isPlaying)
				{
					using var pool = ListPool<Material>.Get(out var list);
					renderer.GetMaterials(list);
					foreach (var material in list)
					{
						material.SetVector(ForwardId, forward);
						material.SetVector(UpId, up);
						material.SetVector(RightId, right);
					}
					renderer.SetMaterials(list);
				}
				else
				{
					// 正式には MaterialPropertyBlock ではなく、Materialへ直接代入する必要がある
					m_Properties.SetVector(ForwardId, forward);
					m_Properties.SetVector(UpId, up);
					m_Properties.SetVector(RightId, right);

					renderer.SetPropertyBlock(m_Properties);
				}
			}
		}
	}
}
