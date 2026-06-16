using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Aria.AssetManagement;
using Aria.AssetManagement.Data;
using Aria.AssetManagement.IO;
using Aria.Common;
using Aria.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZLogger;

namespace Aria.Engine
{
	public class AriaAssetProvider : Singleton<AriaAssetProvider>
	{
		class PreloadData
		{
			public HashSet<string> Assets;
			public Dictionary<string, string> AssetToBundleMap;
			public Dictionary<string, string> GuidToAssetMap;
		}

		[SerializeField] bool LocalLoadMode;

		PreloadData? m_InAppData;
		PreloadData? m_BundleData;

		public async UniTask SetupInAppAsync(DownloadingEvent downloadingEvent = default, CancellationToken cancellationToken = default)
		{
			if (!InAppResourceProvider.TryGetInstance(out var inAppResourceProvider))
				return;
			bool success = await inAppResourceProvider.SetupAsync(downloadingEvent, cancellationToken);
			if (!success)
				m_Logger.ZLogError($"Setup InApp Failed");
			else
				m_Logger.ZLogTrace($"Setup InApp Successed");
		}
		public async UniTask<bool> PreloadInAppAsync(CancellationToken cancellationToken)
		{
			if (!InAppResourceProvider.TryGetInstance(out var inAppResourceProvider))
				return false;
			m_InAppData = await PreloadAsync(inAppResourceProvider, cancellationToken);
			if(m_InAppData == null)
			{
				m_Logger.ZLogError($"Preload InApp Failed");
				return false;
			}
			m_Logger.ZLogTrace($"Preload InApp Successed");
			return m_InAppData != null;
		}

		public async UniTask SetupBundleAsync(AssetBundleStorage storage, DownloadingEvent downloadingEvent = default, CancellationToken cancellationToken = default)
		{
			if (!AssetBundleProvider.TryGetInstance(out var bundleProvider))
				return;
			await bundleProvider.SetupAsync(storage, downloadingEvent, cancellationToken);
			m_Logger.ZLogTrace($"Setup Bundle Successed");
		}
		public async UniTask<bool> PreloadBundleAsync(CancellationToken cancellationToken)
		{
			if (!AssetBundleProvider.TryGetInstance(out var bundleProvider))
				return false;
			m_BundleData = await PreloadAsync(bundleProvider, cancellationToken);
			m_Logger.ZLogTrace($"[{nameof(AriaAssetProvider)}] Preload Bundle Successed");
			return m_BundleData != null;
		}

		IReadOnlyList<string> GetDownloadable(AssetBundleProvider bundleProvider)
			=> bundleProvider.GetDownloadableBundleNames();

		public bool IsDownloadNecessarry()
		{
			if (!AssetBundleProvider.TryGetInstance(out var bundleProvider)) {
				m_Logger.ZLogError($"Missing {nameof(AssetBundleProvider)}");
				return false;
			}
			return GetDownloadable(bundleProvider).Count > 0;
		}

		public async UniTask<bool> ConfirmDownloadAllAsync(CancellationToken cancellationToken)
		{
			if (!AssetBundleProvider.TryGetInstance(out var bundleProvider)) {
				m_Logger.ZLogError($"Missing {nameof(AssetBundleProvider)}");
				return false;
			}

			//var downloadable = GetDownloadable(bundleProvider);
			//if (downloadable.Count > 0) {
			//	// ダウンロード確認ダイアログ？
			//	bool isClose = false;
			//	bool acceptDownload = false;
			//	var totalSize = bundleProvider.GetTotalSize(downloadable);
			//	var dialogRequest = new DialogManager.DialogRequest()
			//	{
			//		Title = "Information",
			//		Text = $"ファイルをダウンロードします({StorageUtils.HumanReadable(totalSize, 2)})",
			//		ButtonFlag = DialogButtonFlag.Both,
			//		ButtonText = DialogButtonText.OkCancel,
			//		OnPositive = () => acceptDownload = true,
			//		OnClose = () => isClose = true,
			//	};
			//	if (!DialogManager.TryGetInstance(out var dialogManager)) {
			//		m_Logger.ZLogError($"Missing {nameof(DialogManager)}");
			//		return false;
			//	}
			//	dialogManager.OpenDialog(dialogRequest);
			//	while (!isClose)
			//		await UniTask.NextFrame(cancellationToken);
			//	if (!acceptDownload) {
			//		m_Logger.ZLogError($"ユーザーによってダウンロードが拒否されました");
			//		return false;
			//	}
			//}
			return true;
		}

