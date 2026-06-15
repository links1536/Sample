using System.IO;
using Aria.AssetManagement.Data;
using AriaEditor.AssetManagement.Context;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace AriaEditor.AssetManagement.Task
{
	public class PublishBundleTask : IBuildTask
	{
		public int Version => 1;

		[InjectContext(ContextUsage.In)]
		AriaBundleBuildParameters m_Parameter;

		[InjectContext(ContextUsage.In)]
		IBundleBuildResults m_Result;

		[InjectContext(ContextUsage.In, true)]
		IBuildLogger m_Logger;

		[InjectContext(ContextUsage.In, true)]
		IProgressTracker m_ProgressTracker;

		public ReturnCode Run()
		{
			int current = 0;
			int bundleCount = m_Result.BundleInfos.Count;
			foreach (var pair in m_Result.BundleInfos)
			{
				string bundleName = pair.Key;
				current++;
				if (!m_ProgressTracker.UpdateInfo($"Publish: {bundleName} ({current} / {bundleCount})"))
					return ReturnCode.Canceled;

				var returnCode = CopyFile(bundleName);
				if (returnCode < 0)
					return returnCode;
			}

			if (!m_ProgressTracker.UpdateInfo($"Publish: {AssetBundleCatalog.CatalogPath}"))
				return ReturnCode.Canceled;

			return CopyFile(AssetBundleCatalog.CatalogPath);
		}

		ReturnCode CopyFile(string bundleName)
		{
			using var scope = m_Logger.ScopedStep(LogLevel.Info, "Publish Files", true);
			string outputPath = m_Parameter.GetEncryptedOutputFilePathForIdentifier(bundleName);
			if (!File.Exists(outputPath))
			{
				m_Logger?.AddEntry(LogLevel.Error, $"Not found {outputPath}");
				return ReturnCode.MissingRequiredObjects;
			}

			string publishPath = m_Parameter.GetPublishFilePathForIdentifier(bundleName);
			Directory.CreateDirectory(Path.GetDirectoryName(publishPath));
			File.Copy(outputPath, publishPath, true);
			m_Logger?.AddEntry(LogLevel.Info, $"{outputPath} -> {publishPath}");
			return ReturnCode.Success;
		}
	}
}
