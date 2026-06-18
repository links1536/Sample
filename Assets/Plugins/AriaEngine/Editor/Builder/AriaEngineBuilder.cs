using System.Linq;
using UnityEditor;

namespace AriaEditor
{
	static class AriaEngineBuilder
	{
		[MenuItem("Build/Player/Windows/Debug")]
		public static void BuildWindowsDebug()
			=> Build(BuildTarget.StandaloneWindows64);
		[MenuItem("Build/Player/Android/Debug")]
		public static void BuildAndroidDebug()
			=> Build(BuildTarget.Android);

		static string PlatformName(BuildTarget target)
			=> target switch
			{
				BuildTarget.StandaloneWindows64 => "Windows",
				BuildTarget.Android => "Android",
				_ => throw new System.PlatformNotSupportedException()
			};
		static string Extension(BuildTarget target)
			=> target switch
			{
				BuildTarget.StandaloneWindows64 => ".exe",
				BuildTarget.Android => ".apk",
				_ => throw new System.PlatformNotSupportedException()
			};
		public static void Build(BuildTarget buildTarget)
		{
			// 埋め込みリソースビルド
			if (!AriaInAppBundleBuilder.BuildBundleCore(buildTarget, false))
			{
				UnityEngine.Debug.LogError("埋め込みリソースのビルドに失敗しました");
				return;
			}

			// 埋め込み用のリソースをコピーする
			if (!AriaInAppBundleBuilder.CopyBundle(buildTarget))
			{
				UnityEngine.Debug.LogError("埋め込みリソースのコピーに失敗しました");
				return;
			}

			string exportDirectory = $"Build/{PlatformName(buildTarget)}";
			string exportName = PlayerSettings.productName + Extension(buildTarget);

			// アプリビルド
			var scenes = EditorBuildSettings.scenes;
			var option = new BuildPlayerOptions()
			{
				locationPathName = System.IO.Path.Combine(System.Environment.CurrentDirectory, exportDirectory, exportName),
				scenes = scenes.Select(x => x.path).ToArray(),
				target = buildTarget,
				targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
				options = BuildOptions.Development,
			};
			BuildPipeline.BuildPlayer(option);
		}
	}
}
