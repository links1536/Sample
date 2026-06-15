using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Aria.AssetManagement;
using Aria.AssetManagement.Data;
using AriaEditor.AssetManagement.Context;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace AriaEditor.AssetManagement.Task
{
	public class CreateBundleCatalogTask : IBuildTask
	{
		public int Version => 1;

		[InjectContext(ContextUsage.In)]
		AriaBundleBuildParameters m_Parameter;

		[InjectContext(ContextUsage.In)]
		AriaBundleBuildContent m_Content;

		[InjectContext(ContextUsage.In)]
		IBundleBuildResults m_Result;

		[InjectContext(ContextUsage.In, true)]
		IBuildLogger m_Logger;

		[InjectContext(ContextUsage.In, true)]
		IProgressTracker m_Tracker;

		public ReturnCode Run()
		{
			int current = 0;
			int count = m_Result.BundleInfos.Count;
			var newBundleInfos = new Dictionary<string, BundleInfo>();
			var newDependencyList = new List<DependencyInfo>();
			foreach (var bundleInfo in m_Result.BundleInfos)
			{
				using var scope = m_Logger?.ScopedStep(LogLevel.Info, "Catalog Add", true);

				current++;
				if (!m_Tracker.UpdateInfo($"Add Catalog: {bundleInfo.Key} ({current} / {count})"))
					return ReturnCode.Canceled;

				// ファイルチェック
				if (!File.Exists(bundleInfo.Value.FileName))
				{
					m_Logger?.AddEntry(LogLevel.Error, $"Not found {bundleInfo.Value.FileName}");
					return ReturnCode.MissingRequiredObjects;
				}

				// AssetBundle情報を保存する
				var guid = m_Content.BundleGuids[bundleInfo.Key];
				var encryptInfo = m_Content.BundleEncryptInfos[bundleInfo.Key];
				long fileSize = new FileInfo(bundleInfo.Value.FileName).Length;
				newBundleInfos[bundleInfo.Key] = new BundleInfo()
				{
					BundleName = bundleInfo.Key,
					Guid = guid,
					CRC = bundleInfo.Value.Crc,
					Hash = encryptInfo.Hash,
					FileSize = fileSize,
					Passward = encryptInfo.Password,
					Salt = encryptInfo.Salt,
				};

				// 依存関係を保存する
				foreach (var dependency in bundleInfo.Value.Dependencies)
				{
					newDependencyList.Add(new DependencyInfo()
					{
						BundleName = bundleInfo.Key,
						DependencyBundleName = dependency,
					});
				}
			}

			// AssetBundleCatalogを出力
			if (!m_Tracker.UpdateInfo($"Output Catalog: {nameof(AssetBundleCatalog)}"))
				return ReturnCode.Canceled;

			OutputCatalog(newBundleInfos, newDependencyList);

			return ReturnCode.Success;
		}

		AssetBundleCatalog CreateNewCatalog(Dictionary<string, BundleInfo> bundleInfos, List<DependencyInfo> dependencyList)
			=> new AssetBundleCatalog()
			{
				Bundles = bundleInfos.Values.ToArray(),
				Dependencies = dependencyList.ToArray(),
			};

		void OutputCatalog(Dictionary<string, BundleInfo> bundleInfos, List<DependencyInfo> dependencyList)
		{
			// 扱いやすいように暗号化していないカタログを出力しておく
			string rawPath = m_Parameter.GetOutputFilePathForIdentifier(AssetBundleCatalog.CatalogPath);
			var newCatalog = CreateNewCatalog(bundleInfos, dependencyList);
			string json = JsonUtility.ToJson(newCatalog, true);
			Directory.CreateDirectory(Path.GetDirectoryName(rawPath));
			File.WriteAllText(rawPath, json);

			// カタログを暗号化
			string catalogPath = m_Parameter.GetEncryptedOutputFilePathForIdentifier(AssetBundleCatalog.CatalogPath);
			Directory.CreateDirectory(Path.GetDirectoryName(catalogPath));
			if (AriaResourceSettings.TryGetInstance(out var instance))
			{
				using var source = File.Open(rawPath, FileMode.Open, FileAccess.Read, FileShare.Read);
				using var dest = File.Open(catalogPath, FileMode.Create);
				using var crypto = new SeekableAesStream(dest, instance.CatalogPassword, instance.CatalogSalt);
				using var compress = new BrotliStream(crypto, CompressionLevel.Optimal, false);
				source.CopyTo(compress);
			}
		}
	}
}
