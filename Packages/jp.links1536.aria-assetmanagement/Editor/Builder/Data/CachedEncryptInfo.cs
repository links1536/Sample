using Aria.AssetManagement.Hash;

namespace AriaEditor.AssetManagement.Data
{
	[System.Serializable]
	class CachedEncryptInfo
	{
		public HashType HashType;
		public string Hash;
		public string Password;
		public string Salt;
	}
}
