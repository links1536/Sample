using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace AriaEngine.Rendering.Grass
{
	struct GrassRenderContext
	{
		public ComputeShader ComputeLodShader;

		public Camera Camera;
		public int Layer;
		public RenderingLayerMask RenderingLayer;

		public Material Material;
		public MaterialPropertyBlock Properties;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct GrassInstanceData
	{
		public float PositionX;
		public float PositionY;
		public float PositionZ;

		public float SinY;
		public float CosY;

		public float ScaleXZ;
		public float ScaleY;

		public float AnimationTimeOffset;

		// ライトプローブからサンプリングした色をもたせるとか？
	}

	class GrassChunk
	{
		GraphicsBuffer? m_GrassInstanceDataBuffer;
		GraphicsBuffer[]? m_LodInstanceIdBuffers;
		GraphicsBuffer[]? m_IndirectDrawArgsBuffers;

		IList<Matrix4x4>? m_TransformList;
		Bounds m_WorldBounds;

		public Bounds Bounds
			=> m_WorldBounds;

		public void Setup(GrassLodSetting[] grassLodSettings, IList<GrassInstanceData> grassInstanceList)
		{
			int grassCount = grassInstanceList.Count;
			if (grassCount <= 0)
				return;

			m_GrassInstanceDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, grassCount, UnsafeUtility.SizeOf<GrassInstanceData>());
			m_GrassInstanceDataBuffer.SetData(grassInstanceList.ToArray());

			var lodLevelTarget = GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Append | GraphicsBuffer.Target.Counter;
			int lodCount = grassLodSettings.Length;
			m_LodInstanceIdBuffers = new GraphicsBuffer[lodCount];
			m_IndirectDrawArgsBuffers = new GraphicsBuffer[lodCount];
			for (int i = 0; i < lodCount; i++)
			{
				m_LodInstanceIdBuffers[i] = new GraphicsBuffer(lodLevelTarget, grassCount, sizeof(uint));
				m_IndirectDrawArgsBuffers[i] = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

				var lodSetting = grassLodSettings[i];
				if (lodSetting == null || !lodSetting.IsValid())
					continue;
				var submesh = lodSetting.Mesh.GetSubMesh(0);
				var indirectDrawArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[]
				{
					new GraphicsBuffer.IndirectDrawIndexedArgs()
					{
						// メッシュ情報
						indexCountPerInstance = (uint)submesh.indexCount,
						baseVertexIndex = (uint)submesh.baseVertex,
						startIndex = (uint)submesh.indexStart,

						// 描画データ
						startInstance = 0,
						instanceCount = 0,
					}
				};
				m_IndirectDrawArgsBuffers[i].SetData(indirectDrawArgs);
			}

			ComputeWorldBounds(grassLodSettings, grassInstanceList);
		}

		void ComputeWorldBounds(GrassLodSetting[] grassLodSettings, IList<GrassInstanceData> grassDataList)
		{
			int grassCount = grassDataList.Count;
			m_TransformList = new List<Matrix4x4>(grassCount);

			var lod0 = grassLodSettings[0];
			var localBounds = lod0.Mesh.bounds;

			bool first = true;
			var min = Vector3.zero;
			var max = Vector3.zero;

			for (int i = 0; i < grassDataList.Count; i++)
			{
				GrassInstanceData grassData = grassDataList[i];

				float eulerY = Mathf.Atan2(grassData.SinY, grassData.CosY) * Mathf.Rad2Deg;
				if (eulerY < 0f)
					eulerY += 360f;

				var matrix = Matrix4x4.TRS(
					new Vector3(grassData.PositionX, grassData.PositionY, grassData.PositionZ),
					Quaternion.Euler(0, eulerY, 0),
					new Vector3(grassData.ScaleXZ, grassData.ScaleY, grassData.ScaleXZ)
				);
				m_TransformList.Add(matrix);

				var grassMin = matrix.MultiplyPoint(localBounds.min);
				var grassMax = matrix.MultiplyPoint(localBounds.max);

				if (first)
				{
					min = grassMin;
					max = grassMax;
					first = false;
				}
				else
				{
					min = Vector3.Min(min, grassMin);
					max = Vector3.Max(max, grassMax);
				}
			}

			m_WorldBounds = new Bounds();
			m_WorldBounds.SetMinMax(
				Vector3.Min(min, max),
				Vector3.Max(min, max)
			);
		}

		public void Render(EntityId entityId, ref GrassRenderContext renderContext, GrassLodSetting[] grassLodSettings)
		{
			if (m_GrassInstanceDataBuffer == null || m_LodInstanceIdBuffers == null || m_IndirectDrawArgsBuffers == null)
				return;

			var kernelId = renderContext.ComputeLodShader.FindKernel("CSMain");

			for (int i = 0; i < m_LodInstanceIdBuffers.Length; i++)
			{
				GraphicsBuffer? buffer = m_LodInstanceIdBuffers[i];
				if (buffer == null)
					continue;
				buffer.SetCounterValue(0);
				renderContext.ComputeLodShader.SetBuffer(kernelId, GrassShaderId._LodInstanceIdBufferIds[i], buffer);
			}

			renderContext.ComputeLodShader.SetBuffer(kernelId, GrassShaderId._GrassInstanceDataBufferId, m_GrassInstanceDataBuffer);

			// LOD用バッファを設定
			int grassCount = m_GrassInstanceDataBuffer.count;
			renderContext.ComputeLodShader.GetKernelThreadGroupSizes(kernelId, out uint groupSize, out _, out _);
			renderContext.ComputeLodShader.Dispatch(kernelId, Mathf.CeilToInt((float)grassCount / groupSize), 1, 1);

			// 描画用情報設定
			renderContext.Properties.SetBuffer(GrassShaderId._GrassInstanceDataBufferId, m_GrassInstanceDataBuffer);

			for (int lodLevel = 0; lodLevel < grassLodSettings.Length; lodLevel++)
			{
				var lodSetting = grassLodSettings[lodLevel];
				var lodInstanceIdBuffer = m_LodInstanceIdBuffers[lodLevel];
				var indirectDrawArgBuffer = m_IndirectDrawArgsBuffers[lodLevel];
				if (lodSetting == null || !lodSetting.IsValid()
				 || lodInstanceIdBuffer == null
				 || indirectDrawArgBuffer == null)
					continue;

				// IndirectDrawIndexedArgs.indexCountPerInstance を飛び越えて instanceCount に渡したい
				GraphicsBuffer.CopyCount(lodInstanceIdBuffer, indirectDrawArgBuffer, sizeof(uint));

				// 描画用情報設定
				renderContext.Properties.SetInt(GrassShaderId._CurrentLodLevelId, lodLevel);
				renderContext.Properties.SetBuffer(GrassShaderId._CurrentLodInstanceIdBufferId, lodInstanceIdBuffer);

				Graphics.RenderMeshIndirect(new RenderParams()
				{
					entityId = entityId,
					layer = renderContext.Layer,
					renderingLayerMask = renderContext.RenderingLayer,
					material = renderContext.Material,
					matProps = renderContext.Properties,

					camera = renderContext.Camera,
					worldBounds = m_WorldBounds,

					receiveShadows = true,
					shadowCastingMode = lodLevel == 0 ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off,
					motionVectorMode = MotionVectorGenerationMode.ForceNoMotion,

					// 広い範囲の複数の草が存在するため、Probeはキレイに出ない
					lightProbeUsage = LightProbeUsage.Off,
					reflectionProbeUsage = ReflectionProbeUsage.Off,
					lightProbeProxyVolume = null,

				}, lodSetting.Mesh, indirectDrawArgBuffer);
			};
		}

		public void Release()
		{
			m_TransformList = null;

			ReleaseBuffer(m_GrassInstanceDataBuffer);
			m_GrassInstanceDataBuffer = null;

			if (m_LodInstanceIdBuffers != null)
				foreach (var buffer in m_LodInstanceIdBuffers)
					ReleaseBuffer(buffer);
			m_LodInstanceIdBuffers = null;

			if (m_IndirectDrawArgsBuffers != null)
				foreach (var buffer in m_IndirectDrawArgsBuffers)
					ReleaseBuffer(buffer);
			m_IndirectDrawArgsBuffers = null;
		}

		void ReleaseBuffer(GraphicsBuffer? buffer)
		{
			if (buffer != null)
			{
				buffer.Release();
			}
		}

		public void Dispose()
			=> Release();

		public void DrawGizmos(Bounds lod0Bounds)
		{
			if (m_TransformList == null)
				return;

			var matrix = Gizmos.matrix;
			try
			{
				foreach (var transform in m_TransformList)
				{
					Gizmos.matrix = transform;
					Gizmos.DrawWireCube(lod0Bounds.center, lod0Bounds.size);
				}
			}
			finally
			{
				Gizmos.matrix = matrix;
			}
		}
	}
}
