using UnityEngine;
using UnityEngine.Rendering;

namespace Aria.Rendering.Universal.PostProcessing
{
	static class AnimeDiffusionUtils
	{
		const string Keyword = "ENABLE_ANIMEDIFFUSION";
		const string SampleScaleProperty = "_AnimeDiffusion_SampleScale";
		const string WeightProperty = "_AnimeDiffusion_Weight";

		static readonly int SampleScalePropertyId = Shader.PropertyToID(SampleScaleProperty);
		static readonly int WeightPropertyId = Shader.PropertyToID(WeightProperty);

		public static void ExecutePass(CommandBuffer cmd, IAnimeDiffusion animeDiffusion)
		{
			CoreUtils.SetKeyword(cmd, Keyword, animeDiffusion.Active);
			cmd.SetGlobalFloat(SampleScalePropertyId, animeDiffusion.SampleScale);
			cmd.SetGlobalFloat(WeightPropertyId, animeDiffusion.Weight);
		}

		public static void Cleanup(CommandBuffer cmd)
		{
			CoreUtils.SetKeyword(cmd, Keyword, false);
		}
	}
}
