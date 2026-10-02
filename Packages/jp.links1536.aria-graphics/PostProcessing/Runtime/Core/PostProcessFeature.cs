using Aria.Rendering.Universal.PostProcessing.Bloom;
using Aria.Rendering.Universal.PostProcessing.CharacterMask;
using Aria.Rendering.Universal.PostProcessing.Distortion;
using Aria.Rendering.Universal.PostProcessing.GodRay;
using Aria.Rendering.Universal.PostProcessing.VolumetricFog;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{
	public enum PassInput
	{
		None = 0,
		DownScale = 1 << 0,
	}

	public partial class PostProcessFeature : ScriptableRendererFeature
	{
		[SerializeField] AriaPostProcessData m_PrePostProcessData;
		[SerializeField] int m_DownScaleCount;

		Material m_BlomUtilMaterial;
		Material m_GodRayMaterial;
		Material m_VolumetricFogMaterial;
		Material m_DistortionMaterial;

		PrePostProcessPass m_PrePostProcessPass;
		LatePostProcessPass m_LatePostProcessPass;

		DistortionPass m_DistortionPass;

		GodRayPass m_GodRayPass;
		VolumetricFogPass m_VolumetricFogPass;

		BloomEmissionPass m_BloomEmissionOpaquePass;
		BloomEmissionPass m_BloomEmissionTransparentPass;

		CharacterMaskPass m_CharacterMaskPass;

		/// <inheritdoc/>
		public override void Create()
		{
			// マテリアルを作成
			m_BlomUtilMaterial = CoreUtils.CreateEngineMaterial(m_PrePostProcessData.BloomUtilShader);
			m_GodRayMaterial = CoreUtils.CreateEngineMaterial(m_PrePostProcessData.GodRayShader);
			m_VolumetricFogMaterial = CoreUtils.CreateEngineMaterial(m_PrePostProcessData.VolumetricFogShader);
			m_DistortionMaterial = CoreUtils.CreateEngineMaterial(m_PrePostProcessData.DistortionShader);

			// 各ポストプロセス用パスを作成
			m_PrePostProcessPass = new PrePostProcessPass(m_DownScaleCount)
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
			};
			m_LatePostProcessPass = new LatePostProcessPass()
			{
				renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing,
			};

			// Distortion用パス
			m_DistortionPass = new DistortionPass()
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingTransparents,
			};

			// GodRay用パス
			m_GodRayPass = new GodRayPass(m_DownScaleCount)
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
			};

			// VolumetricFog用パス
			m_VolumetricFogPass = new VolumetricFogPass(m_DownScaleCount)
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
			};

			// Bloom用描画パス
			m_BloomEmissionOpaquePass = new BloomEmissionPass(RenderQueueRange.opaque, BloomEmissionPass.RenderingTiming.Opaque)
			{
				renderPassEvent = RenderPassEvent.AfterRenderingOpaques,
			};
			m_BloomEmissionTransparentPass = new BloomEmissionPass(RenderQueueRange.transparent, BloomEmissionPass.RenderingTiming.Transparent)
			{
				renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
			};

			// キャラクターマスク
			m_CharacterMaskPass = new CharacterMaskPass()
			{
				renderPassEvent = RenderPassEvent.AfterRenderingOpaques,
			};
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// マテリアルを削除
				if (m_VolumetricFogMaterial != null)
				{
					GameObject.DestroyImmediate(m_VolumetricFogMaterial);
					m_VolumetricFogMaterial = null;
				}
				if (m_GodRayMaterial != null)
				{
					GameObject.DestroyImmediate(m_GodRayMaterial);
					m_GodRayMaterial = null;
				}
				if (m_BlomUtilMaterial != null)
				{
					GameObject.DestroyImmediate(m_BlomUtilMaterial);
					m_BlomUtilMaterial = null;
				}
				if (m_DistortionMaterial != null)
				{
					GameObject.DestroyImmediate(m_DistortionMaterial);
					m_DistortionMaterial = null;
				}
			}
			base.Dispose(disposing);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			var gaussianBlurResourceData = RenderGraphUtils.GetOrCreateFrameData<GaussianBlurResourceData>(renderer);
			gaussianBlurResourceData.Setup(m_PrePostProcessData.GaussianBlurShader);

			// マテリアルを各処理で使用できるように登録する
			var postProccesResourceData = RenderGraphUtils.GetOrCreateFrameData<AriaPostProcessResourceData>(renderer);
			postProccesResourceData.BloomUtilMaterial = m_BlomUtilMaterial;
			postProccesResourceData.GodRayMaterial = m_GodRayMaterial;
			postProccesResourceData.VolumetricFogMaterial = m_VolumetricFogMaterial;
			postProccesResourceData.DistortionMaterial = m_DistortionMaterial;

			renderer.EnqueuePass(m_PrePostProcessPass);
			renderer.EnqueuePass(m_LatePostProcessPass);

			//renderer.EnqueuePass(m_DistortionPass);
			renderer.EnqueuePass(m_GodRayPass);
			renderer.EnqueuePass(m_VolumetricFogPass);

			renderer.EnqueuePass(m_BloomEmissionOpaquePass);
			renderer.EnqueuePass(m_BloomEmissionTransparentPass);

			renderer.EnqueuePass(m_CharacterMaskPass);
		}
	}
}
