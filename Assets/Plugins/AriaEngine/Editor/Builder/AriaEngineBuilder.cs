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
		public static void Build(BuildTarget buildTarget)
		{
			// 埋め込みリソースビルド
			if(!AriaInAppBundleBuilder.BuildBundleCore(buildTarget, false)) {
				UnityEngine.Debug.LogError("埋め込みリソースのビルドに失敗しました");
				return;
			}

			// 埋め込み用のリソースをコピーする
			if (!AriaInAppBundleBuilder.CopyBundle(buildTarget))
			{
				UnityEngine.Debug.LogError("埋め込みリソースのコピーに失敗しました");
				return;
			}


			// アプリビルド
			var scenes = EditorBuildSettings.scenes;
			var option = new BuildPlayerOptions()
			{
				locationPathName = System.IO.Path.Combine(System.Environment.CurrentDirectory, "Build", $"{buildTarget}.{Extension(buildTarget)}"),
				scenes = scenes.Select(x => x.path).ToArray(),
				target = buildTarget,
				targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
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
