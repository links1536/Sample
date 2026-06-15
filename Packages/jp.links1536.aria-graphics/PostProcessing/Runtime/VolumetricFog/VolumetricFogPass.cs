using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing.VolumetricFog
{
	class VolumetricFogPass : ScriptableRenderPass
	{
		enum RenderPassId
		{
			Raymarching,
			Apply,
		}

		const string VolumetricFogTextureName = "_VolumetricFogTexture";
		static readonly int VolumetricFogTextureId = Shader.PropertyToID(VolumetricFogTextureName);

		readonly int m_DownScaleCount;

		public VolumetricFogPass(int downScaleCount)
		{
			profilingSampler = new ProfilingSampler(nameof(VolumetricFogPass));
			m_DownScaleCount = downScaleCount;
		}

		class RayMarchingPassData
		{
			internal Material VolumetricFogMaterial;
		}

		class GaussianBlurPassData
		{
			internal GaussianBlurResourceData.Parameter BlurParameter;
			internal GaussianBlurResourceData GaussianBlurResourceData;
			internal TextureHandle TempBuffer;
			internal TextureHandle ColorBuffer;
			internal Vector2Int TextureSize;
		}

		class ApplyPassData
		{
			internal bool Upscaling;
			internal Material VolumetricFogMaterial;
			internal TextureHandle TempBuffer;
			internal TextureHandle ColorBuffer;
		}

		TextureDesc CreateBufferDescriptor(RenderTextureDescriptor origin, string name)
		{
			var desc = new RenderTextureDescriptor(origin.width, origin.height, RenderTextureFormat.ARGBHalf, 0);
			desc.msaaSamples = 1;
			desc.bindMS = false;
			desc.memoryless = RenderTextureMemoryless.Depth | RenderTextureMemoryless.MSAA;

			var textureDesc = new TextureDesc(desc);
			textureDesc.name = name;
			textureDesc.enableRandomWrite = true;
			return textureDesc;
		}


		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			// 別のカメラやアクティブ切ったときようにキーワードをオフにする

			base.OnCameraCleanup(cmd);
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			var cameraData = frameData.Get<UniversalCameraData>();
			if (!AriaPostProcessUtils.IsPostProcessTarget(cameraData))
				return;

			var fogVolume = VolumeManager.instance.stack.GetComponent<VolumetricFogVolume>();
			if (fogVolume == null || !fogVolume.active || !fogVolume.IsActive())
				return;

			var ariaProcessResourceData = frameData.Get<AriaPostProcessResourceData>();
			var material = ariaProcessResourceData.VolumetricFogMaterial;

			CoreUtils.SetKeyword(material, "ENABLE_FOG_NOISE", fogVolume.EnableFogNoise);

			material.SetFloat("_SampleCount", fogVolume.SampleCount);
			material.SetFloat("_Intensity", fogVolume.Intensity);
			material.SetFloat("_Extinction", fogVolume.Extinction);
			material.SetVector("_NearFar", new Vector2(fogVolume.Near, fogVolume.Far));
			material.SetFloat("_NoiseScale", fogVolume.NoiseScale);
			material.SetColor("_ColorLight", fogVolume.ColorLight);
			material.SetColor("_ColorShadow", fogVolume.ColorShadow);

			material.SetVector("_FogNoiseDirection", fogVolume.FogNoiseDirection);
			material.SetFloat("_FogNoiseSpeed", fogVolume.FogNoiseSpeed);
			material.SetFloat("_FogNoiseScale", fogVolume.FogNoiseScale);
			material.SetVector("_FogNoiseMinMax", new Vector2(fogVolume.FogNoiseMin, fogVolume.FogNoiseMax));

			var renderingData = frameData.Get<UniversalRenderingData>();
			var lightData = frameData.Get<UniversalLightData>();
			var resourceData = frameData.Get<UniversalResourceData>();
			var gaussianBlurResourceData = frameData.Get<GaussianBlurResourceData>();

			ConfigureInput(ScriptableRenderPassInput.Depth);

			// レンダーターゲット作成
			var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
			var downScaleDescriptor = RenderTargetUtils.GetDownsampling(cameraTargetDescriptor, m_DownScaleCount);

			var volumetricFogDesc = CreateBufferDescriptor(downScaleDescriptor, VolumetricFogTextureName);
			volumetricFogDesc.clearBuffer = true;
			volumetricFogDesc.filterMode = FilterMode.Bilinear;
			var volumetricFogTexture = renderGraph.CreateTexture(volumetricFogDesc);

			using (var builder = renderGraph.AddRasterRenderPass<RayMarchingPassData>("RayMarching", out var passData, profilingSampler))
			{
				passData.VolumetricFogMaterial = material;

				builder.SetRenderAttachment(volumetricFogTexture, 0, AccessFlags.Write);

				builder.AllowPassCulling(false);
				builder.SetRenderFunc<RayMarchingPassData>(static (passData, context) =>
				{
					CoreUtils.DrawFullScreen(context.cmd, passData.VolumetricFogMaterial, shaderPassId: (int)RenderPassId.Raymarching);
				});
				builder.SetGlobalTextureAfterPass(volumetricFogTexture, VolumetricFogTextureId);
			}

			if (gaussianBlurResourceData != null && gaussianBlurResourceData.IsValid())
			{
				using (var builder = renderGraph.AddComputePass<GaussianBlurPassData>("Blur", out var passData, profilingSampler))
				{
					var tempDesc = volumetricFogDesc;
					tempDesc.name = "_TempVolumetricFogTexture";
					passData.TempBuffer = builder.CreateTransientTexture(tempDesc);
					passData.ColorBuffer = volumetricFogTexture;
					passData.TextureSize = new Vector2Int(tempDesc.width, tempDesc.height);

					passData.GaussianBlurResourceData = gaussianBlurResourceData;
					passData.BlurParameter = new GaussianBlurResourceData.Parameter()
					{
						KernelSize = fogVolume.BlurKernelSize,
						Sigma = fogVolume.BlurSigma,
					};

					builder.UseTexture(passData.TempBuffer, AccessFlags.ReadWrite);
					builder.UseTexture(passData.ColorBuffer, AccessFlags.ReadWrite);

					builder.SetRenderFunc<GaussianBlurPassData>((passData, context) =>
					{
						passData.GaussianBlurResourceData.Execute(context.cmd, passData.BlurParameter, passData.ColorBuffer, passData.TempBuffer, passData.TextureSize);
					});
				}
			}

			using (var builder = renderGraph.AddRasterRenderPass<ApplyPassData>("Apply", out var passData, profilingSampler))
			{
				passData.Upscaling = m_DownScaleCount > 0;
				passData.VolumetricFogMaterial = material;
				passData.ColorBuffer = resourceData.activeColorTexture;

				var tempDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
				tempDesc.name = "_TempVolumetricFogTexture";
				passData.TempBuffer = builder.CreateTransientTexture(tempDesc);

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);

				builder.UseTexture(passData.ColorBuffer, AccessFlags.ReadWrite);
				builder.UseTexture(passData.TempBuffer, AccessFlags.ReadWrite);
				builder.UseGlobalTexture(VolumetricFogTextureId, AccessFlags.Read);

				builder.SetRenderFunc<ApplyPassData>(static (passData, context) =>
				{
					var cmd = RenderGraphUtils.GetNativeCommandBuffer(context.cmd);
					CoreUtils.SetKeyword(cmd, "ENABLE_UPSAMPLING", passData.Upscaling);
					cmd.CopyTexture(passData.ColorBuffer, passData.TempBuffer);
					Blitter.BlitCameraTexture(cmd, passData.TempBuffer, passData.ColorBuffer, passData.VolumetricFogMaterial, pass: (int)RenderPassId.Apply);
				});
			}
		}
	}
}
