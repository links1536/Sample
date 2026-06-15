using UnityEngine;

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

			foreach(var renderer in m_Renderers)
			{

				if (renderer == null)
					continue;

				var transform = renderer.transform;
				var forward = transform.forward;
				var up = transform.up;
				var right = transform.right;

				// 正式には MaterialPropertyBlock ではなく、Materialへ直接代入する必要がある
				m_Properties.SetVector(ForwardId, forward);
				m_Properties.SetVector(UpId, up);
				m_Properties.SetVector(RightId, right);

				renderer.SetPropertyBlock(m_Properties);
			}
		}
	}
}
