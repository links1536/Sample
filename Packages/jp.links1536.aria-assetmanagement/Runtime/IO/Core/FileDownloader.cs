using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aria.AssetManagement.Hash;
using Cysharp.Threading.Tasks;
using ZLogger;

namespace Aria.AssetManagement.IO.Core
{
	public class FileDownloader : FileDownloaderBase<FileDownloader>
	{
		public FileDownloader(HttpMessageHandler handler)
			: base(handler)
		{
		}

		protected override async Task<bool> DownloadInternalAsync(DownloadHandle handle, Uri uri, string localPath, CancellationToken cancellationToken = default)
		{
			using var message = new HttpRequestMessage(HttpMethod.Get, uri);

			// ヘッダーを取得
			using var client = new HttpClient(m_Handler, false);
			using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				m_Logger.ZLogError($"{uri}: {response.StatusCode} {response.ReasonPhrase}");
				return false;
			}

			using var content = response.Content;
			await using var contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
			m_Logger.ZLogTrace($"{uri}: download start, version: {response.Version}");

			var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(BufferSize);
			var hashCalculator = handle.ValidationHash ? new HashCalculator(handle.HashType) : null;
			try
			{
				long currentDownloaded = 0;
				long? contentLength = content.Headers.ContentLength;

				if (contentLength <= 0)
					throw new InvalidDataException($"{uri}: content size error");

				await using var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Write, FileShare.Write);

				long lastTime = GetCurrentTime();
				while (currentDownloaded < contentLength)
				{
					int readLength = contentStream.Read(buffer, 0, BufferSize);
					if (readLength <= 0)
					{
						break;
					}

					// ファイルに書き込む
					var writeTask = WriteToStream(fileStream, buffer, 0, readLength, cancellationToken);

					if (handle.ValidationHash)
					{
						// 書き込みながらハッシュ値を計算
						bool last = (currentDownloaded + readLength) >= contentLength;
						hashCalculator?.PartialCompute(buffer, 0, readLength, last);
					}

					// 書き込みが終わるのを待機
					await writeTask.ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();

					// ローディング画面とかでダウンロードサイズが欲しいことがあるので
					currentDownloaded += readLength;
					handle.DownloadedBytes += readLength;
					long currentTime = GetCurrentTime();
					if (currentTime != lastTime)
					{
						UniTask.Post(() => handle.DownloadingEvent?.Invoke(handle.Label, handle.FileSize, handle.DownloadedBytes));
						lastTime = currentTime;
					}
				}

				UniTask.Post(() => handle.DownloadingEvent?.Invoke(handle.Label, handle.FileSize, handle.DownloadedBytes));

				if (handle.ValidationHash && hashCalculator != null)
				{
					// ファイルのハッシュ値が一致しなければエラー
					if (hashCalculator.Hash != handle.Hash)
					{
						throw new MismatchedHashException(handle.Label, handle.Hash, hashCalculator.Hash);
					}
				}
			}
			catch (Exception e)
			{
				m_Logger.ZLogError(e, $"{uri}: download error");
				return false;
			}
			finally
			{
				if (hashCalculator != null)
				{
					hashCalculator.Dispose();
					hashCalculator = null;
				}
				System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
			}
			m_Logger.ZLogTrace($"{uri} download end");
			return true;
		}

		async ValueTask WriteToStream(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
			await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
