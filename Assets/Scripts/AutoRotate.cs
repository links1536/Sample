using UnityEngine;

namespace Links
{
	public class AutoRotate : MonoBehaviour
	{
		[SerializeField] float m_RotateSpeed = 360;

		// Update is called once per frame
		void Update()
		{
			transform.Rotate(Vector3.up, m_RotateSpeed * Time.deltaTime);
		}
	}
}

