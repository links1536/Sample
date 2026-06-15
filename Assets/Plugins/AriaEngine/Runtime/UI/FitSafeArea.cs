using UnityEngine;

namespace Aria.UI
{
	public class FitSafeArea : MonoBehaviour
	{
		[System.Flags]
		public enum FitAxis
		{
			Horizontal = 1 << 1,
			Vertical = 1 << 2,
		}

		public enum FitSide
		{
			BothSide,
			OneSide,
		}

		RectTransform m_Transform;

		[SerializeField] FitAxis m_FitAxis = FitAxis.Horizontal | FitAxis.Vertical;
		[SerializeField] FitSide m_FitSide = FitSide.BothSide;

		void Awake()
		{
			m_Transform = GetComponent<RectTransform>();
		}

		// Update is called once per frame
		void Update()
		{
			var area = Screen.safeArea;

			var anchorMin = Vector2.zero;
			var anchorMax = Vector2.one;

			if (m_FitAxis.HasFlag(FitAxis.Horizontal))
			{
				anchorMin.x = area.position.x / Screen.width;
				anchorMax.x = (area.position.x + area.size.x) / Screen.width;
			}
			if (m_FitAxis.HasFlag(FitAxis.Vertical))
			{
				anchorMin.y = area.position.y / Screen.height;
				anchorMax.y = (area.position.y + area.size.y) / Screen.height;
			}

			if (m_FitSide == FitSide.BothSide)
			{
				// 全画面表示だけどセーフエリアは片方しかないため、中央がずれる機種が多い
				// そういった端末向けに両側をセーフエリア分ずらす
				float safeArea = Mathf.Min(1.0f - anchorMin.x, anchorMax.x);
				anchorMin.x = 1.0f - safeArea;
				anchorMax.x = safeArea;
			}

			m_Transform.anchorMin = anchorMin;
			m_Transform.anchorMax = anchorMax;
			m_Transform.sizeDelta = Vector2.zero;
		}
	}
}
