using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aria.AssetManagement.Data;
using Aria.AssetManagement.Hash;
using Aria.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZLogger;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Aria.AssetManagement.IO
{
	public class AssetBundleStorage
	{
		[System.Serializable]
		class Cache
		{
			public CacheInfo[] CacheList;
		}

		[System.Serializable]
		internal class CacheInfo
		{
			public string Guid;
			public string Hash;

			public CacheInfo(string guid, string hash)
			{
				Guid = guid;
				Hash = hash;
			}
		}

		static readonly ILogger m_Logger = AriaLogger<AssetBundleStorage>.Get();

		readonly string m_SaveDirectory;
		readonly string m_LocalDirectory;
		readonly string m_CatalogPath;

		readonly Dictionary<string, CacheInfo> m_CacheDict;

		const long FlushTime = 1;
		long m_LastFlush;

		public AssetBundleStorage()
		{
#if UNITY_EDITOR
			m_SaveDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Library");
#else
			m_SaveDirectory = Application.persistentDataPath;
#endif
			m_LocalDirectory = Path.Combine(m_SaveDirectory, "AB").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			m_CatalogPath = Path.Combine(m_LocalDirectory, "Catalog").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			m_CacheDict = new Dictionary<string, CacheInfo>(0);
		}

		public async UniTask SetupAsync(CancellationToken cancellationToken = default)
		{
			await using var threadScope = UniTask.ReturnToMainThread(cancellationToken);

			m_Logger.ZLogDebug($"Cache path: {m_CatalogPath}");
			if (!File.Exists(m_CatalogPath))
			{
				return;
			}

			// キャッシュファイルを取得する
			using var fileStream = File.Open(m_CatalogPath, FileMode.Open, FileAccess.Read);
			using var compressStream = new BrotliStream(fileStream, CompressionMode.Decompress, false);
			using var reader = new StreamReader(compressStream);
			var json = await reader.ReadToEndAsync().ConfigureAwait(false);
			m_Logger.ZLogDebug($"Cache json: {json}");

			// キャッシュデータとしてまとめる
			cancellationToken.ThrowIfCancellationRequested();
			var cache = JsonUtility.FromJson<Cache>(json);
			var cacheList = cache.CacheList;
			m_CacheDict.EnsureCapacity(cacheList.Length);
			for (int i = 0; i < cacheList.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var cacheInfo = cacheList[i];
				m_CacheDict[GetIdentifer(cacheInfo)] = cacheInfo;
			}
		}

		public async ValueTask FlushAsync(bool forceFlush, CancellationToken cancellationToken = default)
		{
			// 前回の書き込みから一定時間内は基本的に書き込まない
			var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if (!forceFlush && (currentTime - m_LastFlush) < FlushTime)
				return;
			m_LastFlush = currentTime;

			// キャッシュデータを書き込み用のデータにまとめ直す
			int index = 0;
			CacheInfo[] cacheList = new CacheInfo[m_CacheDict.Count];
			foreach (var pair in m_CacheDict)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var cacheInfo = pair.Value;
				cacheList[index] = cacheInfo;
				index++;
			}
			Cache cache = new Cache();
			cache.CacheList = cacheList;
			string json = JsonUtility.ToJson(cache, true);

			// ファイルに保存する
			Directory.CreateDirectory(Path.GetDirectoryName(m_CatalogPath));
			using var fileStream = File.Open(m_CatalogPath, FileMode.Create, FileAccess.ReadWrite);
			using var compressStream = new BrotliStream(fileStream, CompressionLevel.Optimal, false);
			using var writer = new StreamWriter(compressStream);
			await writer.WriteAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
		}

		public bool Contains(BundleInfo bundleInfo, string hash)
			=> m_CacheDict.TryGetValue(GetIdentifer(bundleInfo), out var cache)
			&& cache.Hash == hash;

		public void Register(BundleInfo bundleInfo)
		{
			var identifer = GetIdentifer(bundleInfo);
			if (!m_CacheDict.TryGetValue(identifer, out var cacheInfo))
			{
				cacheInfo = new CacheInfo(identifer, bundleInfo.Hash);
				m_CacheDict.Add(identifer, cacheInfo);
			}
			cacheInfo.Hash = bundleInfo.Hash;
		}

		public void Unregister(BundleInfo bundleInfo)
		{
			var identifer = GetIdentifer(bundleInfo);
			if (!m_CacheDict.TryGetValue(identifer, out var cache))
				return;
			string localPath = GetFilePath(cache);
			try
			{
				if (File.Exists(localPath))
					File.Delete(localPath);
				m_CacheDict.Remove(identifer);
			}
			catch (Exception e)
			{
				m_Logger.ZLogError(e, $"{localPath}の削除に失敗しました", this);
			}
		}

		public async UniTask RemoveAllAsync(CancellationToken cancellationToken = default)
		{
			await UniTask.SwitchToThreadPool();

			foreach (var cacheInfo in m_CacheDict)
			{
				cancellationToken.ThrowIfCancellationRequested();
				string localPath = GetFilePath(cacheInfo.Value);
				try
				{
					if (File.Exists(localPath))
						File.Delete(localPath);
				}
				catch (Exception e)
				{
					m_Logger.ZLogError(e, $"{localPath}の削除に失敗しました", this);
				}
			}
			m_CacheDict.Clear();
			await FlushAsync(true, cancellationToken).ConfigureAwait(false);

			await UniTask.SwitchToMainThread();
		}

		public async UniTask RemoveBrokenCachesAsync(CancellationToken cancellationToken = default)
		{
			if (!AriaResourceSettings.TryGetInstance(out var instance))
			{
				m_Logger.ZLogError($"{nameof(AriaResourceSettings)}が見つかりませんでした");
				return;
			}
			await UniTask.SwitchToThreadPool();
			m_Logger.ZLogDebug($"{nameof(RemoveBrokenCachesAsync)} Start");
			using HashCalculator hashCalculator = new HashCalculator(instance.HashType);
			var brokenList = new List<(string identifer, CacheInfo cacheInfo)>();
			foreach (var cache in m_CacheDict)
			{
				m_Logger.ZLogDebug($"Checking Cache: {cache.Key}");
				string filePath = GetFilePath(cache.Value);
				if (File.Exists(filePath))
				{
					hashCalculator.Reset();
					await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
					string fileHash = hashCalculator.Calculate(fileStream);
					string catalogHash = cache.Value.Hash;
					if (catalogHash == fileHash)
						continue;
				}
				m_Logger.ZLogDebug($"Broken Cache: {cache.Key}");
				brokenList.Add((cache.Key, cache.Value));
			}

			foreach (var broken in brokenList)
			{
				m_Logger.ZLogDebug($"Remove Cache: {broken.identifer}");
				string localPath = GetFilePath(broken.cacheInfo);
				try
				{
					m_CacheDict.Remove(broken.identifer);
					if (File.Exists(localPath))
						File.Delete(localPath);
				}
				catch (Exception e)
				{
					m_Logger.ZLogError(e, $"{localPath}の削除に失敗しました", this);
				}
			}

			await FlushAsync(true, cancellationToken).ConfigureAwait(false);

			m_Logger.ZLogDebug($"{nameof(RemoveBrokenCachesAsync)} End");

			await UniTask.SwitchToMainThread();
		}

		public async UniTask RemoveUnusedCachesAsync(AssetBundleCatalog catalog, CancellationToken cancellationToken = default)
		{
			await UniTask.SwitchToThreadPool();

			HashSet<string> useBundles = new HashSet<string>();
			foreach (var bundleInfo in catalog.Bundles)
				useBundles.Add(GetIdentifer(bundleInfo));

			// 現在のカタログに載っていないファイルなら削除
			var files = Directory.EnumerateFiles(m_LocalDirectory, "*", SearchOption.AllDirectories);
			foreach (var filePath in files)
			{
				if (string.Equals(filePath, m_CatalogPath, StringComparison.CurrentCultureIgnoreCase))
					continue;
				cancellationToken.ThrowIfCancellationRequested();
				string identifer = Path.GetFileNameWithoutExtension(filePath);
				if (!useBundles.Contains(identifer))
				{
					File.Delete(filePath);
				}
			}

			await UniTask.SwitchToMainThread();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetIdentifer(BundleInfo bundleInfo)
			=> bundleInfo.Guid;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string GetFilePath(BundleInfo bundleInfo)
			=> GetFilePathInternal(GetIdentifer(bundleInfo));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetIdentifer(CacheInfo cacheInfo)
			=> cacheInfo.Guid;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		string GetFilePath(CacheInfo cacheInfo)
			=> GetFilePathInternal(GetIdentifer(cacheInfo));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		string GetFilePathInternal(string fileName)
			=> string.Create(
				m_LocalDirectory.Length + 1 + 2 + 1 + fileName.Length,
				(Directory: m_LocalDirectory, FileName: fileName),
				(buffer, arg) =>
				{
					int index = 0;

					// 親フォルダ
					arg.Directory.AsSpan().CopyTo(buffer.Slice(index));
					index += arg.Directory.Length;

					// 先頭2文字をフォルダ名に使う
					buffer[index++] = '/';
					var directoryName = arg.FileName.AsSpan().Slice(0, 2);
					directoryName.CopyTo(buffer.Slice(index));
					index += directoryName.Length;

					// ファイル名はハッシュ値
					buffer[index++] = '/';
					arg.FileName.AsSpan().CopyTo(buffer.Slice(index));
					index += arg.FileName.Length;
				}
			);
	}
}
