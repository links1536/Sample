using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Pipeline;

namespace AriaEditor.AssetManagement.Context
{
	class AriaBundleBuildParameters : BundleBuildParameters
	{
		public const string RawExtension = ".raw";
		public const string EncryptedExtension = ".encrypted";
		HashSet<string> m_TargetBundleNames;

		public readonly BuildPath BuildPath;
		public readonly string PlaneOutputFolder;
		public readonly string EncryptedOutputFolder;
		public readonly string PublishFolder;
		public readonly string Extension;

		public AriaBundleBuildParameters(BuildTarget buildTarget, BuildTargetGroup group, string extension, BuildPath buildPath, AssetBundleBuild[] assetBuildTargests)
			: base(buildTarget, group, buildPath.GetOutputRoot())
		{
			m_TargetBundleNames = new HashSet<string>(assetBuildTargests.Length);
			foreach (var assetBuildTarget in assetBuildTargests)
			{
				m_TargetBundleNames.Add(assetBuildTarget.assetBundleName);
			}

			BuildPath = buildPath;
			PlaneOutputFolder = BuildPath.GetPlaneCacheRoot();
			EncryptedOutputFolder = BuildPath.GetEncryptedCacheRoot();
			PublishFolder = BuildPath.GetPublishRoot();
			Extension = extension;
		}

		public override string GetOutputFilePathForIdentifier(string identifier)
			=> string.Format("{0}/{1}", PlaneOutputFolder, identifier + RawExtension);
		public string GetEncryptedCacheFilePath(string identifier, UnityEngine.Hash128 hash)
			=> string.Format("{0}/{1}", EncryptedOutputFolder, identifier + "-" + hash.ToString() + EncryptedExtension);
		public string GetEncryptedOutputFilePathForIdentifier(string identifier)
			=> string.Format("{0}/{1}", OutputFolder, identifier + Extension);
		public string GetPublishFilePathForIdentifier(string identifier)
			=> string.Format("{0}/{1}", PublishFolder, identifier + Extension);
	}
}
