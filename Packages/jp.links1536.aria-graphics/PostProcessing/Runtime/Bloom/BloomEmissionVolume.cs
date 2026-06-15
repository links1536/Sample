using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.Bloom
{
	[DisplayInfo(name = "Aria/Bloom Emission")]
	[VolumeComponentMenu("Aria/Bloom Emission")]
	[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
	class BloomEmissionVolume : VolumeComponent, IBloomEmission, IPostProcessComponent
	{
		[SerializeField] MinFloatParameter m_Threshold = new MinFloatParameter(0, 0);
		[SerializeField] MinFloatParameter m_Intensity = new MinFloatParameter(0, 0);
		[SerializeField] IntParameter m_Clamp = new IntParameter(65472);

		public float Threshold
			=> m_Threshold.value;

		public float Intensity
			=> m_Intensity.value;

		public int Clamp
			=> m_Clamp.value;

		public bool IsActive()
			=> Intensity > 0;

		public bool IsTileCompatible()
			=> false;
	}
}
