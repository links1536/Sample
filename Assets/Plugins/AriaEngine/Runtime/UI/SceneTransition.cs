using System.Threading;
using Aria.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aria.Engine
{
	public class SceneTransition : Singleton<SceneTransition>
	{
		[SerializeField] FadeController m_FadeController;

		public async UniTask FadeOutAsync(CancellationToken cancellationToken)
			=> await m_FadeController.FadeOutAsync(cancellationToken);
		public async UniTask FadeInAsync(CancellationToken cancellationToken)
			=> await m_FadeController.FadeInAsync(cancellationToken);
	}
}
