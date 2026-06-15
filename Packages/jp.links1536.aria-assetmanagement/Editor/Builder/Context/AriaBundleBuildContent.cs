using System.Collections.Generic;
using Aria.AssetManagement.Data;
using AriaEditor.AssetManagement.Data;
using UnityEditor;
using UnityEditor.Build.Pipeline;

namespace AriaEditor.AssetManagement.Context
{
	class AriaBundleBuildContent : BundleBuildContent
	{
		public Dictionary<string, string> BundleGuids
			=> m_BundleGuids;
		Dictionary<string, string> m_BundleGuids;

		public Dictionary<string, CachedEncryptInfo> BundleEncryptInfos
			=> m_BundleEncryptInfos;
		Dictionary<string, CachedEncryptInfo> m_BundleEncryptInfos;

		public AriaBundleBuildContent()
		{
			m_BundleGuids = new Dictionary<string, string>();
			m_BundleEncryptInfos = new Dictionary<string, CachedEncryptInfo>();
		}
		public AriaBundleBuildContent(IEnumerable<AssetBundleBuild> bundleBuilds)
			: base(bundleBuilds)
		{
			m_BundleGuids = new Dictionary<string, string>();
			m_BundleEncryptInfos = new Dictionary<string, CachedEncryptInfo>();
		}
	}
}
