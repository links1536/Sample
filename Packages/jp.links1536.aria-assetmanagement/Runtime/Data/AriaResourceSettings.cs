using System.Security.Cryptography;
using System.Text;
using Aria.AssetManagement.Hash;
using UnityEngine;

namespace Aria.AssetManagement
{
	public class AriaResourceSettings : ScriptableObject, ISerializationCallbackReceiver
	{
		static AriaResourceSettings m_Instance;

		public static bool TryGetInstance(out AriaResourceSettings instance)
		{
			if (m_Instance == null)
			{
				m_Instance = Resources.Load<AriaResourceSettings>(nameof(AriaResourceSettings));
			}
			instance = m_Instance;
			return instance != null;
		}

		[Header("アセット情報")]
		public string InAppResourceBaseUri = "InAppResource";
		public string InAppResourcePath = "Assets/InAppResource/";
		public string AssetBundleBaseUri = "http://localhost/";
		public string AssetBundlePath = "Assets/AssetBundle/";
		public string Extension = ".aria";
		public HashType HashType;

		[Header("暗号化情報")]
		public string CatalogPassword;
		public int SaltSize = 16;
		public byte[] CatalogSalt;

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			CorrectionRemotePath(ref InAppResourceBaseUri);
			CorrectionRemotePath(ref AssetBundleBaseUri);

			CorrectionAssetPath(ref InAppResourcePath);
			CorrectionAssetPath(ref AssetBundlePath);
		}

		static void CorrectionRemotePath(ref string path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				// 末尾に/がついていなかったらつける
				if (!path.EndsWith("/"))
				{
					path = $"{path}/";
				}
			}
		}

		static void CorrectionAssetPath(ref string path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				// バックスラッシュからスラッシュへ変換する
				path = path.Replace(@"\", @"/");

				// 末尾に/がついていなかったらつける
				if (!path.EndsWith("/"))
				{
					path = $"{path}/";
				}
			}
		}
	}
}