		public async UniTask<bool> DownloadAllAsync(BulkDownloadingEvent bulkDownloadingEvent, DownloadingEvent downloadingEvent, CancellationToken cancellationToken)
		{
			using var timer = new StopwatchArea(m_Logger, $"{nameof(AriaAssetProvider)}.{nameof(DownloadAllAsync)}");
			if (!AssetBundleProvider.TryGetInstance(out var bundleProvider)) {
				m_Logger.ZLogError($"Missing {nameof(AssetBundleProvider)}");
				return false;
			}
			var downloadable = GetDownloadable(bundleProvider);
			if (downloadable.Count > 0) {
				using var bulkDownloadArea = new StopwatchArea(m_Logger, $"{nameof(AssetBundleProvider)}.{nameof(AssetBundleProvider.BulkDownloadAsync)}");
				bool downloadSuccess = await bundleProvider.BulkDownloadAsync(downloadable, DownloadPriority.Normal, bulkDownloadingEvent, downloadingEvent, cancellationToken);
				if (!downloadSuccess) {
					m_Logger.ZLogError($"ダウンロードが失敗しました");
					return false;
				}
			}
			return true;
		}

		async UniTask<PreloadData?> PreloadAsync(IAssetBundleProvider bundleProvider, CancellationToken cancellationToken)
		{
			using var timer = new StopwatchArea(m_Logger, $"{nameof(AriaAssetProvider)}.{nameof(PreloadAsync)}");
			await UniTask.SwitchToMainThread();

			var assets = new HashSet<string>();
			var assetToBundleMap = new Dictionary<string, string>();
			var guidToAssetMap = new Dictionary<string, string>();
#if UNITY_EDITOR
			if (LocalLoadMode) {
				string resourceDirectory = bundleProvider.AssetPathPrefix;
				if (Directory.Exists(resourceDirectory)) {
					var files = Directory.EnumerateFiles(resourceDirectory, "*.meta", SearchOption.AllDirectories);
					foreach (var file in files) {
						string filePath = file;
						filePath = filePath.Replace(@"\", @"/");
						filePath = UnityEditor.AssetDatabase.GetAssetPathFromTextMetaFilePath(filePath);
						if (!File.Exists(filePath))
							continue;
						string guid = UnityEditor.AssetDatabase.AssetPathToGUID(filePath);
						filePath = filePath.Substring(resourceDirectory.Length);

						assets.Add(filePath);
						guidToAssetMap[guid] = filePath;
					}
				}
				return new PreloadData()
				{
					Assets = assets,
					AssetToBundleMap = assetToBundleMap,
					GuidToAssetMap = guidToAssetMap,
				};
			}
#endif

			var allBundleNames = bundleProvider.AllBundleNames ?? System.Array.Empty<string>();
			var bundleNameList = new List<string>(allBundleNames.Count());
			var bundleList = default(AssetBundle[]);
			using (new StopwatchArea(m_Logger, $"Load Asset Bundle")) {
				var bundleLoadList = new List<UniTask<AssetBundle>>(allBundleNames.Count());
				foreach (var bundleName in allBundleNames) {
					var loadTask = bundleProvider.LoadAsync(bundleName, DownloadPriority.Normal, cancellationToken);
					bundleNameList.Add(bundleName);
					bundleLoadList.Add(loadTask);
				}

				// 読み込みを待つ
				bundleList = await UniTask.WhenAll(bundleLoadList);
			}

			//　GUIDからアセットパスに置き換えるやつ
			using (new StopwatchArea(m_Logger, $"Create AssetMaps")) {
				var assetPathPrefix = bundleProvider.AssetPathPrefix;

				for (int i = 0, count = bundleList.Length; i < count; i++) {
					var bundleName = bundleNameList[i];
					var bundle = bundleList[i];
					if (bundle == null) {
						m_Logger.ZLogError($"{bundleName}の読み込みに失敗しました");
						return null;
					}

					// シーンを含むなら対象外
					if (bundle.isStreamedSceneAssetBundle)
					{
						var scenePaths = bundle.GetAllScenePaths();
						foreach (var asset in scenePaths)
						{
							var resourceDirectory = bundleProvider.AssetPathPrefix;
							var scenePath = asset.Replace(resourceDirectory, string.Empty);
							assets.Add(scenePath);
							assetToBundleMap[scenePath] = bundleName;
						}
						continue;
					}

					var textAsset = (await bundle.LoadAssetAsync<TextAsset>($"{assetPathPrefix}{bundleName}/{AssetCatalog.Name}")) as TextAsset;
					if (textAsset == null)
						continue;

					string json = textAsset.text;
					var assetCatalog = JsonUtility.FromJson<AssetCatalog>(json);
					foreach (var asset in assetCatalog.Assets)
					{
						assets.Add(asset.Path);
						assetToBundleMap[asset.Path] = bundleName;
						guidToAssetMap[asset.Guid] = asset.Path;
					}
				}
			}

			using (new StopwatchArea(m_Logger, $"Unload Asset Bundle"))
			{
				await bundleProvider.UnloadAllAsync(true);
			}

			m_Logger.ZLogDebug($"[{nameof(AriaAssetProvider)}] Preload Successed");
			return new PreloadData()
			{
				Assets = assets,
				AssetToBundleMap = assetToBundleMap,
				GuidToAssetMap = guidToAssetMap,
			};
		}

		public async UniTask UnloadAllAsync()
		{
			if (!AssetBundleProvider.TryGetInstance(out var bundleProvider))
				return;
			var unloadTaskList = new List<UniTask>();
			var allBundleNames = bundleProvider.AllBundleNames;
			foreach (var bundleName in allBundleNames) {
				var task = bundleProvider.UnloadAsync(bundleName, true);
				unloadTaskList.Add(task);
			}
			await UniTask.WhenAll(unloadTaskList);
		}

		public async UniTask<T?> LoadInAppAsync<T>(string assetName, CancellationToken cancellationToken)
			where T : UnityEngine.Object
			=> InAppResourceProvider.TryGetInstance(out var bundleProvider)
			? await LoadAssetAsyncInternal<T>(bundleProvider, m_InAppData, assetName, cancellationToken)
			: default;

		public async UniTask<T?> LoadBundleAsync<T>(string assetName, CancellationToken cancellationToken)
			where T : UnityEngine.Object
			=> AssetBundleProvider.TryGetInstance(out var bundleProvider)
			? await LoadAssetAsyncInternal<T>(bundleProvider, m_BundleData, assetName, cancellationToken)
			: default;

		async UniTask<T?> LoadAssetAsyncInternal<T>(IAssetBundleProvider bundleProvider, PreloadData? preloadData, string assetName, CancellationToken cancellationToken)
			where T : UnityEngine.Object
		{
			if (bundleProvider == null)
				return null;
			string assetPath = bundleProvider.AssetPathPrefix + assetName;
#if UNITY_EDITOR
			if (LocalLoadMode) {
				return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
			}
#endif
			if (preloadData == null || !preloadData.AssetToBundleMap.TryGetValue(assetName, out var bundleName)) {
				m_Logger.ZLogError($"{assetPath}に一致するAssetBundleが見つかりませんでした");
				return default;
			}
			var bundle = await bundleProvider.LoadAsync(bundleName, DownloadPriority.Normal, cancellationToken: cancellationToken);
			if (bundle == null)
				return default;
			var asset = await bundle.LoadAssetAsync<T>(assetPath).ToUniTask(cancellationToken: cancellationToken);
			return asset as T;
		}

		public async UniTask<T?> LoadAsync<T>(string assetName, CancellationToken cancellationToken)
			where T : UnityEngine.Object
			=> m_BundleData != null && m_BundleData.Assets.Contains(assetName)
			? await LoadBundleAsync<T>(assetName, cancellationToken)
			: await LoadInAppAsync<T>(assetName, cancellationToken);

		public async UniTask<T?> LoadByGuidAsync<T>(string guid, CancellationToken cancellationToken)
			where T : UnityEngine.Object
			=> m_BundleData != null && m_BundleData.GuidToAssetMap.TryGetValue(guid, out var assetPath)
			? await LoadBundleAsync<T>(assetPath, cancellationToken)
			: m_InAppData.GuidToAssetMap != null && m_InAppData.GuidToAssetMap.TryGetValue(guid, out assetPath)
			? await LoadInAppAsync<T>(assetPath, cancellationToken)
			: null;

		public IEnumerable<string?>? EnumerateBundleFiles(string path, string searchPattern)
		{
			if (!AriaResourceSettings.TryGetInstance(out var ariaResourceSettings))
				return null;
			var assetPathPrefix = ariaResourceSettings.AssetBundlePath;

			string assetPath = assetPathPrefix + path;
#if UNITY_EDITOR
			if (LocalLoadMode) {
				var files = Directory.EnumerateFiles(assetPath, searchPattern, SearchOption.AllDirectories).ToArray();
				for (int i = 0; i < files.Length; i++) {
					files[i] = files[i].Substring(assetPathPrefix.Length).Replace(@"\", "/");
				}
				return files;
			}
#endif
			int index = path.IndexOf('/');
			var root = path.Substring(0, index);
			var fileList = new List<string>();
			var regex = new System.Text.RegularExpressions.Regex(searchPattern.Replace("*", ".+"));
			foreach (var pair in m_BundleData.AssetToBundleMap) {
				if (!pair.Key.StartsWith(assetPath))
					continue;
				string fileName = Path.GetFileName(pair.Key);
				bool isMatch = regex.IsMatch(fileName);
				if (isMatch) {
					fileList.Add(pair.Key.Substring(assetPathPrefix.Length));
				}
			}
			return fileList;
		}

		public async UniTask LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode, CancellationToken cancellationToken)
		{
			sceneName += ".unity";

#if UNITY_EDITOR
			if (LocalLoadMode)
			{
				if (AriaResourceSettings.TryGetInstance(out var ariaResourceSettings))
				{
					var assetPathPrefix = ariaResourceSettings.AssetBundlePath;
					string localPath = assetPathPrefix + sceneName;
					if (!UnityEditor.AssetDatabase.AssetPathExists(localPath))
					{
						assetPathPrefix = ariaResourceSettings.InAppResourcePath;
						localPath = assetPathPrefix + sceneName;
					}
					await UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
						localPath,
						new UnityEngine.SceneManagement.LoadSceneParameters { loadSceneMode = mode }
					).ToUniTask(cancellationToken: cancellationToken);
					return;
				}
			}
#endif
			IAssetBundleProvider bundleProvider = null;
			PreloadData? preloadData = null;
			bool isAssetBundle = m_BundleData != null && m_BundleData.Assets.Contains(sceneName);
			if (isAssetBundle)
			{
				AssetBundleProvider.TryGetInstance(out var _bundleProvider);
				bundleProvider = _bundleProvider;
				preloadData = m_BundleData;
			}
			else
			{
				InAppResourceProvider.TryGetInstance(out var _bundleProvider);
				bundleProvider = _bundleProvider;
				preloadData = m_InAppData;
			}

			if (bundleProvider == null || preloadData == null)
			{
				return;
			}

			if (!preloadData.AssetToBundleMap.TryGetValue(sceneName, out var bundleName))
			{
				return;
			}

			await bundleProvider.LoadAsync(bundleName, DownloadPriority.High, cancellationToken);

			string loadSceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
			//string loadSceneName = sceneName;
			await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(loadSceneName, mode).ToUniTask(cancellationToken: cancellationToken);
		}
	}
}
