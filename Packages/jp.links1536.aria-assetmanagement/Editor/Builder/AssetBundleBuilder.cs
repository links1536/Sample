using System.Text;
using AriaEditor.AssetManagement.Context;
using AriaEditor.AssetManagement.Task;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine;

namespace AriaEditor.AssetManagement
{
	public class AssetBundleBuilder
	{
		const string BundleExtension = ".aria";

		public static bool Build(BuildTarget buildTarget, AssetBundleBuild[] buildTargets, BuildPath buildPath, bool userCache)
		{
			// 設定
			var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
			var parameter = new AriaBundleBuildParameters(buildTarget, buildTargetGroup, BundleExtension, buildPath, buildTargets)
			{
				UseCache = userCache,
				BundleCompression = BuildCompression.LZ4,
				NonRecursiveDependencies = false,
			};

			// ビルド対象
			var content = new AriaBundleBuildContent(buildTargets);

			// ビルド時の処理
			var buildTaskList = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);
			buildTaskList.Add(new CreateBundleGuidTask());
			buildTaskList.Add(new EncryptBundlesTask());
			buildTaskList.Add(new CreateBundleCatalogTask());
			buildTaskList.Add(new PublishBundleTask());

			// ビルド実行
			var status = ContentPipeline.BuildAssetBundles(parameter, content, out var result, buildTaskList);
			foreach (var pair in result.BundleInfos)
			{
				var bundleName = pair.Key;
				var bundleInfo = pair.Value;
				var builder = new StringBuilder();
				builder.Append("===============================================")
					.AppendLine().Append("BundleName: ").Append(bundleName)
					.AppendLine().Append("Path: ").Append(bundleInfo.FileName)
					.AppendLine().Append("Hash: ").Append(bundleInfo.Hash)
					.AppendLine().Append("Crc: ").Append(bundleInfo.Crc);
				foreach (var dependency in bundleInfo.Dependencies)
					builder.AppendLine().Append("Dependency: ").Append(dependency);
				Debug.Log(builder.ToString());
			}
			Debug.Log(status);
			return status == ReturnCode.Success;
		}
	}
}
