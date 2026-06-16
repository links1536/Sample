using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aria.AssetManagement;
using Aria.AssetManagement.Data;
using Aria.Common.IO;
using AriaEditor.AssetManagement;
using UnityEditor;
using UnityEngine;

namespace AriaEditor
{
	class AriaInAppBundleBuilder
	{
		//=========================================
		// Windows
		//=========================================

		[MenuItem("Build/InAppBundle/Windows/FullBuild")]
		static void BuildBundleWindowsFull()
			=> BuildBundleCore(BuildTarget.StandaloneWindows64, false);

		[MenuItem("Build/InAppBundle/Windows/Incremental")]
		static void BuildBundleWindowsIncremental()
			=> BuildBundleCore(BuildTarget.StandaloneWindows64, true);

		[MenuItem("Build/InAppBundle/Windows/Copy to StreamingAssets")]
		static void CopyBundleWindows()
			=> CopyBundle(BuildTarget.StandaloneWindows64);


		//=========================================
		// Android
		//=========================================

		[MenuItem("Build/InAppBundle/Android/FullBuild")]
		static void BuildBundleAndroidFull()
			=> BuildBundleCore(BuildTarget.Android, false);

		[MenuItem("Build/InAppBundle/Android/Incremental")]
		static void BuildBundleAndroidIncremental()
			=> BuildBundleCore(BuildTarget.Android, true);

		[MenuItem("Build/InAppBundle/Android/Copy to StreamingAssets")]
		static void CopyBundleAnroid()
			=> CopyBundle(BuildTarget.Android);

		//=========================================
		// iOS
		//=========================================

		[MenuItem("Build/InAppBundle/iOS/FullBuild")]
		static void BuildBundleIOSFull()
			=> BuildBundleCore(BuildTarget.iOS, false);

		[MenuItem("Build/InAppBundle/iOS/Incremental")]
		static void BuildBundleIOSIncremental()
			=> BuildBundleCore(BuildTarget.iOS, true);

		[MenuItem("Build/InAppBundle/iOS/Copy to StreamingAssets")]
		static void CopyBundleIOS()
			=> CopyBundle(BuildTarget.iOS);

		//=========================================
		// Core
		//=========================================

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

		public static bool CopyBundle(BuildTarget buildTarget)
		{
			if (!AriaResourceSettings.TryGetInstance(out var resourceSettings))
			{
				UnityEngine.Debug.LogError("CopyBundle: Failed");
				return false;
			}

			try
			{
				// ビルド先フォルダ
				string buildCachePath = AriaResourceBuildSettings.instance.InAppCachePath;
				string publishRootPath = AriaResourceBuildSettings.instance.InAppPublishPath;

				// フォルダコピー
				BuildPath buildPath = new BuildPath(buildTarget, buildCachePath, publishRootPath);
				string inAppResourceDestination = buildPath.GetPublishRoot();
				var destination = Path.Combine(UnityEngine.Application.streamingAssetsPath, resourceSettings.InAppResourceBaseUri);
				DirectoryUtils.SafeDeleteDirectory(destination);
				DirectoryUtils.CopyDirectory(inAppResourceDestination, destination);
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

				return true;
			}
			catch (System.Exception e)
			{
				UnityEngine.Debug.LogError("CopyBundle: Failed");
				UnityEngine.Debug.LogException(e);
				return false;
			}
		}
	}
}
