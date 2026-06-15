using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AriaEditor.AssetManagement
{
	internal class EditorPlatformUtility
	{
		public static string BuildTargetIdentifer(BuildTarget platform)
		{
			switch (platform)
			{
				case BuildTarget.StandaloneWindows:
				case BuildTarget.StandaloneWindows64:
					return "windows";
				case BuildTarget.Android:
					return "android";
				case BuildTarget.iOS:
					return "ios";
			}
			;
			return "none";
		}
	}
}
