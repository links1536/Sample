using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aria.AssetManagement.Data;
using UnityEditor;
using UnityEngine;

namespace AriaEditor
{
	static class AriaBundleLabelUtils
	{
		// バンドル名の深さ
		const int BundleDirectoryDepth = 2;

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
			var dependencyList = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

			var bundleAssetListMap = new Dictionary<string, HashSet<string>>(100, StringComparer.CurrentCultureIgnoreCase);
			var bundleContainsSceneMap = new Dictionary<string, bool>(100, StringComparer.CurrentCultureIgnoreCase);
			var bundleDirectoryMap = new Dictionary<string, string>(100, StringComparer.CurrentCultureIgnoreCase);

			var files = System.IO.Directory.EnumerateFiles(assetBundleDirectory, "*", System.IO.SearchOption.AllDirectories);
			foreach (var file in files)
			{
				string normalizedFile = file.Replace(@"\", "/");

				if (normalizedFile.EndsWith(".meta"))
					continue;

				//カタログファイル
				var fileName = System.IO.Path.GetFileName(normalizedFile);
				if (string.Equals(fileName, AssetCatalog.Name, StringComparison.CurrentCultureIgnoreCase))
					continue;

				if (!normalizedFile.StartsWith(assetBundleDirectory, StringComparison.CurrentCultureIgnoreCase))
					continue;

				// AssetBundleフォルダを削る
				string relativePath = normalizedFile.Substring(assetBundleDirectory.Length);
				string bundleName = GetBundleNameByDirectoryDepth(relativePath, BundleDirectoryDepth);

				if (string.IsNullOrEmpty(bundleName))
					continue;

				if (!bundleAssetListMap.TryGetValue(bundleName, out var assetList))
				{
					assetList = new HashSet<string>();
					bundleAssetListMap.Add(bundleName, assetList);
					bundleContainsSceneMap.Add(bundleName, false);
					bundleDirectoryMap.Add(bundleName, assetBundleDirectory + bundleName);
				}

				assetList.Add(normalizedFile);

				if (IsSceneAsset(relativePath))
					bundleContainsSceneMap[bundleName] = true;

				// AssetBundleフォルダ外の依存ファイルをまとめる
				var dependencies = AssetDatabase.GetDependencies(normalizedFile, true);
				foreach (var dependency in dependencies)
				{
					if (!IsValidBundleTarget(assetBundleDirectory, dependency))
						continue;

					dependencyList.Add(dependency);
				}
			}

			foreach (var pair in bundleAssetListMap)
			{
				string bundleName = pair.Key;
				HashSet<string> assetList = pair.Value;
				bool containsScene = bundleContainsSceneMap[bundleName];
				string directory = bundleDirectoryMap[bundleName];

				if (containsScene)
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
					string catalogPath = $"{directory}/{AssetCatalog.Name}";
					string json = JsonUtility.ToJson(assetCatalog);
					File.WriteAllText(catalogPath, json);

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

			// 依存ファイルはまとめる
			buildTargetList.Add(new AssetBundleBuild()
			{
				assetBundleName = "dependencies",
				assetNames = dependencyList.ToArray(),
			});

			foreach (var bundle in buildTargetList)
			{
				UnityEngine.Debug.Log(bundle.assetBundleName + "\n" + string.Join("\n", bundle.assetNames));
			}

			return buildTargetList.ToArray();
		}

		/// <summary>
		/// 指定した階層でラベル名を作成する
		/// </summary>
		static string GetBundleNameByDirectoryDepth(string relativeAssetPath, int directoryDepth)
		{
			if (directoryDepth <= 0)
				directoryDepth = 1;

			relativeAssetPath = relativeAssetPath.Replace(@"\", "/");

			int lastSlashIndex = relativeAssetPath.LastIndexOf('/');
			if (lastSlashIndex < 0)
				return null;

			string directoryPath = relativeAssetPath.Substring(0, lastSlashIndex);
			var directories = directoryPath.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (directories.Length <= 0)
				return null;

			int depth = System.Math.Min(directoryDepth, directories.Length);
			return string.Join("/", directories.Take(depth));
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
