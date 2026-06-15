using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aria.AssetManagement.IO;
using Aria.Logging;
using Cysharp.Net.Http;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ZLogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Aria.AssetManagement.Data
{
	[System.Serializable]
	public class BundleInfo
	{
		public string BundleName;
		public string Guid;
		public string Hash;
		public uint CRC;
		public long FileSize;
		public string Passward;
		public string Salt;

		public override string ToString()
			=> $"BundleName: {BundleName} Guid: {Guid} Hash:{Hash}";
	}

	[System.Serializable]
	public class DependencyInfo
	{
		public string BundleName;
		public string DependencyBundleName;
	}

	[System.Serializable]
	public class AssetBundleCatalog
	{
		static readonly ILogger m_Logger = AriaLogger<AssetBundleCatalog>.Get();

		public const string CatalogPath = "bundle_catalog";
		public const int SaltSize = 16;

		public const int DefaultBufferSize = 64 * 1024;
		public static int BufferSize = DefaultBufferSize;

		public BundleInfo[] Bundles;
		public DependencyInfo[] Dependencies;

		public AssetBundleCatalog()
		{
			Bundles = Array.Empty<BundleInfo>();
			Dependencies = Array.Empty<DependencyInfo>();
		}

		public static async UniTask<AssetBundleCatalog?> DownloadAsync(string url, string password, byte[] salt, DownloadingEvent downloadingEvent = null, CancellationToken cancellationToken = default)
		{
			m_Logger.ZLogTrace($"{nameof(AssetBundleCatalog)}.{nameof(DownloadAsync)}: {url}");
			try
			{
				if (url.StartsWith(Uri.UriSchemeHttp) || url.StartsWith(Uri.UriSchemeHttps))
				{
					await using var threadScope = UniTask.ReturnToMainThread(cancellationToken);
					using var request = new HttpRequestMessage(HttpMethod.Get, url);
					using var handler = new YetAnotherHttpHandler();
					using var client = new HttpClient(handler, false);
					// 圧縮してる関係でBodyを読み切るまで待機する
					using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
					if (!response.IsSuccessStatusCode)
					{
						m_Logger.ZLogError($"{url}: {response.ReasonPhrase}");
						return null;
					}
					using var content = response.Content;
					await using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
					return await ParseAsync(stream, password, salt, downloadingEvent, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					var request = UnityWebRequest.Get(url);
					await request.SendWebRequest();
					if (request.result != UnityWebRequest.Result.Success)
					{
						m_Logger.ZLogError($"{url}: {request.error}");
						return null;
					}
					using var stream = new MemoryStream(request.downloadHandler.data, false);
					return await ParseAsync(stream, password, salt, downloadingEvent, cancellationToken).ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				m_Logger.ZLogError(e, $"{url}: throw exception");
				return null;
			}
		}

		public static async UniTask<AssetBundleCatalog> LoadAsync(string filePath, string password, byte[] salt, DownloadingEvent downloadingEvent = null, CancellationToken cancellationToken = default)
		{
			await using var threadScope = UniTask.ReturnToMainThread(cancellationToken);
			AssetBundleCatalog catalog = null;
			if (File.Exists(filePath))
			{
				try
				{
					using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
					catalog = await ParseAsync(stream, password, salt, downloadingEvent, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					m_Logger.ZLogError(e, $"{catalog}");
					catalog = new AssetBundleCatalog();
				}
			}
			else
			{
				m_Logger.ZLogInformation($"Not found {filePath}");
				catalog = new AssetBundleCatalog();
			}
			return catalog;
		}

		static async Task<AssetBundleCatalog> ParseAsync(Stream stream, string password, byte[] salt, DownloadingEvent downloadingEvent, CancellationToken cancellationToken)
		{
			await using var threadScope = UniTask.ReturnToMainThread(cancellationToken);
			AssetBundleCatalog catalog = null;
			using var buffer = System.Buffers.MemoryPool<byte>.Shared.Rent(BufferSize);
			try
			{
				long current = 0;
				long length = stream.Length;

				var encoding = Encoding.Default;
				var builder = new StringBuilder();
				await using var cryption = new SeekableAesStream(stream, password, salt);
				await using var compress = new BrotliStream(cryption, CompressionMode.Decompress, false);
				while (true)
				{
					int readLength = await compress.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);
					if (readLength <= 0)
						break;
					current += readLength;
					if (downloadingEvent != null)
					{
						UniTask.Post(() => downloadingEvent?.Invoke(nameof(AssetBundleCatalog), current, length));
					}

					var str = encoding.GetString(buffer.Memory.Slice(0, readLength).Span);
					builder.Append(str);
				}
				if (downloadingEvent != null)
				{
					UniTask.Post(() => downloadingEvent?.Invoke(nameof(AssetBundleCatalog), length, length));
				}

				string json = builder.ToString();
				catalog = JsonUtility.FromJson<AssetBundleCatalog>(json);
			}
			catch (Exception e)
			{
				m_Logger.ZLogError(e, $"{catalog}");
				catalog = new AssetBundleCatalog();
			}
			return catalog;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			foreach (var item in Bundles)
			{
				builder.Append(item);
				builder.AppendLine();
			}
			return builder.ToString();
		}
	}
}
