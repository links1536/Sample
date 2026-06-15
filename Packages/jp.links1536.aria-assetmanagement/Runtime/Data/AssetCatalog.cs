using System.Collections.Generic;
using System.Diagnostics;

namespace Aria.AssetManagement.Data
{
	[System.Serializable, DebuggerDisplay("Path:{Path}, Guid: {Guid}")]
	public class AssetMap
	{
		public string Path;
		public string Guid;
	}

	[System.Serializable, DebuggerDisplay("{Assets}")]
	public class AssetCatalog
	{
		public const string Name = "catalog.txt";
		public List<AssetMap> Assets;

		public AssetCatalog()
		{
			Assets = new List<AssetMap>();
		}


	}
}
