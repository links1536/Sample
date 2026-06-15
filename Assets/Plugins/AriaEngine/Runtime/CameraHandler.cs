using Aria.Common;
using UnityEngine;

namespace Aria.Engine
{
	public class CameraHandler : Singleton<CameraHandler>
	{
		[SerializeField] Camera m_MainCamera;
		[SerializeField] Camera m_UICamera;

		public Camera MainCamera
			=> m_MainCamera;
		public Camera UICamera
			=> m_UICamera;
	}
}
