using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace AriaEditor.AssetManagement
{
	[FilePath("ProjectSettings/AriaResourceBuildSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	public class AriaResourceBuildSettings : ScriptableSingleton<AriaResourceBuildSettings>
	{
		public string BuildCachePath = "Library/AssetBundle/Cache/";
		public string PublishRootPath = "Library/AssetBundle/Publish/";

		public string InAppCachePath = "Library/InAppBundle/Cache/";
		public string InAppPublishPath = "Library/InAppBundle/Publish/";

		public void Save()
		{
			Save(true);
		}
	}
}
