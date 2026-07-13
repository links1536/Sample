using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AriaEngine.Rendering.Grass.Job
{
	struct CreatePlacementRaycastJob : IJob
	{
		const uint JobSeed = 1960893023;

		public uint RandomSeed;

		public float3 Center;
		public float3 Size;

		public int2 Grid;

		public int LayerMask;

		[WriteOnly] public NativeArray<RaycastCommand> RaycastCommands;

		public void Execute()
		{
			int length = Grid.x * Grid.y;
			for (int i = 0; i < length; i++)
			{
				var random = Unity.Mathematics.Random.CreateFromIndex((uint)i ^ RandomSeed ^ JobSeed);

				var halfSize = Size / 2;
				var min = Center - halfSize;
				var max = Center + halfSize;
				min.y = Center.y + halfSize.y;
				max.y = Center.y + halfSize.y;

				int x = i % Grid.x;
				int z = i / Grid.x;

				// レイを上から打ち下ろす
				var position = new Vector3(
					Mathf.Lerp(min.x, max.x, Mathf.InverseLerp(0, Grid.x - 1, x)),
					max.y,
					Mathf.Lerp(min.z, max.z, Mathf.InverseLerp(0, Grid.y - 1, z))
				);
				RaycastCommands[i] = new RaycastCommand(position, Vector3.down, new QueryParameters(layerMask: LayerMask), distance: Size.y);
			}
		}
	}

	struct CreateGrassInstanceJob : IJob
	{
		const uint JobSeed = 501227372;

		// シェーダー内で扱うアニメーション用オフセット
		// パラメーターにすると必要以上にパラメーターが複雑になりそうなので、とりあえず定数にする
		const float TimeOffsetMax = 0.5f;

		public uint RandomSeed;

		public GrassScale ScaleMin;
		public GrassScale ScaleMax;
		public float ThinningOut;

		[ReadOnly] public NativeArray<RaycastHit> RaycastHits;
		[WriteOnly] public NativeList<GrassInstanceData> GrassInstanceList;

		public void Execute()
		{
			int length = RaycastHits.Length;

			for (int i = 0; i < length; i++)
			{
				var hit = RaycastHits[i];
				if (!hit.colliderEntityId.IsValid())
					continue;

				// 再現性のためにインデックスとシード値で作る
				var random = Unity.Mathematics.Random.CreateFromIndex((uint)i ^ RandomSeed ^ JobSeed);

				// 間引き
				float thinningOut = math.saturate(ThinningOut);
				if (random.NextFloat() < thinningOut)
					continue;

				var eulerAnglesY = random.NextFloat(360);
				math.sincos(math.radians(eulerAnglesY), out float sin, out float cos);

				var grassData = new GrassInstanceData()
				{
					PositionX = hit.point.x,
					PositionY = hit.point.y,
					PositionZ = hit.point.z,

					SinY = sin,
					CosY = cos,

					ScaleXZ = random.NextFloat(ScaleMin.ScaleXZ, ScaleMax.ScaleXZ),
					ScaleY = random.NextFloat(ScaleMin.ScaleY, ScaleMax.ScaleY),

					AnimationTimeOffset = random.NextFloat(TimeOffsetMax),
				};
				GrassInstanceList.Add(grassData);
			}
		}
	}
}
