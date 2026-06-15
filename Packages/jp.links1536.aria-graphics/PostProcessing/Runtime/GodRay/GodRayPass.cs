using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.GodRay
{
	partial class GodRayPass : ScriptableRenderPass
	{
		enum RenderPassId
		{
			Mark,
			Blur,
			Apply,
		}

		const string GodRayKeyword = "ENABLE_GODRAY";
		const string GodRayMarkTextureName = "_GodRayMarkTexture";
		const string GodRayTextureName = "_GodRayTexture";
		const string GodRayApplyTextureName = "_GodRayApplyTexture";
		static readonly int GodRayMarkTextureId = Shader.PropertyToID(GodRayMarkTextureName);
		static readonly int GodRayTextureId = Shader.PropertyToID(GodRayTextureName);

		readonly int m_DownScaleCount;

		public GodRayPass(int downScale)
		{
			profilingSampler = new ProfilingSampler(nameof(GodRayPass));
			m_DownScaleCount = downScale;
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			// 別のカメラやアクティブ切ったときようにキーワードをオフにする
			CoreUtils.SetKeyword(cmd, GodRayKeyword, false);

			base.OnCameraCleanup(cmd);
		}

		bool TryGetVolume(out GodRayVolume godRayVolume)
		{
			godRayVolume = VolumeManager.instance.stack.GetComponent<GodRayVolume>();
			return godRayVolume != null && godRayVolume.active && godRayVolume.IsActive();
		}

		void SetupMaterial(Material material, GodRayVolume godRayVolume)
		{
			CoreUtils.SetKeyword(material, "ENABLE_FOG_NOISE", godRayVolume.EnableFogNoise);

			float rcpSampleCount = 1.0f / godRayVolume.SampleCount;
			float power = rcpSampleCount * godRayVolume.MaxSampleCount;
			material.SetVector("_GodRayMarkParams", new Vector4(
				godRayVolume.Threshold,
				godRayVolume.Distance,
				godRayVolume.Intensity,
				0
			));
			material.SetVector("_GodRayBlurParams1", new Vector4(
				godRayVolume.SampleCount,
				rcpSampleCount,
				godRayVolume.NoiseScale,
				0
			));
			material.SetVector("_GodRayBlurParams2", new Vector4(
				Mathf.Pow(godRayVolume.Density, power),
				Mathf.Pow(godRayVolume.Weight, power),
				Mathf.Pow(godRayVolume.Decay, power),
				0
			));
			material.SetVector("_GodRayApplyParams", new Vector4(
				0,
				0,
				0,
				0
			));

			material.SetVector("_ColorLight", godRayVolume.ColorLight);
			material.SetVector("_ColorShadow", godRayVolume.ColorShadow);

			material.SetVector("_FogNoiseDirection", godRayVolume.FogNoiseDirection);
			material.SetFloat("_FogNoiseSpeed", godRayVolume.FogNoiseSpeed);
			material.SetFloat("_FogNoiseScale", godRayVolume.FogNoiseScale);
			material.SetVector("_FogNoiseMinMax", new Vector2(godRayVolume.FogNoiseMin, godRayVolume.FogNoiseMax));
		}
	}
}
