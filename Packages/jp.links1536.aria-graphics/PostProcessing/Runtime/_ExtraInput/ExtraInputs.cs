using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{
	public class ExtraInputsResourceData : UniversalResourceDataBase
	{
		public TextureHandle CharacterMask;

		public override void Reset()
		{
			CharacterMask = TextureHandle.nullHandle;
		}
	}
}
