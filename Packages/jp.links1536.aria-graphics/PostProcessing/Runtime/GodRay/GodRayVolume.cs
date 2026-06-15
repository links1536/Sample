using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.GodRay
{
	[DisplayInfo(name = "Aria/God Ray")]
	[VolumeComponentMenu("Aria/God Ray")]
	[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
	class GodRayVolume : VolumeComponent, IGodRay, IPostProcessComponent
	{
		const int SampleCountMax = 128;
		const int SampleCountMin = 4;

		// デフォルトはローカルの境目などでウェイトに応じて補間が行われるのでNoInterpを使用 (GodRay同士でのブレンドはうまくできなくなるけど)

		[Header("God Ray")]
		[SerializeField] MinFloatParameter m_Threshold = new MinFloatParameter(0.0f, 0.0f);
		[SerializeField] MinFloatParameter m_Distance = new MinFloatParameter(1000.0f, 0.0f);
		[SerializeField] MinFloatParameter m_Intensity = new MinFloatParameter(0.0f, 0.0f);

		[SerializeField] NoInterpClampedIntParameter m_SampleCount = new NoInterpClampedIntParameter(50, SampleCountMin, SampleCountMax);
		[SerializeField] ClampedFloatParameter m_NoiseScale = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

		[SerializeField] MinFloatParameter m_Density = new MinFloatParameter(1.0f, 0.0f);
		[SerializeField] MinFloatParameter m_Weight = new MinFloatParameter(1.0f, 0.0f);
		[SerializeField] ClampedFloatParameter m_Decay = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

		[SerializeField] ColorParameter m_ColorLight = new ColorParameter(Color.white, true, false, true);
		[SerializeField] ColorParameter m_ColorShadow = new ColorParameter(Color.black, true, false, true);

		[Header("God Ray Noise")]
		[SerializeField] BoolParameter m_EnableFogNoise = new BoolParameter(false);
		[SerializeField] NoInterpVector3Parameter m_FogNoiseDirection = new NoInterpVector3Parameter(Vector3.forward);
		[SerializeField] NoInterpFloatParameter m_FogNoiseSpeed = new NoInterpFloatParameter(1.0f);
		[SerializeField] NoInterpFloatParameter m_FogNoiseScale = new NoInterpFloatParameter(0.1f);
		[SerializeField] FloatParameter m_FogNoiseMin = new FloatParameter(0.0f);
		[SerializeField] FloatParameter m_FogNoiseMax = new FloatParameter(1.0f);

		public float Threshold
			=> m_Threshold.value;
		public float Distance
			=> m_Distance.value;
		public float Intensity
			=> m_Intensity.value;

		public int MaxSampleCount
			=> SampleCountMax;
		public int SampleCount
			=> m_SampleCount.value;
		public float NoiseScale
			=> m_NoiseScale.value;

		public float Density
			=> m_Density.value;
		public float Weight
			=> m_Weight.value;
		public float Decay
			=> m_Decay.value;


		public Color ColorLight
			=> m_ColorLight.value;
		public Color ColorShadow
			=> m_ColorShadow.value;


		public bool EnableFogNoise
			=> m_EnableFogNoise.value;
		public Vector3 FogNoiseDirection
			=> m_FogNoiseDirection.value;
		public float FogNoiseSpeed
			=> m_FogNoiseSpeed.value;
		public float FogNoiseScale
			=> m_FogNoiseScale.value;
		public float FogNoiseMin
			=> m_FogNoiseMin.value;
		public float FogNoiseMax
			=> m_FogNoiseMax.value;

		public bool IsActive()
			=> Intensity > 0
			&& SampleCount > 0
			&& ColorLight.a > 0
			&& ColorShadow.a > 0;

		public bool IsTileCompatible()
			=> false;
	}
}
