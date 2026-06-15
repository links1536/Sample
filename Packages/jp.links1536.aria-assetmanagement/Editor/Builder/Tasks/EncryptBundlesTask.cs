using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Aria.AssetManagement;
using Aria.AssetManagement.Hash;
using AriaEditor.AssetManagement.Context;
using AriaEditor.AssetManagement.Data;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace AriaEditor.AssetManagement.Task
{
	public class EncryptBundlesTask : IBuildTask
	{
		const int PasswordLength = 32;
		const int SaltSize = 16;

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
		IProgressTracker m_ProgressTracker;

		AriaResourceSettings m_ResourceSettings;

		public ReturnCode Run()
		{
			if (!AriaResourceSettings.TryGetInstance(out m_ResourceSettings))
			{
				Debug.LogError($"{nameof(AriaResourceSettings)}が見つかりませんでした");
				return ReturnCode.Error;
			}
			// 16バイトのソルト
			using var saltGenerator = AriaResourceKeyGenerator.CreateSaltGenerator();
			byte[] saltBytes = new byte[SaltSize];

			int current = 0;
			int bundleCount = m_Result.BundleInfos.Count;
			foreach (var pair in m_Result.BundleInfos)
			{
				string bundleName = pair.Key;
				var bundleDetails = pair.Value;

				current++;
				if (!m_ProgressTracker.UpdateInfo($"Encryption: {bundleName} ({current} / {bundleCount})"))
					return ReturnCode.Canceled;

				// ファイルを暗号化する
				using var scope = m_Logger.ScopedStep(LogLevel.Info, "Encryption Bundles", true);
				var code = EncryptFile(saltGenerator, saltBytes, bundleName, bundleDetails);
				if (code < 0)
					return code;
			}


			return ReturnCode.Success;
		}

		ReturnCode EncryptFile(RNGCryptoServiceProvider rng, byte[] saltBytes, string bundleName, BundleDetails bundleDetail)
		{
			// 元ファイルがあるかチェックする
			string rawPath = m_Parameter.GetOutputFilePathForIdentifier(bundleName);
			if (!File.Exists(rawPath))
			{
				m_Logger?.AddEntry(LogLevel.Error, $"Not found {rawPath}");
				return ReturnCode.MissingRequiredObjects;
			}

			ReturnCode returnCode = ReturnCode.Success;

			// キャッシュファイルがあればそっちを利用する
			string cachePath = m_Parameter.GetEncryptedCacheFilePath(bundleName, bundleDetail.Hash);
			string infoFilePath = cachePath + "-info";
			var encryptInfo = default(CachedEncryptInfo);
			if (File.Exists(cachePath) && File.Exists(infoFilePath))
			{
				// 暗号化情報をまとめる
				try
				{
					string infoJson = File.ReadAllText(infoFilePath);
					encryptInfo = JsonUtility.FromJson<CachedEncryptInfo>(infoJson);
					m_Logger?.AddEntry(LogLevel.Info, $"[{bundleName}] cached");
					returnCode = ReturnCode.SuccessCached;
				}
				catch (Exception e)
				{
					m_Logger?.AddEntry(LogLevel.Error, $"[{bundleName}] cache crashed.");
					Debug.LogException(e);
					return ReturnCode.Exception;
				}
			}

			// 暗号化情報もしくは暗号化済みファイルがないときは暗号化する
			if (encryptInfo == null)
			{
				// 暗号化情報
				AriaResourceKeyGenerator.GetSaltBytes(rng, saltBytes);

				var password = AriaResourceKeyGenerator.GeneratePassword(PasswordLength);
				var salt = Convert.ToBase64String(saltBytes, 0, SaltSize);
				encryptInfo = new CachedEncryptInfo()
				{
					Password = password,
					Salt = salt,
				};

				// ファイルを暗号化
				m_Logger?.AddEntry(LogLevel.Info, $"[{bundleName}] encrypting");
				Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
				using (var source = File.Open(rawPath, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (var dest = File.Open(cachePath, FileMode.Create))
				using (var crypto = new SeekableAesStream(dest, password, saltBytes))
				{
					source.CopyTo(crypto);
				}

				// ログ出力
				m_Logger?.AddEntry(LogLevel.Info, $"[{bundleName}] encrypted {returnCode}");
			}

			if (string.IsNullOrEmpty(encryptInfo.Hash) || encryptInfo.HashType != m_ResourceSettings.HashType)
			{
				// 暗号化済みのハッシュ値を求める
				using (var hashCalculate = new HashCalculator(m_ResourceSettings.HashType))
				using (var stream = File.Open(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
					encryptInfo.Hash = hashCalculate.Calculate(stream);

				// 暗号化の鍵を保存する
				string infoJson = JsonUtility.ToJson(encryptInfo, true);
				File.WriteAllText(infoFilePath, infoJson);

				// ログ出力
				m_Logger?.AddEntry(LogLevel.Info, $"[{bundleName}] hash calculated {returnCode}");

				returnCode = ReturnCode.Success;
			}

			// バンドル情報を作成
			m_Content.BundleEncryptInfos[bundleName] = encryptInfo;

			// コピーする
			string encryptedPath = m_Parameter.GetEncryptedOutputFilePathForIdentifier(bundleName);
			Directory.CreateDirectory(Path.GetDirectoryName(encryptedPath));
			File.Copy(cachePath, encryptedPath, true);

			return returnCode;
		}
	}
}
