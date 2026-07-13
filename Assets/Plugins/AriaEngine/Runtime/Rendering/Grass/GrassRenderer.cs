using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aria.Engine;
using AriaEngine.Rendering.Grass.Job;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace AriaEngine.Rendering.Grass
{
	[System.Serializable]
	class GrassLodSetting
	{
		public Mesh Mesh;

		public bool IsValid()
			=> Mesh != null;
	}

	[System.Serializable]
	[DebuggerDisplay("{ScaleXZ}, {ScaleY}")]
	struct GrassScale
	{
		public float ScaleXZ;
		public float ScaleY;

		public GrassScale(float scaleXZ, float scaleY)
		{
			ScaleXZ = scaleXZ;
			ScaleY = scaleY;
		}
	}

	static class GrassShaderId
	{
		public static int _FrustumPlanesId = Shader.PropertyToID("_FrustumPlanes");
		public static int _CameraPositionWSId = Shader.PropertyToID("_CameraPositionWS");

		public static int _BoundsCenterId = Shader.PropertyToID("_BoundsCenter");
		public static int _BoundsRadiusId = Shader.PropertyToID("_BoundsRadius");

		public static int _GrassInstanceDataBufferId = Shader.PropertyToID("_GrassInstanceDataBuffer");

		public static int[] _LodInstanceIdBufferIds = new int[]
		{
			Shader.PropertyToID("_Lod0InstanceIdBuffer"),
			Shader.PropertyToID("_Lod1InstanceIdBuffer"),
			Shader.PropertyToID("_Lod2InstanceIdBuffer"),
		};


		public static int _SupportLodLevelId = Shader.PropertyToID("_SupportLodLevel");
		public static int _LodDistanceId = Shader.PropertyToID("_LodDistance");
		public static int _LodFadeDistanceId = Shader.PropertyToID("_LodFadeDistance");

		public static int _CurrentLodLevelId = Shader.PropertyToID("_CurrentLodLevel");
		public static int _CurrentLodInstanceIdBufferId = Shader.PropertyToID("_CurrentLodInstanceIdBuffer");

	}

	[ExecuteAlways]
	public class GrassRenderer : MonoBehaviour
	{
		static ComputeShader? m_ComputeShader;

		[RuntimeInitializeOnLoadMethod]
		static void SetupShader()
		{
			if (m_ComputeShader != null)
				return;
			m_ComputeShader = Resources.Load<ComputeShader>("Grass/ComputeGrassLodShader");
		}

		const int SupportLodLevelMax = 3;

		static Vector4[] m_PlaneBuffers = new Vector4[CullingUtility.PlaneCount];

		[SerializeField] uint m_RandomSeed = 3679971065;

		[Header("Grass")]
		[SerializeField] GrassLodSetting[] m_GrassLodSettings;
		[SerializeField] Material m_Material;
		[SerializeField] float m_RenderDistance = 30;
		[SerializeField] Vector3 m_LodDistanceRatios = new Vector3(0.25f, 0.5f, 1.0f);
		[SerializeField] float m_LodFadeDistance = 5;
		[SerializeField] GrassScale m_ScaleMin = new GrassScale(0.8f, 0.8f);
		[SerializeField] GrassScale m_ScaleMax = new GrassScale(1.0f, 1.0f);
		[SerializeField] RenderingLayerMask m_RenderingLayerMask = 1;

		[Header("Placement")]
		[SerializeField] LayerMask m_RaycastLayerMask = -1;
		[SerializeField] Vector3 m_AreaSize = new Vector3(10, 10, 10);
		[SerializeField, Range(0, 2)] float m_Density = 1;
		[SerializeField, Range(0, 1)] float m_ThinningOut = 0;

		[Header("Debug")]
		[SerializeField] bool m_DrawGizmos = true;
		[SerializeField] bool m_DrawGizmosSelectOnly = true;
		[SerializeField] bool m_DrawInstanceGizmos = true;

		List<GrassChunk> m_ChunkList;
		List<GrassChunk> m_VisibleChunkList;

		public void Setup()
		{
			if (m_ChunkList != null && m_ChunkList.Count > 0)
				return;

			// Listがnullだと都合が悪いので先にリストを作る
			m_ChunkList ??= new List<GrassChunk>();
			m_VisibleChunkList ??= new List<GrassChunk>();

			// 描画効率やGraphisBufferのサイズには上限の問題があるため、インスタンス数に応じてチャンクを自動分割する
			int instanceMaxPerChunk = (int)(SystemInfo.maxGraphicsBufferSize / GrassInstanceData.Size);
			instanceMaxPerChunk = Mathf.Min(GrassChunk.InstanceMax, instanceMaxPerChunk);
			int chunkInstanceSqrt = (int)Mathf.Sqrt(instanceMaxPerChunk);

			var areaSize = (float3)m_AreaSize;
			var grid = math.int2(
				Mathf.CeilToInt(m_AreaSize.x * m_Density),
				Mathf.CeilToInt(m_AreaSize.z * m_Density)
			);

			if (grid.x <= 0 || grid.y <= 0)
				return;

			var chunkCount = (int2)math.ceil((float2)grid / chunkInstanceSqrt);
			var chunkGrid = (int2)math.floor((float2)grid / chunkCount);
			var chunkAreaSize = math.float3(
				areaSize.x / chunkCount.x,
				areaSize.y,
				areaSize.z / chunkCount.y
			);

			int grassInstanceCount = chunkGrid.x * chunkGrid.y;

			float3 areaOrigin = (float3)transform.position - math.float3(m_AreaSize.x * 0.5f, 0f, m_AreaSize.z * 0.5f);
			for (int x = 0; x < chunkCount.x; x++)
			{
				for (int y = 0; y < chunkCount.y; y++)
				{
					var chunkPosition = areaOrigin + math.float3(
						(x * chunkAreaSize.x) + (chunkAreaSize.x * 0.5f),
						0f,
						(y * chunkAreaSize.z) + (chunkAreaSize.z * 0.5f)
					);

					m_ChunkList.Add(CreateChunk(grassInstanceCount, chunkPosition, chunkAreaSize, chunkGrid));
				}
			}
		}

		GrassChunk CreateChunk(int grassInstanceCount, float3 center, float3 areaSize, int2 placementGrid)
		{
			using var raycastCommands = new NativeArray<RaycastCommand>(grassInstanceCount, Allocator.TempJob);
			using var raycastHits = new NativeArray<RaycastHit>(grassInstanceCount, Allocator.TempJob);
			using var grassInstanceList = new NativeList<GrassInstanceData>(grassInstanceCount, Allocator.TempJob);

			var createRaycastJob = new CreatePlacementRaycastJob()
			{
				Center = center,
				Size = areaSize,
				Grid = placementGrid,
				LayerMask = m_RaycastLayerMask,
				RandomSeed = m_RandomSeed,
				RaycastCommands = raycastCommands,
			};

			var createInstanceJob = new CreateGrassInstanceJob()
			{
				RandomSeed = m_RandomSeed,

				ScaleMin = m_ScaleMin,
				ScaleMax = m_ScaleMax,
				ThinningOut = m_ThinningOut,

				RaycastHits = raycastHits,
				GrassInstanceList = grassInstanceList,
			};

			JobHandle jobHandle = default;
			jobHandle = createRaycastJob.Schedule(jobHandle);
			jobHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 10, jobHandle);
			jobHandle = createInstanceJob.Schedule(jobHandle);
			jobHandle.Complete();

			var readonlyList = grassInstanceList.AsReadOnly();

			var chunk = new GrassChunk();
			chunk.Setup(m_GrassLodSettings, new List<GrassInstanceData>(readonlyList));
			return chunk;
		}

		void Release()
		{
			if (m_ChunkList != null)
			{
				foreach (var chunk in m_ChunkList)
					chunk.Dispose();
				m_ChunkList.Clear();
			}
		}

		void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			Render(camera);
		}

		void Render(Camera camera)
		{
			SetupShader();

			if (m_ChunkList == null || m_ChunkList.Count <= 0
			 || m_VisibleChunkList == null
			 || m_ComputeShader == null
			 || m_GrassLodSettings == null || m_GrassLodSettings.Length <= 0)
				return;
			var lod0 = m_GrassLodSettings.FirstOrDefault();
			if (lod0 == null || !lod0.IsValid())
				return;

			var properties = new MaterialPropertyBlock();
			var renderContext = new GrassRenderContext()
			{
				ComputeLodShader = m_ComputeShader,

				Camera = camera,
				Layer = gameObject.layer,
				RenderingLayer = m_RenderingLayerMask,

				Material = m_Material,
				Properties = properties,
			};

			// 視錐台カリング用の情報
			var cameraPosition = camera.transform.position;
			System.Span<Plane> planes = stackalloc Plane[CullingUtility.PlaneCount];
			CullingUtility.CalculateFrustumPlanes(camera, planes);
			for (int i = 0; i < planes.Length; i++)
			{
				var plane = planes[i];
				var normal = plane.normal;
				var distance = plane.distance;
				m_PlaneBuffers[i] = new Vector4(normal.x, normal.y, normal.z, distance);
			}

			// カメラ情報を渡す
			m_ComputeShader.SetVectorArray(GrassShaderId._FrustumPlanesId, m_PlaneBuffers);
			m_ComputeShader.SetVector(GrassShaderId._CameraPositionWSId, camera.transform.position);

			// LOD情報
			int supportLodLevel = m_GrassLodSettings.Count(x => x.IsValid());
			var lodDistance = m_RenderDistance * m_LodDistanceRatios;
			m_ComputeShader.SetVector(GrassShaderId._LodDistanceId, lodDistance);
			m_ComputeShader.SetInt(GrassShaderId._SupportLodLevelId, supportLodLevel);
			m_ComputeShader.SetFloat(GrassShaderId._LodFadeDistanceId, m_LodFadeDistance);

			// SphreCull用にBoundsを丸めた値を渡す
			var lod0Bounds = lod0.Mesh.bounds;
			m_ComputeShader.SetVector(GrassShaderId._BoundsCenterId, lod0Bounds.center);
			m_ComputeShader.SetFloat(GrassShaderId._BoundsRadiusId, lod0Bounds.extents.magnitude);

			renderContext.Properties.SetVector(GrassShaderId._LodDistanceId, lodDistance);
			renderContext.Properties.SetInt(GrassShaderId._SupportLodLevelId, supportLodLevel);
			renderContext.Properties.SetFloat(GrassShaderId._LodFadeDistanceId, m_LodFadeDistance);

			m_VisibleChunkList.Clear();
			foreach (var chunk in m_ChunkList)
			{
				if (chunk == null)
					continue;
				if (CullingUtility.TryCullBounds(planes, chunk.Bounds))
					continue;
				m_VisibleChunkList.Add(chunk);
			}

			foreach (var chunk in m_VisibleChunkList)
				chunk.Render(gameObject.GetEntityId(), ref renderContext, m_GrassLodSettings);
		}

		void OnEnable()
		{
			Setup();
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		void OnDisable()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		void OnDestroy()
		{
			Release();
		}

		void OnValidate()
		{
			Release();
			Setup();
		}

		void OnDrawGizmos()
		{
			if (!m_DrawGizmos || m_DrawGizmosSelectOnly)
				return;
			DrawGizmos();
		}

		void OnDrawGizmosSelected()
		{
			if (!m_DrawGizmos || !m_DrawGizmosSelectOnly)
				return;
			DrawGizmos();
		}

		void DrawGizmos()
		{
			Gizmos.DrawWireCube(transform.position, m_AreaSize);

			var lod0 = m_GrassLodSettings?.FirstOrDefault();
			if (lod0 == null || !lod0.IsValid())
				return;
			var lod0Bounds = lod0.Mesh.bounds;
			foreach (var chunk in m_ChunkList)
			{
				if (chunk == null)
					continue;

				// チャンクの Gizmos 描画
				Gizmos.DrawWireCube(chunk.Bounds.center, chunk.Bounds.size);

				// 草ごとに Gizmos を描画
				if (m_DrawInstanceGizmos)
					chunk.DrawGizmos(lod0Bounds);
			}
		}
	}
}
