using UnityEngine;

namespace Aria.Common
{
	public class Immortal : MonoBehaviour
	{
		private void Awake()
		{
			DontDestroyOnLoad(this.gameObject);
		}
	}
}
