using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.CharacterMask
{
	[DisplayInfo(name = "Aria/Character Mask")]
	[VolumeComponentMenu("Aria/Character Mask")]
	[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
	class CharacterMaskVolume : VolumeComponent, IPostProcessComponent
	{
		[SerializeField] ClampedFloatParameter m_Character = new ClampedFloatParameter(1.0f, 0, 1);
		[SerializeField] ClampedFloatParameter m_SpecularMask = new ClampedFloatParameter(1.0f, 0, 1);
		[SerializeField] ClampedFloatParameter m_RimMask = new ClampedFloatParameter(1.0f, 0, 1);
		[SerializeField] ClampedFloatParameter m_Skin = new ClampedFloatParameter(1.0f, 0, 1);

		public float Character
			=> m_Character.value;

		public float SpecularMask
			=> m_SpecularMask.value;

		public float RimMask
			=> m_RimMask.value;

		public float Skin
			=> m_Skin.value;

		public bool IsActive()
			=> true;

		public bool IsTileCompatible()
			=> false;
	}
}
