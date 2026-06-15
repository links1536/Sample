using UnityEngine;

namespace Aria.Rendering.Universal.PostProcessing
{
	[CreateAssetMenu(fileName = "AriaPostProcessData.asset", menuName = "Aria/PostProcessing/AriaPostProcessData")]
	class AriaPostProcessData : ScriptableObject
	{
		[Header("コンピュートシェーダー")]
		[SerializeField] ComputeShader m_GaussianBlurShader;

		[Header("シェーダー")]
		[SerializeField] Shader m_BloomUtilShader;
		[SerializeField] Shader m_GodRayShader;
		[SerializeField] Shader m_DistortionShader;
		[SerializeField] Shader m_VolumetricFogShader;

		public ComputeShader GaussianBlurShader
			=> m_GaussianBlurShader;

		public Shader BloomUtilShader
			=> m_BloomUtilShader;

		public Shader GodRayShader
			=> m_GodRayShader;

		public Shader DistortionShader
			=> m_DistortionShader;

		public Shader VolumetricFogShader
			=> m_VolumetricFogShader;
	}
}
