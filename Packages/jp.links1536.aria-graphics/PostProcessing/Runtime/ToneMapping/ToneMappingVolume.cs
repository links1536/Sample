using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{
	[DisplayInfo(name = "Aria/Tone Mapping")]
	[VolumeComponentMenu("Aria/Tone Mapping")]
	[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
	class ToneMappingVolume : VolumeComponent, IToneMapping, IPostProcessComponent
	{
		[SerializeField] BoolParameter m_Active = new BoolParameter(false);

		public bool Active
			=> m_Active.value;

		public bool IsActive()
			=> Active;
	}
}
