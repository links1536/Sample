using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Aria.Engine
{
	public class FadeController : MonoBehaviour
	{
		[SerializeField] Image m_Image;
		[SerializeField, ColorUsage(false)] Color m_Color;
		[SerializeField] float m_FadeTime;

		public async UniTask FadeOutAsync(CancellationToken cancellationToken)
			=> await FadeAsync(m_FadeTime, false, m_Color, cancellationToken);
		public async UniTask FadeInAsync(CancellationToken cancellationToken)
			=> await FadeAsync(m_FadeTime, true, m_Color, cancellationToken);

		public async UniTask FadeOutAsync(float time, Color color, CancellationToken cancellationToken)
			=> await FadeAsync(time, false, color, cancellationToken);
		public async UniTask FadeInAsync(float time, Color color, CancellationToken cancellationToken)
			=> await FadeAsync(time, true, color, cancellationToken);

		async UniTask FadeAsync(float time, bool fadeIn, Color color, CancellationToken cancellationToken)
		{
			Color inColor = new Color(color.r, color.g, color.b, 0);
			Color outColor = new Color(color.r, color.g, color.b, 1);

			Color start = fadeIn ? outColor : inColor;
			Color end = fadeIn ? inColor : outColor;

			m_Image.color = start;
			m_Image.enabled = true;
			float elapsed = 0;
			while(elapsed < time) {
				cancellationToken.ThrowIfCancellationRequested();
				float t = elapsed / time;
				m_Image.color = Color.Lerp(start, end, t);
				await UniTask.Yield();
				elapsed += Time.deltaTime;
			}
			cancellationToken.ThrowIfCancellationRequested();
			m_Image.color = end;

			m_Image.enabled = !fadeIn;
		}
	}
}
