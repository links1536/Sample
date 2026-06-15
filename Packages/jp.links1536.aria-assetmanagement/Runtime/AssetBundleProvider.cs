using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Aria.AssetManagement.Data;
using Aria.AssetManagement.IO;
using Aria.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZLogger;

namespace Aria.AssetManagement
{
	public class AssetBundleProvider : AssetBundleProviderBase<AssetBundleProvider>
	{
		AssetBundleStorage m_Storage;
		ResourceServerInfo m_ServerInfo;
		AssetBundleDownloader m_Downloader;

		/// <summary>
		/// アセットパスのプレフィックス
		/// </summary>
		public override string AssetPathPrefix
			=> AriaResourceSettings.TryGetInstance(out var ariaResourceSettings)
			? ariaResourceSettings.AssetBundlePath
			: string.Empty;

		/// <summary>
		/// アセットのフルパス
		/// </summary>
		public override string GetLocalPath(BundleInfo bundleInfo)
			=> m_Storage.GetFilePath(bundleInfo);

		public async UniTask<bool> SetupAsync(AssetBundleStorage assetBundleStorage, DownloadingEvent downloadingEvent, CancellationToken cancellationToken = default)
		{
			if (!AriaResourceSettings.TryGetInstance(out var instance))
			{
				m_Logger.ZLogError($"{nameof(AriaResourceSettings)}を取得できませんでした");
				return false;
			}

			await UnloadAllAsync(true);

			var baseUrl = instance.AssetBundleBaseUri;
			if (!baseUrl.EndsWith('/'))
				baseUrl += '/';

			// キャッシュのストレージ
			m_Storage = assetBundleStorage;

			// 拡張子登録
			AssetExtension = instance.Extension;

			// プラットフォームごとにDL先を切り替える
			string platformIdentifer = PlatformUtility.RuntimePlatformIdentifer(Application.platform);
			baseUrl = string.Format("{0}{1}/", baseUrl, platformIdentifer);

			// カタログをDLしてDictionaryにまとめ直す
			string path = string.Format("{0}{1}{2}", baseUrl, AssetBundleCatalog.CatalogPath, AssetExtension);
			m_Catalog = await AssetBundleCatalog.DownloadAsync(path, instance.CatalogPassword, instance.CatalogSalt, downloadingEvent);
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

			m_ServerInfo = new ResourceServerInfo(baseUrl);
			m_Logger.ZLogTrace($"AssetManagement Base Url: {baseUrl}");

			m_Downloader = new AssetBundleDownloader(m_ServerInfo, m_Storage, AssetExtension, destroyCancellationToken);

			m_LoadingBundles = new HashSet<string>();
			m_UnloadingBundles = new HashSet<string>();
			m_LoadedBundles = new Dictionary<string, BundleHandle>(m_Catalog.Bundles.Length);

			return true;
		}

		public IReadOnlyList<string> GetDownloadableBundleNames()
		{
			var bundleNameList = new List<string>();
			foreach (var bundleName in AllBundleNames)
			{
				if (!m_BundleInfos.TryGetValue(bundleName, out var bundleInfo))
					continue;
				// キャッシュに乗っているものはDLしない
				if (m_Storage.Contains(bundleInfo, bundleInfo.Hash))
					continue;
				bundleNameList.Add(bundleInfo.BundleName);
			}
			return bundleNameList;
		}

		public async UniTask<bool> BulkDownloadAsync(IEnumerable<string> bundleNames, DownloadPriority priority, BulkDownloadingEvent bulkDownloadingEvent, DownloadingEvent downloadingEvent, CancellationToken cancellationToken = default)
		{
			List<BundleInfo> bundleInfoList = new List<BundleInfo>();
			foreach (var bundleName in bundleNames)
				if (m_BundleInfos.TryGetValue(bundleName, out var bundleInfo))
					bundleInfoList.Add(bundleInfo);
			if (bundleInfoList.Count <= 0)
			{
				m_Logger.ZLogError($"一括ダウンロード可能なファイルが存在しませんでした\n{string.Join("\n", bundleNames)}");
				return false;
			}
			return await m_Downloader.BulkDownloadAsync(bundleInfoList.ToArray(), priority, bulkDownloadingEvent, downloadingEvent, cancellationToken);
		}


		protected override async UniTask<bool> PrepareAssetBundleAsync(BundleInfo bundleInfo, DownloadPriority priority)
		{
			if (m_Storage.Contains(bundleInfo, bundleInfo.Hash))
				return true;
			bool success = await m_Downloader.DownloadAsync(bundleInfo, priority, null);
			if (!success)
			{
				m_Logger.ZLogError($"[{bundleInfo.BundleName}] ダウンロードに失敗しました");
			}
			return success;
		}

		void Update()
		{
			if (m_Downloader == null)
				return;
			m_Downloader.DownloadStart();
		}
	}
}
