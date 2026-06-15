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
	public abstract class AssetBundleProviderBase<T> : Singleton<T>, IAssetBundleProvider
		where T : AssetBundleProviderBase<T>
	{
		protected class BundleHandle
		{
			public IReadOnlyList<Stream> StreamList;
			public AssetBundle AssetBundle;
		}

		protected AssetBundleCatalog m_Catalog;

		protected Dictionary<string, BundleInfo> m_BundleInfos;
		protected Dictionary<string, HashSet<string>> m_Dependencies;

		protected HashSet<string> m_LoadingBundles;
		protected HashSet<string> m_UnloadingBundles;
		protected Dictionary<string, BundleHandle> m_LoadedBundles;

		/// <summary>
		/// カタログ内のAssetBundleリスト
		/// </summary>
		public IEnumerable<string> AllBundleNames
			=> m_BundleInfos?.Keys;

		/// <summary>
		/// ロード済みのAssetBundleリスト
		/// </summary>
		public IEnumerable<string> LoadedBundleNames
			=> m_LoadedBundles?.Keys;

		/// <summary>
		/// アセットパスのプレフィックス
		/// </summary>
		public abstract string AssetPathPrefix { get; }

		/// <summary>
		/// アセットの拡張子
		/// </summary>
		public string AssetExtension { get; protected set; }

		/// <summary>
		/// アセットのフルパス
		/// </summary>
		public abstract string GetLocalPath(BundleInfo bundleInfo);

		/// <summary>
		/// 対象AssetBundleの合計サイズ
		/// </summary>
		public long GetTotalSize(IEnumerable<string> bundleNames)
		{
			long totalSize = 0;
			foreach (var bundleName in bundleNames)
				if (m_BundleInfos.TryGetValue(bundleName, out var bundleInfo))
					totalSize += bundleInfo.FileSize;
			return totalSize;
		}

		/// <summary>
		/// 読み込み処理
		/// </summary>
		public async UniTask<AssetBundle> LoadAsync(string bundleName, DownloadPriority priority, CancellationToken cancellationToken = default)
		{
			// 依存ファイルを先に読み込む
			bool successDependencies = true;
			if (m_Dependencies.TryGetValue(bundleName, out var dependecies))
			{
				// 依存関係を読み込む
				var loadTaskList = new List<UniTask<AssetBundle>>();
				foreach (var dependency in dependecies)
				{
					var task = LoadInternalAsync(dependency, priority, cancellationToken);
					loadTaskList.Add(task);
				}
				if (loadTaskList.Count > 0)
				{
					var results = await UniTask.WhenAll(loadTaskList);
					successDependencies = results.All(x => x != null);
				}
			}

			if (!successDependencies)
			{
				m_Logger.ZLogError($"[{bundleName}] 依存ファイルの読み込みに失敗しました");
				return null;
			}

			return await LoadInternalAsync(bundleName, priority, cancellationToken);
		}

		/// <summary>
		/// 全てのAssetBundleを解放する
		/// </summary>
		public void UnloadAll(bool unloadAllLoadedObjects)
		{
			if (m_LoadedBundles == null)
				return;

			foreach (var pair in m_LoadedBundles)
			{
				m_UnloadingBundles.Add(pair.Key);
				pair.Value.AssetBundle.Unload(unloadAllLoadedObjects);
				foreach (var stream in pair.Value.StreamList)
					stream.Dispose();
			}
			m_UnloadingBundles.Clear();
			m_LoadedBundles.Clear();
		}

		/// <summary>
		/// 全てのAssetBundleを解放する
		/// </summary>
		public async UniTask UnloadAllAsync(bool unloadAllLoadedObjects)
		{
			if (m_LoadedBundles == null)
				return;

			var taskList = new List<UniTask>(m_LoadedBundles.Count);
			foreach (var pair in m_LoadedBundles)
			{
				m_UnloadingBundles.Add(pair.Key);
				taskList.Add(UniTask.Create(async () =>
				{
					await pair.Value.AssetBundle.UnloadAsync(unloadAllLoadedObjects);
					foreach (var stream in pair.Value.StreamList)
						stream.Dispose();
				}));
			}
			await UniTask.WhenAll(taskList);
			m_UnloadingBundles.Clear();
			m_LoadedBundles.Clear();
		}

		/// <summary>
		/// 指定したAssetBundleを解放する
		/// </summary>
		public async UniTask UnloadAsync(string bundleName, bool unloadAllLoadedObjects)
		{
			while (m_LoadingBundles.Contains(bundleName))
				await UniTask.Yield();
			if (!m_LoadedBundles.TryGetValue(bundleName, out var handle))
				return;
			// 読み込み済みから削除
			m_LoadedBundles.Remove(bundleName);

			// 削除する
			m_UnloadingBundles.Add(bundleName);
			await handle.AssetBundle.UnloadAsync(unloadAllLoadedObjects);
			foreach (var stream in handle.StreamList)
				await stream.DisposeAsync();
			m_UnloadingBundles.Remove(bundleName);
		}


		async UniTask<AssetBundle> LoadInternalAsync(string bundleName, DownloadPriority priority, CancellationToken cancellationToken = default)
		{
			if (!m_BundleInfos.TryGetValue(bundleName, out var bundleInfo))
			{
				m_Logger.ZLogWarning($"Not found bundle: {bundleName}");
				return null;
			}

			// アンロード中に読み込もうとすると困るので待たせる
			while (m_UnloadingBundles.Contains(bundleName))
				await UniTask.Yield();

			// 多重ロードはできないので読み込み中なら待つ
			while (m_LoadingBundles.Contains(bundleName))
				await UniTask.Yield();

			if (!m_LoadedBundles.TryGetValue(bundleName, out var handle))
			{
				// 読み込み中に追加
				m_LoadingBundles.Add(bundleName);

				// 読み込み準備
				m_Logger.ZLogDebug($"[{bundleName}] 事前準備");
				bool prepare = await PrepareAssetBundleAsync(bundleInfo, priority);
				if (!prepare)
					throw new IOException($"[{bundleName}] 事前準備に失敗しました");

				m_Logger.ZLogDebug($"[{bundleName}] 読み込み開始");

				var streamList = new List<Stream>();
				try
				{
					// ファイルを読み込む
					var localPath = GetLocalPath(bundleInfo);
					var saltBytes = Convert.FromBase64String(bundleInfo.Salt);
					var source = File.Open(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
					streamList.Add(source);

					var crypto = new SeekableAesStream(source, bundleInfo.Passward, saltBytes);
					streamList.Add(crypto);

					// 読み込み完了待ちがファイルサイズに比例して長くなるため、普段の読み込みではCRCを使わない
					var bundle = await AssetBundle.LoadFromStreamAsync(crypto);
					if (bundle == null)
						throw new IOException($"[{bundleName}] 読み込みに失敗しました");

					handle = new BundleHandle()
					{
						StreamList = new Stream[] { source, crypto },
						AssetBundle = bundle,
					};
					m_LoadedBundles[bundleName] = handle;
				}
				catch (Exception e)
				{
					if (streamList != null)
					{
						foreach (var stream in streamList)
						{
							stream.Dispose();
						}
						streamList.Clear();
					}
					m_Logger.ZLogError(e, $"{bundleInfo.BundleName}({bundleInfo.Hash}) load failed");
					return null;
				}
				finally
				{
					// 読み込み中から削除
					m_LoadingBundles.Remove(bundleName);
				}

				m_Logger.ZLogDebug($"[{bundleName}] 読み込み完了");
			}

			return handle.AssetBundle;
		}

		/// <summary>
		/// AssetBundleの読み込み準備
		/// </summary>
		protected abstract UniTask<bool> PrepareAssetBundleAsync(BundleInfo bundleInfo, DownloadPriority proiority);

		void OnDestroy()
		{
			if (m_LoadedBundles != null)
			{
				foreach (var pair in m_LoadedBundles)
				{
					pair.Value.AssetBundle.Unload(true);
					foreach (var stream in pair.Value.StreamList)
						stream.Dispose();
				}
				m_LoadedBundles.Clear();
			}
		}
	}
}
