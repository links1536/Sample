using System.Collections.Generic;
using System.Threading;
using Aria.AssetManagement.Data;
using Aria.AssetManagement.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aria.AssetManagement
{
	public interface IAssetBundleProvider
	{
		/// <summary>
		/// カタログ内のAssetBundleリスト
		/// </summary>
		public IEnumerable<string> AllBundleNames { get; }

		/// <summary>
		/// ロード済みのAssetBundleリスト
		/// </summary>
		public IEnumerable<string> LoadedBundleNames { get; }

		/// <summary>
		/// アセットパスのプレフィックス
		/// </summary>
		public string AssetPathPrefix { get; }

		public UniTask<AssetBundle> LoadAsync(string bundleName, DownloadPriority priority, CancellationToken cancellationToken = default);
		public UniTask UnloadAsync(string bundleName, bool unloadAllLoadedObjects);
		public void UnloadAll(bool unloadAllLoadedObjects);
		public UniTask UnloadAllAsync(bool unloadAllLoadedObjects);

		/// <summary>
		/// ファイルパスを取得する
		/// </summary>
		public string GetLocalPath(BundleInfo bundleInfo);
	}
}
