using System;
using UnityEngine;

namespace Aria.Engine
{
	public static class CullingUtility
	{
		public const int PlaneCount = 6;

		public static void CalculateFrustumPlanes(Camera camera, Plane[] planes)
		{
			var view = camera.worldToCameraMatrix;
			var projection = camera.projectionMatrix;
			var viewProjection = projection * view;
			GeometryUtility.CalculateFrustumPlanes(viewProjection, planes);
		}

		public static void CalculateFrustumPlanes(Camera camera, Span<Plane> planes)
		{
			var view = camera.worldToCameraMatrix;
			var projection = camera.projectionMatrix;
			var viewProjection = projection * view;
			GeometryUtility.CalculateFrustumPlanes(viewProjection, planes);
		}

		public static void CalculateFrustumPlanes(Matrix4x4 viewProjection, Plane[] planes)
			=> GeometryUtility.CalculateFrustumPlanes(viewProjection, planes);

		public static void CalculateFrustumPlanes(Matrix4x4 viewProjection, Span<Plane> planes)
			=> GeometryUtility.CalculateFrustumPlanes(viewProjection, planes);


		public static bool TryCullBounds(Plane[] planes, Bounds bounds)
			=> !GeometryUtility.TestPlanesAABB(planes, bounds);

		public static bool TryCullBounds(Span<Plane> planes, Bounds bounds)
			=> !GeometryUtility.TestPlanesAABB(planes, bounds);
	}
}
