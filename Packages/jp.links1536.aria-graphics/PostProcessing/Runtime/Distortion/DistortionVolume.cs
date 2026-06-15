using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.Distortion
{
	[DisplayInfo(name = "Aria/Distortion")]
	[VolumeComponentMenu("Aria/Distortion")]
	[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
	class DistortionVolume : VolumeComponent, IDistortion, IPostProcessComponent
	{
		[SerializeField] MinFloatParameter m_Intensity = new MinFloatParameter(0, 0);

		public float Intensity
			=> m_Intensity.value;

		public bool IsActive()
			=> Intensity > 0;

		public bool IsTileCompatible()
			=> false;
	}
}
