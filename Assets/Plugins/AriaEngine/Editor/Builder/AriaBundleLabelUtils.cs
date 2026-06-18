using System;
using System.Collections.Generic;
using System.Linq;
using Aria.AssetManagement.Data;
using AriaEditor.AssetManagement;
using UnityEditor;
using UnityEngine;

namespace AriaEditor
{
	static class AriaBundleLabelUtils
	{
		public static AssetBundleBuild[] GetAssetBundleBuildTargets(bool inAppBundle)
		{
			if (!Aria.AssetManagement.AriaResourceSettings.TryGetInstance(out var ariaResourceSettings))
				return null;

			bool IsSceneAsset(string path)
				=> path.EndsWith(".unity");

			string assetBundleDirectory = inAppBundle
				? ariaResourceSettings.InAppResourcePath
				: ariaResourceSettings.AssetBundlePath;
			assetBundleDirectory = assetBundleDirectory.Replace(@"\", "/").TrimEnd('/') + "/";

			var buildTargetList = new List<AssetBundleBuild>();

			var assetToBundleMap = AriaBundleLabelSettings.instance.CreateBundleAssetMap(assetBundleDirectory);
			foreach (var pair in assetToBundleMap)
			{
				string bundleName = pair.Key;
				HashSet<string> assetList = pair.Value;
				string bundleDirectory = $"{assetBundleDirectory}/{bundleName}";

				if (assetList.Any(IsSceneAsset))
				{
					// シーンを含む場合はシーン以外のアセットを明示的に含むことができない
					assetList.RemoveWhere(x => !IsSceneAsset(x));
				}
				else
				{
					// バンドルにはカタログをつくる
					AssetCatalog assetCatalog = new AssetCatalog();

					foreach (var path in assetList)
					{
						var guid = AssetDatabase.AssetPathToGUID(path);
						var assetPath = path;

						if (assetPath.StartsWith(assetBundleDirectory, StringComparison.CurrentCultureIgnoreCase))
							assetPath = assetPath.Substring(assetBundleDirectory.Length);

						assetCatalog.Assets.Add(new AssetMap()
						{
							Path = assetPath,
							Guid = guid.ToString(),
						});
					}

					// アセットリストをファイルに保存
					string catalogPath = $"{bundleDirectory}/{AssetCatalog.Name}";
					string json = JsonUtility.ToJson(assetCatalog);
					System.IO.File.WriteAllText(catalogPath, json);

					// Unityに認識させる
					AssetDatabase.ImportAsset(catalogPath);

					// カタログに追加
					assetList.Add(catalogPath);
				}

				// アセットバンドルビルド用情報作成
				buildTargetList.Add(new AssetBundleBuild()
				{
					assetBundleName = bundleName,
					assetNames = assetList.ToArray(),
				});
			}

			foreach (var bundle in buildTargetList.OrderBy(x=>x.assetBundleName))
			{
				UnityEngine.Debug.Log(bundle.assetBundleName + "\n" + string.Join("\n", bundle.assetNames));
			}

			return buildTargetList.ToArray();
		}

		static bool IsValidBundleTarget(string assetBundleDirectory, string path)
		{
			if (!Aria.AssetManagement.AriaResourceSettings.TryGetInstance(out var ariaResourceSettings))
				return false;
			if (path.EndsWith(".cs"))
				return false;
			if (path.Contains("/Editor/", System.StringComparison.CurrentCultureIgnoreCase))
				return false;
			if (path.Contains("/Editor Resources/", System.StringComparison.CurrentCultureIgnoreCase))
				return false;
			if (path.StartsWith(assetBundleDirectory, System.StringComparison.CurrentCultureIgnoreCase))
				return false;
			return true;
		}

	}
}
