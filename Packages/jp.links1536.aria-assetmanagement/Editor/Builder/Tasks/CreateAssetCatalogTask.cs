using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aria.AssetManagement;
using Aria.AssetManagement.Data;
using AriaEditor.AssetManagement.Context;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AriaEditor.AssetManagement.Task
{
	public class CreateAssetCatalogTask : IBuildTask
	{
		public int Version { get { return 1; } }

		[InjectContext(ContextUsage.In)]
		AriaBundleBuildContent m_Content;

		[InjectContext(ContextUsage.In, true)]
		IProgressTracker m_Tracker;

		public ReturnCode Run()
		{
			m_Tracker.UpdateInfo(nameof(CreateAssetCatalogTask));

			if (!AriaResourceSettings.TryGetInstance(out var resourceSetting))
			{
				BuildLogger.LogError($"{nameof(AriaResourceSettings)} is missing");
				return ReturnCode.MissingRequiredObjects;
			}

			var assetBundlePath = resourceSetting.AssetBundlePath;

			int taskCount = m_Content.BundleLayout.Count;
			m_Tracker.TaskCount = taskCount;

			HashSet<GUID> scenes = new HashSet<GUID>();
			foreach (var guid in m_Content.Scenes)
				scenes.Add(guid);

			// 各AssetBundleに含まれるアセットの情報をまとめる
			foreach (var bundleLayout in m_Content.BundleLayout)
			{
				if (!m_Tracker.UpdateInfo($"{nameof(CreateAssetCatalogTask)}:{bundleLayout.Key}"))
					return ReturnCode.Canceled;

				// シーンとアセットは共存できない
				if (bundleLayout.Value.Any(x => scenes.Contains(x)))
					continue;

				AssetCatalog assetCatalog = new AssetCatalog();

				foreach (var guid in bundleLayout.Value)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);

					if (assetPath.StartsWith(assetBundlePath))
						assetPath = assetPath.Substring(assetBundlePath.Length);

					assetCatalog.Assets.Add(new AssetMap()
					{
						Path = assetPath,
						Guid = guid.ToString(),
					});
				}

				string directory = $"{assetBundlePath}{GetBundleName(bundleLayout.Key)}";
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				// アセットリストをファイルに書き込む
				string catalogPath = $"{directory}/catalog.txt";
				string json = JsonUtility.ToJson(assetCatalog);
				File.WriteAllText(catalogPath, json);

				// Unityエディタに新しいアセットとして認識させる
				AssetDatabase.ImportAsset(catalogPath);
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			// シーンアセットと共存できないので専用のバンドルを作成する
			foreach (var bundleName in m_Content.BundleLayout.Keys.ToArray())
			{
				string catalogPath = $"{assetBundlePath}{GetBundleName(bundleName)}/catalog.txt";
				var catalogGuid = new GUID(AssetDatabase.AssetPathToGUID(catalogPath));
				m_Content.Assets.Add(catalogGuid);
				m_Content.BundleLayout[GetBundleName(bundleName)] = new List<GUID>()
				{
					catalogGuid
				};
			}

			return ReturnCode.Success;
		}

		static string GetBundleName(string bundleName)
			=> bundleName;
	}
}
