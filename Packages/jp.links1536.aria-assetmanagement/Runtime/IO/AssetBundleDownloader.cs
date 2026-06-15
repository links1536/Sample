using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Aria.AssetManagement.Data;
using Aria.AssetManagement.Hash;
using Aria.AssetManagement.IO.Core;
using Cysharp.Net.Http;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZLogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Aria.AssetManagement.IO
{
	public class ResourceServerInfo
	{
		public readonly string Host;

		public ResourceServerInfo(string host)
		{
			Host = host;
		}

		public Uri GetRequestUri(BundleInfo assetInfo, string extention)
		{
			string route = assetInfo.BundleName;
			var uri = new Uri(
				string.Create(
					Host.Length + 1 + route.Length + extention.Length,
					(Host: Host, Route: route, Extention: extention),
					(buffer, arg) =>
					{
						int index = 0;
						arg.Host.AsSpan().CopyTo(buffer.Slice(index));
						index += arg.Host.Length;
						buffer[index++] = '/';
						arg.Route.AsSpan().CopyTo(buffer.Slice(index));
						index += arg.Route.Length;
						arg.Extention.AsSpan().CopyTo(buffer.Slice(index));
					}
				)
			);
			return uri;
		}
	}

	public enum DownloadPriority
	{
		Highest,
		High,
		Normal,
		Low,
		Lowest,

		Max,
	}

	public delegate void DownloadingEvent(string bundleName, long fileSize, long downloaded);
	public delegate void BulkDownloadingEvent(int totalCount, int downloadedCount, long fileSize, long downloaded);

	public class AssetBundleDownloader
	{
		class AssetBundleDownloadHandle : DownloadHandle
		{
			public readonly BundleInfo BundleInfo;

			public override string Label
				=> BundleInfo.BundleName;

			public override long FileSize
				=> BundleInfo.FileSize;

			public override bool ValidationHash
				=> true;
			public override HashType HashType { get; }
			public override string Hash
				=> BundleInfo.Hash;

			public AssetBundleDownloadHandle(BundleInfo bundleInfo, HashType hashType, DownloadingEvent? downloadingEvent)
				: base(downloadingEvent)
			{
				BundleInfo = bundleInfo;
				HashType = hashType;
			}
		}

		static readonly ILogger m_Logger = Logging.AriaLogger<AssetBundleDownloader>.Get();

		const int ParallelDownloadCount = 5;

		readonly ResourceServerInfo m_ResourceServerInfo;
		readonly YetAnotherHttpHandler m_Handler;

		Queue<string>[] m_DownloadQueue;

		Dictionary<string, AssetBundleDownloadHandle> m_DownloadHandles;
		HashSet<string> m_Downloadings;
		FileDownloaderBase m_FileDownloader;

		AssetBundleStorage m_Storage;
		string m_Extention;

		CancellationToken m_CancellationToken;

		public AssetBundleDownloader(ResourceServerInfo resourceServerInfo, AssetBundleStorage storage, string extention, CancellationToken cancellationToken)
		{
			m_ResourceServerInfo = resourceServerInfo;
			m_Handler = new YetAnotherHttpHandler()
			{
				//Http2Only = true,
				Http2AdaptiveWindow = true,
				Http2InitialStreamWindowSize = 10 * 1024 * 1024,
				Http2InitialConnectionWindowSize = 10 * 1024 * 1024,
			};

			m_DownloadQueue = new Queue<string>[(int)DownloadPriority.Max];
			for (int i = 0; i < (int)DownloadPriority.Max; ++i)
			{
				m_DownloadQueue[i] = new Queue<string>();
			}
			m_DownloadHandles = new Dictionary<string, AssetBundleDownloadHandle>();
			m_Downloadings = new HashSet<string>();
			m_FileDownloader = new FileDownloader(m_Handler);

			m_Storage = storage;
			m_Extention = extention;

			m_CancellationToken = cancellationToken;
		}

		public async UniTask<bool> BulkDownloadAsync(BundleInfo[] bundleInfos, DownloadPriority priority, BulkDownloadingEvent bulkDownloadEvent, DownloadingEvent downloadPerFile, CancellationToken cancellationToken = default)
		{
			using var pool = UnityEngine.Pool.DictionaryPool<string, (long downloaded, long fileSize)>.Get(out var downloadings);
			long totalFileSize = bundleInfos.Sum(x => x.FileSize);
			UniTask<bool>[] tasks = new UniTask<bool>[bundleInfos.Length];
			for (int i = 0; i < bundleInfos.Length; i++)
			{
				DownloadingEvent @event = default;
				if (bulkDownloadEvent != null)
				{
					@event += (bundleName, fileSize, downloadedSize) =>
					{
						downloadPerFile?.Invoke(bundleName, fileSize, downloadedSize);

						// 一括ダウンロードでは総ダウンロードサイズを返す
						long downloaded = 0;
						int downloadEndCount = 0;
						lock (downloadings)
						{
							downloadings[bundleName] = (downloadedSize, fileSize);
							foreach (var pair in downloadings)
							{
								// トータルのDL量
								downloaded += pair.Value.downloaded;
								if (pair.Value.downloaded < pair.Value.fileSize)
									continue;
								// ダウンロード完了済みの数
								downloadEndCount++;
							}
						}
						bulkDownloadEvent?.Invoke(bundleInfos.Length, downloadEndCount, totalFileSize, downloaded);
					};
				}
				tasks[i] = DownloadAsync(bundleInfos[i], priority, @event, cancellationToken);
			}
			var taskResults = await UniTask.WhenAll(tasks);
			bool success = taskResults.All(x => x == true);
			return success;
		}

		public async UniTask<bool> DownloadAsync(BundleInfo bundleInfo, DownloadPriority priority, DownloadingEvent? downloadingEvent, CancellationToken cancellationToken = default)
		{
			if (!AriaResourceSettings.TryGetInstance(out var resourceSetting))
				return false;

			int index = (int)priority;
			if (m_DownloadQueue.Length <= (uint)index)
				return false;

			// 優先度別キューに入れる
			var priorityQueues = m_DownloadQueue[(int)priority];
			if (!priorityQueues.Contains(bundleInfo.BundleName))
				priorityQueues.Enqueue(bundleInfo.BundleName);

			if (m_DownloadHandles.TryGetValue(bundleInfo.BundleName, out var downloadHandle))
			{
				// すでにDL中なら、そのDLの結果を返す
				while (!downloadHandle.IsDownloadEnd)
					await UniTask.Yield(cancellationToken);

				return downloadHandle.IsDownloadSuccess;
			}

			// ダウンロードリストに入れる
			downloadHandle = new AssetBundleDownloadHandle(bundleInfo, resourceSetting.HashType, downloadingEvent);
			m_DownloadHandles.Add(bundleInfo.BundleName, downloadHandle);

			// ダウンロード終了待ち
			while (!downloadHandle.IsDownloadEnd)
				await UniTask.Yield(cancellationToken);

			return downloadHandle.IsDownloadSuccess;
		}

		public void DownloadStart()
		{
			if (m_Downloadings.Count < ParallelDownloadCount)
			{
				while (TryDeque(out var bundleName))
				{
					if (!m_DownloadHandles.TryGetValue(bundleName, out var handle))
						continue;
					if (!handle.IsDownloadWait)
						continue;
					DownloadOneAsync(handle).Forget();
					if (m_Downloadings.Count >= ParallelDownloadCount)
					{
						break;
					}
				}
			}
		}

		bool TryDeque(out string bundleName)
		{
			foreach (var queue in m_DownloadQueue)
				if (queue.TryDequeue(out bundleName))
					return true;

			bundleName = null;
			return false;
		}

		async UniTask DownloadOneAsync(AssetBundleDownloadHandle handle)
		{
			// 優先度ごとに分かれている関係で同時に同じファイルをDLしようとすることがある
			if (m_Downloadings.Contains(handle.BundleInfo.BundleName))
				return;

			var cancellationToken = m_CancellationToken;

			try
			{
				m_Downloadings.Add(handle.BundleInfo.BundleName);

				// ダウンロード実行
				Uri remotePath = m_ResourceServerInfo.GetRequestUri(handle.BundleInfo, m_Extention);

				// ダウンロード中に変更する
				handle.Status = DownloadStatus.Downloading;

				// ローカルファイル
				string localPath = m_Storage.GetFilePath(handle.BundleInfo);
				Directory.CreateDirectory(Path.GetDirectoryName(localPath));

				// 一旦ファイルを空で作成
				await new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Write).DisposeAsync();

				// 分割できないので単体で全部取得する
				if (!await m_FileDownloader.DownloadAsync(handle, remotePath, localPath, cancellationToken: cancellationToken))
					throw new HttpRequestException($"{remotePath}: download failed");

				// キャッシュに追加する
				m_Storage.Register(handle.BundleInfo);
				await m_Storage.FlushAsync(false, cancellationToken).ConfigureAwait(false);

				// ダウンロード成功
				handle.Status = DownloadStatus.Success;
			}
			catch (Exception e)
			{
				handle.Status = DownloadStatus.Failed;
				m_Logger.ZLogError(e, $"DLに失敗しました");
			}
			finally
			{
				// ダウンロード中リストから削除
				m_DownloadHandles.Remove(handle.BundleInfo.BundleName);
				m_Downloadings.Remove(handle.BundleInfo.BundleName);
			}
		}
	}
}
