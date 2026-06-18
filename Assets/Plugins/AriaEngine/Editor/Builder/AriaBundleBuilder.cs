using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aria.AssetManagement.Data;
using Aria.Common.IO;
using AriaEditor.AssetManagement;
using AriaEditor.AssetManagement.Task;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine;

namespace AriaEditor
{
	class AriaBundleBuilder
	{
		[MenuItem("Build/AssetBundle/Windows/FullBuild")]
		static void BuildBundleWindowsFull()
			=> BuildBundleCore(BuildTarget.StandaloneWindows64, false);

		[MenuItem("Build/AssetBundle/Windows/Incremental")]
		static void BuildBundleWindowsIncremental()
			=> BuildBundleCore(BuildTarget.StandaloneWindows64, true);


		[MenuItem("Build/AssetBundle/Android/FullBuild")]
		static void BuildBundleAndroidFull()
			=> BuildBundleCore(BuildTarget.Android, false);

		[MenuItem("Build/AssetBundle/Android/Incremental")]
		static void BuildBundleAndroidIncremental()
			=> BuildBundleCore(BuildTarget.Android, true);


		[MenuItem("Build/AssetBundle/iOS/FullBuild")]
		static void BuildBundleIOSFull()
			=> BuildBundleCore(BuildTarget.iOS, false);

		[MenuItem("Build/AssetBundle/iOS/Incremental")]
		static void BuildBundleIOSIncremental()
			=> BuildBundleCore(BuildTarget.iOS, true);

		[MenuItem("Build/AssetBundle/Clear Build Cache")]
		static void BuildBundleClearCache()
		{
			string buildCachePath = AriaResourceBuildSettings.instance.BuildCachePath;
			string publishRootPath = AriaResourceBuildSettings.instance.PublishRootPath;
			DirectoryUtils.SafeDeleteDirectory(buildCachePath);
			DirectoryUtils.SafeDeleteDirectory(publishRootPath);
		}

		static void BuildBundleCore(BuildTarget buildTarget, bool useCache)
		{
			var buildTargets = AriaBundleLabelUtils.GetAssetBundleBuildTargets(false);
			string buildCachePath = AriaResourceBuildSettings.instance.BuildCachePath;
			string publishRootPath = AriaResourceBuildSettings.instance.PublishRootPath;
			BuildPath buildPath = new BuildPath(buildTarget, buildCachePath, publishRootPath);
			bool success = AssetBundleBuilder.Build(buildTarget, buildTargets, buildPath, useCache);
			if (success)
				UnityEngine.Debug.Log("Asset Bundle Build: Success");
			else
				UnityEngine.Debug.LogError("Asset Bundle Build: Failed");
		}

	}
}
