using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aria.AssetManagement;
using Aria.AssetManagement.Data;
using AriaEditor.AssetManagement;
using UnityEditor;
using UnityEngine;

namespace AriaEditor
{
	class AriaInAppBundleBuilder
	{
		[MenuItem("Build/InAppBundle/Windows/FullBuild")]
		static void BuildBundleWindowsFull()
			=> BuildBundleCore(BuildTarget.StandaloneWindows64, false);

		[MenuItem("Build/InAppBundle/Windows/Incremental")]
		static void BuildBundleWindowsIncremental()
			=> BuildBundleCore(BuildTarget.StandaloneWindows64, true);


		[MenuItem("Build/InAppBundle/Android/FullBuild")]
		static void BuildBundleAndroidFull()
			=> BuildBundleCore(BuildTarget.Android, false);

		[MenuItem("Build/InAppBundle/Android/Incremental")]
		static void BuildBundleAndroidIncremental()
			=> BuildBundleCore(BuildTarget.Android, true);


		[MenuItem("Build/InAppBundle/iOS/FullBuild")]
		static void BuildBundleIOSFull()
			=> BuildBundleCore(BuildTarget.iOS, false);

		[MenuItem("Build/InAppBundle/iOS/Incremental")]
		static void BuildBundleIOSIncremental()
			=> BuildBundleCore(BuildTarget.iOS, true);

		public static bool BuildBundleCore(BuildTarget buildTarget, bool useCache)
		{
			if(!AriaResourceSettings.TryGetInstance(out var resourceSettings)) {
				UnityEngine.Debug.LogError("Asset Bundle Build: Failed");
				return false;
			}
			var buildTargets = AriaBundleLabelUtils.GetAssetBundleBuildTargets(true);
			string buildCachePath = AriaResourceBuildSettings.instance.InAppCachePath;
			string publishRootPath = AriaResourceBuildSettings.instance.InAppPublishPath;
			BuildPath buildPath = new BuildPath(buildTarget, buildCachePath, publishRootPath);
			bool success = AssetBundleBuilder.Build(buildTarget, buildTargets, buildPath, useCache);
			if (success)
				UnityEngine.Debug.Log("Asset Bundle Build: Success");
			else
				UnityEngine.Debug.LogError("Asset Bundle Build: Failed");

			return success;
		}
	}
}
