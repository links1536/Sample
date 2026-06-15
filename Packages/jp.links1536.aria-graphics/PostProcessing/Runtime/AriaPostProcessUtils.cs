using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{
	static class AriaPostProcessUtils
	{
		public static bool IsPostProcessTarget(UniversalCameraData cameraData)
			=> cameraData.postProcessEnabled && cameraData.cameraType switch
			{
				_ => true
			};
	}
}
