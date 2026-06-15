using UnityEngine;
using UnityEngine.Rendering;

namespace Aria.Rendering.Universal.PostProcessing
{
	class AriaPostProcessResourceData : ContextItem
	{
		public Material BloomUtilMaterial;
		public Material GodRayMaterial;
		public Material VolumetricFogMaterial;
		public Material DistortionMaterial;

		public override void Reset()
		{
			BloomUtilMaterial = null;
			GodRayMaterial = null;
			VolumetricFogMaterial = null;
			DistortionMaterial = null;
		}
	}
}
