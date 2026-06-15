using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{
	[DisplayInfo(name = "Aria/Anime Diffusion")]
	[VolumeComponentMenu("Aria/Anime Diffusion")]
	[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
	class AnimeDiffusitionVolume : VolumeComponent, IAnimeDiffusion, IPostProcessComponent
	{
		[SerializeField] BoolParameter m_Active = new BoolParameter(false);
		[SerializeField] ClampedFloatParameter m_SampleScale = new ClampedFloatParameter(1, 0, 10);
		[SerializeField] ClampedFloatParameter m_Weight = new ClampedFloatParameter(0.3f, 0, 1);

		public bool Active
			=> m_Active.value;

		public float SampleScale
			=> m_SampleScale.value;

		public float Weight
			=> m_Weight.value;

		public bool IsActive()
			=> Active
			&& Weight > 0;
	}
}
