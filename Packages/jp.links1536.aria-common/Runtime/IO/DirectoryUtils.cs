using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aria.Logging;
using ZLogger;

namespace Aria.Common.IO
{
	public static class DirectoryUtils
	{
		public static void CopyDirectory(string source, string destination)
		{
			if (!Directory.Exists(source))
				return;

			SafeCreateDirectory(destination);
			var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories);
			var taskList = new List<Task>();
			Parallel.ForEach(files, sourcePath =>
			{
				var relativePath = Path.GetRelativePath(source, sourcePath);
				var destinationPath = Path.Combine(destination, relativePath);
				var directory = Path.GetDirectoryName(destinationPath);
				SafeCreateDirectory(directory);
				File.Copy(sourcePath, destinationPath, true);
			});
		}

		public static async Task CopyDirectoryAsync(string source, string destination, CancellationToken cancellationToken)
		{
			if (!Directory.Exists(source)) {
				LoggerHandler.DefaultLogger.ZLogError($"{nameof(CopyDirectoryAsync)}: Not found {source}");
				return;
			}

			SafeCreateDirectory(destination);
			var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories);
			var taskList = new List<Task>();
			foreach (var sourcePath in files) {
				var task = Task.Factory.StartNew(() =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					var relativePath = Path.GetRelativePath(source, sourcePath);
					var destinationPath = Path.Combine(destination, relativePath);
					var directory = Path.GetDirectoryName(destinationPath);
					SafeCreateDirectory(directory);
					File.Copy(sourcePath, destinationPath, true);
				}, TaskCreationOptions.LongRunning);
				taskList.Add(task);
			}
			await Task.WhenAll(taskList).ConfigureAwait(false);
		}

		public static void SafeCreateDirectory(string directory)
		{
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
		}

		public static void SafeDeleteDirectory(string directory)
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}
	}
}
