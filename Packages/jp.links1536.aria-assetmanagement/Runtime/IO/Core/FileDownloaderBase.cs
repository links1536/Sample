using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aria.Logging;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aria.AssetManagement.IO.Core
{
	public abstract class FileDownloaderBase : IDisposable
	{
		public abstract UniTask<bool> DownloadAsync(DownloadHandle handle, Uri remotePath, string localPath, CancellationToken cancellationToken = default);
		public abstract void Dispose();
	}

	public abstract class FileDownloaderBase<T> : FileDownloaderBase
	{
		static protected readonly ILogger m_Logger = AriaLogger<T>.Get();

		// バッファの標準値
		public const int DefaultBufferSize = 10 * 1024 * 1024;
		public static int BufferSize = DefaultBufferSize;

		protected const int RetryDelaySecond = 1;

		protected HttpMessageHandler m_Handler;

		public FileDownloaderBase(HttpMessageHandler handler)
		{
			m_Handler = handler;
		}

		public override void Dispose()
		{
			m_Handler.Dispose();
		}

		public override async UniTask<bool> DownloadAsync(DownloadHandle handle, Uri remotePath, string localPath, CancellationToken cancellationToken = default)
		{
			await using var returnToNormal = UniTask.ReturnToMainThread();

			Directory.CreateDirectory(Path.GetDirectoryName(localPath));

			// 一旦ファイルを空で作成
			await new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Write).DisposeAsync();
			return await DownloadInternalAsync(handle, remotePath, localPath, cancellationToken).ConfigureAwait(false);
		}

		protected abstract Task<bool> DownloadInternalAsync(DownloadHandle handle, Uri uri, string localPath, CancellationToken cancellationToken = default);

		protected static long GetCurrentTime()
			=> DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 50;
	}
}
