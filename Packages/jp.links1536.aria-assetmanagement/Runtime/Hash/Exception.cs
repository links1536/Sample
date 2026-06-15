using System;

namespace Aria.AssetManagement.Hash
{
	public sealed class MismatchedHashException : Exception
	{
		public MismatchedHashException(string bundleName, string catalogHash, string fileHash)
			: base($"{bundleName} is mismatched hash. catalog: {catalogHash}, file: {fileHash}")
		{

		}
	}
}
