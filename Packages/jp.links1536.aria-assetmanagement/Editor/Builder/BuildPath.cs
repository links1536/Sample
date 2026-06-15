using Aria.AssetManagement;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine;

namespace AriaEditor.AssetManagement
{
	public class BuildPath
	{
		public readonly string Identifer;
		public readonly string BuildOutputRoot;
		public readonly string PublishRoot;

		public BuildPath(UnityEditor.BuildTarget buildTarget, string buildOutputRoot, string publishRoot)
		{
			Identifer = EditorPlatformUtility.BuildTargetIdentifer(buildTarget);
			BuildOutputRoot = buildOutputRoot;
			PublishRoot = publishRoot;
		}

		public string GetOutputRootPath()
			=> System.IO.Path.Combine(BuildOutputRoot, "AssetBuild", Identifer);
		public string GetOutputRoot()
			=> System.IO.Path.Combine(GetOutputRootPath(), "Publish");
		public string GetPlaneCacheRoot()
			=> System.IO.Path.Combine(GetOutputRootPath(), "Cache", "Plane");
		public string GetEncryptedCacheRoot()
			=> System.IO.Path.Combine(GetOutputRootPath(), "Cache", "Encrypted");

		public string GetPublishRoot()
			=> System.IO.Path.Combine(PublishRoot, Identifer);
	}
}
