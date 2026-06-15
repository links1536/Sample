using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Aria.AssetManagement.Data;
using Aria.AssetManagement.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ZLogger;

namespace Aria.AssetManagement
{
	public class InAppResourceProvider : AssetBundleProviderBase<InAppResourceProvider>, IAssetBundleProvider
	{

		string m_TemporaryDirectory;
		string m_StreamingAssetsDirectory;

		/// <summary>
		/// アセットパスのプレフィックス
		/// </summary>
		public override string AssetPathPrefix
			=> AriaResourceSettings.TryGetInstance(out var ariaResourceSettings)
			? ariaResourceSettings.InAppResourcePath
			: string.Empty;

		public async UniTask<bool> SetupAsync(DownloadingEvent downloadingEvent, CancellationToken cancellationToken = default)
		{
			await UniTask.SwitchToMainThread();
			m_TemporaryDirectory = Application.persistentDataPath;
			m_StreamingAssetsDirectory = Application.streamingAssetsPath;

			if (!AriaResourceSettings.TryGetInstance(out var instance))
			{
				m_Logger.ZLogError($"{nameof(AriaResourceSettings)}を取得できませんでした");
				return false;
			}

			await UnloadAllAsync(true);

			// 拡張子登録
			AssetExtension = instance.Extension;

			// カタログをDLしてDictionaryにまとめ直す
			string path = GetStreamingAssetsPath(AssetBundleCatalog.CatalogPath + instance.Extension);
			m_Catalog = await AssetBundleCatalog.DownloadAsync(path, instance.CatalogPassword, instance.CatalogSalt, downloadingEvent, cancellationToken);
			if (m_Catalog == null)
			{
				m_Logger.ZLogError($"{nameof(AssetBundleCatalog)}を取得できませんでした");
				return false;
			}

			// Traceで出力
			m_Logger.ZLogTrace($"{m_Catalog}");

			// AssetBundleリスト
			m_BundleInfos = new Dictionary<string, BundleInfo>(m_Catalog.Bundles.Length);
			foreach (var bundleInfo in m_Catalog.Bundles)
				m_BundleInfos[bundleInfo.BundleName] = bundleInfo;

			// 依存ファイルをまとめる
			m_Dependencies = new Dictionary<string, HashSet<string>>(m_Catalog.Dependencies.Length);
			foreach (var dependency in m_Catalog.Dependencies)
			{
				if (!m_Dependencies.TryGetValue(dependency.BundleName, out var dependenceis))
				{
					dependenceis = new HashSet<string>();
					m_Dependencies[dependency.BundleName] = dependenceis;
				}
				dependenceis.Add(dependency.DependencyBundleName);
			}

			// サイズを合わせておく
			foreach (var dependency in m_Dependencies)
				dependency.Value.TrimExcess();

			m_LoadingBundles = new HashSet<string>();
			m_UnloadingBundles = new HashSet<string>();
			m_LoadedBundles = new Dictionary<string, BundleHandle>(m_Catalog.Bundles.Length);

			return true;
		}

		public override string GetLocalPath(BundleInfo bundleInfo)
			=> AssetBundlePathUtils.GetLocalFilePath(GetLocalRoot(), bundleInfo);

		public string GetLocalRoot()
			=> AriaResourceSettings.TryGetInstance(out var instance)
			? Path.Combine(m_TemporaryDirectory, instance.InAppResourceBaseUri)
			: string.Empty;

		public string GetStreamingAssetsPath(BundleInfo bundleInfo)
			=> GetStreamingAssetsPath(bundleInfo.BundleName + AssetExtension);

		public string GetStreamingAssetsPath(string fileName)
			=> Path.Combine(GetStreamingAssets(), fileName);

		public string GetStreamingAssets()
			=> AriaResourceSettings.TryGetInstance(out var instance)
			? Path.Combine(m_StreamingAssetsDirectory, instance.InAppResourceBaseUri)
			: string.Empty;

		protected override async UniTask<bool> PrepareAssetBundleAsync(BundleInfo bundleInfo, DownloadPriority proiority)
		{
			string url = GetStreamingAssetsPath(bundleInfo);
			string localPath = GetLocalPath(bundleInfo);

			try
			{
				var directory = System.IO.Path.GetDirectoryName(localPath);
				if (!System.IO.Directory.Exists(directory))
					System.IO.Directory.CreateDirectory(directory);

				await UniTask.SwitchToMainThread();

				m_Logger.ZLogTrace($"{nameof(PrepareAssetBundleAsync)} [{bundleInfo.BundleName}]: {url} to {localPath}");
				using var fileHandler = new DownloadHandlerFile(localPath, false);
				using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, fileHandler, null);
				await request.SendWebRequest();
				if (request.result != UnityWebRequest.Result.Success)
				{
					m_Logger.ZLogError($"{url}: {request.error}");
				}
			}
			catch (Exception e)
			{
				m_Logger.ZLogError(e, $"{url}: throw exception");
				return false;
			}

			return true;
		}
	}
}
