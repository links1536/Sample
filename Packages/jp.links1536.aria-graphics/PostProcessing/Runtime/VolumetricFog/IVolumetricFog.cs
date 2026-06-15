using UnityEngine;

namespace Aria.Rendering.Universal.PostProcessing.VolumetricFog
{
	public interface IVolumetricFog
	{
		public Color ColorLight { get; }
		public Color ColorShadow { get; }

		public int SampleCount { get; }

		public float Intensity { get; }
		public float Extinction { get; }
		public float Near { get; }
		public float Far { get; }
		public float NoiseScale { get; }

		public bool EnableFogNoise { get; }
		public Vector3 FogNoiseDirection { get; }
		public float FogNoiseSpeed { get; }
		public float FogNoiseScale { get; }
		public float FogNoiseMin { get; }
		public float FogNoiseMax { get; }
	}
}
