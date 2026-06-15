using System.IO;
using System.Linq;
using Aria.AssetManagement;
using Aria.Common.IO;
using AriaEditor.AssetManagement;
using UnityEditor;

namespace AriaEditor
{
	static class AriaEngineBuilder
	{
		static string Extension(BuildTarget target)
			=> target switch
			{
				BuildTarget.StandaloneWindows64 => "exe",
				BuildTarget.Android => "apk",
				_ => throw new System.PlatformNotSupportedException()
			};
		public static void Build(BuildTarget target)
		{
			// 埋め込みリソースビルド
			if(!AriaInAppBundleBuilder.BuildBundleCore(target, false)) {
				UnityEngine.Debug.LogError("埋め込みリソースのビルドに失敗しました");
				return;
			}

			if (!AriaResourceSettings.TryGetInstance(out var instance)) {
				UnityEngine.Debug.LogError($"{nameof(AriaResourceSettings)}の取得に失敗しました");
				return;
			}
			// フォルダコピー
			string buildCachePath = AriaResourceBuildSettings.instance.InAppCachePath;
			string publishRootPath = AriaResourceBuildSettings.instance.InAppPublishPath;
			BuildPath buildPath = new BuildPath(target, buildCachePath, publishRootPath);
			string inAppResourceDestination = buildPath.GetPublishRoot();
			var destination = Path.Combine(UnityEngine.Application.streamingAssetsPath, instance.InAppResourceBaseUri);
			DirectoryUtils.SafeDeleteDirectory(destination);
			DirectoryUtils.CopyDirectory(inAppResourceDestination, destination);
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			// アプリビルド
			var scenes = EditorBuildSettings.scenes;
			var option = new BuildPlayerOptions()
			{
				locationPathName = System.IO.Path.Combine(System.Environment.CurrentDirectory, "Build", $"{target}.{Extension(target)}"),
				scenes = scenes.Select(x => x.path).ToArray(),
				target = target,
				targetGroup = BuildPipeline.GetBuildTargetGroup(target),
				options = BuildOptions.AutoRunPlayer | BuildOptions.Development | BuildOptions.ConnectWithProfiler,
			};
			//CopyAssetBundle.Copy(target);
			BuildPipeline.BuildPlayer(option);
			//CopyAssetBundle.Delete(target);
		}

		[MenuItem("Build/Player/Windows")]
		public static void BuildWindows()
			=> Build(BuildTarget.StandaloneWindows64);
		[MenuItem("Build/Player/Android")]
		public static void BuildAndroid()
			=> Build(BuildTarget.Android);
	}
}
