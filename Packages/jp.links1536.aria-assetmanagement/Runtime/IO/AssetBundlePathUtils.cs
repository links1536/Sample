using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Aria.AssetManagement.Data;

namespace Aria.AssetManagement
{
	static class AssetBundlePathUtils
	{

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetIdentifer(BundleInfo bundleInfo)
			=> bundleInfo.Guid;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetLocalFilePath(string directory, BundleInfo bundleInfo)
			=> GetFilePathInternal(directory, GetIdentifer(bundleInfo));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetIdentifer(IO.AssetBundleStorage.CacheInfo cacheInfo)
			=> cacheInfo.Guid;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetFilePath(string directory, IO.AssetBundleStorage.CacheInfo cacheInfo)
			=> GetFilePathInternal(directory, GetIdentifer(cacheInfo));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetFilePathInternal(string directory, string fileName)
#if true
			=> System.IO.Path.Combine(directory, fileName.Remove(2), fileName).Replace(@"\", @"/");
#else
			=> string.Create(
				directory.Length + 1 + 2 + 1 + fileName.Length,
				(Directory: directory, FileName: fileName),
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
#endif
	}
}
