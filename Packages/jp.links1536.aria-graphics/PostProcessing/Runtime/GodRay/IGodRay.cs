using UnityEngine;

namespace Aria.Rendering.Universal.PostProcessing.GodRay
{
	public interface IGodRay
	{
		public float Threshold { get; }
		public float Distance { get; }
		public float Intensity { get; }

		public int MaxSampleCount { get; }
		public int SampleCount { get; }
		public float NoiseScale { get; }

		public float Density { get; }
		public float Weight { get; }
		public float Decay { get; }

		public Color ColorLight { get; }
		public Color ColorShadow { get; }

		public bool EnableFogNoise { get; }
		public Vector3 FogNoiseDirection { get; }
		public float FogNoiseSpeed { get; }
		public float FogNoiseScale { get; }
		public float FogNoiseMin { get; }
		public float FogNoiseMax { get; }
	}
}
