using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AriaEditor.AssetManagement.Context;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace AriaEditor.AssetManagement.Task
{
	public class CreateBundleGuidTask : IBuildTask
	{
		public int Version => 1;

		[InjectContext(ContextUsage.In)]
		AriaBundleBuildContent m_Content;

		[InjectContext(ContextUsage.In)]
		IDependencyData m_DependencyData;

		[InjectContext(ContextUsage.In, true)]
		IProgressTracker m_Tracker;

		public ReturnCode Run()
		{
			var missingGuidBundles = new HashSet<string>();
			var guids = new HashSet<string>();

			foreach (var pair in m_Content.BundleLayout)
			{
				string bundleName = pair.Key;
				if (!m_Tracker.UpdateInfo($"Setting Guid: {bundleName}"))
					return ReturnCode.Canceled;

				if (!TryGetGuid(bundleName, out var guid))
				{
					missingGuidBundles.Add(bundleName);
					continue;
				}
				guids.Add(guid);
				m_Content.BundleGuids[bundleName] = guid;
			}

			foreach (var bundleName in missingGuidBundles)
			{
				string guid = CreateGuid(bundleName, guids);
				m_Content.BundleGuids[bundleName] = guid;
			}

			return ReturnCode.Success;
		}

		static bool TryGetGuid(string bundleName, out string guid)
		{
			string path = FilePath(bundleName);
			if (!File.Exists(path))
			{
				guid = null;
				return false;
			}
			guid = File.ReadLines(path).FirstOrDefault();
			if (string.IsNullOrEmpty(guid))
			{
				return false;
			}
			return true;
		}

		static string CreateGuid(string bundleName, HashSet<string> guids)
		{
			string path = FilePath(bundleName);
			string guid = null;
			// 重複しなくなるまで作り直す
			while (guid == null || guids.Contains(guid))
			{
				guid = Guid.NewGuid().ToString().Replace("-", string.Empty);
			}
			guids.Add(guid);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, guid);
			return guid;
		}

		static string FilePath(string bundleName)
			=> string.Format("Library/_Guid/{0}{1}", bundleName, ".guid");
	}
}
