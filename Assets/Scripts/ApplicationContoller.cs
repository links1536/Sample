using UnityEngine;

namespace Links
{
	public class ApplicationController : MonoBehaviour
	{
		public void Exit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}
