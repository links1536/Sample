using Unity.Burst;
using Unity.Mathematics;

namespace Aria.Rendering.Universal.PlanarReflection
{
	[BurstCompile]
	static class MirrorCalculator
	{
		[BurstCompile]
		public static void CameraSpacePlane(in float4x4 viewMatrix, in float3 position, in float3 normal, in float sideSign, in float clipPlaneOffset, out float4 clipSpacePlane)
		{
			float3 offsetPos = position + normal * clipPlaneOffset;
			float3 cpos = math.transform(viewMatrix, offsetPos);
			float3 cnormal = math.normalize(math.mul((float3x3)viewMatrix, normal)) * sideSign;
			clipSpacePlane = new float4(cnormal.x, cnormal.y, cnormal.z, -math.dot(cpos, cnormal));
		}

		[BurstCompile]
		public static void ReflectionMatrix(in float3 position, in float3 normal, in float clipPlaneOffset, out float4x4 matrix)
		{
			float d = -math.dot(normal, position) - clipPlaneOffset;
			float4 plane = math.float4(normal.x, normal.y, normal.z, d);

			matrix = math.float4x4(
				(1.0f - 2.0f * plane.x * plane.x),
				(-2.0f * plane.x * plane.y),
				(-2.0f * plane.x * plane.z),
				(-2.0f * plane.w * plane.x),

				(-2.0f * plane.y * plane.x),
				(1.0f - 2.0f * plane.y * plane.y),
				(-2.0f * plane.y * plane.z),
				(-2.0f * plane.w * plane.y),

				(-2.0f * plane.z * plane.x),
				(-2.0f * plane.z * plane.y),
				(1.0f - 2.0f * plane.z * plane.z),
				(-2.0f * plane.w * plane.z),

				0.0f,
				0.0f,
				0.0f,
				1.0f
			);
		}
	}
}
