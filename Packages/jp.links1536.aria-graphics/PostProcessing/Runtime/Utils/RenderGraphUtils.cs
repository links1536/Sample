using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PostProcessing
{
	public static class RenderGraphUtils
	{
		enum RenderingSteps
		{
			DepthCopy,
		}

		class CopyDepthPassData
		{
			public TextureHandle SourceDepth;
			public TextureHandle DestinationDepth;
			public Vector2 Scaling;
		}

		static FieldInfo m_WrappedCommandBuffer;
		static FieldInfo m_FrameData;

		static RenderGraphUtils()
		{
			var flags = BindingFlags.Instance | BindingFlags.NonPublic;
			m_WrappedCommandBuffer = typeof(BaseCommandBuffer).GetField("m_WrappedCommandBuffer", flags);
			m_FrameData = typeof(ScriptableRenderer).GetField("m_frameData", flags);
		}

		internal static CommandBuffer GetNativeCommandBuffer(BaseCommandBuffer cmd)
			=> m_WrappedCommandBuffer.GetValue(cmd) as CommandBuffer;

		public static void BlitDepth(RasterCommandBuffer cmd, RenderTexture sourceDepth, Vector4 scaleBias, float mipLevel)
		{
			var nativeCmd = GetNativeCommandBuffer(cmd);
			Blitter.BlitDepth(nativeCmd, sourceDepth, scaleBias, mipLevel);
		}


		public static void CopyTexture(RasterCommandBuffer cmd, TextureHandle source, TextureHandle destination, bool isBlitTexture)
			=> CopyTexture(GetNativeCommandBuffer(cmd), source, destination, isBlitTexture);

		public static void CopyTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, bool isBlitTexture)
		{
			// SceneView上だとCopyTextureでは動かない
			// 他に動作しないパターンがあるので
			if (isBlitTexture)
			{
				Blitter.BlitCameraTexture(cmd,
					source: source, destination: destination,
					loadAction: RenderBufferLoadAction.DontCare, storeAction: RenderBufferStoreAction.Store,
					material: Blitter.GetBlitMaterial(TextureDimension.Tex2D), pass: 0
				);
			}
			else
			{
				cmd.CopyTexture(source, destination);
			}
		}

		static ContextContainer GetContextContainer(ScriptableRenderer renderer)
			=> m_FrameData.GetValue(renderer) as ContextContainer;

		public static T GetOrCreateFrameData<T>(ScriptableRenderer renderer)
			where T : ContextItem, new()
		{
			var frameData = GetContextContainer(renderer);
			if (frameData == null)
				return default;
			return frameData.GetOrCreate<T>();
		}

	}
}
