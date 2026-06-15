using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.VolumetricFog
{
	[DisplayInfo(name = "Aria/Volumetric Fog")]
	[VolumeComponentMenu("Aria/Volumetric Fog")]
	[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
	class VolumetricFogVolume : VolumeComponent, IVolumetricFog, IPostProcessComponent
	{
		// デフォルトはローカルの境目などでウェイトに応じて補間が行われるのでNoInterpを使用 (VolumetricFog同士でのブレンドはうまくできなくなるけど)

		[Header("Volumetric Fog")]
		[SerializeField] ColorParameter m_ColorLight = new ColorParameter(Color.white, true, false, true);
		[SerializeField] ColorParameter m_ColorShadow = new ColorParameter(Color.black, true, false, true);

		[SerializeField] NoInterpClampedIntParameter m_SampleCount = new NoInterpClampedIntParameter(50, 1, 128);

		[SerializeField] MinFloatParameter m_Intensity = new MinFloatParameter(0.0f, 0.0f);
		[SerializeField] MinFloatParameter m_Extinction = new MinFloatParameter(1.0f, 0.0f);
		[SerializeField] NoInterpMinFloatParameter m_Near = new NoInterpMinFloatParameter(0.0f, 0.0f);
		[SerializeField] NoInterpMinFloatParameter m_Far = new NoInterpMinFloatParameter(100.0f, 0.0f);
		[SerializeField] NoInterpClampedFloatParameter m_NoiseScale = new NoInterpClampedFloatParameter(0.1f, 0.0f, 5.0f);

		[Header("Blur")]
		[SerializeField] IntParameter m_BlurKernelSize = new ClampedIntParameter(15, 3, 31);
		[SerializeField] ClampedFloatParameter m_BlurSigma = new ClampedFloatParameter(3.0f, 0.1f, 10f);

		[Header("Fog Noise")]
		[SerializeField] BoolParameter m_EnableFogNoise = new BoolParameter(false);
		[SerializeField] NoInterpVector3Parameter m_FogNoiseDirection = new NoInterpVector3Parameter(Vector3.forward);
		[SerializeField] NoInterpFloatParameter m_FogNoiseSpeed = new NoInterpFloatParameter(1.0f);
		[SerializeField] NoInterpFloatParameter m_FogNoiseScale = new NoInterpFloatParameter(0.1f);
		[SerializeField] FloatParameter m_FogNoiseMin = new FloatParameter(0.0f);
		[SerializeField] FloatParameter m_FogNoiseMax = new FloatParameter(1.0f);

		public Color ColorLight
			=> m_ColorLight.value;
		public Color ColorShadow
			=> m_ColorShadow.value;

		public int SampleCount
			=> m_SampleCount.value;

		public float Intensity
			=> m_Intensity.value;
		public float Extinction
			=> m_Extinction.value;
		public float Near
			=> m_Near.value;
		public float Far
			=> m_Far.value;
		public float NoiseScale
			=> m_NoiseScale.value;

		public int BlurKernelSize
			=> m_BlurKernelSize.value;
		public float BlurSigma
			=> m_BlurSigma.value;

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
			&& Far > 0
			&& Near < Far
			&& ColorLight.a > 0
			&& ColorShadow.a > 0;

		public bool IsTileCompatible()
			=> false;
	}
}
