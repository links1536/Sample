using System;
using System.Threading;
using Aria.Engine;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aria
{
	public enum BuiltInScene
	{
		Initialize,
		Empty,
	}

	public static class SceneController
	{
		class ActiveSceneInfo : MonoBehaviour
		{
			public string Name;
			public Scene Scene;

			public bool IsValid()
				=> Scene.isLoaded
				&& Scene.IsValid();
		}

		public static string CurrentName
			=> m_ActiveScene?.Name;
		static ActiveSceneInfo m_ActiveScene;

		public static event Action OnPreUnloadScene;
		public static event Func<CancellationToken, UniTask> OnPreUnloadSceneAsync;
		public static event Func<CancellationToken, UniTask> OnChangeSceneAsync;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Setup()
		{
			GameObject gameObject = new GameObject("ActiveSceneInfo");
			GameObject.DontDestroyOnLoad(gameObject);
			m_ActiveScene = gameObject.AddComponent<ActiveSceneInfo>();

			var scene = SceneManager.GetActiveScene();
			if (scene.buildIndex != (int)BuiltInScene.Initialize)
			{
				SceneManager.LoadScene((int)BuiltInScene.Initialize, LoadSceneMode.Single);
				scene = SceneManager.GetActiveScene();
			}
			m_ActiveScene.Scene = scene;
			m_ActiveScene.Name = scene.name;
		}

		public static void ChangeScene(BuiltInScene builtInScene)
			=> ChangeSceneAsync(builtInScene).Forget();

		public static async UniTask ChangeSceneAsync(BuiltInScene builtInScene)
		{
			// 一度空のシーンを挟んでリソース解放を行う
			await LoadEmptySceneAsync();

			// 新しいシーンを読み込む
			await LoadSceneAsyncCore(builtInScene);

			// シーン切り替え処理
			if (OnChangeSceneAsync != null)
				await OnChangeSceneAsync.Invoke(default);
		}

		public static void ChangeScene(string sceneName)
			=> ChangeSceneAsync(sceneName).Forget();

		public static async UniTask ChangeSceneAsync(string sceneName)
		{
			// 一度空のシーンを挟んでリソース解放を行う
			await LoadEmptySceneAsync();

			// 新しいシーンを読み込む
			await LoadSceneAsyncCore(sceneName);

			// シーン切り替え処理
			if (OnChangeSceneAsync != null)
				await OnChangeSceneAsync.Invoke(default);
		}

		static async UniTask LoadSceneAsyncCore(BuiltInScene builtInScene)
		{
			Debug.Log($"Load Scene: {builtInScene}");
			await SceneManager.LoadSceneAsync((int)builtInScene, LoadSceneMode.Additive).ToUniTask();
			var scene = SceneManager.GetSceneByBuildIndex((int)builtInScene);
			if (SceneManager.SetActiveScene(scene))
			{
				m_ActiveScene.Scene = scene;
				m_ActiveScene.Name = scene.name;
			}
		}

		static async UniTask LoadSceneAsyncCore(string sceneName)
		{
			if (!AriaAssetProvider.TryGetInstance(out var assetProvider))
				return;
			Debug.Log($"Load Scene: {sceneName}");
			await assetProvider.LoadSceneAsync(sceneName, LoadSceneMode.Single, default);
			var scene = SceneManager.GetActiveScene();
			if (!scene.IsValid())
			{
				Debug.LogError($"Load Error Scene: {sceneName}");
				return;
			}
			m_ActiveScene.Scene = scene;
			m_ActiveScene.Name = scene.name;
		}

		static async UniTask LoadEmptySceneAsync()
		{
			// 一度空のシーンを挟んでリソース解放を行う
			await SceneManager.LoadSceneAsync((int)BuiltInScene.Empty).ToUniTask();

			if (OnPreUnloadScene != null)
				OnPreUnloadScene?.Invoke();
			if (OnPreUnloadSceneAsync != null)
				await OnPreUnloadSceneAsync.Invoke(default);

			// メモリ解放
			await Resources.UnloadUnusedAssets().ToUniTask();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

		}
	}
}
