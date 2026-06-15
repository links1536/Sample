using UnityEngine.Rendering;

namespace Aria.Rendering.Universal.PostProcessing
{
	static class ToneMappingUtils
	{
		const string Keyword = "ENABLE_TONEMAPPING";

		public static void ExecutePass(CommandBuffer cmd, IToneMapping tonemapping)
		{
			CoreUtils.SetKeyword(cmd, Keyword, tonemapping.Active);
		}

		public static void Cleanup(CommandBuffer cmd)
		{
			CoreUtils.SetKeyword(cmd, Keyword, false);
		}
	}
}
