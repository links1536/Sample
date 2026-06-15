using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Aria.Logging;
using ZLogger;

namespace Aria.Common.IO
{
	public static class StorageUtils
	{
		// Windowsは1024単位、ほかは1000単位
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		public const long SizeUnit = 1024;
#else
		public const long SizeUnit = 1000;
#endif

		public const long Kiro = SizeUnit;
		public const long Mega = SizeUnit * SizeUnit;
		public const long Giga = SizeUnit * SizeUnit * SizeUnit;
		public const long Tera = SizeUnit * SizeUnit * SizeUnit * SizeUnit;

		public static double ToKiro(long bytes)
			=> (double)bytes / Kiro;
		public static double ToMega(long bytes)
			=> (double)bytes / Mega;
		public static double ToGiga(long bytes)
			=> (double)bytes / Giga;
		public static double ToTera(long bytes)
			=> (double)bytes / Tera;

		public static string HumanReadable(long bytes, int digit)
			=> (bytes > Tera) ? string.Format($"{{0:0.{string.Concat('#', digit)}}}TB", (double)bytes / Tera)
			 : (bytes > Giga) ? string.Format($"{{0:0.{string.Concat('#', digit)}}}GB", (double)bytes / Giga)
			 : (bytes > Mega) ? string.Format($"{{0:0.{string.Concat('#', digit)}}}MB", (double)bytes / Mega)
			 : (bytes > Kiro) ? string.Format($"{{0:0.{string.Concat('#', digit)}}}KB", (double)bytes / Kiro)
			 : $"{bytes}B";

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetDiskFreeSpaceEx(
			string lpDirectoryName,					// ドライブのルートパス (例: "C:\\")
			out ulong lpFreeBytesAvailable,			// 呼び出し側が利用できるバイト数
			out ulong lpTotalNumberOfBytes,			// ドライブの総バイト数
			out ulong lpTotalNumberOfFreeBytes		// ドライブの空きバイト数
		);
#endif

#if UNITY_IOS && !UNITY_EDITOR
		// Objective-CのメソッドをC#から呼び出すための宣言
		[DllImport("__Internal")]
		static extern long GetTotalDiskSpace();

		[DllImport("__Internal")]
		static extern long GetFreeDiskSpace();

		[DllImport("__Internal")]
		static extern long GetAvailableDiskSpace();
#endif

		public static bool TryGetDiskFreeSpace(string directoryPath, out ulong availableBytes, out ulong totalBytes, out ulong freeBytes)
		{
			try {
#if !UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
				return GetDiskFreeSpaceEx(directoryPath, out availableBytes, out totalBytes, out freeBytes);
#elif UNITY_ANDROID
				// AndroidのStatFsクラスをC#から呼び出す
				using var statFs = new UnityEngine.AndroidJavaObject("android.os.StatFs", directoryPath);

				// 容量の基準となるブロックのサイズ
				long blockSize = statFs.Call<long>("getBlockSizeLong");
				// ブロック数
				long totalBlocks = statFs.Call<long>("getBlockCountLong");
				long availableBlocks = statFs.Call<long>("getAvailableBlocksLong");
				long freeBlocks = statFs.Call<long>("getFreeBlocksLong");

				// ブロック数×ブロックサイズ
				totalBytes = (ulong)(totalBlocks * blockSize);
				availableBytes = (ulong)(availableBlocks * blockSize);
				freeBytes = (ulong)(freeBlocks * blockSize);

				return true;
#endif
			}
			catch (Exception e) {
				LoggerHandler.DefaultLogger.ZLogError(e, $"[{nameof(TryGetDiskFreeSpace)}] 容量取得に失敗しました: {directoryPath}");
			}
			availableBytes = 0;
			totalBytes = 0;
			freeBytes = 0;
			return false;
		}
	}
}
