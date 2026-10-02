using System.Linq;
using UnityEditor;

namespace AriaEditor
{
	static class AriaEngineBuilder
	{
		[MenuItem("Build/Player/Windows/Debug")]
		public static void BuildWindowsDebug()
			=> Build(BuildTarget.StandaloneWindows64, true, false);
		[MenuItem("Build/Player/Windows/Debug && Play")]
		public static void BuildWindowsDebugAndPlay()
			=> Build(BuildTarget.StandaloneWindows64, true, true);

		[MenuItem("Build/Player/Android/Debug")]
		public static void BuildAndroidDebug()
			=> Build(BuildTarget.Android, true, false);
		[MenuItem("Build/Player/Android/Debug && Play")]
		public static void BuildAndroidDebugAndPlay()
			=> Build(BuildTarget.Android, true, true);

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
		public static void Build(BuildTarget buildTarget, bool development, bool autoRunPlayer)
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

			// ビルドオプション
			BuildOptions buildOptions = BuildOptions.None;
			if (development)
				buildOptions |= BuildOptions.Development;
			if (autoRunPlayer)
				buildOptions |= BuildOptions.AutoRunPlayer;

			// アプリビルド
			var scenes = EditorBuildSettings.scenes;
			var buildPlayerOptions = new BuildPlayerOptions()
			{
				locationPathName = System.IO.Path.Combine(System.Environment.CurrentDirectory, exportDirectory, exportName),
				scenes = scenes.Select(x => x.path).ToArray(),
				target = buildTarget,
				targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
				options = buildOptions,
			};
			BuildPipeline.BuildPlayer(buildPlayerOptions);
		}
	}
}
